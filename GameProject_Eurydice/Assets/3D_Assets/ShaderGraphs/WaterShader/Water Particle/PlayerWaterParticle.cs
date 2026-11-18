using UnityEngine;

public class PlayerWaterParticle : MonoBehaviour
{
    private static readonly int PlayerPosId = Shader.PropertyToID("_Player");
    private static readonly int PlayerPosV4Id = Shader.PropertyToID("_PlayerPosV4");

    [SerializeField] private ParticleSystem ParticleSystem;
    [SerializeField] private GameObject ParticleCam;
    [SerializeField] private float VelocityXZ, VelocityY;

    [Header("방출 설정")]
    [SerializeField] private float minHorizontalSpeed = 0.7f;
    [SerializeField] private float minVerticalSpeed = 0.8f;
    [SerializeField] private float stayEmitInterval = 0.083f;
    [SerializeField] private float spawnRadius = 0.5f;

    private Vector3 prevPosition;
    private float moveYawDeg;
    private float nextStayEmitTime;
    private int waterLayer = -1;
    private bool hasPrevPosition;

    private void Awake()
    {
        waterLayer = LayerMask.NameToLayer("Water");
    }

    public void BindWaterRig(ParticleSystem ps, GameObject cam)
    {
        ParticleSystem = ps;
        if (ParticleSystem != null)
        {
            var main = ParticleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }
        ParticleCam = cam;
    }

    public void UnbindWaterRig(ParticleSystem ps)
    {
        if (ParticleSystem != ps) return;
        ParticleSystem = null;
        ParticleCam = null;
    }

    private void OnEnable()
    {
        prevPosition = transform.position;
        hasPrevPosition = true;
        moveYawDeg = transform.eulerAngles.y;
    }

    private void Update()
    {
        Vector3 pos = transform.position;
        if (!hasPrevPosition)
        {
            prevPosition = pos;
            hasPrevPosition = true;
        }

        Vector3 delta = pos - prevPosition;
        prevPosition = pos;

        float dt = Time.deltaTime;
        if (dt > 0.0001f && delta.sqrMagnitude <= 25f)
        {
            float horizDistSq = delta.x * delta.x + delta.z * delta.z;
            if (horizDistSq > 1e-6f)
            {
                float horizDist = Mathf.Sqrt(horizDistSq);
                VelocityXZ = horizDist / dt;
                moveYawDeg = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            }
            else
            {
                VelocityXZ = 0f;
            }
            VelocityY = Mathf.Abs(delta.y) / dt;
        }
        else
        {
            VelocityXZ = 0f;
            VelocityY = 0f;
        }

        if (ParticleCam != null)
            ParticleCam.transform.position = pos + Vector3.up * 10f;

        Shader.SetGlobalVector(PlayerPosId, pos);
        Shader.SetGlobalVector(PlayerPosV4Id, new Vector4(pos.x, pos.y, pos.z, transform.localScale.x));
    }

    private void CreateParticle(float startAngle, float endAngle, float deltaAngle, float speed, float size, float lifetime)
    {
        if (ParticleSystem == null || deltaAngle <= 0f)
            return;

        Vector3 basePos = transform.position;
        var emitParams = new ParticleSystem.EmitParams
        {
            startSize = size,
            startLifetime = lifetime,
            startColor = Color.white
        };

        float startRad = startAngle * Mathf.Deg2Rad;
        float stepRad = deltaAngle * Mathf.Deg2Rad;
        float dx = Mathf.Sin(startRad);
        float dz = Mathf.Cos(startRad);
        float sinStep = Mathf.Sin(stepRad);
        float cosStep = Mathf.Cos(stepRad);

        for (float angle = startAngle; angle < endAngle; angle += deltaAngle)
        {
            emitParams.position = new Vector3(basePos.x + dx * spawnRadius, basePos.y, basePos.z + dz * spawnRadius);
            emitParams.velocity = new Vector3(dx * speed, 0f, dz * speed);
            ParticleSystem.Emit(emitParams, 1);

            float nextX = dx * cosStep + dz * sinStep;
            dz = dz * cosStep - dx * sinStep;
            dx = nextX;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == waterLayer && VelocityY > minVerticalSpeed)
            CreateParticle(-180f, 180f, 3f, 2f, 1.5f, 5f);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == waterLayer && VelocityXZ > minHorizontalSpeed && Time.time >= nextStayEmitTime)
        {
            nextStayEmitTime = Time.time + stayEmitInterval;
            CreateParticle(moveYawDeg - 100f, moveYawDeg + 100f, 3f, 3f, 2f, 4f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == waterLayer && VelocityY > minVerticalSpeed)
            CreateParticle(-180f, 180f, 3f, 2f, 1.5f, 5f);
    }
}
