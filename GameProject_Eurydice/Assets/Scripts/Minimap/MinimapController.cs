using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("미니맵 ID (비워두면 씬 이름 사용)")]
    [SerializeField] private string mapId;

    public string CurrentMapId
    {
        get
        {
            if (string.IsNullOrEmpty(mapId)) return SceneManager.GetActiveScene().name;
            return mapId;
        }
    }

    [Header("미니맵 UI")]
    [SerializeField] private RectTransform minimapDisplayRect;
    [SerializeField] private RawImage mapBackgroundImage;
    [SerializeField] private Shader fogMaskShader;
    [SerializeField] private RectTransform playerIcon;

    [Header("플레이어 아이콘 & 방향 연출")]
    [SerializeField] private bool autoScaleWithMap = true;
    [Range(0.01f, 0.1f)]
    [Tooltip("뷰포트 크기 대비 아이콘 크기 비율")]
    [SerializeField] private float iconMapRatio = 0.045f;
    [SerializeField] private float minIconSize = 16f;
    [SerializeField] private float maxIconSize = 64f;
    [SerializeField] private float iconBaseSize = 24f;
    [SerializeField] private bool showDirectionPointer = true;
    [SerializeField] private bool trackCameraDirection = false;
    [SerializeField] private Color pointerColor = new Color(1f, 0.85f, 0.4f, 0.95f);

    [Header("맵 그리기")]
    [SerializeField] private Transform player;
    [Tooltip("지정 시 동적 캡처/자동 바운드 대신 베이크된 월드 맵 사용")]
    [SerializeField] private MinimapBakeData bakeData;
    [SerializeField] private bool autoDetectBounds = true;
    [SerializeField] private Transform mapRoot;
    [SerializeField] private LayerMask mapLayer = ~0;
    [SerializeField] private Vector2 worldCenter = Vector2.zero;
    [SerializeField] private Vector2 worldSize = new Vector2(200f, 200f);

    [Header("동적 맵 그리기")]
    [SerializeField] private bool useDynamicCapture = true;
    [SerializeField] private int captureResolution = 512;
    [SerializeField] private Color mapClearColor = new Color(0.12f, 0.12f, 0.15f, 1f);

    [Header("Fog 설정")]
    [SerializeField] private int fogResolution = 128;
    [SerializeField] private float revealRadius = 15f;
    [Range(0f, 1f)]
    [Tooltip("안개가 완전히 걷히는 중심 반경 비율")]
    [SerializeField] private float innerRevealPercent = 0.6f;
    [SerializeField] private float updateDistanceThreshold = 0.5f;

    [Header("캡쳐 높낮이 설정")]
    [SerializeField] private bool useHeightCheck = false;
    [Tooltip("플레이어 기준 천장 컷플레인 높이 오프셋")]
    [SerializeField] private float ceilingOffset = 3f;

    private MinimapFog fog;
    private MinimapCapture capture;
    private MinimapPlayerIcon icon;
    private Material fogMaskMaterial;
    private Vector3 lastPlayerPos;
    private float maxWorldY = 100f;
    private Transform mainCameraTransform;
    private bool wasUIVisible;

    private void OnEnable()
    {
        SaveManager.OnSaveDataDeleted += ResetFog;
    }

    private void OnDisable()
    {
        SaveManager.OnSaveDataDeleted -= ResetFog;
    }

    private void Awake()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        if (minimapDisplayRect == null && mapBackgroundImage != null)
        {
            minimapDisplayRect = mapBackgroundImage.rectTransform;
        }

        if (playerIcon != null)
        {
            icon = new MinimapPlayerIcon(playerIcon, pointerColor);
        }

        if (mapBackgroundImage != null)
        {
            mapBackgroundImage.enabled = false;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            TryFindPlayer();
        }

        if (bakeData != null)
        {
            worldCenter = bakeData.worldCenter;
            worldSize = bakeData.worldSize;
            if (mapBackgroundImage != null) mapBackgroundImage.texture = bakeData.mapTexture;
        }
        else if (autoDetectBounds)
        {
            CalculateWorldBounds();
        }

        InitializeFog();

        if (bakeData == null && useDynamicCapture && mapBackgroundImage != null)
        {
            CaptureMapSnapshot();
        }

        SyncPlayer();

        if (mapBackgroundImage != null)
        {
            mapBackgroundImage.enabled = true;
        }
    }

    private void Update()
    {
        if (player == null)
        {
            if (!TryFindPlayer()) return;
        }

        if (fog == null) return;

        bool isVisible = minimapDisplayRect != null && minimapDisplayRect.gameObject.activeInHierarchy;
        if (isVisible && !wasUIVisible)
        {
            UpdatePlayerIcon();
        }
        wasUIVisible = isVisible;

        float movedX = player.position.x - lastPlayerPos.x;
        float movedZ = player.position.z - lastPlayerPos.z;
        float movedDistSqr = movedX * movedX + movedZ * movedZ;

        if (movedDistSqr >= updateDistanceThreshold * updateDistanceThreshold)
        {
            lastPlayerPos = player.position;
            RevealFog(player.position);
        }
    }

    public void RefreshMapDisplay()
    {
        SyncPlayer();
    }

    private void OnDestroy()
    {
        if (mapBackgroundImage != null)
        {
            mapBackgroundImage.enabled = false;
        }

        SaveFog();

        if (fogMaskMaterial != null)
        {
            Destroy(fogMaskMaterial);
            fogMaskMaterial = null;
        }

        if (fog != null)
        {
            fog.Dispose();
            fog = null;
        }

        if (capture != null)
        {
            capture.Dispose();
            capture = null;
        }
    }

    public void ResetFog()
    {
        InitializeFog();
        SyncPlayer();
    }

    public void CalculateWorldBounds()
    {
        Bounds b;
        if (!MinimapCapture.TryCalculateBounds(mapRoot, out b)) return;

        worldCenter = new Vector2(b.center.x, b.center.z);
        float maxSide = Mathf.Max(b.size.x, b.size.z);
        if (maxSide <= 10f) maxSide = 200f;
        worldSize = new Vector2(maxSide, maxSide);
        maxWorldY = b.max.y;
    }

    public void CaptureMapSnapshot()
    {
        if (capture == null)
        {
            capture = new MinimapCapture(captureResolution, mapClearColor, mapLayer);
        }

        bool useCut = useHeightCheck && player != null;
        float cutY = player != null ? player.position.y + ceilingOffset : 0f;

        capture.Render(worldCenter, worldSize, maxWorldY, useCut, cutY);

        if (mapBackgroundImage != null)
        {
            mapBackgroundImage.texture = capture.Texture;
        }
    }

    public void RevealFog(Vector3 worldPos)
    {
        if (fog == null || worldSize.x <= 0f || worldSize.y <= 0f) return;

        Vector2 normalizedPos = WorldToNormalized(worldPos);
        float radiusInPixels = (revealRadius / worldSize.x) * fogResolution;

        fog.Reveal(normalizedPos, radiusInPixels, innerRevealPercent);
    }

    public void SaveFog()
    {
        if (fog != null)
        {
            SaveManager.SaveMinimapFog(CurrentMapId, fog.GetAlpha());
        }
    }

    public void LoadFog()
    {
        if (fog != null)
        {
            byte[] savedData = SaveManager.LoadMinimapFog(CurrentMapId);
            fog.SetAlpha(savedData);
        }
    }

    private void InitializeFog()
    {
        if (fog == null)
        {
            fog = new MinimapFog(fogResolution);
        }
        else
        {
            fog.Clear();
        }

        if (mapBackgroundImage != null)
        {
            if (fogMaskMaterial == null)
            {
                Shader shader = fogMaskShader != null ? fogMaskShader : Shader.Find("UI/MinimapFogMask");
                if (shader != null)
                {
                    fogMaskMaterial = new Material(shader);
                }
            }

            if (fogMaskMaterial != null)
            {
                fogMaskMaterial.SetTexture("_FogTex", fog.Texture);
                mapBackgroundImage.material = fogMaskMaterial;
            }
        }

        LoadFog();
    }

    private bool TryFindPlayer()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return false;

        player = pc.transform;
        SyncPlayer();
        return true;
    }

    private void SyncPlayer()
    {
        if (player == null) return;

        lastPlayerPos = player.position;
        UpdatePlayerIcon();
        RevealFog(player.position);
    }

    private void UpdatePlayerIcon()
    {
        if (icon == null || player == null || minimapDisplayRect == null) return;

        Vector2 size = minimapDisplayRect.rect.size;
        float currentIconSize = iconBaseSize;

        if (autoScaleWithMap)
        {
            float minDim = Mathf.Min(size.x, size.y);
            currentIconSize = Mathf.Clamp(minDim * iconMapRatio, minIconSize, maxIconSize);
        }

        float yaw = player.eulerAngles.y;
        if (trackCameraDirection)
        {
            if (mainCameraTransform != null)
            {
                yaw = mainCameraTransform.eulerAngles.y;
            }
        }

        Vector2 normPos = WorldToNormalized(player.position);
        icon.Update(minimapDisplayRect, normPos, yaw, currentIconSize, showDirectionPointer);
    }

    private Vector2 WorldToNormalized(Vector3 worldPos)
    {
        float minX = worldCenter.x - worldSize.x * 0.5f;
        float minY = worldCenter.y - worldSize.y * 0.5f;

        float normX = Mathf.Clamp01((worldPos.x - minX) / worldSize.x);
        float normY = Mathf.Clamp01((worldPos.z - minY) / worldSize.y);

        return new Vector2(normX, normY);
    }
}
