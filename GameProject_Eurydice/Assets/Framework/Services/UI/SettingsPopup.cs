using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Services
{
    /// <summary>
    /// 설정창 템플릿. BGM/SFX 볼륨 슬라이더가 SoundManager와 자동 연동된다.
    /// 열기: UIManager.Instance.Show<SettingsPopup>("SettingsPopup");
    ///
    /// [그림 교체 가이드]
    /// - Panel Image, 슬라이더의 Background/Fill/Handle 스프라이트,
    ///   버튼 스프라이트를 Inspector에서 교체하면 끝.
    /// - 항목 추가(해상도, 언어 등)는 Panel에 Row를 복제해서 확장.
    /// </summary>
    public class SettingsPopup : UIPanel
    {
        [Header("자동 연결됨 (템플릿 생성기)")]
        public Text titleText;
        public Slider bgmSlider;
        public Slider sfxSlider;
        public Button closeButton;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            bgmSlider.onValueChanged.AddListener(v => SoundManager.Instance.SetBgmVolume(v));
            sfxSlider.onValueChanged.AddListener(v =>
            {
                SoundManager.Instance.SetSfxVolume(v);
            });
            // 핸들을 놓을 때 확인음을 내고 싶다면 EventTrigger PointerUp에서 PlaySFX 호출
        }

        public override void OnShow(object args)
        {
            // 현재 값으로 초기화 (리스너 호출 없이)
            bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BgmVolume);
            sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SfxVolume);
        }
    }
}
