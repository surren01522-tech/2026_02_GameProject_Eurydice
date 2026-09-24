using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace GameFramework.Core
{
    /// <summary>
    /// 프리팹 기반 오브젝트 풀 매니저. (Unity 6 내장 ObjectPool 사용)
    /// 사용 예)
    ///   var bullet = PoolManager.Instance.Get(bulletPrefab, pos, rot);
    ///   PoolManager.Instance.Release(bullet);        // 즉시 반환
    ///   PoolManager.Instance.Release(bullet, 2f);    // 2초 후 반환 (할당 없음)
    /// </summary>
    public class PoolManager : MonoSingleton<PoolManager>
    {
        [SerializeField] private int defaultCapacity = 16;
        [SerializeField] private int maxSize = 256;

        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();
        private readonly Dictionary<GameObject, PooledObject> _pooledCache = new();
        private readonly Dictionary<GameObject, Transform> _poolRoots = new();

        // 지연 반환 타이머 (코루틴/WaitForSeconds 할당 없이 Update에서 처리)
        private struct TimedRelease
        {
            public PooledObject Target;
            public int Version;     // Get 시점의 버전 — 재사용된 객체를 잘못 반환하지 않게
            public float ReleaseAt;
        }
        private readonly List<TimedRelease> _timed = new();

        // ===================== Get =====================

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var go = GetOrCreatePool(prefab).Get();
            var t = go.transform;
            t.SetParent(parent != null ? parent : _poolRoots[prefab]);
            t.SetPositionAndRotation(position, rotation);
            return go;
        }

        public GameObject Get(GameObject prefab) => Get(prefab, Vector3.zero, Quaternion.identity);

        public T Get<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
            => Get(prefab, position, rotation).GetComponent<T>();

        public void Prewarm(GameObject prefab, int count)
        {
            var pool = GetOrCreatePool(prefab);
            var temp = new List<GameObject>(count);
            for (int i = 0; i < count; i++) temp.Add(pool.Get());
            foreach (var go in temp) pool.Release(go);
        }

        // ===================== Release =====================

        public void Release(GameObject instance)
        {
            if (instance == null) return;

            if (_pooledCache.TryGetValue(instance, out var po))
            {
                if (po.InPool) return; // 이중 반환 방지
                _pools[_instanceToPrefab[instance]].Release(instance);
            }
            else
            {
                Destroy(instance); // 풀에서 나온 게 아니면 그냥 파괴
            }
        }

        /// <summary>delay초 후 반환. 그 사이 객체가 반환/재사용되면 자동으로 무시된다.</summary>
        public void Release(GameObject instance, float delay)
        {
            if (instance == null) return;
            if (delay <= 0f) { Release(instance); return; }

            if (_pooledCache.TryGetValue(instance, out var po))
                _timed.Add(new TimedRelease { Target = po, Version = po.Version, ReleaseAt = Time.time + delay });
            else
                Destroy(instance, delay);
        }

        private void Update()
        {
            float now = Time.time;
            for (int i = _timed.Count - 1; i >= 0; i--)
            {
                var t = _timed[i];
                if (now < t.ReleaseAt) continue;

                // 그 사이 이미 반환됐거나(InPool) 재사용됐다면(Version 변경) 건너뜀
                if (t.Target != null && !t.Target.InPool && t.Target.Version == t.Version)
                    Release(t.Target.gameObject);

                _timed.RemoveAt(i);
            }
        }

        // ===================== 내부 =====================

        private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool)) return pool;

            var root = new GameObject($"Pool_{prefab.name}").transform;
            root.SetParent(transform);
            _poolRoots[prefab] = root;

            pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var go = Instantiate(prefab, root);
                    _instanceToPrefab[go] = prefab;
                    var po = go.GetComponent<PooledObject>();
                    if (po == null) po = go.AddComponent<PooledObject>();
                    po.Init(this);
                    _pooledCache[go] = po;
                    return go;
                },
                actionOnGet: go =>
                {
                    var po = _pooledCache[go];
                    po.InPool = false;
                    po.Version++;
                    go.SetActive(true);
                },
                actionOnRelease: go =>
                {
                    _pooledCache[go].InPool = true;
                    go.SetActive(false);
                    go.transform.SetParent(root);
                },
                actionOnDestroy: go =>
                {
                    _instanceToPrefab.Remove(go);
                    _pooledCache.Remove(go);
                },
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);

            _pools[prefab] = pool;
            return pool;
        }
    }
}
