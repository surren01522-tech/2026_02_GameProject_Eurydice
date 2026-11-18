using UnityEngine;

public class CrystalControll : MonoBehaviour
{
    public Material material;

    [Header("Crystal Control")]
    [Range(0f, 1f)]
    public float CrystalValue = 1f;

    [SerializeField] private float smoothTime = 0.3f;

    private float currentCrystalValue;
    private float currentVelocity;

    private static readonly int OnOffPropertyID = Shader.PropertyToID("_OnOff");

    private void Start()
    {
        currentCrystalValue = CrystalValue;

        if (material != null)
        {
            material.SetFloat(OnOffPropertyID, currentCrystalValue);
        }
    }

    private void Update()
    {
        if (material == null) return;

        currentCrystalValue = Mathf.SmoothDamp(currentCrystalValue,CrystalValue,ref currentVelocity,smoothTime);


        material.SetFloat(OnOffPropertyID, currentCrystalValue);
    }

    public void ChangeCrystalValue(float target)
    {
        CrystalValue = Mathf.Clamp01(target);
    }
}