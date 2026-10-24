using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using DG.Tweening;

public class MainMenuController : MonoBehaviour
{
    [Header("시네머신 참조")]
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera titleCamera;
    [SerializeField] private int titlePriority = 20;

    [Header("스폰 설정")]
    [Tooltip("새 게임 시 플레이어가 배치될 시작 위치")]
    [SerializeField] private Transform initialSpawnPoint;

    [Header("에디터 설정")]
    [Tooltip("게임 시작 시 메인 메뉴를 자동으로 띄울지 여부")]
    [SerializeField] private bool autoShowOnStart = true;
    [Tooltip("에디터 플레이 시 메인 메뉴를 스킵하고 현재 위치에서 바로 플레이할지 여부")]
    [SerializeField] private bool bypassInEditor = true;

    [Header("메인 메뉴 UI")]
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private float fadeDuration = 0.3f;

    public static bool HasGameStarted { get; set; } = false;

    private bool isTransitioning;
    private CinemachineSplineDolly splineDolly;
    private Tween fadeTween;

    private void Awake()
    {
        FindReferences();
        InitTitleCamera();
        BindButtons();
    }

    private void OnEnable()
    {
        GameStateManager.OnInputModeChanged += HandleInputModeChanged;
    }

    private void OnDisable()
    {
        GameStateManager.OnInputModeChanged -= HandleInputModeChanged;
        fadeTween?.Kill();
    }

    private void Start()
    {
        bool shouldBypass = (Application.isEditor && bypassInEditor) || HasGameStarted || !autoShowOnStart;
        if (shouldBypass)
        {
            BypassMainMenu();
            return;
        }

        GameStateManager.SetMainMenuActive(true);
        if (continueButton != null)
            continueButton.interactable = SaveManager.HasSaveData();
    }

    private void BypassMainMenu()
    {
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
            menuCanvasGroup.gameObject.SetActive(false);
        }

        if (splineDolly != null)
            splineDolly.enabled = false;

        if (titleCamera != null)
        {
            titleCamera.Priority.Value = 0;
            titleCamera.gameObject.SetActive(false);
        }

        GameStateManager.SetMainMenuActive(false);
    }

    private void FindReferences()
    {
        if (cinemachineBrain == null && Camera.main != null)
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();

        if (titleCamera == null)
            titleCamera = GetComponentInChildren<CinemachineCamera>(true);

        if (menuCanvasGroup == null)
            menuCanvasGroup = GetComponent<CanvasGroup>() ?? GetComponentInChildren<CanvasGroup>(true);
    }

    private void InitTitleCamera()
    {
        if (titleCamera == null) return;
        titleCamera.Priority.Value = titlePriority;
        splineDolly = titleCamera.GetComponent<CinemachineSplineDolly>();
    }

    private void BindButtons()
    {
        BindMenuButton(startButton, () => StartGame(isNewGame: true));
        BindMenuButton(continueButton, () => StartGame(isNewGame: false));
        BindMenuButton(settingButton, OnSettingClicked);
        BindMenuButton(quitButton, OnQuitClicked);
    }

    private void BindMenuButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.AddListener(action);
        UIButtonTween.Bind(btn);
    }

    private void HandleInputModeChanged(InputMode mode)
    {
        if (isTransitioning) return;

        if (mode == InputMode.MainMenu)
        {
            FadeMenu(1f, fadeDuration, activateOnStart: true);
        }
    }

    private void StartGame(bool isNewGame)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(isNewGame));
    }

    public void OnSettingClicked()
    {
        FadeMenu(0f, 0.15f, deactivateOnEnd: true, onComplete: () =>
        {
            UIWindowManager.Instance?.OpenTab(UIWindowManager.WindowTab.Setting);
        });
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator TransitionRoutine(bool isNewGame)
    {
        isTransitioning = true;

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }

        if (splineDolly != null)
            splineDolly.enabled = false;

        if (isNewGame)
        {
            SaveManager.DeleteSaveData();
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null && initialSpawnPoint != null)
            {
                player.Teleport(initialSpawnPoint.position, initialSpawnPoint.rotation, initialSpawnPoint.position, initialSpawnPoint.rotation);
            }
        }

        Tween fade = FadeMenu(0f, fadeDuration, deactivateOnEnd: true);
        if (fade != null) yield return fade.WaitForCompletion();

        if (titleCamera != null)
            titleCamera.Priority.Value = 0;

        yield return null;
        while (cinemachineBrain != null && cinemachineBrain.IsBlending)
            yield return null;

        if (titleCamera != null)
            titleCamera.gameObject.SetActive(false);

        HasGameStarted = true;
        GameStateManager.SetMainMenuActive(false);
        isTransitioning = false;
    }

    private Tween FadeMenu(float targetAlpha, float duration, bool activateOnStart = false, bool deactivateOnEnd = false, System.Action onComplete = null)
    {
        fadeTween?.Kill();
        if (menuCanvasGroup == null)
        {
            onComplete?.Invoke();
            return null;
        }

        if (activateOnStart)
            menuCanvasGroup.gameObject.SetActive(true);

        menuCanvasGroup.interactable = targetAlpha > 0f;
        menuCanvasGroup.blocksRaycasts = targetAlpha > 0f;

        fadeTween = menuCanvasGroup.DOFade(targetAlpha, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(menuCanvasGroup.gameObject)
            .OnComplete(() =>
            {
                if (deactivateOnEnd && targetAlpha <= 0f)
                    menuCanvasGroup.gameObject.SetActive(false);
                onComplete?.Invoke();
            });

        return fadeTween;
    }
}
