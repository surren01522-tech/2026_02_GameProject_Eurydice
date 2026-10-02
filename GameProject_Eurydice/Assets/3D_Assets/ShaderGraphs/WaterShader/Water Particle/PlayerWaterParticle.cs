using UnityEditor.Rendering;
using UnityEngine;

public class PlayerWaterParticle : MonoBehaviour
{
    [SerializeField] private ParticleSystem ParticleSystem;

    [SerializeField] private GameObject ParticleCam;

    [SerializeField] private float VelocityXZ, VelocityY;
    private Vector3 PlayerPos;
    

    private void Update()
    {
        VelocityXZ = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(PlayerPos.x, 0, PlayerPos.z));
        VelocityY = Vector3.Distance(new Vector3(0, transform.position.y, 0), new Vector3(0, PlayerPos.y, 0));
        PlayerPos = transform.position;

        ParticleCam.transform.position = transform.position + Vector3.up * 10;

        Shader.SetGlobalVector("_Player", transform.position);
    }


    void CreateParticle(int Start, int End, int Delta, float Speed, float Size, float Lifetime)
    {
        Vector3 forward = ParticleSystem.transform.eulerAngles;
        forward.y = Start;
        ParticleSystem.transform.eulerAngles = forward;

        for (int i = 0; i < 360; i += 3)
        {
            ParticleSystem.Emit(transform.position + ParticleSystem.transform.forward * 0.5f, ParticleSystem.transform.forward * Speed, Size, Lifetime, Color.white);
            ParticleSystem.transform.eulerAngles += Vector3.up * 3;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Water") && VelocityY > 0.003f)
        {
            CreateParticle(-180, 180, 3, 2, 2, 2);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water") && VelocityXZ > 0.001f && Time.renderedFrameCount % 3 == 0)
        {
            int y = (int)transform.eulerAngles.y;
            CreateParticle(y-100, y+100, 3, 5, 2, 1);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water") && VelocityY > 0.003f)
        {
            CreateParticle(-180, 180, 3, 2, 2, 2);
        }
    }
}
