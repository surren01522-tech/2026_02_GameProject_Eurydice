using System.Collections.Generic;
using UnityEngine;
using GameFramework.Core;

namespace GameFramework.Services
{
    /// <summary>
    /// UI 매니저. 팝업 스택 + 뒤로가기(ESC) 처리.
    /// UI 프리팹은 Resources/UI/{프리팹명} 에 위치, 루트에 UIPanel 상속 컴포넌트 부착.
    /// 사용 예)
    ///   UIManager.Instance.Show<InventoryPopup>("InventoryPopup");
    ///   UIManager.Instance.HideTop();
    /// </summary>
    public class UIManager : MonoSingleton<UIManager>
    {
        [SerializeField] private int canvasSortOrder = 100;

        private Canvas _rootCanvas;
        private readonly Dictionary<string, UIPanel> _cache = new();
        private readonly List<UIPanel> _popupStack = new();

        public int OpenPopupCount => _popupStack.Count;

        /// <summary>토스트 등 팝업 스택 밖 UI를 얹을 수 있는 루트 캔버스.</summary>
        public RectTransform CanvasRoot => (RectTransform)_rootCanvas.transform;

        protected override void OnInitialize()
        {
            var go = new GameObject("UIRootCanvas");
            go.transform.SetParent(transform);
            go.layer = LayerMask.NameToLayer("UI");

            _rootCanvas = go.AddComponent<Canvas>();
            _rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _rootCanvas.sortingOrder = canvasSortOrder;

            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // EventSystem 보장 (New Input System용 InputSystemUIInputModule 사용)
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                DontDestroyOnLoad(es);
            }

            // 뒤로가기(ESC/백버튼/패드 Start)는 InputManager가 발행하는 이벤트로 처리
            EventBus.Subscribe<BackPressedEvent>(OnBackPressed);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<BackPressedEvent>(OnBackPressed);
            base.OnDestroy();
        }

        private void OnBackPressed(BackPressedEvent e) => HideTop();

        public T Show<T>(string prefabName, object args = null) where T : UIPanel
        {
            if (!_cache.TryGetValue(prefabName, out var panel) || panel == null)
            {
                var prefab = Resources.Load<GameObject>($"UI/{prefabName}");
                if (prefab == null)
                {
                    Debug.LogError($"[UI] Resources/UI/{prefabName} 프리팹 없음");
                    return null;
                }
                panel = Instantiate(prefab, _rootCanvas.transform).GetComponent<UIPanel>();
                _cache[prefabName] = panel;
            }

            panel.transform.SetAsLastSibling();
            panel.gameObject.SetActive(true);
            panel.OnShow(args);

            if (panel.isPopup && !_popupStack.Contains(panel))
                _popupStack.Add(panel);

            return panel as T;
        }

        /// <summary>열려 있으면 닫고, 닫혀 있으면 연다. (토글키용)</summary>
        public T Toggle<T>(string prefabName, object args = null) where T : UIPanel
        {
            if (_cache.TryGetValue(prefabName, out var panel)
                && panel != null && panel.gameObject.activeSelf)
            {
                Hide(panel);
                return panel as T;
            }
            return Show<T>(prefabName, args);
        }

        public void Hide(UIPanel panel)
        {
            if (panel == null) return;
            _popupStack.Remove(panel);
            panel.OnHide();
            panel.gameObject.SetActive(false);
        }

        public void HideTop()
        {
            if (_popupStack.Count == 0) return;
            var top = _popupStack[^1];
            if (top.OnBackRequested()) Hide(top);
        }

        public void HideAll()
        {
            for (int i = _popupStack.Count - 1; i >= 0; i--)
            {
                var p = _popupStack[i];
                p.OnHide();
                p.gameObject.SetActive(false);
            }
            _popupStack.Clear();
        }
    }
}
