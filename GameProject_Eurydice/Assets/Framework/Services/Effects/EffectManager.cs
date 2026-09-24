using UnityEngine;
using GameFramework.Core;
using GameFramework.Data;

namespace GameFramework.Services
{
    /// <summary>
    /// 파티클 이펙트 매니저. PoolManager 기반으로 자동 풀링/반환.
    /// Resources/EffectLibrary.asset 자동 로드.
    /// 사용 예)
    ///   EffectManager.Instance.Play("explosion", hitPos);
    ///   EffectManager.Instance.Play("aura", transform.position, transform); // 부모 추적
    /// </summary>
    public class EffectManager : MonoSingleton<EffectManager>
    {
        [SerializeField] private EffectLibrary library;

        protected override void OnInitialize()
        {
            if (library == null) library = Resources.Load<EffectLibrary>("EffectLibrary");
        }

        public GameObject Play(string id, Vector3 position, Transform parent = null)
            => Play(id, position, Quaternion.identity, parent);

        public GameObject Play(string id, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var entry = library?.Get(id);
            if (entry == null || entry.prefab == null)
            {
                Debug.LogWarning($"[Effect] 이펙트 없음: {id}");
                return null;
            }

            var go = PoolManager.Instance.Get(entry.prefab, position, rotation, parent);

            // 수명 계산: 지정값 > 파티클 duration + 최대 lifetime
            float life = entry.lifetime;
            if (life < 0f)
            {
                var ps = go.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    life = main.duration + main.startLifetime.constantMax;
                    ps.Clear();
                    ps.Play();
                }
                else life = 2f;
            }

            PoolManager.Instance.Release(go, life);
            return go;
        }

        /// <summary>루프 이펙트(오라 등)는 수동으로 정지/반환.</summary>
        public GameObject PlayLoop(string id, Vector3 position, Transform parent = null)
        {
            var entry = library?.Get(id);
            if (entry == null || entry.prefab == null) return null;
            return PoolManager.Instance.Get(entry.prefab, position, Quaternion.identity, parent);
        }

        public void Stop(GameObject effectInstance) => PoolManager.Instance.Release(effectInstance);
    }
}
