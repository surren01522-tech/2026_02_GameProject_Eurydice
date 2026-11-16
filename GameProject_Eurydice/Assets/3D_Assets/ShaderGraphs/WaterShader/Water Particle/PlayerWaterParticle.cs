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

    private Camera particleCamera;
    private Vector3 prevPosition;
    private float moveYawDeg;
    private float nextStayEmitTime;
    private int waterLayer = -1;
    private bool hasPrevPosition;
    private bool hasClearedEmptyCam;

    private void Awake()
    {
        waterLayer = LayerMask.NameToLayer("Water");
        if (ParticleCam != null)
            particleCamera = ParticleCam.GetComponent<Camera>();
    }

    public void BindWaterRig(ParticleSystem ps, GameObject cam)
    {
        ParticleSystem = ps;
        ParticleCam = cam;
        particleCamera = cam != null ? cam.GetComponent<Camera>() : null;
        hasClearedEmptyCam = false;
    }

    public void UnbindWaterRig(ParticleSystem ps)
    {
        if (ParticleSystem != ps) return;
        ParticleSystem = null;
        ParticleCam = null;
        particleCamera = null;
    }

    private void OnEnable()
    {
        prevPosition = transform.position;
        hasPrevPosition = true;
        hasClearedEmptyCam = false;
        moveYawDeg = transform.eulerAngles.y;
    }

    private void LateUpdate()
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
            float horizDist = horizDistSq > 1e-8f ? Mathf.Sqrt(horizDistSq) : 0f;
            VelocityXZ = horizDist / dt;
            VelocityY = Mathf.Abs(delta.y) / dt;

            if (horizDist > 0.001f)
                moveYawDeg = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
        }
        else
        {
            VelocityXZ = 0f;
            VelocityY = 0f;
        }

        Shader.SetGlobalVector(PlayerPosId, pos);
        Shader.SetGlobalVector(PlayerPosV4Id, new Vector4(pos.x, pos.y, pos.z, transform.localScale.x));

        UpdateParticleCamera(pos);
    }

    private void UpdateParticleCamera(Vector3 playerPos)
    {
        if (ParticleCam == null)
            return;

        bool hasParticles = ParticleSystem != null && ParticleSystem.particleCount > 0;
        if (hasParticles)
        {
            ParticleCam.transform.position = playerPos + Vector3.up * 10f;
            hasClearedEmptyCam = false;
            if (particleCamera != null && !particleCamera.enabled)
                particleCamera.enabled = true;
            return;
        }

        if (!hasClearedEmptyCam)
        {
            // Render one empty frame after particles die so _Particle_RT clears its last trail.
            ParticleCam.transform.position = playerPos + Vector3.up * 10f;
            hasClearedEmptyCam = true;
            if (particleCamera != null && !particleCamera.enabled)
                particleCamera.enabled = true;
        }
        else if (particleCamera != null && particleCamera.enabled)
        {
            particleCamera.enabled = false;
        }
    }

    private void CreateParticle(float startAngle, float endAngle, float deltaAngle, float speed, float size, float lifetime)
    {
        if (ParticleSystem == null || deltaAngle <= 0f)
            return;

        ParticleSystem.MainModule main = ParticleSystem.main;
        ParticleSystemSimulationSpace simSpace = main.simulationSpace;
        Transform simTransform = null;
        if (simSpace == ParticleSystemSimulationSpace.Local)
            simTransform = ParticleSystem.transform;
        else if (simSpace == ParticleSystemSimulationSpace.Custom && main.customSimulationSpace != null)
            simTransform = main.customSimulationSpace;

        Matrix4x4 worldToSim = simTransform != null ? simTransform.worldToLocalMatrix : Matrix4x4.identity;
        bool useSimMatrix = simTransform != null;
        Vector3 basePos = transform.position;

        var emitParams = new ParticleSystem.EmitParams
        {
            startSize = size,
            startLifetime = lifetime,
            startColor = Color.white
        };

        for (float angle = startAngle; angle < endAngle; angle += deltaAngle)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            Vector3 worldPos = basePos + dir * spawnRadius;
            Vector3 worldVel = dir * speed;

            if (useSimMatrix)
            {
                emitParams.position = worldToSim.MultiplyPoint3x4(worldPos);
                emitParams.velocity = worldToSim.MultiplyVector(worldVel);
            }
            else
            {
                emitParams.position = worldPos;
                emitParams.velocity = worldVel;
            }

            ParticleSystem.Emit(emitParams, 1);
        }

        hasClearedEmptyCam = false;
        if (particleCamera != null && !particleCamera.enabled)
            particleCamera.enabled = true;
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
