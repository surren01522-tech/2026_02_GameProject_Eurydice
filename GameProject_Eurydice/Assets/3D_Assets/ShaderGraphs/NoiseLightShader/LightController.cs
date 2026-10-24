using UnityEngine;

public class LightController : MonoBehaviour
{
    [SerializeField] private GameObject Light1;
    [SerializeField] private GameObject Light2;

    private bool isMlightOn;


    private void Awake()
    {
        if(Light1.activeSelf == false && Light2.activeSelf == false)
        {
            isMlightOn = true;
            Light1.SetActive(true);
            Light2.SetActive(false);
        }
        if(Light1.activeSelf == false)
        {
            isMlightOn = true;
            Light1.SetActive(true);
            Light2.SetActive(false);
        }
        else
        {
            Light1.SetActive(false);
            Light2.SetActive(true);
            isMlightOn = false;
        }
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
