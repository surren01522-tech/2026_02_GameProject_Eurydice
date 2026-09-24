using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Core;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// 인벤토리 팝업 템플릿. InventoryChangedEvent 구독으로 자동 갱신.
    /// 열기: UIManager.Instance.Show<InventoryPopup>("InventoryPopup");
    ///
    /// [그림 교체 가이드 — 코드 수정 없이 Inspector에서]
    /// - 루트 dimImage      : 뒷배경 딤 (색/스프라이트)
    /// - Panel의 Image      : 팝업 배경 프레임
    /// - slotTemplate 내부  : 슬롯 배경/아이콘/폰트
    /// - 버튼들의 Image     : 버튼 스프라이트
    /// </summary>
    public class InventoryPopup : UIPanel
    {
        [Header("자동 연결됨 (템플릿 생성기)")]
        public Transform slotParent;          // GridLayoutGroup
        public InventorySlotView slotTemplate; // 비활성 템플릿, 복제해서 사용
        public Text titleText;
        public Button sortButton;
        public Button closeButton;

        [Header("옵션")]
        public int minSlots = 20;             // 비어 있어도 보여줄 최소 슬롯 수

        private readonly List<InventorySlotView> _views = new();

        private void Awake()
        {
            slotTemplate.gameObject.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (sortButton != null) sortButton.onClick.AddListener(
                () => InventoryManager.Instance.SortByType());
        }

        public override void OnShow(object args)
        {
            EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
            Rebuild();
        }

        public override void OnHide()
        {
            EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnInventoryChanged(InventoryChangedEvent e) => Rebuild();

        private void Rebuild()
        {
            var inv = InventoryManager.Instance;
            var stacks = inv.Stacks;
            int need = Mathf.Max(minSlots, stacks.Count);

            // 슬롯 뷰 확보 (부족하면 템플릿 복제)
            while (_views.Count < need)
            {
                var v = Instantiate(slotTemplate, slotParent);
                v.gameObject.SetActive(true);
                _views.Add(v);
            }

            for (int i = 0; i < _views.Count; i++)
            {
                if (i < stacks.Count)
                {
                    var s = stacks[i];
                    _views[i].gameObject.SetActive(true);
                    _views[i].Set(inv.Database.Get(s.itemId), s.count);
                }
                else if (i < need)
                {
                    _views[i].gameObject.SetActive(true);
                    _views[i].SetEmpty();
                }
                else
                {
                    _views[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
