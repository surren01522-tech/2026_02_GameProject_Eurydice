using UnityEngine;

namespace GameFramework.Core
{
    /// <summary>
    /// 풀에서 생성된 인스턴스에 자동 부착.
    /// - 스스로 풀로 복귀 가능
    /// - InPool/Version으로 이중 반환·오반환을 방지
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        private PoolManager _owner;

        /// <summary>현재 풀 안에 있는지 (true면 Release 금지)</summary>
        internal bool InPool;

        /// <summary>Get될 때마다 증가. 지연 반환 시 "그때 그 사용"인지 식별.</summary>
        internal int Version;

        internal void Init(PoolManager owner) => _owner = owner;

        public void ReturnToPool()
        {
            if (_owner != null) _owner.Release(gameObject);
            else Destroy(gameObject);
        }

        public void ReturnToPool(float delay)
        {
            if (_owner != null) _owner.Release(gameObject, delay);
            else Destroy(gameObject, delay);
        }
    }
}
