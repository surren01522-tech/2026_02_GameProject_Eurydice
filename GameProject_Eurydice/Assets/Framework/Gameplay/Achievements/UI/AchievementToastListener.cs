using UnityEngine;
using GameFramework.Core;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// AchievementUnlockedEvent를 구독해 토스트를 자동 표시한다.
    /// Resources/UI/AchievementToast 프리팹이 있으면 동작, 없으면 조용히 넘어감.
    /// (프리팹은 Tools > GameFramework > UI 템플릿 생성 으로 생성)
    /// </summary>
    public class AchievementToastListener : MonoSingleton<AchievementToastListener>
    {
        private AchievementToastView _view;
        private bool _warned;

        protected override void OnInitialize()
            => EventBus.Subscribe<AchievementUnlockedEvent>(OnUnlocked);

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnUnlocked);
            base.OnDestroy();
        }

        private void OnUnlocked(AchievementUnlockedEvent e)
        {
            var data = AchievementManager.Instance.GetData(e.AchievementId);
            if (data == null) return;

            if (_view == null && !TrySpawnView())
                return;

            _view.Enqueue(data);
        }

        private bool TrySpawnView()
        {
            var prefab = Resources.Load<GameObject>("UI/AchievementToast");
            if (prefab == null)
            {
                if (!_warned)
                {
                    Debug.Log("[Toast] Resources/UI/AchievementToast 프리팹이 없어 토스트를 건너뜁니다. " +
                              "(Tools > GameFramework > UI 템플릿 생성)");
                    _warned = true;
                }
                return false;
            }

            _view = Instantiate(prefab, UIManager.Instance.CanvasRoot)
                .GetComponent<AchievementToastView>();
            return _view != null;
        }
    }
}
