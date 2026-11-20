using UnityEngine;
using UnityEngine.Animations;

public class PlayerTracker : MonoBehaviour
{
    private static readonly int PlayerPosId = Shader.PropertyToID("_Player");

    public static PlayerTracker Instance { get; private set; }

    [Header("Grass Step Effect (Optional)")]
    [SerializeField] private Transform stepEffectTransform;
    [SerializeField] private float stepDiameter = 1.2f;
    [SerializeField] private float maxGroundGap = 0.35f;
    [SerializeField] private LayerMask groundLayer = (1 << 0) | (1 << 3) | (1 << 6);
    [SerializeField] private float footOffsetOverride = 0f;

    private CharacterController characterController;
    private Collider cachedCollider;
    private PositionConstraint stepConstraint;
    private Renderer stepRenderer;
    private TrailRenderer stepTrail;

    public Vector3 FootPosition => transform.position + new Vector3(0f, GetFootOffsetY(), 0f);

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        cachedCollider = GetComponent<Collider>();
        CacheStepEffectComponents();
    }

    private void OnEnable()
    {
        Instance = this;
        CacheStepEffectComponents();
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        Shader.SetGlobalVector(PlayerPosId, transform.position);
    }

    private void LateUpdate()
    {
        UpdateStepEffect();
    }

    public void BindStepEffect(Transform target)
    {
        stepEffectTransform = target;
        CacheStepEffectComponents();

        if (stepEffectTransform == null)
            return;

        if (stepConstraint != null)
        {
            var source = new ConstraintSource { sourceTransform = transform, weight = 1f };
            if (stepConstraint.sourceCount > 0)
                stepConstraint.SetSource(0, source);
            else
                stepConstraint.AddSource(source);
        }

        stepEffectTransform.position = FootPosition;
        if (stepTrail != null)
            stepTrail.Clear();
    }

    public void UnbindStepEffect(Transform target)
    {
        if (stepEffectTransform != target)
            return;

        if (stepRenderer != null)
            stepRenderer.enabled = false;
        if (stepTrail != null)
            stepTrail.emitting = false;

        stepEffectTransform = null;
        stepConstraint = null;
        stepRenderer = null;
        stepTrail = null;
    }

    private void CacheStepEffectComponents()
    {
        stepConstraint = null;
        stepRenderer = null;
        stepTrail = null;

        if (stepEffectTransform == null)
            return;

        stepConstraint = stepEffectTransform.GetComponent<PositionConstraint>();
        stepRenderer = stepEffectTransform.GetComponent<Renderer>();
        stepTrail = stepEffectTransform.GetComponentInChildren<TrailRenderer>();
    }

    private float GetFootOffsetY()
    {
        if (Mathf.Abs(footOffsetOverride) > 0.0001f)
            return footOffsetOverride;

        if (characterController != null)
            return characterController.center.y - characterController.height * 0.5f;

        if (cachedCollider != null)
            return cachedCollider.bounds.min.y - transform.position.y;

        return 0f;
    }

    private void UpdateStepEffect()
    {
        if (stepEffectTransform == null)
            return;

        float footOffsetY = GetFootOffsetY();
        if (stepConstraint != null)
        {
            stepConstraint.translationOffset = new Vector3(0f, footOffsetY, 0f);
        }
        else
        {
            stepEffectTransform.position = transform.position + new Vector3(0f, footOffsetY, 0f);
        }

        if (stepDiameter > 0.01f)
            stepEffectTransform.localScale = new Vector3(stepDiameter, stepDiameter, stepDiameter);

        bool grounded = IsNearGround(transform.position + new Vector3(0f, footOffsetY, 0f));

        if (stepRenderer != null)
            stepRenderer.enabled = grounded;

        if (stepTrail != null)
            stepTrail.emitting = grounded;
    }

    private bool IsNearGround(Vector3 footPos)
    {
        if (characterController != null && characterController.isGrounded)
            return true;

        if (groundLayer.value == 0)
            return true;

        if (Physics.Raycast(
            footPos + Vector3.up * 0.25f,
            Vector3.down,
            out RaycastHit hit,
            0.25f + maxGroundGap,
            groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            return hit.transform.root != transform.root;
        }

        return false;
    }
}
