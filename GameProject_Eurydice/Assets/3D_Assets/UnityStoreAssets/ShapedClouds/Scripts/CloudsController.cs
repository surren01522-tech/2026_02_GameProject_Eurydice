using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Random = UnityEngine.Random;

using ShapedClouds.CloudRenderers;
using ShapedClouds.Settings;


namespace ShapedClouds {

	/// <summary>
	/// Component responsible for managing the entire process of rendering clouds.
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("Shaped Clouds/Clouds Controller")]
	public class CloudsController: MonoBehaviour {
		/// <summary>
		/// Holds the <see cref="CloudRenderer">renderers</see> in the scene.
		/// List is automatically managed based on direct children in hierarchy.
		/// </summary>
		[NonSerialized]
		public List<CloudRenderer> renderers = new();
		/// <summary>
		/// When true, the list of renderers should be updated.
		/// </summary>
		private bool dirty = false;

		/// <summary>
		/// When enabled, timescale is set to 0 in the editor. Useful to set a specific starting position when the scene starts
		/// </summary>
		[Tooltip("When enabled, timescale is set to 0 in the editor. Useful to set a specific starting position when the scene starts.")]
		public bool pauseInEditor = false;


		/// <summary>
		/// The visual height of the clouds in world space, affects how they move with wind and perspective,
		/// but doesn't affect how big generated noise appears to be.
		/// </summary>
		///
		/// <remarks>
		/// Clouds with greater height are rendered earlier, appearing to be higher.
		/// This behaviour can be overridden by changing the <see cref="order">order</see> field.
		/// </remarks>
		[Header("Appearance")]
		[Tooltip("The visual height of the clouds in world space, affects how they move with wind and perspective, "
		+ "but doesn't affect how big generated noise appears to be."
		+ "\n\nClouds with greater height are rendered earlier, appearing to be higher. "
		+ "This behaviour can be overridden by changing the Order field.")]
		public float height = 2000f;

		/// <summary>
		/// The curvature of the clouds, 0 being flat.
		/// </summary>
		///
		/// <remarks>
		/// A high curvature will heavily deform cloud shape and trajectory, as well as make clouds seem considerably larger.
		/// Takes a hemispheric shape, making distant clouds appear less flat.
		/// </remarks>
		[Tooltip("The curvature of the clouds, 0 being flat."
		+ "\n\nA high curvature will heavily deform cloud shape and trajectory, as well as make clouds seem considerably larger. "
		+ "Takes a hemispheric shape, making distant clouds appear less flat.")]
		[Range(0f, 1f)]
		public float curvature = 1f;

		/// <summary>
		/// How deep, in world space distance (meters), the clouds appear to be when overlapping opaque objects.
		/// </summary>
		///
		/// <remarks>
		/// 0 makes them flat, higher numbers will fake depth by using the density of the cloud at that fragment.
		/// Does nothing if <see cref="renderAboveOpaques">renderAboveOpaques</see> is false.
		/// </remarks>
		[Tooltip("How deep, in world space distance (meters), the clouds appear to be when overlapping opaque objects."
		+ "\n\n0 makes them flat, higher numbers will fake depth by using the density of the cloud at that fragment. "
		+ "Does nothing if Render Above Opaques is disabled.")]
		public float depth = 500f;

		/// <summary>
		/// The field of view to use for orthographic cameras.
		/// </summary>
		[Tooltip("The field of view to use for orthographic cameras.")]
		[Range(0, 180)]
		public float orthographicFOV = 60;

		/// <summary>
		/// How quickly all <see cref="CloudRenderer">cloud renderers</see> update.
		/// </summary>
		[Tooltip("How quickly all cloud renderers update.")]
		public float timeScale = 1f;


		/// <summary>
		/// Used to blur the base texture.
		/// </summary>
		[Tooltip("Used to blur the base texture.")]
		public CloudBlur baseBlur = new();

		/// <summary>
		/// Used to blur the final texture.
		/// </summary>
		[Tooltip("Used to blur the final texture.")]
		public CloudBlur finalBlur = new();

		/// <summary>
		/// Holds settings for how to render clouds to the final texture, using the base texture as input.
		/// </summary>
		[Tooltip("Holds settings for how to render clouds to the final texture, using the base texture as input.")]
		public CloudFilter filter = new();


		/// <summary>
		/// The resolution of the final texture, which is then drawn to the screen.
		/// </summary>
		///
		/// <remarks>
		/// Low detail and blurry noises (like the default one) will look fine with low resolutions.
		/// </remarks>
		[Header("Performance")]
		[Tooltip("The resolution of the final texture, which is then drawn to the screen. "
		+ "\n\nLow detail and blurry noises (like the default one) will look fine with low resolutions.")]
		public CloudTextureResolution finalResolution = CloudTextureResolution.Half;

		/// <summary>
		/// The resolution of the base texture, which is used to determine the cloud density at any particular pixel.
		/// </summary>
		///
		/// <remarks>
		/// Cannot be higher than the resolution of the final texture.
		/// Since details are only added in the final texture, this texture can be low resolution and blurry while the end result still looks sharp.
		/// </remarks>
		[Tooltip("The resolution of the base texture, which is used to determine the cloud density at any particular pixel."
		+ "\n\nCannot be higher than the resolution of the final texture. "
		+ "Since details are only added in the final texture, this texture can be low resolution and blurry while the end result still looks sharp.")]
		public CloudTextureResolution baseResolution = CloudTextureResolution.Quarter;

		/// <summary>
		/// The filtering mode of the cloud textures.
		/// </summary>
		///
		/// <remarks>
		/// Bilinear is usually the best choice - point will look pixelated.
		/// </remarks>
		[Tooltip("The filtering mode of the cloud textures."
		+ "\n\nBilinear is usually the best choice - point will look pixelated.")]
		public FilterMode filterMode = FilterMode.Bilinear;

		/// <summary>
		/// Enables dynamic scaling for textures.
		/// </summary>
		[Tooltip("Enables dynamic scaling for textures.")]
		public bool useDynamicScale = true;

		/// <summary>
		/// The texture format used by the final cloud texture, must have all 4 channels to work properly.
		/// </summary>
		///
		/// <remarks>
		/// The safest option is RenderTextureFormat.DefaultHDR, but lower accuracy can provide a meaningful performance boost or save VRAM.
		/// Recommended is RenderTextureFormat.ARGBHalf, if the platform supports it.
		/// </remarks>
		[Tooltip("The texture format used by the final cloud texture, must have all 4 channels to work properly."
		+ "\n\nThe safest option is Default HDR, but lower accuracy can provide a meaningful performance boost or save VRAM. "
		+ "Recommended is ARGB Half if the platform supports it.")]
		public RenderTextureFormat finalColorFormat = RenderTextureFormat.DefaultHDR;

		/// <summary>
		/// The texture format used by the base cloud texture, included shaders only use the first channel (Red).
		/// </summary>
		///
		/// <remarks>
		/// The safest option is RenderTextureFormat.DefaultHDR, but less channels/lower accuracy can provide a meaningful performance boost or save VRAM.
		/// Recommended is RenderTextureFormat.RHalf, if the platform supports it. <para />
		///
		/// RenderTextureFormat.R8 might be good enough, if doing very simple clouds.
		/// </remarks>
		[Tooltip("The texture format used by the base cloud texture, included shaders only use the first channel (Red)."
		+ "\n\nThe safest option is Default HDR, but less channels/lower accuracy can provide a meaningful performance boost or save VRAM. "
		+ "Recommended is R Half, if the platform supports it.")]
		public RenderTextureFormat baseColorFormat = RenderTextureFormat.DefaultHDR;


		/// <summary>
		/// The effect will ignore cameras in these layers, use to save performance or to selectively only have clouds act on certain cameras.
		/// </summary>
		///
		/// <remarks>
		/// Alternatively, change the layer of the GameObject this behaviour is attached to.
		/// </remarks>
		[Header("Advanced")]
		[Tooltip("The effect will ignore cameras in these layers, use to save performance or to selectively only have clouds act on certain cameras."
		+ "\n\nAlternatively, change the layer of the GameObject this behaviour is attached to.")]
		public LayerMask ignoreCameraLayers;

		/// <summary>
		/// The effect will ignore cameras of these types, use to save performance or to selectively only have clouds act on certain cameras.
		/// </summary>
		[Tooltip("The effect will ignore cameras of these types, use to save performance or to selectively only have clouds act on certain cameras.")]
		public CameraType ignoreCameraTypes = CameraType.Reflection | CameraType.Preview;

		/// <summary>
		/// The priority of the clouds rendered by this component, a higher number will render later and appear in front.
		/// </summary>
		///
		/// <remarks>
		/// When two controllers have the same priority, order is then decided by the smaller cloud height, which should be closer to the camera.
		/// If that's also the same, there are no guarantees for the order.
		/// </remarks>
		[Tooltip("The priority of the clouds rendered by this component, a higher number will render later and appear in front."
		+ "\n\nWhen two controllers have the same priority, order is then decided by the smaller cloud height, which should be closer to the camera. "
		+ "If that's also the same, there are no guarantees for the order.")]
		public int order = 0;

		/// <summary>
		/// The shader used to copy the cloud texture to the screen.
		/// </summary>
		[Tooltip("The shader used to copy the cloud texture to the screen.")]
		public Shader blitShader;

		/// <summary>
		/// Material created from the shader, this is done every time the shader changes.
		/// </summary>
		[NonSerialized]
		public Material blitMaterial;

		/// <summary>
		/// When enabled, renders clouds on top of opaque objects if they're further from the camera than the clouds.
		/// </summary>
		///
		/// <remarks>
		/// Uses a more complex calculation in the default shader and requires Depth Texture to be enabled in the Render Pipeline Asset.
		/// Large curvatures will often lead to clouds blocking geometry in the distance, if this is undesired, either disable the feature or set CloudsController.depthCurvature.
		/// </remarks>
		[Tooltip("When enabled, renders clouds on top of opaque objects if they're further from the camera than the clouds."
		+ "\n\nUses a more complex calculation in the default shader and requires Depth Texture to be enabled in the Render Pipeline Asset. "
		+ "Large curvatures will often lead to clouds blocking geometry in the distance, if this is undesired, either disable the feature or set depthCurvature.")]
		public bool renderAboveOpaques = true;

		/// <summary>
		/// If true, uses a different curvature when calculating distance from the camera.
		/// </summary>
		[Tooltip("If true, uses a different curvature when calculating distance from the camera.")]
		public bool separateDepthCurvature = false;

		/// <summary>
		/// The curvature of the clouds when calculating distance from the camera, 0 being flat.
		/// </summary>
		///
		/// <remarks>
		/// Objects further than that distance will appear behind clouds.
		/// Cloud height is also taken into account when calculating distance.
		/// </remarks>
		[Tooltip("The curvature of the clouds when calculating distance from the camera, 0 being flat."
		+ "\n\nObjects further than that distance will appear behind clouds. "
		+ "Cloud height is also taken into account when calculating distance.")]
		[Range(0f, 1f)]
		public float depthCurvature = 1f;

		/// <summary>
		/// Enables enqueuing the pass through this object.
		/// </summary>
		///
		/// <remarks>
		/// If disabled, a <see cref="ShapedCloudsRendererFeature">renderer feature</see> is required. <para />
		/// Used to render other passes before this.
		/// Controllers with this disabled are ALWAYS rendered after (on top) of the ones with it enabled, but order with other controllers not enqueuing is maintained.
		/// </remarks>
		[Tooltip("Enables enqueuing the pass through this object."
		+ "\n\nIf disabled, a renderer feature is required."
		+ "\n\nUsed to render other passes before this. "
		+ "Controllers with this disabled are ALWAYS rendered after (on top) of the ones with it enabled, but order with other controllers not enqueuing is maintained.")]
		public bool enqueuePass = true;


#if UNITY_6000_0_OR_NEWER
		/// <summary>
		/// Render pass used in Unity 6 or newer.
		/// </summary>
		///
		/// <remarks>
		/// Doesn't necessarily correspond to this particular controller.
		/// </remarks>
		private ShapedCloudsRenderPass pass;
#endif


#if !UNITY_6000_4_OR_NEWER
		/// <summary>
		/// Compatibility render pass used both up to Unity 6.3 with compatibility mode enabled and Unity 2022.
		/// </summary>
		///
		/// <remarks>
		/// Doesn't necessarily correspond to this particular controller.
		/// </remarks>
		private ShapedCloudsCompatibilityRenderPass compatibilityPass;
#endif

		void OnTransformChildrenChanged() {
			dirty = true;

		}

		void Reset() {
			baseBlur.shader = Resources.Load<Shader>("Shaped Clouds/Shaders/ShapedClouds_BlurBlit");
			finalBlur.shader = Resources.Load<Shader>("Shaped Clouds/Shaders/ShapedClouds_BlurBlit");
			filter.shader = Resources.Load<Shader>("Shaped Clouds/Shaders/ShapedClouds_CloudFilter");
			filter.noiseTexture = Resources.Load<Texture>("Shaped Clouds/Textures/ShapedClouds_SimpleClouds");
			blitShader = Resources.Load<Shader>("Shaped Clouds/Shaders/ShapedClouds_CloudBlit");

			filter.layers = new CloudNoiseLayer[] {
				new(new(Random.value, Random.value), new(7.96421051f, 13.8770065f), 4f, .1f),
				new(new(Random.value, Random.value), new(9.628571f, 7.16175f), 8f, .075f)
			};

		}

		void Start() {
			dirty = true;

		}

		void OnEnable() {
			RenderPipelineManager.beginCameraRendering += RenderClouds;

#if UNITY_6000_0_OR_NEWER
			pass = new();
#endif

#if !UNITY_6000_4_OR_NEWER
			compatibilityPass = new();
#endif

			UpdateList();

		}

		void OnDisable() {
			RenderPipelineManager.beginCameraRendering -= RenderClouds;

#if UNITY_6000_0_OR_NEWER
			pass = null;
#endif

#if !UNITY_6000_4_OR_NEWER
#pragma warning disable 0612
			compatibilityPass.Dispose();
#pragma warning restore 0612
			compatibilityPass = null;
#endif

			earlyList.Remove(this);

		}

		void OnDestroy() {
			baseBlur.DestroyMaterial();
			finalBlur.DestroyMaterial();
			DestroyMaterial();

		}

		void LateUpdate() {
			if (dirty)
				UpdateList();

			float t = (pauseInEditor && !Application.isPlaying) ? 0 : Time.deltaTime * timeScale;

			foreach (CloudRenderer r in renderers)
				if (r != null)
					r.CloudUpdate(this, t);

			filter.CloudUpdate(this, t);

		}

		private void UpdateList() {
			// Updates list of children.
			renderers.Clear();
			foreach (Transform t in transform) {
				var cr = t.GetComponents<CloudRenderer>();
				if (cr.Length > 0)
					renderers.AddRange(cr);

			}

		}


		/// <summary>
		/// List of active CloudsController objects that enqueue their passes through the controller itself, but haven't been rendered yet.
		/// </summary>
		///
		/// <remarks>
		/// Called early list because it always comes before the <see cref="lateList">late list</see>, as well as any other Renderer Feature.
		/// Items on this list are sorted from first to last to render.
		/// </remarks>
		public static readonly List<CloudsController> earlyList = new();

		/// <summary>
		/// List of active CloudsController objects that should be enqueued by a <see cref="ShapedCloudsRendererFeature">ShapedCloudsRendererFeature</see>, and haven't rendered yet.
		/// </summary>
		///
		/// <remarks>
		/// Called late list because it always comes after the <see cref="earlyList">early list</see>.
		/// Items on this list are sorted from first to last to render.
		/// </remarks>
		public static readonly List<CloudsController> lateList = new();
		private void RenderClouds(ScriptableRenderContext context, Camera camera) {
			if (!CheckReady(camera))
				return;

			Setup(camera);

			if (enqueuePass) {
				camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(GetPass());

				// Adds these clouds to the early list, sorted by order and then by height.
				// Regardless of which pass comes first, it'll reference the first controller on the list and remove it when it's done.
				// That also means the pass created by this controller won't necessarily be the one drawing it.
				if (earlyList.Count > 0 && !earlyList.Contains(this))
					for (int i = 0; i < earlyList.Count; i++) {
						var controller = earlyList[i];
						if (controller.order > order || (controller.order == order && controller.height < height)) {
							earlyList.Insert(i, this);
							break;

						}

						if (i == earlyList.Count - 1) {
							earlyList.Add(this);
							break;

						}

					}
				else if (!earlyList.Contains(this))
					earlyList.Add(this);

				if (lateList.Contains(this))
					lateList.Remove(this);

				SetPassUnqueued(false);

			} else {
				// If not enqueuing, the pass should render from a Renderer Feature instead.
				// The same sorting is done, but with a separate list that always renders after the first.
				if (lateList.Count > 0 && !lateList.Contains(this))
					for (int i = 0; i < lateList.Count; i++) {
						var controller = lateList[i];
						if (controller.order > order || (controller.order == order && controller.height < height)) {
							lateList.Insert(i, this);
							break;

						}

						if (i == lateList.Count - 1) {
							lateList.Add(this);
							break;

						}

					}
				else if (!lateList.Contains(this))
					lateList.Add(this);

				if (earlyList.Contains(this))
					earlyList.Remove(this);

				SetPassUnqueued(true);

			}

		}

		[NonSerialized]
		private static bool _warnedAboutDepthTexture = false;
		private bool CheckReady(Camera camera) {
			var urpAsset = (QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.currentRenderPipeline) as UniversalRenderPipelineAsset;
			if (!urpAsset.supportsCameraDepthTexture && renderAboveOpaques && !_warnedAboutDepthTexture) {
				Debug.LogWarning("Depth Texture is disabled in the Render Pipeline Asset, the Render Above Opaques feature won't work. Either disable it, or enable Depth Texture in the Render Pipeline Asset.");
				_warnedAboutDepthTexture = true;

			}

			// Check if the clouds should/shouldn't be rendered.
			if (!isActiveAndEnabled || (ignoreCameraLayers & (1 << camera.gameObject.layer)) != 0 || (camera.cullingMask & (1 << gameObject.layer)) == 0 || (ignoreCameraTypes & camera.cameraType) != 0)
				return false;

			// Checks if essential shaders are present.
			if (filter.shader == null || blitShader == null) {
				Debug.LogError($"[Shaped Clouds] filter shader or blit shader missing from {name}.");
				return false;

			}

			// Checks if blur shaders are there and blur is enabled.
			// It's fine to continue rendering since blur is not essential, but they're disabled and a warning is issued.
			if (baseBlur.enabled && baseBlur.shader == null) {
				baseBlur.enabled = false;
				Debug.LogError($"[Shaped Clouds] {name} has base blur enabled, but no blur shader assigned. Base blur disabled.");

			}
			if (finalBlur.enabled && finalBlur.shader == null) {
				finalBlur.enabled = false;
				Debug.LogError($"[Shaped Clouds] {name} has final blur enabled, but no blur shader assigned. Final blur disabled.");

			}

			return true;

		}

		private void Setup(Camera camera) {
			// Sets values for materials.

			if (baseBlur.enabled)
				baseBlur.Setup();
			if (finalBlur.enabled)
				finalBlur.Setup();

			filter.Setup(this, camera);

			if (blitMaterial == null || blitMaterial.shader != blitShader) {
				DestroyMaterial();
				blitMaterial = new(blitShader);

			}
			blitMaterial.SetFloat("_CloudDepth", depth);
			blitMaterial.SetFloat("_CloudHeight", height);
			blitMaterial.SetFloat("_Curvature", separateDepthCurvature ? depthCurvature : curvature);
			blitMaterial.SetKeyword(new(blitShader, "SC_RENDER_ABOVE_OPAQUES"), renderAboveOpaques);

		}

		/// <summary>
		/// Selects the correct pass based on the version of Unity and compatibility mode being enabled or not.
		/// </summary>
		/// <returns>The pass that should be used.</returns>
		public ScriptableRenderPass GetPass() {
			// Up until Unity 6.3, compatibility mode can be used to disable Render Graph.
			// Check for that to determine which version to enqueue.
#if UNITY_6000_4_OR_NEWER
			// Compatibility mode is gone.
			return pass;
#elif UNITY_6000_0_OR_NEWER
			// Check for compatibility mode and use the appropriate pass.
			var settings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
            if (settings != null && !settings.enableRenderCompatibilityMode)
				return pass;
			else
				return compatibilityPass;
#else
			// Unity 2022 or earlier, only compatibility pass is available.
			return compatibilityPass;
#endif

		}

		/// <summary>
		/// Set the pass as queued or unenqueued.
		/// </summary>
		///
		/// <remarks>
		/// An unenqueued pass is queued later by the <see cref="ShapedCloudsRendererFeature">Renderer Feature</see>, and uses a separate list of controllers.
		/// As such, it needs to know if it's enqueued or not, so it knows which list to use.
		/// </remarks>
		/// <param name="value">True if unenqueued.</param>
		public void SetPassUnqueued(bool value) {
#if UNITY_6000_4_OR_NEWER
			pass.unqueued = value;
#elif UNITY_6000_0_OR_NEWER
			var settings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
			if (settings != null && !settings.enableRenderCompatibilityMode)
				pass.unqueued = value;
			else
				compatibilityPass.unqueued = value;
#else
			compatibilityPass.unqueued = value;
#endif

		}

		private void DestroyMaterial() {
			// Avoids a warning.
			if (blitMaterial != null
#if UNITY_EDITOR
			&& !EditorUtility.IsPersistent(blitMaterial)
#endif
			)
				DestroyImmediate(blitMaterial);

			// Even if the material wasn't destroyed (because it was persistent), set it to null so it can be considered gone.
			blitMaterial = null;

		}

	}

}
