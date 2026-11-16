using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The volumetric fog renderer feature.
/// </summary>
[Tooltip("Adds support to render volumetric fog.")]
[DisallowMultipleRendererFeature("Volumetric Fog")]
public sealed class VolumetricFogRendererFeature : ScriptableRendererFeature
{
	private static readonly System.Collections.Generic.HashSet<int> ForcedFogCameraIds = new();
	internal static event System.Action<int> OnFogCameraUnregistered;
	public static event System.Action<ScriptableRenderer, Camera> OnAddCameraPasses;

	public static bool IsForcedFogCamera(Camera cam)
	{
		return cam != null && ForcedFogCameraIds.Contains(cam.GetInstanceID());
	}

	public static void RegisterFogCamera(Camera cam)
	{
		if (cam != null) ForcedFogCameraIds.Add(cam.GetInstanceID());
	}

	public static void UnregisterFogCamera(Camera cam)
	{
		if (cam == null) return;
		int id = cam.GetInstanceID();
		if (ForcedFogCameraIds.Remove(id))
			OnFogCameraUnregistered?.Invoke(id);
	}

	#region Private Attributes

	[HideInInspector]
	[SerializeField] private Shader downsampleDepthShader;
	[HideInInspector]
	[SerializeField] private Shader volumetricFogShader;

	[Header("Portal Optimization")]
	[SerializeField, Range(4, 128)] private int portalMaxSteps = 36;
	[SerializeField, Range(0, 4)] private int portalBlurIterations = 1;
	[SerializeField] private bool portalDisableAdditionalLights = true;

	private Material downsampleDepthMaterial;
	private Material volumetricFogMaterial;
	private Material portalVolumetricFogMaterial;

	private VolumetricFogRenderPass volumetricFogRenderPass;

	#endregion

	#region Scriptable Renderer Feature Methods

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public override void Create()
	{
#if !UNITY_6000_4_OR_NEWER
		volumetricFogRenderPass?.Dispose();
#endif
		ValidateResourcesForVolumetricFogRenderPass(true);

		volumetricFogRenderPass = new VolumetricFogRenderPass(downsampleDepthMaterial, volumetricFogMaterial, portalVolumetricFogMaterial, VolumetricFogRenderPass.DefaultRenderPassEvent);
		volumetricFogRenderPass.ConfigurePortalQuality(portalMaxSteps, portalBlurIterations, portalDisableAdditionalLights);
	}

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	/// <param name="renderer"></param>
	/// <param name="renderingData"></param>
	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		Camera cam = renderingData.cameraData.camera;
		OnAddCameraPasses?.Invoke(renderer, cam);
		bool isForcedCamera = cam != null && ForcedFogCameraIds.Contains(cam.GetInstanceID());
		bool isPostProcessEnabled = renderingData.postProcessingEnabled && (renderingData.cameraData.postProcessEnabled || isForcedCamera);
		bool shouldAddVolumetricFogRenderPass = isPostProcessEnabled && ShouldAddVolumetricFogRenderPass(renderingData.cameraData.cameraType);

		if (shouldAddVolumetricFogRenderPass)
		{
			volumetricFogRenderPass.ConfigurePortalQuality(portalMaxSteps, portalBlurIterations, portalDisableAdditionalLights);
			volumetricFogRenderPass.renderPassEvent = GetRenderPassEvent();
			volumetricFogRenderPass.ConfigureInput(ScriptableRenderPassInput.Depth);
			renderer.EnqueuePass(volumetricFogRenderPass);
		}
	}

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	/// <param name="disposing"></param>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

#if !UNITY_6000_4_OR_NEWER
		volumetricFogRenderPass?.Dispose();
#endif
		CoreUtils.Destroy(downsampleDepthMaterial);
		CoreUtils.Destroy(volumetricFogMaterial);
		CoreUtils.Destroy(portalVolumetricFogMaterial);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Validates the resources used by the volumetric fog render pass.
	/// </summary>
	/// <param name="forceRefresh"></param>
	/// <returns></returns>
	private bool ValidateResourcesForVolumetricFogRenderPass(bool forceRefresh)
	{
		if (forceRefresh)
		{
#if UNITY_EDITOR
			downsampleDepthShader = Shader.Find("Hidden/DownsampleDepth");
			volumetricFogShader = Shader.Find("Hidden/VolumetricFog");
#endif
			CoreUtils.Destroy(downsampleDepthMaterial);
			downsampleDepthMaterial = CoreUtils.CreateEngineMaterial(downsampleDepthShader);

			CoreUtils.Destroy(volumetricFogMaterial);
			volumetricFogMaterial = CoreUtils.CreateEngineMaterial(volumetricFogShader);

			CoreUtils.Destroy(portalVolumetricFogMaterial);
			portalVolumetricFogMaterial = CoreUtils.CreateEngineMaterial(volumetricFogShader);
		}

		bool okDepth = downsampleDepthShader != null && downsampleDepthMaterial != null;
		bool okVolumetric = volumetricFogShader != null && volumetricFogMaterial != null && portalVolumetricFogMaterial != null;

		return okDepth && okVolumetric;
	}

	/// <summary>
	/// Gets whether the volumetric fog render pass should be enqueued to the renderer.
	/// </summary>
	/// <param name="cameraType"></param>
	/// <returns></returns>
	private bool ShouldAddVolumetricFogRenderPass(CameraType cameraType)
	{
		VolumetricFogVolumeComponent fogVolume = VolumeManager.instance.stack.GetComponent<VolumetricFogVolumeComponent>();

		bool isVolumeOk = fogVolume != null && fogVolume.IsActive();
		bool isCameraOk = cameraType != CameraType.Preview && cameraType != CameraType.Reflection;
		bool areResourcesOk = ValidateResourcesForVolumetricFogRenderPass(false);

		return isActive && isVolumeOk && isCameraOk && areResourcesOk;
	}

	/// <summary>
	/// Returns the render pass event for the volumetric fog.
	/// </summary>
	/// <returns></returns>
	private RenderPassEvent GetRenderPassEvent()
	{
		VolumetricFogVolumeComponent fogVolume = VolumeManager.instance.stack.GetComponent<VolumetricFogVolumeComponent>();

		return (RenderPassEvent)fogVolume.renderPassEvent.value;
	}

	#endregion
}
