using UnityEngine;

public class LightController : MonoBehaviour
{
    [SerializeField] private GameObject Light1;
    [SerializeField] private GameObject Light2;

    private bool isMlightOn = true;


    private void Awake()
    {
        Light1.SetActive(true);
        Light2.SetActive(false);
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            if (isMlightOn == false)
            {
                Light1.SetActive(true);
                Light2.SetActive(false);
                isMlightOn = true;
            }
            else
            {
                Light1.SetActive(false);
                Light2.SetActive(true);
                isMlightOn = false;
            }
        }

    }
}
