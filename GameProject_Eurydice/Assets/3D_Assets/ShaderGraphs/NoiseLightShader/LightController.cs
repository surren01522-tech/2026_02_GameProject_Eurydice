using UnityEngine;

public class LightController : MonoBehaviour
{
    [SerializeField] private GameObject Light1;
    [SerializeField] private GameObject Light2;




    private void Awake()
    {
        if (Light1 != null && Light2 != null && !Light1.activeSelf && !Light2.activeSelf)
        {
            Light1.SetActive(true);
            Light2.SetActive(false);
        }
    }

    private void Start()
    {
        string saved = SaveManager.GetActiveLight(gameObject.scene.name);
        if (!string.IsNullOrEmpty(saved))
        {
            if (Light1 != null) Light1.SetActive(Light1.name == saved);
            if (Light2 != null) Light2.SetActive(Light2.name == saved);
        }
        SceneEnvironmentManager.RefreshSceneLights(gameObject.scene.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Light1 != null && Light1.activeSelf)
            {
                Light1.SetActive(false);
                if (Light2 != null)
                {
                    Light2.SetActive(true);
                    if (Light2.TryGetComponent<Light>(out var l2)) l2.enabled = true;
                    SaveManager.SaveActiveLight(Light2.name, gameObject.scene.name);
                }
            }
            else if (Light2 != null && Light2.activeSelf)
            {
                if (Light1 != null)
                {
                    Light1.SetActive(true);
                    if (Light1.TryGetComponent<Light>(out var l1)) l1.enabled = true;
                    SaveManager.SaveActiveLight(Light1.name, gameObject.scene.name);
                }
                Light2.SetActive(false);
            }
            SceneEnvironmentManager.RefreshSceneLights(gameObject.scene.name);
        }
    }
}
