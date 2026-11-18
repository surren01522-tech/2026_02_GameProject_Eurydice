using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class InfiniteGrassRenderer : MonoBehaviour
{
    [HideInInspector] public static InfiniteGrassRenderer instance;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool renderInSceneView = true;

    [Header("Internal")]
    public Material grassMaterial;
    public ComputeBuffer argsBuffer;
    public ComputeBuffer tBuffer;

    [Header("Grass Properties")]
    public float spacing = 0.5f;
    public float drawDistance = 300f;
    public float fullDensityDistance = 50f;
    public int grassMeshSubdivision = 5;
    public float textureUpdateThreshold = 10.0f;

    [Header("Interaction & Local Slope")]
    public float slopeVerticalTolerance = 0.75f;
    public float localSlopeRadius = 35f;

    [Header("Max Buffer Count (Millions)")]
    public float maxBufferCount = 2f;

    [Header("Debug (Enabling this will make the performance drop a lot)")]
    public bool previewVisibleGrassCount = false;

    private Mesh cachedGrassMesh;
    private int oldSubdivision = -1;
    private readonly uint[] argsData = new uint[5];
    private readonly Vector3[] frustumCorners = new Vector3[8];

    public Camera TargetCamera => targetCamera != null ? targetCamera : Camera.main;

    private void OnEnable()
    {
        instance = this;
        GrassDataRendererFeature.MarkRenderersDirty();
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        if (instance == this)
            instance = null;

        ReleaseBuffers();

        if (cachedGrassMesh != null)
        {
            if (Application.isPlaying)
                Destroy(cachedGrassMesh);
            else
                DestroyImmediate(cachedGrassMesh);

            cachedGrassMesh = null;
            oldSubdivision = -1;
        }
    }

    public bool ShouldRenderCamera(Camera cam, CameraType cameraType)
    {
        if (cam == null || PortalManager.IsPortalCamera(cam))
            return false;

        if (cameraType == CameraType.SceneView)
            return renderInSceneView;

        if (cameraType != CameraType.Game)
            return false;

        Camera active = TargetCamera;
        return active != null && cam == active;
    }

    public void EnsureBuffers()
    {
        Mesh mesh = GetGrassMeshCache();
        bool needArgsInit = argsBuffer == null;

        if (argsBuffer == null)
            argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        if (previewVisibleGrassCount && tBuffer == null)
            tBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);
        else if (!previewVisibleGrassCount && tBuffer != null)
        {
            tBuffer.Release();
            tBuffer = null;
        }

        uint indexCount = (uint)mesh.GetIndexCount(0);
        uint indexStart = (uint)mesh.GetIndexStart(0);
        uint baseVertex = (uint)mesh.GetBaseVertex(0);

        if (needArgsInit || argsData[0] != indexCount || argsData[2] != indexStart || argsData[3] != baseVertex)
        {
            argsData[0] = indexCount;
            argsData[1] = 0;
            argsData[2] = indexStart;
            argsData[3] = baseVertex;
            argsData[4] = 0;
            argsBuffer.SetData(argsData);
        }
    }

    private void ReleaseBuffers()
    {
        argsBuffer?.Release();
        argsBuffer = null;
        tBuffer?.Release();
        tBuffer = null;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (spacing <= 0f || grassMaterial == null)
            return;

        if (!ShouldRenderCamera(cam, cam.cameraType))
            return;

        EnsureBuffers();

        Bounds cameraBounds = CalculateCameraBounds(cam, drawDistance);
        cameraBounds.Expand(4f);

        Graphics.DrawMeshInstancedIndirect(
            GetGrassMeshCache(),
            0,
            grassMaterial,
            cameraBounds,
            argsBuffer,
            0,
            null,
            ShadowCastingMode.Off,
            true,
            gameObject.layer,
            cam
        );
    }

    private void OnGUI()
    {
        if (!previewVisibleGrassCount || tBuffer == null)
            return;

        Camera cam = TargetCamera;
        if (cam == null)
            return;

        GUI.contentColor = Color.black;
        GUIStyle style = new GUIStyle { fontSize = 25 };

        uint[] count = new uint[1];
        tBuffer.GetData(count);

        Bounds cameraBounds = CalculateCameraBounds(cam, drawDistance);
        Vector2Int gridSize = new Vector2Int(
            Mathf.CeilToInt(cameraBounds.size.x / spacing),
            Mathf.CeilToInt(cameraBounds.size.z / spacing)
        );

        GUI.Label(new Rect(50, 50, 400, 200), "Dispatch Size : " + gridSize.x + "x" + gridSize.y + " = " + (gridSize.x * gridSize.y), style);
        GUI.Label(new Rect(50, 80, 400, 200), "Visible Grass Count : " + count[0], style);
    }

    public Mesh GetGrassMeshCache()
    {
        int sub = Mathf.Max(0, grassMeshSubdivision);
        if (cachedGrassMesh == null || oldSubdivision != sub)
        {
            if (cachedGrassMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(cachedGrassMesh);
                else
                    DestroyImmediate(cachedGrassMesh);
            }

            cachedGrassMesh = new Mesh { name = "InfiniteGrassBladeMesh" };

            Vector3[] vertices = new Vector3[3 + 4 * sub];
            int[] triangles = new int[(1 + 2 * sub) * 3];

            for (int i = 0; i < sub; i++)
            {
                float y1 = (float)i / (sub + 1);
                float y2 = (float)(i + 1) / (sub + 1);

                int bottomLeftIndex = i * 4;
                int bottomRightIndex = i * 4 + 1;
                int topLeftIndex = i * 4 + 2;
                int topRightIndex = i * 4 + 3;

                vertices[bottomLeftIndex] = new Vector3(-0.25f, y1);
                vertices[bottomRightIndex] = new Vector3(0.25f, y1);
                vertices[topLeftIndex] = new Vector3(-0.25f, y2);
                vertices[topRightIndex] = new Vector3(0.25f, y2);

                triangles[i * 6] = bottomLeftIndex;
                triangles[i * 6 + 1] = topRightIndex;
                triangles[i * 6 + 2] = bottomRightIndex;

                triangles[i * 6 + 3] = bottomLeftIndex;
                triangles[i * 6 + 4] = topLeftIndex;
                triangles[i * 6 + 5] = topRightIndex;
            }

            vertices[sub * 4] = new Vector3(-0.25f, (float)sub / (sub + 1));
            vertices[sub * 4 + 1] = new Vector3(0f, 1f);
            vertices[sub * 4 + 2] = new Vector3(0.25f, (float)sub / (sub + 1));

            triangles[sub * 6] = sub * 4;
            triangles[sub * 6 + 1] = sub * 4 + 1;
            triangles[sub * 6 + 2] = sub * 4 + 2;

            cachedGrassMesh.SetVertices(vertices);
            cachedGrassMesh.SetTriangles(triangles, 0);
            cachedGrassMesh.RecalculateBounds();

            oldSubdivision = sub;
        }

        return cachedGrassMesh;
    }

    public Bounds CalculateCameraBounds(Camera camera, float distance)
    {
        float near = camera.nearClipPlane;
        frustumCorners[0] = camera.ViewportToWorldPoint(new Vector3(0f, 1f, near));
        frustumCorners[1] = camera.ViewportToWorldPoint(new Vector3(1f, 1f, near));
        frustumCorners[2] = camera.ViewportToWorldPoint(new Vector3(0f, 0f, near));
        frustumCorners[3] = camera.ViewportToWorldPoint(new Vector3(1f, 0f, near));
        frustumCorners[4] = camera.ViewportToWorldPoint(new Vector3(0f, 1f, distance));
        frustumCorners[5] = camera.ViewportToWorldPoint(new Vector3(1f, 1f, distance));
        frustumCorners[6] = camera.ViewportToWorldPoint(new Vector3(0f, 0f, distance));
        frustumCorners[7] = camera.ViewportToWorldPoint(new Vector3(1f, 0f, distance));

        Vector3 min = frustumCorners[0];
        Vector3 max = frustumCorners[0];
        for (int i = 1; i < 8; i++)
        {
            min = Vector3.Min(min, frustumCorners[i]);
            max = Vector3.Max(max, frustumCorners[i]);
        }

        Bounds bounds = new Bounds((min + max) * 0.5f, max - min);
        bounds.Expand(1f);
        return bounds;
    }
}
