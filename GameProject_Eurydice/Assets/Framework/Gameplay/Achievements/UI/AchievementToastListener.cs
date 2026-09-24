using UnityEngine;
using GameFramework.Core;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// Spawns and feeds the achievement toast prefab when achievements are unlocked.
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
            if (data == null)
                return;

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
