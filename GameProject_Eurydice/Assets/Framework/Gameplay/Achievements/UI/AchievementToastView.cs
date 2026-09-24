using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Data;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// Toast shown when an achievement is unlocked.
    /// </summary>
    public class AchievementToastView : MonoBehaviour
    {
        [Header("Auto Wiring (Template Generator)")]
        public CanvasGroup group;
        public Image background;
        public Image icon;
        public TMP_Text headerTmpText;
        public Text headerText;
        public TMP_Text nameTmpText;
        public Text nameText;

        [Header("Animation")]
        public float slideTime = 0.25f;
        public float holdTime = 2.0f;
        public float hiddenY = 120f;
        public float shownY = -24f;

        private readonly Queue<AchievementData> _queue = new();
        private bool _playing;
        private RectTransform _rt;

        private void Awake()
        {
            UIFontUtility.ApplyToHierarchy(transform);
            if (headerTmpText != null && string.IsNullOrWhiteSpace(headerTmpText.text))
                headerTmpText.text = "업적 달성!";
            if (headerText != null && string.IsNullOrWhiteSpace(headerText.text))
                headerText.text = "업적 달성!";

            _rt = (RectTransform)transform;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public void Enqueue(AchievementData data)
        {
            _queue.Enqueue(data);
            if (!_playing)
                StartCoroutine(PlayLoop());
        }

        private IEnumerator PlayLoop()
        {
            _playing = true;
            while (_queue.Count > 0)
            {
                var data = _queue.Dequeue();
                if (nameTmpText != null)
                    nameTmpText.text = data.displayName;
                if (nameText != null)
                    nameText.text = data.displayName;
                icon.enabled = data.icon != null;
                icon.sprite = data.icon;

                yield return Slide(hiddenY, shownY, 0f, 1f);
                yield return new WaitForSecondsRealtime(holdTime);
                yield return Slide(shownY, hiddenY, 1f, 0f);
            }

            _playing = false;
        }

        private IEnumerator Slide(float fromY, float toY, float fromA, float toA)
        {
            float t = 0f;
            var pos = _rt.anchoredPosition;
            while (t < slideTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / slideTime);
                _rt.anchoredPosition = new Vector2(pos.x, Mathf.Lerp(fromY, toY, k));
                group.alpha = Mathf.Lerp(fromA, toA, k);
                yield return null;
            }

            _rt.anchoredPosition = new Vector2(pos.x, toY);
            group.alpha = toA;
        }
    }
}
