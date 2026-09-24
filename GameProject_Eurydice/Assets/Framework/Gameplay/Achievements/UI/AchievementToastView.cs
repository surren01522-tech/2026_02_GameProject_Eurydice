using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Data;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// 화면 상단에서 내려오는 업적 달성 토스트. 연속 달성은 큐로 순차 표시.
    /// AchievementToastListener가 자동으로 생성/호출하므로 직접 쓸 일은 없다.
    /// 그림 교체 포인트: background / icon / 폰트. 연출 시간은 Inspector에서 조절.
    /// </summary>
    public class AchievementToastView : MonoBehaviour
    {
        [Header("자동 연결됨 (템플릿 생성기)")]
        public CanvasGroup group;
        public Image background;
        public Image icon;
        public Text headerText;   // "업적 달성!"
        public Text nameText;

        [Header("연출")]
        public float slideTime = 0.25f;
        public float holdTime  = 2.0f;
        public float hiddenY   = 120f;   // 화면 밖 (위)
        public float shownY    = -24f;   // 표시 위치 (상단에서 아래로)

        private readonly Queue<AchievementData> _queue = new();
        private bool _playing;
        private RectTransform _rt;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public void Enqueue(AchievementData data)
        {
            _queue.Enqueue(data);
            if (!_playing) StartCoroutine(PlayLoop());
        }

        private IEnumerator PlayLoop()
        {
            _playing = true;
            while (_queue.Count > 0)
            {
                var data = _queue.Dequeue();
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
