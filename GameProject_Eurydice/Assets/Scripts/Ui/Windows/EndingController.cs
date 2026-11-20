using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EndingController : MonoBehaviour
{
    [Header("엔딩 UI")]
    [SerializeField] private CanvasGroup contentCanvasGroup;
    [SerializeField] private Button titleButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private float fadeInDuration = 0.8f;

    [Header("전환 및 오디오")]
    [SerializeField] private string titleSceneName = "Main";
    [SerializeField] private bool clearSaveOnEnter = true;
    [SerializeField] private bool clearSaveOnTitle = false;
    [SerializeField] private AudioClip endingBgm;
    [SerializeField, Min(0f)] private float bgmFadeDuration = 1.5f;

    private bool isTransitioning;

    private void Start()
    {
        if (clearSaveOnEnter)
            SaveManager.DeleteSaveData();

        GameStateManager.SetModalActive(true);
        BindButtons();
        PlayOpening();
    }

    private void OnDestroy()
    {
        contentCanvasGroup?.DOKill();
    }

    private void BindButtons()
    {
        if (titleButton != null)
        {
            titleButton.onClick.AddListener(ReturnToTitle);
            UIButtonTween.Bind(titleButton);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
            UIButtonTween.Bind(quitButton);
        }
    }

    private void PlayOpening()
    {
        if (endingBgm != null && AudioManager.Instance != null)
            AudioManager.Instance.FadeBGM(endingBgm, bgmFadeDuration);

        if (contentCanvasGroup == null) return;

        contentCanvasGroup.alpha = 0f;
        contentCanvasGroup.interactable = false;
        contentCanvasGroup.DOFade(1f, fadeInDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                if (contentCanvasGroup != null)
                    contentCanvasGroup.interactable = true;
            });
    }

    /// <summary>타이틀 씬으로 복귀합니다.</summary>
    public void ReturnToTitle()
    {
        if (isTransitioning || SceneLoader.IsLoading) return;
        isTransitioning = true;

        if (clearSaveOnTitle)
            SaveManager.DeleteSaveData();

        MainMenuController.HasGameStarted = false;
        GameStateManager.SetModalActive(false);
        SceneLoader.Load(titleSceneName, savePlayer: false);
    }

    /// <summary>게임을 종료합니다.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
