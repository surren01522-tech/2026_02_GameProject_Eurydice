using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("미니맵 ID (비워두면 씬 이름 사용)")]
    [SerializeField] private string mapId;

    public string CurrentMapId => string.IsNullOrEmpty(mapId) ? SceneManager.GetActiveScene().name : mapId;

    [Header("미니맵 UI")]
    [SerializeField] private RectTransform minimapDisplayRect;
    [SerializeField] private RawImage mapBackgroundImage;
    [SerializeField] private RawImage fogOverlayImage;
    [SerializeField] private RectTransform playerIcon;

    [Header("플레이어 아이콘 & 방향 연출")]
    [SerializeField] private bool autoScaleWithMap = true;
    [Range(0.01f, 0.1f)]
    [Tooltip("미니맵 뷰포트 크기 대비 아이콘 크기 비율 (기본 0.045 = 4.5%)")]
    [SerializeField] private float iconMapRatio = 0.045f;
    [SerializeField] private float minIconSize = 16f;
    [SerializeField] private float maxIconSize = 64f;
    [SerializeField] private float iconBaseSize = 24f;
    [SerializeField] private bool showDirectionPointer = true;
    [SerializeField] private bool trackCameraDirection = false;
    [SerializeField] private Color pointerColor = new Color(1f, 0.85f, 0.4f, 0.95f);

    [Header("맵 그리기")]
    [SerializeField] private Transform player;
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
    [Tooltip("완전히 밝혀지는 중심 영역 비율 (0.6 = 60%, 1.0 = 페이드 없이 완전 클리어)")]
    [SerializeField] private float innerRevealPercent = 0.6f;
    [SerializeField] private float updateDistanceThreshold = 0.5f;

    [Header("캡쳐 높낮이 설정")]
    [SerializeField] private bool useHeightCheck = false;
    [Tooltip("플레이어 머리 위 몇 미터에서 천장을 잘라낼 것인가 (Cut-Plane)")]
    [SerializeField] private float ceilingOffset = 3f;

    private Texture2D fogTexture;
    private RenderTexture mapRenderTexture;
    private Camera captureCamera;
    private Material fogMaskMaterial;
    private Color32[] fogColors;
    private Vector3 lastPlayerPos;
    private float maxWorldY = 100f;
    private bool isInitialized;
    private RectTransform directionPointer;

    private void OnEnable()
    {
        SaveManager.OnSaveDataDeleted += ResetFog;
    }

    private void OnDisable()
    {
        SaveManager.OnSaveDataDeleted -= ResetFog;
    }

    /// <summary>
    /// 미니맵 Fog를 초기 상태로 리셋하고 플레이어 주변만 다시 밝힙니다.
    /// </summary>
    public void ResetFog()
    {
        InitializeFog();
        if (player != null)
        {
            lastPlayerPos = player.position;
            UpdatePlayerIcon();
            RevealFog(player.position);
        }
    }

    private void Awake()
    {
        ResolveDisplayContainer();
        if (mapBackgroundImage != null) mapBackgroundImage.enabled = false;
    }

    private void Start()
    {
        ResolveDisplayContainer();
        TryFindPlayer();

        if (autoDetectBounds)
        {
            CalculateWorldBounds();
        }

        InitializeFog();

        if (useDynamicCapture && mapBackgroundImage != null)
        {
            CaptureMapSnapshot();
        }

        if (player != null)
        {
            lastPlayerPos = player.position;
            UpdatePlayerIcon();
            RevealFog(player.position);
        }

        if (mapBackgroundImage != null) mapBackgroundImage.enabled = true;
    }

    private void Update()
    {
        if (player == null)
        {
            TryFindPlayer();
            if (player == null) return;
        }

        if (!isInitialized) return;

        UpdatePlayerIcon();

        float movedDistance = Vector2.Distance(
            new Vector2(player.position.x, player.position.z),
            new Vector2(lastPlayerPos.x, lastPlayerPos.z)
        );

        if (movedDistance >= updateDistanceThreshold)
        {
            lastPlayerPos = player.position;
            RevealFog(player.position);
        }
    }

    private void OnDestroy()
    {
        if (mapBackgroundImage != null) mapBackgroundImage.enabled = false;
        if (fogOverlayImage != null) fogOverlayImage.enabled = false;

        SaveFog();

        if (fogMaskMaterial != null)
        {
            Destroy(fogMaskMaterial);
            fogMaskMaterial = null;
        }

        if (fogTexture != null)
        {
            Destroy(fogTexture);
            fogTexture = null;
        }

        if (mapRenderTexture != null)
        {
            mapRenderTexture.Release();
            Destroy(mapRenderTexture);
            mapRenderTexture = null;
        }

        if (captureCamera != null)
        {
            Destroy(captureCamera.gameObject);
        }
    }

    /// <summary>
    /// 씬 내의 지형 및 렌더러/콜라이더를 분석하여 월드 센터와 크기 자동 계산
    /// </summary>
    public void CalculateWorldBounds()
    {
        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;

        if (mapRoot != null)
        {
            var renderers = mapRoot.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!hasBounds)
                {
                    combinedBounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else combinedBounds.Encapsulate(renderers[i].bounds);
            }
        }
        else
        {
            var terrains = Terrain.activeTerrains;
            if (terrains != null && terrains.Length > 0)
            {
                for (int i = 0; i < terrains.Length; i++)
                {
                    TerrainData td = terrains[i].terrainData;
                    Vector3 pos = terrains[i].transform.position;
                    Bounds tBounds = new Bounds(pos + td.size * 0.5f, td.size);
                    if (!hasBounds)
                    {
                        combinedBounds = tBounds;
                        hasBounds = true;
                    }
                    else combinedBounds.Encapsulate(tBounds);
                }
            }
            else
            {
                var colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i].isTrigger) continue;
                    if (!hasBounds)
                    {
                        combinedBounds = colliders[i].bounds;
                        hasBounds = true;
                    }
                    else combinedBounds.Encapsulate(colliders[i].bounds);
                }
            }
        }

        if (hasBounds)
        {
            worldCenter = new Vector2(combinedBounds.center.x, combinedBounds.center.z);
            float maxSide = Mathf.Max(combinedBounds.size.x, combinedBounds.size.z);
            if (maxSide <= 10f) maxSide = 200f;
            worldSize = new Vector2(maxSide, maxSide);
            maxWorldY = combinedBounds.max.y;
        }
    }

    /// <summary>
    /// 동적 카메라를 생성하여 맵 탑뷰 스냅샷 캡처 (천장 컷플레인 지원)
    /// </summary>
    public void CaptureMapSnapshot()
    {
        if (mapRenderTexture == null)
        {
            mapRenderTexture = new RenderTexture(captureResolution, captureResolution, 16, RenderTextureFormat.ARGB32);
            mapRenderTexture.Create();
        }

        if (captureCamera == null)
        {
            GameObject camObj = new GameObject("MinimapCaptureCamera");
            captureCamera = camObj.AddComponent<Camera>();
            captureCamera.enabled = false;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = mapClearColor;
            captureCamera.cullingMask = mapLayer;
            captureCamera.orthographic = true;
            captureCamera.targetTexture = mapRenderTexture;
        }

        captureCamera.orthographicSize = Mathf.Max(worldSize.x, worldSize.y) * 0.5f;

        float camY = maxWorldY + 100f;
        captureCamera.transform.position = new Vector3(worldCenter.x, camY, worldCenter.y);
        captureCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        if (useHeightCheck && player != null)
        {
            float cutY = player.position.y + ceilingOffset;
            captureCamera.nearClipPlane = Mathf.Max(0.1f, camY - cutY);
        }
        else
        {
            captureCamera.nearClipPlane = 0.1f;
        }

        captureCamera.farClipPlane = camY + 200f;
        captureCamera.Render();

        if (mapBackgroundImage != null)
        {
            mapBackgroundImage.texture = mapRenderTexture;
        }
    }

    /// <summary>
    /// Fog 텍스처 초기화 및 RawImage 바인딩
    /// </summary>
    public void InitializeFog()
    {
        int totalPixels = fogResolution * fogResolution;
        if (fogColors == null || fogColors.Length != totalPixels)
            fogColors = new Color32[totalPixels];

        Color32 initialFog = new Color32(0, 0, 0, 255);
        for (int i = 0; i < totalPixels; i++)
            fogColors[i] = initialFog;

        if (fogTexture == null)
        {
            fogTexture = new Texture2D(fogResolution, fogResolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        fogTexture.SetPixels32(fogColors);
        fogTexture.Apply(false);

        if (fogOverlayImage != null)
        {
            fogOverlayImage.texture = fogTexture;
        }

        if (mapBackgroundImage != null)
        {
            if (fogMaskMaterial == null)
            {
                var shader = Shader.Find("UI/MinimapFogMask");
                if (shader != null)
                {
                    fogMaskMaterial = new Material(shader);
                }
            }

            if (fogMaskMaterial != null)
            {
                fogMaskMaterial.SetTexture("_FogTex", fogTexture);
                mapBackgroundImage.material = fogMaskMaterial;
            }
        }

        isInitialized = true;
        LoadFog();
    }

    /// <summary>
    /// 미니맵의 실제 표시 영역(RectTransform)을 결정
    /// </summary>
    private void ResolveDisplayContainer()
    {
        if (minimapDisplayRect != null) return;

        if (fogOverlayImage != null)
        {
            minimapDisplayRect = fogOverlayImage.rectTransform;
        }
        else if (mapBackgroundImage != null)
        {
            minimapDisplayRect = mapBackgroundImage.rectTransform;
        }
    }

    /// <summary>
    /// 씬에서 PlayerController를 탐색하여 Transform 바인딩
    /// </summary>
    private void TryFindPlayer()
    {
        if (player != null) return;

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;
            lastPlayerPos = player.position;
            UpdatePlayerIcon();
            RevealFog(player.position);
        }
    }

    private void UpdatePlayerIcon()
    {
        if (playerIcon == null || player == null) return;

        if (minimapDisplayRect == null)
        {
            ResolveDisplayContainer();
        }

        if (minimapDisplayRect == null) return;

        if (playerIcon.parent != minimapDisplayRect)
        {
            playerIcon.SetParent(minimapDisplayRect, false);
        }

        playerIcon.anchorMin = new Vector2(0.5f, 0.5f);
        playerIcon.anchorMax = new Vector2(0.5f, 0.5f);
        playerIcon.pivot = new Vector2(0.5f, 0.5f);

        Vector2 norm = WorldToNormalized(player.position);
        Vector2 size = minimapDisplayRect.rect.size;

        Vector2 localPos = new Vector2(
            (norm.x - 0.5f) * size.x,
            (norm.y - 0.5f) * size.y
        );

        playerIcon.anchoredPosition = localPos;

        float currentIconSize = autoScaleWithMap
            ? Mathf.Clamp(Mathf.Min(size.x, size.y) * iconMapRatio, minIconSize, maxIconSize)
            : iconBaseSize;

        playerIcon.sizeDelta = new Vector2(currentIconSize, currentIconSize);
        playerIcon.localScale = Vector3.one;

        float targetY = player.eulerAngles.y;
        if (trackCameraDirection && Camera.main != null)
        {
            targetY = Camera.main.transform.eulerAngles.y;
        }
        playerIcon.localEulerAngles = new Vector3(0f, 0f, -targetY);

        if (showDirectionPointer)
        {
            EnsureDirectionPointer(currentIconSize);
        }
        else if (directionPointer != null)
        {
            directionPointer.gameObject.SetActive(false);
        }
    }

    private void EnsureDirectionPointer(float iconSize)
    {
        if (directionPointer == null)
        {
            var child = playerIcon.Find("DirectionPointer");
            if (child != null)
            {
                directionPointer = child.GetComponent<RectTransform>();
            }
            else
            {
                var pointerObj = new GameObject("DirectionPointer");
                pointerObj.transform.SetParent(playerIcon, false);

                directionPointer = pointerObj.AddComponent<RectTransform>();
                directionPointer.anchorMin = new Vector2(0.5f, 0.5f);
                directionPointer.anchorMax = new Vector2(0.5f, 0.5f);
                directionPointer.pivot = new Vector2(0.5f, 0.5f);
                directionPointer.localEulerAngles = new Vector3(0f, 0f, 45f);

                var img = pointerObj.AddComponent<Image>();
                img.color = pointerColor;
                img.raycastTarget = false;
            }
        }

        directionPointer.gameObject.SetActive(true);
        float pointerSize = iconSize * 0.5f;
        directionPointer.sizeDelta = new Vector2(pointerSize, pointerSize);
        directionPointer.anchoredPosition = new Vector2(0f, iconSize * 0.45f);
    }

    public void RevealFog(Vector3 worldPos)
    {
        if (!isInitialized || worldSize.x <= 0f || worldSize.y <= 0f) return;

        Vector2 norm = WorldToNormalized(worldPos);
        int centerPx = Mathf.RoundToInt(norm.x * (fogResolution - 1));
        int centerPy = Mathf.RoundToInt(norm.y * (fogResolution - 1));

        float radiusInPixels = (revealRadius / worldSize.x) * fogResolution;
        int pxRadius = Mathf.CeilToInt(radiusInPixels);
        float radiusSqr = radiusInPixels * radiusInPixels;

        int minX = Mathf.Clamp(centerPx - pxRadius, 0, fogResolution - 1);
        int maxX = Mathf.Clamp(centerPx + pxRadius, 0, fogResolution - 1);
        int minY = Mathf.Clamp(centerPy - pxRadius, 0, fogResolution - 1);
        int maxY = Mathf.Clamp(centerPy + pxRadius, 0, fogResolution - 1);

        float innerPercent = Mathf.Clamp01(innerRevealPercent);
        float outerRange = Mathf.Max(0.001f, 1f - innerPercent);
        bool modified = false;

        for (int y = minY; y <= maxY; y++)
        {
            int rowOffset = y * fogResolution;
            float dy = y - centerPy;
            float dySqr = dy * dy;

            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - centerPx;
                float distSqr = dx * dx + dySqr;

                if (distSqr <= radiusSqr)
                {
                    int index = rowOffset + x;
                    float dist = Mathf.Sqrt(distSqr);
                    float normDist = dist / radiusInPixels;

                    byte targetAlpha = 0;
                    if (innerPercent < 0.999f && normDist > innerPercent)
                    {
                        float fadeFactor = (normDist - innerPercent) / outerRange;
                        targetAlpha = (byte)(fadeFactor * fadeFactor * 255f);
                    }

                    if (fogColors[index].a > targetAlpha)
                    {
                        fogColors[index].a = targetAlpha;
                        modified = true;
                    }
                }
            }
        }

        if (modified)
        {
            fogTexture.SetPixels32(fogColors);
            fogTexture.Apply(false);
        }
    }

    /// <summary>
    /// 월드 XZ 좌표를 0~1 정규화 좌표로 변환
    /// </summary>
    private Vector2 WorldToNormalized(Vector3 worldPos)
    {
        float x = (worldPos.x - (worldCenter.x - worldSize.x * 0.5f)) / worldSize.x;
        float y = (worldPos.z - (worldCenter.y - worldSize.y * 0.5f)) / worldSize.y;
        return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    /// <summary>
    /// 세이브/로드를 위한 직렬화 바이트 데이터 반환
    /// </summary>
    public byte[] GetFogSaveData()
    {
        if (fogColors == null) return null;
        byte[] alphaBytes = new byte[fogColors.Length];
        for (int i = 0; i < fogColors.Length; i++)
        {
            alphaBytes[i] = fogColors[i].a;
        }
        return alphaBytes;
    }

    /// <summary>
    /// 세이브 데이터로부터 Fog 마스크 복원
    /// </summary>
    public void LoadFogSaveData(byte[] alphaBytes)
    {
        if (alphaBytes == null || fogColors == null || alphaBytes.Length != fogColors.Length) return;

        for (int i = 0; i < fogColors.Length; i++)
        {
            fogColors[i].a = alphaBytes[i];
        }

        fogTexture.SetPixels32(fogColors);
        fogTexture.Apply(false);
    }

    public void SaveFog()
    {
        byte[] bytes = GetFogSaveData();
        if (bytes != null)
        {
            SaveManager.SaveMinimapFog(CurrentMapId, bytes);
        }
    }

    public void LoadFog()
    {
        byte[] bytes = SaveManager.LoadMinimapFog(CurrentMapId);
        if (bytes != null)
        {
            LoadFogSaveData(bytes);
        }
    }
}
