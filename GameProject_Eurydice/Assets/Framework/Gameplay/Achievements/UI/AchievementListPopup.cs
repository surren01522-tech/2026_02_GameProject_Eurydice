using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Core;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// 업적 리스트 팝업 템플릿. 진행/달성 이벤트 구독으로 실시간 갱신.
    /// 열기: UIManager.Instance.Show<AchievementListPopup>("AchievementListPopup");
    /// </summary>
    public class AchievementListPopup : UIPanel
    {
        [Header("자동 연결됨 (템플릿 생성기)")]
        public Transform listParent;              // ScrollRect의 Content
        public AchievementEntryView entryTemplate; // 비활성 템플릿
        public Text titleText;
        public Button closeButton;

        private readonly List<AchievementEntryView> _views = new();

        private void Awake()
        {
            entryTemplate.gameObject.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public override void OnShow(object args)
        {
            EventBus.Subscribe<AchievementProgressEvent>(OnProgress);
            EventBus.Subscribe<AchievementUnlockedEvent>(OnUnlocked);
            Rebuild();
        }

        public override void OnHide()
        {
            EventBus.Unsubscribe<AchievementProgressEvent>(OnProgress);
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnUnlocked);
        }

        private void OnProgress(AchievementProgressEvent e) => Rebuild();
        private void OnUnlocked(AchievementUnlockedEvent e) => Rebuild();

        private void Rebuild()
        {
            var mgr = AchievementManager.Instance;
            var all = mgr.AllData;

            while (_views.Count < all.Count)
            {
                var v = Instantiate(entryTemplate, listParent);
                v.gameObject.SetActive(true);
                _views.Add(v);
            }

            int shown = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var data = all[i];
                if (data == null) continue;
                var (cur, target) = mgr.GetProgressInfo(data.id);
                _views[shown].gameObject.SetActive(true);
                _views[shown].Set(data, cur, target, mgr.IsUnlocked(data.id));
                shown++;
            }
            for (int i = shown; i < _views.Count; i++)
                _views[i].gameObject.SetActive(false);
        }
    }
}
