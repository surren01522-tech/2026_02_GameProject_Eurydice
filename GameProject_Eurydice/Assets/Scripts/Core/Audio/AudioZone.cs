using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AudioZone : MonoBehaviour
{
    private static readonly List<AudioZone> ActiveZones = new List<AudioZone>();

    [Header("오버라이드 설정")]
    [SerializeField] private bool overrideBgm = true;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private bool overrideAmbience = true;
    [SerializeField] private AudioClip ambienceClip;
    [SerializeField, Min(0f)] private float fadeDuration = 1.5f;

    [Header("기즈모")]
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.6f, 1f, 0.25f);

    private Collider col;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStaticState() => ActiveZones.Clear();

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnDisable()
    {
        if (ActiveZones.Remove(this) && Application.isPlaying)
            RefreshActiveAudio();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;

        if (!ActiveZones.Contains(this))
            ActiveZones.Add(this);

        RefreshActiveAudio();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;

        if (ActiveZones.Remove(this))
            RefreshActiveAudio();
    }

    public static void RefreshActiveAudio()
    {
        if (AudioManager.Instance == null) return;

        for (int i = ActiveZones.Count - 1; i >= 0; i--)
        {
            if (ActiveZones[i] == null || !ActiveZones[i].isActiveAndEnabled)
                ActiveZones.RemoveAt(i);
        }

        AudioZone bgmZone = FindTopZone(z => z.overrideBgm);
        if (bgmZone != null)
            AudioManager.Instance.FadeBGM(bgmZone.bgmClip, bgmZone.fadeDuration);
        else
            SceneEnvironmentManager.ApplyActiveSceneAudio(includeBgm: true, includeAmbience: false);

        AudioZone ambZone = FindTopZone(z => z.overrideAmbience);
        if (ambZone != null)
            AudioManager.Instance.PlayAmbience(ambZone.ambienceClip, ambZone.fadeDuration);
        else
            SceneEnvironmentManager.ApplyActiveSceneAudio(includeBgm: false, includeAmbience: true);
    }

    private static AudioZone FindTopZone(System.Predicate<AudioZone> predicate)
    {
        for (int i = ActiveZones.Count - 1; i >= 0; i--)
        {
            if (predicate(ActiveZones[i]))
                return ActiveZones[i];
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(box.center, box.size);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.8f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
