using System.Collections;
using UnityEngine;
using GameFramework.Core;
using GameFramework.Services;

namespace GameFramework.Jam
{
    /// <summary>
    /// 게임잼용 "손맛(Juice)" 헬퍼. 한 줄로 타격감을 만든다.
    /// 사용 예)
    ///   Juice.Hit(hitPos);                          // 이펙트+사운드+쉐이크+히트스탑
    ///   Juice.Hit(hitPos, "boom_fx", "boom", 0.4f); // 강한 버전
    /// 주의: 카메라 쉐이크는 Camera.main의 localPosition을 흔든다.
    ///       Cinemachine 사용 시엔 Impulse Source로 교체할 것.
    /// </summary>
    public static class Juice
    {
        public static void Hit(Vector3 pos, string fx = "hit_fx", string sfx = "hit",
                               float shake = 0.2f, float hitStop = 0.05f)
        {
            if (!string.IsNullOrEmpty(fx))  EffectManager.Instance.Play(fx, pos);
            if (!string.IsNullOrEmpty(sfx)) SoundManager.Instance.PlaySFX(sfx);
            if (shake > 0f)   JuiceRunner.Instance.Shake(0.15f, shake);
            if (hitStop > 0f) JuiceRunner.Instance.HitStop(hitStop);
        }
    }

    /// <summary>코루틴이 필요한 연출(쉐이크/히트스탑)을 실행하는 러너.</summary>
    public class JuiceRunner : MonoSingleton<JuiceRunner>
    {
        private Coroutine _shake, _stop;

        // ===================== 카메라 쉐이크 =====================

        public void Shake(float duration, float strength)
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (_shake != null) StopCoroutine(_shake);
            _shake = StartCoroutine(ShakeCo(cam.transform, duration, strength));
        }

        private IEnumerator ShakeCo(Transform t, float dur, float str)
        {
            Vector3 origin = t.localPosition;
            float e = 0f;
            while (e < dur)
            {
                e += Time.unscaledDeltaTime;
                float falloff = 1f - (e / dur); // 점점 약해지게
                t.localPosition = origin + (Vector3)(Random.insideUnitCircle * str * falloff);
                yield return null;
            }
            t.localPosition = origin;
        }

        // ===================== 히트스탑 =====================

        public void HitStop(float duration)
        {
            if (_stop != null) StopCoroutine(_stop);
            _stop = StartCoroutine(HitStopCo(duration));
        }

        private IEnumerator HitStopCo(float dur)
        {
            float prev = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(dur);
            Time.timeScale = prev <= 0f ? 1f : prev;
        }
    }
}
