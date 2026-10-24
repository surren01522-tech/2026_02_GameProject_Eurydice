using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AchievementNoticeUIManager : MonoBehaviour
{
    public static AchievementNoticeUIManager Instance { get; private set; }

    [Header("UI 컴포넌트")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI tagText;

    [Header("연출 설정")]
    [SerializeField] private float displayDuration = 3.5f;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private Vector2 slideOffset = new Vector2(0f, -30f);

    [Header("오디오")]
    [SerializeField] private AudioClip unlockSound;

    private RectTransform rectTransform;
    private readonly Queue<AchievementData> pendingQueue = new();
    private Coroutine displayCoroutine;
    private Sequence activeSeq;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = canvasGroup != null ? canvasGroup.transform as RectTransform : null;

        HideImmediate();
    }

    private void OnEnable()
    {
        AchievementManager.OnAchievementUnlocked += EnqueueAchievement;
    }

    private void OnDisable()
    {
        AchievementManager.OnAchievementUnlocked -= EnqueueAchievement;
        activeSeq?.Kill();
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }
        HideImmediate();
    }

    public void EnqueueAchievement(AchievementData data)
    {
        if (data == null) return;

        pendingQueue.Enqueue(data);

        if (displayCoroutine == null)
            displayCoroutine = StartCoroutine(CoProcessQueue());
    }

    private IEnumerator CoProcessQueue()
    {
        while (pendingQueue.Count > 0)
        {
            var data = pendingQueue.Dequeue();
            ApplyData(data);
            PlayUnlockSound();

            yield return CoShowSingle();

            if (pendingQueue.Count > 0)
                yield return new WaitForSecondsRealtime(0.2f);
        }

        displayCoroutine = null;
    }

    private IEnumerator CoShowSingle()
    {
        if (canvasGroup == null) yield break;

        activeSeq?.Kill();
        activeSeq = canvasGroup.PlayNoticeSequence(displayDuration, rectTransform, slideOffset, fadeDuration);

        yield return activeSeq.WaitForCompletion();
    }

    private void ApplyData(AchievementData data)
    {
        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.gameObject.SetActive(data.Icon != null);
        }

        if (tagText != null)
            tagText.text = "Unlocked!";

        if (titleText != null)
            titleText.text = data.Title;

        if (descriptionText != null)
            descriptionText.text = data.Description;
    }

    private void PlayUnlockSound()
    {
        if (unlockSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayUISound(unlockSound);
    }

    public void HideImmediate()
    {
        activeSeq?.Kill();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }
}
