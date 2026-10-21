using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SeamlessWormhole
{
    /// <summary>
    /// 양방향 웜홀의 한쪽 입구입니다.
    ///
    /// 연결된 입구 주변을 Cubemap으로 렌더링해 원형 렌즈 안에 표시합니다.
    /// 이동과 렌더링은 같은 좌표 변환을 사용하므로 통과 위치와 보이는 공간이 일치합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SphericalWormholePortal : MonoBehaviour
    {
        private static readonly List<SphericalWormholePortal> activePortals = new List<SphericalWormholePortal>();
        private static readonly Quaternion HalfTurn = Quaternion.Euler(0f, 180f, 0f);
        private static bool isCapturingCubemap;

        [Header("Student setup")]
        [Tooltip("통과한 뒤 도착할 상대 웜홀입니다. 렌즈에 보이는 공간과 통과 목적지를 함께 결정합니다.")]
        [SerializeField] private SphericalWormholePortal destinationPortal;

        [Header("Internal render helpers")]
        [SerializeField] private Renderer lensRenderer;
        [SerializeField] private Material lensMaterial;
        [SerializeField] private Camera captureCamera;

        [Header("Traversal")]
        [Tooltip("중심에서 통과 가능한 보이지 않는 원판의 반지름입니다.")]
        [SerializeField, Min(0.1f)] private float traversalRadius = 1.78f;

        [Tooltip("낮은 프레임에서 출구로 즉시 되돌아오는 현상을 막는 작은 여유 거리입니다.")]
        [SerializeField, Min(0f)] private float exitClearance = 0.06f;

        [Header("Performance")]
        [Tooltip("한 번 캡처할 때 여섯 면을 렌더링합니다. 384는 선명도와 성능의 균형값입니다.")]
        [SerializeField, Range(64, 1024)] private int cubemapResolution = 384;

        private RenderTexture linkedWorldCubemap;
        /// <summary>Inspector 또는 LinkTo로 연결한 상대 입구입니다.</summary>
        public SphericalWormholePortal LinkedPortal => destinationPortal;

        /// <summary>이동 컴포넌트가 통과 판정에 사용하는 활성 입구 목록입니다.</summary>
        public static IReadOnlyList<SphericalWormholePortal> ActivePortals => activePortals;

        private void OnEnable()
        {
            if (!activePortals.Contains(this)) activePortals.Add(this);
            RenderPipelineManager.beginCameraRendering += RenderLinkedWorld;
        }

        private void OnDisable()
        {
            activePortals.Remove(this);
            RenderPipelineManager.beginCameraRendering -= RenderLinkedWorld;
        }

        private void OnDestroy()
        {
            ReleaseCubemap();
        }

        /// <summary>이 입구를 상대 입구에 연결합니다.</summary>
        public void LinkTo(SphericalWormholePortal other)
        {
            destinationPortal = other;
        }

        /// <summary>
        /// Inspector에서 호출하기 쉬운 연결 함수입니다.
        /// </summary>
        public void SetDestination(SphericalWormholePortal other)
        {
            LinkTo(other);
        }

        /// <summary>반 바퀴 회전을 적용해 월드 위치를 상대 입구로 변환합니다.</summary>
        public Vector3 MapPosition(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            localPosition = HalfTurn * localPosition;
            return LinkedPortal.transform.TransformPoint(localPosition);
        }

        /// <summary>위치 변환과 같은 방식으로 방향을 변환합니다.</summary>
        public Quaternion MapRotation(Quaternion worldRotation)
        {
            Quaternion localRotation = Quaternion.Inverse(transform.rotation) * worldRotation;
            return LinkedPortal.transform.rotation * HalfTurn * localRotation;
        }

        /// <summary>
        /// 중심 원판을 가로질렀는지 검사합니다. Physics Trigger를 쓰지 않아 빠르게 움직여도 통과를 놓치지 않습니다.
        /// </summary>
        public bool TryMapCrossing(
            Vector3 previousWorldPosition,
            Vector3 currentWorldPosition,
            Quaternion currentWorldRotation,
            out Vector3 mappedPosition,
            out Quaternion mappedRotation)
        {
            mappedPosition = currentWorldPosition;
            mappedRotation = currentWorldRotation;
            if (LinkedPortal == null) return false;

            Vector3 previousLocal = transform.InverseTransformPoint(previousWorldPosition);
            Vector3 currentLocal = transform.InverseTransformPoint(currentWorldPosition);
            float zDelta = currentLocal.z - previousLocal.z;
            bool crossesPlane = (previousLocal.z < 0f && currentLocal.z >= 0f)
                             || (previousLocal.z > 0f && currentLocal.z <= 0f);
            if (!crossesPlane || Mathf.Abs(zDelta) < 0.00001f) return false;

            float crossingT = Mathf.Clamp01(-previousLocal.z / zDelta);
            Vector3 pointOnPlane = Vector3.Lerp(previousLocal, currentLocal, crossingT);
            if (new Vector2(pointOnPlane.x, pointOnPlane.y).sqrMagnitude > traversalRadius * traversalRadius)
                return false;

            // 중심점만 옮기지 않고 이번 프레임의 나머지 이동량도 출구 쪽에 보존합니다.
            Vector3 mappedLocalPosition = HalfTurn * currentLocal;
            if (Mathf.Abs(mappedLocalPosition.z) < exitClearance)
            {
                float exitSide = mappedLocalPosition.z == 0f
                    ? -Mathf.Sign(currentLocal.z)
                    : Mathf.Sign(mappedLocalPosition.z);
                mappedLocalPosition.z = exitSide * exitClearance;
            }

            mappedPosition = LinkedPortal.transform.TransformPoint(mappedLocalPosition);
            mappedRotation = MapRotation(currentWorldRotation);
            return true;
        }

        private void RenderLinkedWorld(ScriptableRenderContext context, Camera viewer)
        {
            // Game 카메라만 처리합니다. Cubemap 렌더링 중 다시 호출되는 재귀를 막습니다.
            if (isCapturingCubemap || viewer != Camera.main || LinkedPortal == null
                || captureCamera == null || lensMaterial == null)
                return;

            EnsureCubemap();

            // Cubemap 축은 월드 공간에 고정합니다. 카메라는 변환된 위치만 받고 방향은 유지합니다.
            captureCamera.transform.SetPositionAndRotation(MapPosition(viewer.transform.position), Quaternion.identity);
            captureCamera.nearClipPlane = viewer.nearClipPlane;
            captureCamera.farClipPlane = viewer.farClipPlane;
            captureCamera.cullingMask = ~0;

            isCapturingCubemap = true;
            var temporarilyHiddenLenses = new List<Renderer>();
            try
            {
                // 예전 레이어 분리를 대체합니다. 일반 오브젝트는 모두 캡처하되 렌즈는 숨겨 자기 화면을 다시 담지 않습니다.
                foreach (SphericalWormholePortal portal in activePortals)
                {
                    if (portal != null && portal.lensRenderer != null && portal.lensRenderer.enabled)
                    {
                        portal.lensRenderer.enabled = false;
                        temporarilyHiddenLenses.Add(portal.lensRenderer);
                    }
                }
                captureCamera.RenderToCubemap(linkedWorldCubemap);
            }
            finally
            {
                foreach (Renderer renderer in temporarilyHiddenLenses)
                    if (renderer != null) renderer.enabled = true;
                isCapturingCubemap = false;
            }

            lensMaterial.SetTexture("_TargetCube", linkedWorldCubemap);
            lensMaterial.SetMatrix("_PortalRayMap", CreateRayMappingMatrix());
        }

        private Matrix4x4 CreateRayMappingMatrix()
        {
            return LinkedPortal.transform.localToWorldMatrix
                   * Matrix4x4.Rotate(HalfTurn)
                   * transform.worldToLocalMatrix;
        }

        private void EnsureCubemap()
        {
            if (linkedWorldCubemap != null && linkedWorldCubemap.width == cubemapResolution) return;

            ReleaseCubemap();
            linkedWorldCubemap = new RenderTexture(cubemapResolution, cubemapResolution, 24, RenderTextureFormat.DefaultHDR)
            {
                name = name + " Linked World Cubemap",
                dimension = TextureDimension.Cube,
                useMipMap = true,
                autoGenerateMips = true
            };
            linkedWorldCubemap.Create();
        }

        private void ReleaseCubemap()
        {
            if (linkedWorldCubemap == null) return;
            linkedWorldCubemap.Release();
            Destroy(linkedWorldCubemap);
            linkedWorldCubemap = null;
        }
    }
}
