using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorldMapUI : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
{
    private static readonly int FogTexId = Shader.PropertyToID("_FogTex");
    private static readonly int UnexploredColorId = Shader.PropertyToID("_UnexploredColor");
    private static readonly int MainUvRectId = Shader.PropertyToID("_MainUvRect");

    [Header("UI Structure")]
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private RectTransform mapContentRect;
    [SerializeField] private RectTransform tileLayer;
    [SerializeField] private RectTransform overlayLayer;
    [SerializeField] private RawImage tileTemplate;
    [SerializeField] private MapMarkerUI markerTemplate;
    [SerializeField] private RectTransform playerIconRect;
    [SerializeField] private RectTransform playerDirectionPointer;

    [Header("Controls & Tooltip")]
    [SerializeField] private TMP_Text regionTitleText;
    [SerializeField] private Button prevRegionButton;
    [SerializeField] private Button nextRegionButton;
    [SerializeField] private Button recenterButton;
    [SerializeField] private RectTransform tooltipRoot;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Rendering")]
    [SerializeField] private Shader fogMaskShader;
    [SerializeField] private Color fogColor = new Color(0.05f, 0.06f, 0.09f, 0.94f);

    [Header("Navigation & Zoom")]
    [SerializeField] private float initialZoom = 1.15f;
    [SerializeField] private float minZoom = 0.7f;
    [SerializeField] private float maxZoom = 4.0f;
    [SerializeField] private float zoomSpeed = 0.25f;
    [SerializeField] private bool centerOnPlayerWhenOpened = true;

    [Header("Icons")]
    [SerializeField] private float playerIconSize = 28f;
    [SerializeField] private Color pointerColor = new Color(1f, 0.85f, 0.4f, 0.95f);
    [SerializeField] private bool showDirectionPointer = false;
    [SerializeField] private bool trackCameraDirection = false;

    private readonly List<RawImage> spawnedTiles = new();
    private readonly List<Material> spawnedMaterials = new();
    private readonly List<MapMarkerUI> spawnedMarkers = new();
    private readonly HashSet<string> usedMarkerIds = new();

    private Canvas rootCanvas;
    private MapRegionData viewingRegion;
    private float currentZoom = 1f;
    private Vector2 panOffset;
    private bool hasValidBindings;
    private bool pendingGeometryUpdate;
    private bool pendingCenterOnPlayer;

    private void Awake()
    {
        Object[] required =
        {
            viewportRect, mapContentRect, tileLayer, overlayLayer, tileTemplate, markerTemplate,
            playerIconRect, regionTitleText, prevRegionButton, nextRegionButton, recenterButton,
            tooltipRoot, tooltipText, fogMaskShader
        };
        foreach (Object obj in required)
        {
            if (obj != null) continue;
            Debug.LogError("[WorldMapUI] Required Inspector UI or Shader references are missing.", this);
            enabled = false;
            return;
        }

        hasValidBindings = true;
        rootCanvas = GetComponentInParent<Canvas>();
        tileTemplate.gameObject.SetActive(false);
        markerTemplate.gameObject.SetActive(false);
        tooltipRoot.gameObject.SetActive(false);

        if (playerDirectionPointer != null && playerDirectionPointer.TryGetComponent(out Image ptrImg))
            ptrImg.color = pointerColor;

        prevRegionButton.onClick.AddListener(() => CycleRegion(-1));
        nextRegionButton.onClick.AddListener(() => CycleRegion(1));
        recenterButton.onClick.AddListener(RecenterToActivePlayer);
    }

    private void OnDestroy()
    {
        if (prevRegionButton != null) prevRegionButton.onClick.RemoveAllListeners();
        if (nextRegionButton != null) nextRegionButton.onClick.RemoveAllListeners();
        if (recenterButton != null) recenterButton.onClick.RemoveAllListeners();
        ClearAllSpawned();
    }

    private void OnEnable()
    {
        if (!hasValidBindings) return;
        MinimapManager mgr = MinimapManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("[WorldMapUI] MinimapManager.Instance is null.", this);
            return;
        }

        mgr.OnActiveRegionChanged += HandleActiveRegionChanged;
        mgr.OnFogUpdated += HandleFogUpdated;
        mgr.OnMapStateReset += HandleMapStateReset;
        MapMarker.OnChanged += HandleMarkersChanged;

        Canvas.ForceUpdateCanvases();
        OpenRegion(mgr.ActiveRegion, centerOnPlayerWhenOpened);
    }

    private void OnDisable()
    {
        if (MinimapManager.Instance != null)
        {
            MinimapManager.Instance.OnActiveRegionChanged -= HandleActiveRegionChanged;
            MinimapManager.Instance.OnFogUpdated -= HandleFogUpdated;
            MinimapManager.Instance.OnMapStateReset -= HandleMapStateReset;
        }
        MapMarker.OnChanged -= HandleMarkersChanged;
        HideTooltip();
    }

    private void LateUpdate()
    {
        if (!hasValidBindings) return;
        if (pendingGeometryUpdate) TryApplyMapGeometry();
        if (!pendingGeometryUpdate && pendingCenterOnPlayer) CenterOnPlayer();
        UpdatePlayerIcon();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (hasValidBindings && isActiveAndEnabled && viewingRegion != null) TryApplyMapGeometry();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!hasValidBindings || viewingRegion == null || !HasValidViewport()) return;
        float delta = eventData.scrollDelta.y;
        if (Mathf.Abs(delta) < 0.01f) return;

        float nextZoom = Mathf.Clamp(currentZoom * (1f + delta * zoomSpeed), minZoom, maxZoom);
        if (Mathf.Approximately(currentZoom, nextZoom)) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, eventData.position, eventData.pressEventCamera, out Vector2 cursorLocal);
        panOffset = cursorLocal - (cursorLocal - panOffset) * (nextZoom / currentZoom);
        currentZoom = nextZoom;
        TryApplyMapGeometry();
    }

    public void OnBeginDrag(PointerEventData eventData) => HideTooltip();

    public void OnDrag(PointerEventData eventData)
    {
        if (!hasValidBindings || viewingRegion == null || !HasValidViewport()) return;
        panOffset += eventData.delta / rootCanvas.scaleFactor;
        ApplyPan();
    }

    private bool HasValidViewport() => viewportRect.rect.width > 1f && viewportRect.rect.height > 1f && rootCanvas != null && rootCanvas.scaleFactor > 0f;

    private void OpenRegion(MapRegionData region, bool center)
    {
        viewingRegion = region;
        currentZoom = Mathf.Clamp(initialZoom, minZoom, maxZoom);
        panOffset = Vector2.zero;
        pendingCenterOnPlayer = center;

        RebuildRegionView();
        if (center) CenterOnPlayer();
        else TryApplyMapGeometry();
    }

    private void RebuildRegionView()
    {
        ClearAllSpawned();
        HideTooltip();
        UpdateControlsState();

        MinimapManager mgr = MinimapManager.Instance;
        if (mgr == null || viewingRegion == null || !viewingRegion.HasValidTiles)
        {
            regionTitleText.text = viewingRegion != null ? DisplayTitle(viewingRegion) : string.Empty;
            if (viewingRegion != null && !viewingRegion.HasValidTiles)
                Debug.LogError($"[WorldMapUI] Region '{viewingRegion.mapId}' has invalid bounds or empty tiles.", viewingRegion);
            playerIconRect.gameObject.SetActive(false);
            return;
        }

        regionTitleText.text = DisplayTitle(viewingRegion);

        foreach (MapTileData tile in viewingRegion.tiles)
        {
            if (tile == null || !tile.IsValid || tile.mapTexture == null) continue;
            Texture2D fogTex = mgr.GetTileFogTexture(viewingRegion, tile);
            if (fogTex == null) continue;

            RawImage raw = Instantiate(tileTemplate, tileLayer);
            raw.name = $"Tile_{tile.tileId}";
            raw.gameObject.SetActive(true);
            raw.texture = tile.mapTexture;
            raw.color = Color.white;
            raw.raycastTarget = false;

            Material mat = new Material(fogMaskShader);
            mat.SetTexture(FogTexId, fogTex);
            mat.SetColor(UnexploredColorId, fogColor);
            mat.SetVector(MainUvRectId, new Vector4(tile.uvRect.x, tile.uvRect.y, tile.uvRect.width, tile.uvRect.height));
            raw.material = mat;

            RectTransform rt = raw.rectTransform;
            rt.anchorMin = viewingRegion.WorldToRegionNormalized(new Vector3(tile.WorldMin.x, 0f, tile.WorldMin.y));
            rt.anchorMax = viewingRegion.WorldToRegionNormalized(new Vector3(tile.WorldMax.x, 0f, tile.WorldMax.y));
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            spawnedTiles.Add(raw);
            spawnedMaterials.Add(mat);
        }

        RebuildMarkers();
        UpdatePlayerIcon();
    }

    private void RebuildMarkers()
    {
        ClearObjects(spawnedMarkers);
        usedMarkerIds.Clear();

        MinimapManager mgr = MinimapManager.Instance;
        if (mgr == null || mgr.DefaultStyleSet == null || viewingRegion == null || !viewingRegion.HasValidBounds) return;

        if (viewingRegion.staticMarkers != null)
            foreach (MapStaticMarkerData m in viewingRegion.staticMarkers)
                TrySpawnMarker(m, mgr);

        foreach (MapMarker live in MapMarker.Active)
            if (live != null && live.isActiveAndEnabled && viewingRegion.MatchesScene(live.SceneName))
                TrySpawnMarker(live.ToStaticData(), mgr);
    }

    private void TrySpawnMarker(MapStaticMarkerData data, MinimapManager mgr)
    {
        if (data == null || string.IsNullOrEmpty(data.markerId) || !usedMarkerIds.Add(data.markerId)) return;
        if (data.revealWithFog && !mgr.IsWorldPositionRevealed(viewingRegion, data.worldPosition)) return;
        if (!mgr.DefaultStyleSet.TryGetStyle(data.markerType, out MapMarkerStyleSet.Entry style)) return;

        MapMarkerUI ui = Instantiate(markerTemplate, overlayLayer);
        ui.name = $"Marker_{data.markerId}";
        ui.gameObject.SetActive(true);
        ui.Setup(
            data,
            data.customIcon != null ? data.customIcon : style.icon,
            data.useCustomColor ? data.customColor : style.color,
            style.size,
            viewingRegion.WorldToRegionNormalized(data.worldPosition),
            HandleMarkerClicked,
            HandleMarkerHover);

        spawnedMarkers.Add(ui);
    }

    private bool TryApplyMapGeometry()
    {
        if (viewingRegion == null || !viewingRegion.HasValidBounds) return false;

        Vector2 viewSize = viewportRect.rect.size;
        if (viewSize.x <= 1f || viewSize.y <= 1f)
        {
            pendingGeometryUpdate = true;
            return false;
        }

        pendingGeometryUpdate = false;
        float worldAspect = viewingRegion.worldSize.x / viewingRegion.worldSize.y;
        float viewAspect = viewSize.x / viewSize.y;
        Vector2 fitSize = worldAspect > viewAspect
            ? new Vector2(viewSize.x, viewSize.x / worldAspect)
            : new Vector2(viewSize.y * worldAspect, viewSize.y);

        mapContentRect.sizeDelta = fitSize * currentZoom;
        ApplyPan();
        return true;
    }

    private void ApplyPan()
    {
        Vector2 viewSize = viewportRect.rect.size;
        Vector2 contentSize = mapContentRect.sizeDelta;
        float maxX = Mathf.Max(0f, (contentSize.x - viewSize.x) * 0.5f + viewSize.x * 0.15f);
        float maxY = Mathf.Max(0f, (contentSize.y - viewSize.y) * 0.5f + viewSize.y * 0.15f);
        panOffset.x = Mathf.Clamp(panOffset.x, -maxX, maxX);
        panOffset.y = Mathf.Clamp(panOffset.y, -maxY, maxY);
        mapContentRect.anchoredPosition = panOffset;
    }

    private void CenterOnPlayer()
    {
        if (!TryApplyMapGeometry()) return;
        pendingCenterOnPlayer = false;

        MinimapManager mgr = MinimapManager.Instance;
        if (mgr == null || mgr.PlayerTransform == null || viewingRegion != mgr.ActiveRegion || !viewingRegion.HasValidBounds)
        {
            panOffset = Vector2.zero;
            ApplyPan();
            return;
        }

        Vector2 norm = viewingRegion.WorldToRegionNormalized(mgr.PlayerTransform.position);
        Vector2 size = mapContentRect.sizeDelta;
        panOffset = -new Vector2((norm.x - 0.5f) * size.x, (norm.y - 0.5f) * size.y);
        ApplyPan();
    }

    private void UpdatePlayerIcon()
    {
        MinimapManager mgr = MinimapManager.Instance;
        bool visible = mgr != null && mgr.PlayerTransform != null && viewingRegion != null && viewingRegion == mgr.ActiveRegion && viewingRegion.HasValidBounds;
        if (playerIconRect.gameObject.activeSelf != visible) playerIconRect.gameObject.SetActive(visible);
        if (!visible) return;

        Vector2 norm = viewingRegion.WorldToRegionNormalized(mgr.PlayerTransform.position);
        float yaw = trackCameraDirection && Camera.main != null ? Camera.main.transform.eulerAngles.y : mgr.PlayerTransform.eulerAngles.y;

        playerIconRect.anchorMin = playerIconRect.anchorMax = norm;
        playerIconRect.anchoredPosition = Vector2.zero;
        playerIconRect.sizeDelta = Vector2.one * playerIconSize;
        playerIconRect.localEulerAngles = new Vector3(0f, 0f, -yaw);
        if (playerDirectionPointer != null) playerDirectionPointer.gameObject.SetActive(showDirectionPointer);
    }

    private void UpdateControlsState()
    {
        MinimapManager mgr = MinimapManager.Instance;
        List<MapRegionData> discovered = mgr?.GetDiscoveredRegions();
        bool hasMultiple = discovered != null && discovered.Count > 1;
        prevRegionButton.interactable = hasMultiple;
        nextRegionButton.interactable = hasMultiple;
        recenterButton.interactable = mgr != null && mgr.ActiveRegion != null;
    }

    private void CycleRegion(int step)
    {
        List<MapRegionData> discovered = MinimapManager.Instance?.GetDiscoveredRegions();
        if (discovered == null || discovered.Count <= 1) return;
        int index = Mathf.Max(0, discovered.IndexOf(viewingRegion));
        OpenRegion(discovered[(index + step + discovered.Count) % discovered.Count], false);
    }

    private void RecenterToActivePlayer()
    {
        MinimapManager mgr = MinimapManager.Instance;
        if (mgr == null || mgr.ActiveRegion == null) return;
        if (viewingRegion != mgr.ActiveRegion) OpenRegion(mgr.ActiveRegion, true);
        else { currentZoom = Mathf.Clamp(initialZoom, minZoom, maxZoom); CenterOnPlayer(); }
    }

    private void HandleMarkerClicked(MapMarkerUI marker)
    {
        if (marker?.Data == null || string.IsNullOrEmpty(marker.Data.targetMapId)) return;
        MinimapManager mgr = MinimapManager.Instance;
        MapRegionData dest = mgr?.FindRegionById(marker.Data.targetMapId);
        if (dest != null && (dest == mgr.ActiveRegion || SaveManager.IsMapRegionDiscovered(dest.mapId)))
            OpenRegion(dest, false);
    }

    private void HandleMarkerHover(MapMarkerUI marker, bool enter)
    {
        if (!enter || marker?.Data == null || string.IsNullOrEmpty(marker.Data.label)) { HideTooltip(); return; }
        tooltipText.text = marker.Data.label;
        tooltipRoot.gameObject.SetActive(true);
        if (viewportRect != null && marker.transform is RectTransform markerRect)
            tooltipRoot.anchoredPosition = (Vector2)viewportRect.InverseTransformPoint(markerRect.TransformPoint(markerRect.rect.center)) + new Vector2(0f, 26f);
    }

    private void HideTooltip() { if (tooltipRoot != null && tooltipRoot.gameObject.activeSelf) tooltipRoot.gameObject.SetActive(false); }
    private void HandleFogUpdated(string mapId) { if (viewingRegion != null && viewingRegion.mapId == mapId) RebuildMarkers(); }
    private void HandleMapStateReset() => OpenRegion(MinimapManager.Instance?.ActiveRegion, false);
    private void HandleMarkersChanged() { if (viewingRegion != null) RebuildMarkers(); }

    private void HandleActiveRegionChanged(MapRegionData active)
    {
        if (viewingRegion == null || viewingRegion == active) OpenRegion(active, centerOnPlayerWhenOpened);
        else UpdateControlsState();
    }

    private void ClearAllSpawned()
    {
        ClearObjects(spawnedTiles);
        ClearObjects(spawnedMarkers);
        foreach (Material mat in spawnedMaterials) if (mat != null) Destroy(mat);
        spawnedMaterials.Clear();
    }

    private static void ClearObjects<T>(List<T> list) where T : Component
    {
        foreach (T item in list) if (item != null) Destroy(item.gameObject);
        list.Clear();
    }

    private static string DisplayTitle(MapRegionData r) => string.IsNullOrEmpty(r.displayName) ? r.mapId : r.displayName;
}
