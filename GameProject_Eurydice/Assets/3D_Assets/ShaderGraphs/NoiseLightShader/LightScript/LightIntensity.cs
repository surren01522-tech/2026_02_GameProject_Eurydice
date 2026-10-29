using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightIntensity : MonoBehaviour
{
    [Header("Light Target")]
    [SerializeField] private Light targetLight;

    [Header("Settings")]
    [Tooltip("Intensity가 변화할 때 걸리는 소요 시간 (초)")]
    [SerializeField, Range(0.05f, 3f)] private float smoothTime = 0.5f;
    [SerializeField] private float cooldown;

    [SerializeField] private float targetIntensity;

    private float originalIntensity;
    private float currentVelocity;
    private Coroutine _pulseCoroutine;

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        if (targetLight != null)
        {
            targetLight.intensity = originalIntensity;
        }
    }

    private void Start()
    {
        InvokeRepeating(nameof(LightToTarget),0.1f,cooldown);
    }

    private void Update()
    {
        if (targetLight == null) return;

        
        
    }

    private void LightToOrigina()
    {
        targetLight.intensity = Mathf.SmoothDamp(targetLight.intensity, originalIntensity, ref currentVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
    }

    private void LightToTarget()
    {
        targetLight.intensity = Mathf.SmoothDamp(targetLight.intensity, originalIntensity + targetIntensity, ref currentVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
    }
    
}