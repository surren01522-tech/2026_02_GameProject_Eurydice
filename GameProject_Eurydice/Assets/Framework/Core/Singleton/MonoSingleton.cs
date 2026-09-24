using UnityEngine;

namespace GameFramework.Core
{
    /// <summary>
    /// 모든 매니저의 베이스가 되는 제네릭 싱글턴.
    /// - 씬에 없으면 자동 생성
    /// - DontDestroyOnLoad 처리
    /// - 중복 생성 방지
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static bool _isQuitting;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (_isQuitting) return null;

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // Unity 6.5: FindFirstObjectByType은 deprecated (CS0618).
                        // 싱글턴은 '아무거나 하나'면 충분하므로 FindAnyObjectByType이 정답 (순서 비의존 + 더 빠름)
                        _instance = FindAnyObjectByType<T>();

                        if (_instance == null)
                        {
                            var go = new GameObject($"[{typeof(T).Name}]");
                            _instance = go.AddComponent<T>();
                        }
                    }
                    return _instance;
                }
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            OnInitialize();
        }

        /// <summary>Awake 시점에 1회 호출되는 초기화 훅.</summary>
        protected virtual void OnInitialize() { }

        protected virtual void OnApplicationQuit() => _isQuitting = true;

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
