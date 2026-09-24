using UnityEngine;

namespace GameFramework.Core
{
    /// <summary>
    /// 게임 시작 시 매니저 초기화 순서를 보장하는 부트스트래퍼.
    /// 방법 1) 첫 씬에 빈 오브젝트를 두고 이 컴포넌트 부착
    /// 방법 2) autoBootstrap = true면 씬에 없어도 자동 실행 (RuntimeInitializeOnLoadMethod)
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        private static bool _bootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            if (_bootstrapped) return;
            _bootstrapped = true;

            // ===== 초기화 순서: 코어 → 서비스 → 게임플레이 =====
            // Instance 접근만으로 생성 + Awake(OnInitialize)가 실행된다.
            _ = PoolManager.Instance;
            _ = Services.InputManager.Instance;
            _ = Services.SaveManager.Instance;
            _ = Services.SoundManager.Instance;
            _ = Services.EffectManager.Instance;
            _ = Services.SceneLoader.Instance;
            _ = Services.UIManager.Instance;
            _ = Gameplay.InventoryManager.Instance;
            _ = Gameplay.AchievementManager.Instance;
            _ = Gameplay.AchievementToastListener.Instance;

            Debug.Log("[Bootstrapper] 프레임워크 초기화 완료");
        }
    }
}
