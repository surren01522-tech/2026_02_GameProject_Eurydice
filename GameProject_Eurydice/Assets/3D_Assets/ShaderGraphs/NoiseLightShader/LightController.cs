using UnityEngine;

public class LightController : MonoBehaviour
{
    [SerializeField] private GameObject Light1;
    [SerializeField] private GameObject Light2;




    private void Awake()
    {
        if(Light1.activeSelf == false && Light2.activeSelf == false)
        {
            Light1.SetActive(true);
            Light2.SetActive(false);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            if (Light1.activeSelf == true)
            {
                Light1.SetActive(false);
                Light2.SetActive(true);
            }
            else if(Light2.activeSelf == true)
            {
                Light1.SetActive(true);
                Light2.SetActive(false);
            }
        }

    }
}
