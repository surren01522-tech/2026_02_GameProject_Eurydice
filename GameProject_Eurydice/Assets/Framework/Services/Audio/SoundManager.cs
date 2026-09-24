using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using GameFramework.Core;
using GameFramework.Data;

namespace GameFramework.Services
{
    /// <summary>
    /// BGM/SFX 사운드 매니저.
    /// - BGM 크로스페이드, SFX AudioSource 풀링, 3D 사운드, 볼륨 설정 저장
    /// - Resources/SoundLibrary.asset 자동 로드
    /// 사용 예)
    ///   SoundManager.Instance.PlayBGM("title");
    ///   SoundManager.Instance.PlaySFX("hit");
    ///   SoundManager.Instance.PlaySFX3D("explosion", position);
    /// </summary>
    public class SoundManager : MonoSingleton<SoundManager>
    {
        [SerializeField] private SoundLibrary library;
        [SerializeField] private AudioMixerGroup bgmGroup;   // 선택: AudioMixer 사용 시 연결
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private int sfxSourceCount = 12;    // 동시 재생 한도
        [SerializeField] private float bgmFadeTime = 0.8f;

        private AudioSource _bgmA, _bgmB;
        private bool _usingA = true;
        private readonly List<AudioSource> _sfxSources = new();
        private int _sfxIndex;
        private Coroutine _fadeRoutine;

        private const string KeyBgmVolume = "opt_bgm_volume";
        private const string KeySfxVolume = "opt_sfx_volume";

        public float BgmVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public string CurrentBgmId { get; private set; }

        protected override void OnInitialize()
        {
            if (library == null) library = Resources.Load<SoundLibrary>("SoundLibrary");

            _bgmA = CreateSource("BGM_A", bgmGroup, loop: true);
            _bgmB = CreateSource("BGM_B", bgmGroup, loop: true);

            for (int i = 0; i < sfxSourceCount; i++)
                _sfxSources.Add(CreateSource($"SFX_{i}", sfxGroup, loop: false));

            BgmVolume = PlayerPrefs.GetFloat(KeyBgmVolume, 1f);
            SfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, 1f);
        }

        private AudioSource CreateSource(string name, AudioMixerGroup group, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            if (group != null) src.outputAudioMixerGroup = group;
            return src;
        }

        // ===================== BGM =====================

        public void PlayBGM(string id, bool fade = true)
        {
            if (CurrentBgmId == id) return;
            var entry = library?.Get(id);
            if (entry == null || entry.clip == null) { Debug.LogWarning($"[Sound] BGM 없음: {id}"); return; }

            CurrentBgmId = id;
            var from = _usingA ? _bgmA : _bgmB;
            var to = _usingA ? _bgmB : _bgmA;
            _usingA = !_usingA;

            to.clip = entry.clip;
            to.pitch = entry.pitch;
            to.Play();

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            if (fade) _fadeRoutine = StartCoroutine(CrossFade(from, to, entry.volume * BgmVolume));
            else { from.Stop(); to.volume = entry.volume * BgmVolume; }
        }

        public void StopBGM(bool fade = true)
        {
            CurrentBgmId = null;
            var cur = _usingA ? _bgmA : _bgmB;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            if (fade) _fadeRoutine = StartCoroutine(FadeOut(cur));
            else cur.Stop();
        }

        private IEnumerator CrossFade(AudioSource from, AudioSource to, float targetVol)
        {
            float t = 0f, fromStart = from.volume;
            to.volume = 0f;
            while (t < bgmFadeTime)
            {
                t += Time.unscaledDeltaTime;
                float k = t / bgmFadeTime;
                from.volume = Mathf.Lerp(fromStart, 0f, k);
                to.volume = Mathf.Lerp(0f, targetVol, k);
                yield return null;
            }
            from.Stop();
            to.volume = targetVol;
        }

        private IEnumerator FadeOut(AudioSource src)
        {
            float t = 0f, start = src.volume;
            while (t < bgmFadeTime)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(start, 0f, t / bgmFadeTime);
                yield return null;
            }
            src.Stop();
        }

        // ===================== SFX =====================

        public void PlaySFX(string id)
        {
            var entry = library?.Get(id);
            if (entry == null || entry.clip == null) { Debug.LogWarning($"[Sound] SFX 없음: {id}"); return; }

            var src = NextSfxSource();
            src.transform.localPosition = Vector3.zero;
            src.spatialBlend = 0f; // 2D
            src.pitch = entry.randomPitch ? entry.pitch + Random.Range(-0.1f, 0.1f) : entry.pitch;
            src.PlayOneShot(entry.clip, entry.volume * SfxVolume);
        }

        public void PlaySFX3D(string id, Vector3 position, float maxDistance = 30f)
        {
            var entry = library?.Get(id);
            if (entry == null || entry.clip == null) return;

            var src = NextSfxSource();
            src.transform.position = position;
            src.spatialBlend = 1f; // 3D
            src.maxDistance = maxDistance;
            src.pitch = entry.randomPitch ? entry.pitch + Random.Range(-0.1f, 0.1f) : entry.pitch;
            src.PlayOneShot(entry.clip, entry.volume * SfxVolume);
        }

        private AudioSource NextSfxSource()
        {
            var src = _sfxSources[_sfxIndex];
            _sfxIndex = (_sfxIndex + 1) % _sfxSources.Count;
            return src;
        }

        // ===================== 볼륨 설정 =====================

        public void SetBgmVolume(float v)
        {
            BgmVolume = Mathf.Clamp01(v);
            var cur = _usingA ? _bgmA : _bgmB;
            var entry = CurrentBgmId != null ? library?.Get(CurrentBgmId) : null;
            cur.volume = (entry?.volume ?? 1f) * BgmVolume;
            PlayerPrefs.SetFloat(KeyBgmVolume, BgmVolume);
        }

        public void SetSfxVolume(float v)
        {
            SfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(KeySfxVolume, SfxVolume);
        }
    }
}
