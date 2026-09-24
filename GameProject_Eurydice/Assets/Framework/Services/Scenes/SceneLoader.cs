using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameFramework.Core;

namespace GameFramework.Services
{
    /// <summary>
    /// 비동기 씬 로더. 페이드 + 진행률 이벤트 발행.
    /// 사용 예)
    ///   SceneLoader.Instance.LoadScene("Stage01");
    ///   EventBus.Subscribe<SceneLoadProgressEvent>(e => bar.value = e.Progress);
    /// </summary>
    public class SceneLoader : MonoSingleton<SceneLoader>
    {
        [SerializeField] private float fadeTime = 0.4f;
        [SerializeField] private float minLoadingTime = 0.5f; // 로딩 화면이 깜빡이지 않게 최소 표시 시간

        private CanvasGroup _fadeGroup;
        public bool IsLoading { get; private set; }

        protected override void OnInitialize()
        {
            // 페이드용 풀스크린 캔버스 생성
            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(canvasGo.transform, false);
            var img = imgGo.AddComponent<UnityEngine.UI.Image>();
            img.color = Color.black;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            _fadeGroup = canvasGo.AddComponent<CanvasGroup>();
            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;
        }

        public void LoadScene(string sceneName)
        {
            if (IsLoading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            EventBus.Publish(new SceneLoadStartedEvent { SceneName = sceneName });

            yield return Fade(0f, 1f);

            float start = Time.unscaledTime;
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                EventBus.Publish(new SceneLoadProgressEvent { Progress = op.progress / 0.9f });
                yield return null;
            }
            EventBus.Publish(new SceneLoadProgressEvent { Progress = 1f });

            // 최소 로딩 시간 보장
            while (Time.unscaledTime - start < minLoadingTime) yield return null;

            op.allowSceneActivation = true;
            yield return op;

            EventBus.Publish(new SceneLoadedEvent { SceneName = sceneName });
            yield return Fade(1f, 0f);

            IsLoading = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            _fadeGroup.blocksRaycasts = true;
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                _fadeGroup.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            _fadeGroup.alpha = to;
            _fadeGroup.blocksRaycasts = to > 0.5f;
        }
    }
}
