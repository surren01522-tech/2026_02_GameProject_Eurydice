using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class MainMenuController : MonoBehaviour
{
    [Header("시네머신 참조")]
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera titleCamera;
    [SerializeField] private int titlePriority = 20;

    [Header("스폰 설정")]
    [Tooltip("새 게임 시 플레이어가 배치될 시작 위치")]
    [SerializeField] private Transform initialSpawnPoint;

    [Header("메인 메뉴 UI")]
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private float fadeDuration = 0.3f;

    private bool isTransitioning;
    private CinemachineSplineDolly splineDolly;

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
    }

    private void Start()
    {
        GameStateManager.SetMainMenuActive(true);
        if (continueButton != null)
            continueButton.interactable = SaveManager.HasSaveData();
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
        if (startButton != null) startButton.onClick.AddListener(() => StartGame(isNewGame: true));
        if (continueButton != null) continueButton.onClick.AddListener(() => StartGame(isNewGame: false));
        if (settingButton != null) settingButton.onClick.AddListener(OnSettingClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void HandleInputModeChanged(InputMode mode)
    {
        if (isTransitioning) return;

        if (mode == InputMode.MainMenu)
        {
            StartCoroutine(FadeCanvasGroup(menuCanvasGroup, 0f, 1f, fadeDuration, activateOnStart: true));
        }
    }

    private void StartGame(bool isNewGame)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(isNewGame));
    }

    public void OnSettingClicked()
    {
        StartCoroutine(FadeCanvasGroup(menuCanvasGroup, 1f, 0f, 0.15f, activateOnStart: false, deactivateOnEnd: true, onComplete: () =>
        {
            UIWindowManager.Instance?.OpenTab(UIWindowManager.WindowTab.Setting);
        }));
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

        // 새 게임일 때만 위치 초기화 및 스폰 지점 이동
        if (isNewGame)
        {
            SaveManager.DeleteSaveData();
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null && initialSpawnPoint != null)
            {
                player.Teleport(initialSpawnPoint.position, initialSpawnPoint.rotation, initialSpawnPoint.position, initialSpawnPoint.rotation);
            }
        }

        // 메뉴 UI 페이드아웃
        yield return FadeCanvasGroup(menuCanvasGroup, menuCanvasGroup != null ? menuCanvasGroup.alpha : 1f, 0f, fadeDuration, deactivateOnEnd: true);

        // 타이틀 카메라 우선순위 해제 -> 플레이어 카메라로 시네머신 비행
        if (titleCamera != null)
            titleCamera.Priority.Value = 0;

        yield return null;
        while (cinemachineBrain != null && cinemachineBrain.IsBlending)
            yield return null;

        if (titleCamera != null)
            titleCamera.gameObject.SetActive(false);

        GameStateManager.SetMainMenuActive(false);
        isTransitioning = false;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, bool activateOnStart = false, bool deactivateOnEnd = false, System.Action onComplete = null)
    {
        if (cg == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (activateOnStart)
        {
            cg.gameObject.SetActive(true);
        }

        cg.interactable = to > 0f;
        cg.blocksRaycasts = to > 0f;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
        if (deactivateOnEnd && to <= 0f)
        {
            cg.gameObject.SetActive(false);
        }

        onComplete?.Invoke();
    }
}
