using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif


using ShapedClouds.Settings;


namespace ShapedClouds {

#if UNITY_6000_0_OR_NEWER
	// Uses RenderGraph, which was officially added to URP with Unity 6.
	// Some (or all?) 2023.3 previews could also run this code, if you want to specifically use RenderGraph for those versions, you can comment conditional compilation on this file and CloudsController.cs.

	/// <summary>
	/// Pass responsible for rendering clouds to the screen.
	/// </summary>
	///
	/// <remarks>
	/// Renders a list of <see cref="CloudRenderers.CloudRenderer">CloudRenderer</see> to a base texture, optionally blurs it, then copies it through a cloud filter into a final texture.
	/// The final texture is then copied to the screen (after also being optionally blurred), behind geometry already drawn.
	/// </remarks>
	public class ShapedCloudsRenderPass: ScriptableRenderPass {
		class PassData {
			internal CloudsController controller;
			internal Camera camera;
			internal Matrix4x4 viewMatrix;
			internal Matrix4x4 projectionMatrix;
			internal TextureHandle baseTexture;
			internal TextureHandle blurTexture;
			internal TextureHandle finalTexture;
			internal TextureHandle finalBlurTexture;
			internal TextureHandle target;
			internal TextureHandle depth;
			internal bool isTargetFlipped;

		}

		private CloudsController controller;
		public bool unqueued;

		private static readonly int ViewMatrixId = Shader.PropertyToID("unity_MatrixV");
		private static readonly int InverseViewMatrixId = Shader.PropertyToID("unity_MatrixInvV");
		private static readonly int ProjectionMatrixId = Shader.PropertyToID("unity_MatrixP");
		private static readonly int InverseProjectionMatrixId = Shader.PropertyToID("unity_MatrixInvP");
		private static readonly int ViewAndProjectionMatrixId = Shader.PropertyToID("unity_MatrixVP");
		private static readonly int InverseViewAndProjectionMatrixId = Shader.PropertyToID("unity_MatrixInvVP");

		/// <summary>
		/// Restores CPU and GPU view/projection shader matrices for the active render target flip state.
		/// </summary>
		internal static void SetAllViewProjMatrices(CommandBuffer cmd, Matrix4x4 view, Matrix4x4 proj, bool renderIntoTexture) {
			cmd.SetViewProjectionMatrices(view, proj);
			Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(proj, renderIntoTexture);
			Matrix4x4 vp = gpuProj * view;
			cmd.SetGlobalMatrix(ViewMatrixId, view);
			cmd.SetGlobalMatrix(InverseViewMatrixId, view.inverse);
			cmd.SetGlobalMatrix(ProjectionMatrixId, gpuProj);
			cmd.SetGlobalMatrix(InverseProjectionMatrixId, gpuProj.inverse);
			cmd.SetGlobalMatrix(ViewAndProjectionMatrixId, vp);
			cmd.SetGlobalMatrix(InverseViewAndProjectionMatrixId, vp.inverse);
		}

		public ShapedCloudsRenderPass() {
			renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
			ConfigureInput(ScriptableRenderPassInput.Depth);

		}

		void ExecutePass(PassData data, UnsafeGraphContext context) {
			var c = data.controller;
			CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

			cmd.SetRenderTarget(data.baseTexture);
			cmd.ClearRenderTarget(false, true, Color.clear);
			foreach (var r in c.renderers)
				if (r != null)
					r.Render(c, data.camera, cmd);

			if (c.baseBlur.enabled)
				RenderBlur(c.baseBlur, cmd, data.baseTexture, data.blurTexture);

			cmd.SetRenderTarget(data.finalTexture);
			cmd.ClearRenderTarget(false, true, Color.clear);
			if (c.renderers.Count > 0)
				SetAllViewProjMatrices(cmd, data.viewMatrix, data.projectionMatrix, true);
			RenderFilter(c, data.camera, cmd, data.baseTexture);

			if (c.finalBlur.enabled)
				RenderBlur(c.finalBlur, cmd, data.finalTexture, data.finalBlurTexture);

			if (c.renderAboveOpaques)
				cmd.SetRenderTarget(data.target);
			else
				cmd.SetRenderTarget(data.target, data.depth);
			if (c.renderers.Count > 0)
				SetAllViewProjMatrices(cmd, data.viewMatrix, data.projectionMatrix, data.isTargetFlipped);
			Blitter.BlitTexture(cmd, data.finalTexture, Vector4.one, c.blitMaterial, 0);

		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext) {
			var list = unqueued ? CloudsController.lateList : CloudsController.earlyList;

			if (list.Count == 0)
				return;

			controller = list[0];
			list.RemoveAt(0);

			string passName = $"Shaped Clouds ({controller.name})";

			using var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData);

			var cameraData = frameContext.Get<UniversalCameraData>();
			passData.controller = controller;
			passData.camera = cameraData.camera;
			passData.viewMatrix = cameraData.GetViewMatrix();
			passData.projectionMatrix = cameraData.GetProjectionMatrix();
			
			var resourceData = frameContext.Get<UniversalResourceData>();
			passData.target = resourceData.activeColorTexture;
			passData.isTargetFlipped = cameraData.cameraType == CameraType.SceneView ||
			                           cameraData.targetTexture != null ||
			                           !resourceData.isActiveTargetBackBuffer;

			builder.SetRenderAttachment(passData.target, 0);

			if (controller.renderAboveOpaques) {
				if (resourceData.cameraDepthTexture.IsValid())
					builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
			} else {
				passData.depth = resourceData.activeDepthTexture;
				builder.SetRenderAttachmentDepth(passData.depth, AccessFlags.Read);

			}

			// Descriptor for textures.
			RenderTextureDescriptor descriptor = new() {
				colorFormat = controller.baseColorFormat,
				dimension = TextureDimension.Tex2D,

				width = cameraData.scaledWidth / Mathf.Max((int)controller.baseResolution, (int)controller.finalResolution),
				height = cameraData.scaledHeight / Mathf.Max((int)controller.baseResolution, (int)controller.finalResolution),
				msaaSamples = 1,
				volumeDepth = 1,
				useMipMap = false,

				depthBufferBits = 0,

				useDynamicScale = controller.useDynamicScale

			};

			passData.baseTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, $"Shaped Clouds Base ({controller.name})", true, controller.filterMode);
			builder.UseTexture(passData.baseTexture, AccessFlags.ReadWrite);

			if (controller.baseBlur.enabled) {
				passData.blurTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, $"Shaped Clouds Blur ({controller.name})", false, controller.filterMode);
				builder.UseTexture(passData.blurTexture, AccessFlags.ReadWrite);
			
			}

			descriptor.colorFormat = controller.finalColorFormat;
			descriptor.width = cameraData.scaledWidth / (int)controller.finalResolution;
			descriptor.height = cameraData.scaledHeight / (int)controller.finalResolution;
				
			passData.finalTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, $"Shaped Clouds Final ({controller.name})", true, controller.filterMode);
			builder.UseTexture(passData.finalTexture, AccessFlags.ReadWrite);
			
			if (controller.finalBlur.enabled) {
				passData.finalBlurTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, $"Shaped Clouds Final Blur ({controller.name})", false, controller.filterMode);
				builder.UseTexture(passData.finalBlurTexture, AccessFlags.ReadWrite);
			
			}

			builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));

		}
		
		public void RenderBlur(CloudBlur blur, CommandBuffer cmd, RTHandle texture1, RTHandle texture2) {
			Blitter.BlitCameraTexture(cmd, texture1, texture2, blur.material, 0);
			Blitter.BlitCameraTexture(cmd, texture2, texture1, blur.material, 1);

		}

		public void RenderFilter(CloudsController controller, Camera camera, CommandBuffer cmd, RTHandle baseTexture) {
			var filter = controller.filter;

			filter.material.SetTexture("_MainTex", baseTexture);

			// cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
			// cmd.DrawMesh(controller.mesh, Matrix4x4.Translate(camera.transform.position) * Matrix4x4.Scale(new Vector3(1, controller.verticalScale, 1) * controller.scale), filter.material, 0, 0, mp);
			
			Blitter.BlitTexture(cmd, new(1,1,0,0), filter.material, 0);

		}


	}

#endif

#if !UNITY_6000_4_OR_NEWER
	// Uses old method, should work on Unity 2022. Will NOT work with Unity 2021, as the included version of URP doesn't support RTHandles.
	// Aside from older versions, it's also automatically switched to when compatibility mode is enabled (which disables Render Graph).
	// Not available in Unity 6.4 or newer, as the compatibility mode has been fully removed.

	/// <summary>
	/// Pass responsible for rendering clouds to the screen, made for Unity 2022 and compatibility mode in Unity 6.
	/// </summary>
	///
	/// <remarks>
	/// Renders a list of <see cref="CloudRenderers.CloudRenderer">CloudRenderer</see> to a base texture, optionally blurs it, then copies it through a cloud filter into a final texture.
	/// The final texture is then copied to the screen (after also being optionally blurred), behind geometry already drawn.
	/// </remarks>
	public class ShapedCloudsCompatibilityRenderPass: ScriptableRenderPass {
		private RTHandle baseTexture, blurTexture, finalTexture, finalBlurTexture;

		private CloudsController controller;
		public bool unqueued;

		public ShapedCloudsCompatibilityRenderPass() {
			renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

		}

#if UNITY_6000_0_OR_NEWER
		[System.Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
			var cmd = CommandBufferPool.Get($"Shaped Clouds ({controller.name})");

			var camera = renderingData.cameraData.camera;

			CoreUtils.SetRenderTarget(cmd, baseTexture);
			cmd.ClearRenderTarget(false, true, Color.clear);

			foreach (var r in controller.renderers)
				if (r != null)
					r.Render(controller, camera, cmd);

			
			if (controller.baseBlur.enabled)
				RenderBlur(controller.baseBlur, cmd, baseTexture, blurTexture);

			Matrix4x4 viewMatrix = renderingData.cameraData.GetViewMatrix();
			Matrix4x4 projMatrix = renderingData.cameraData.GetProjectionMatrix();
			var colorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
			bool isTargetFlipped = renderingData.cameraData.cameraType == CameraType.SceneView ||
			                       camera.targetTexture != null ||
			                       colorTarget.rt != null;

			CoreUtils.SetRenderTarget(cmd, finalTexture);
			cmd.ClearRenderTarget(false, true, Color.clear);
			if (controller.renderers.Count > 0)
				ShapedCloudsRenderPass.SetAllViewProjMatrices(cmd, viewMatrix, projMatrix, true);
			RenderFilter(controller, camera, cmd, baseTexture);

			if (controller.finalBlur.enabled)
				RenderBlur(controller.finalBlur, cmd, finalTexture, finalBlurTexture);


			if (controller.renderAboveOpaques)
				cmd.SetRenderTarget(colorTarget);
			else
				cmd.SetRenderTarget(colorTarget, renderingData.cameraData.renderer.cameraDepthTargetHandle);

			if (controller.renderers.Count > 0)
				ShapedCloudsRenderPass.SetAllViewProjMatrices(cmd, viewMatrix, projMatrix, isTargetFlipped);
			Blitter.BlitTexture(cmd, finalTexture, Vector4.one, controller.blitMaterial, 0);

			context.ExecuteCommandBuffer(cmd);

			cmd.Clear();
			CommandBufferPool.Release(cmd);

		}

		RenderTextureFormat _oldbaseformat;
		RenderTextureFormat _oldfinalformat;
#if UNITY_6000_0_OR_NEWER
		[System.Obsolete]
#endif
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData data) {
			var list = unqueued ? CloudsController.lateList : CloudsController.earlyList;

			if (list.Count == 0)
				return;

			controller = list[0];
			list.RemoveAt(0);

			// Descriptor for textures.
			RenderTextureDescriptor descriptor = new() {
				colorFormat = controller.baseColorFormat,
				dimension = TextureDimension.Tex2D,

				width = data.cameraData.cameraTargetDescriptor.width / Mathf.Max((int)controller.baseResolution, (int)controller.finalResolution),
				height = data.cameraData.cameraTargetDescriptor.height / Mathf.Max((int)controller.baseResolution, (int)controller.finalResolution),
				msaaSamples = 1,
				useMipMap = false,
				volumeDepth = 1,

				depthBufferBits = 0,

				useDynamicScale = controller.useDynamicScale

			};
			if (_oldbaseformat != controller.baseColorFormat && baseTexture != null) {
				baseTexture.Release();
				if (blurTexture != null)
					blurTexture.Release();

				_oldbaseformat = controller.baseColorFormat;

			}

			RenderingUtils.ReAllocateIfNeeded(ref baseTexture, descriptor, controller.filterMode, TextureWrapMode.Clamp, name: $"Shaped Clouds Base ({controller.name})");

			if (controller.baseBlur.enabled)
				RenderingUtils.ReAllocateIfNeeded(ref blurTexture, descriptor, controller.filterMode, TextureWrapMode.Clamp, name: $"Shaped Clouds Blur ({controller.name})");

			// Change descriptor values for final textures.
			descriptor.colorFormat = controller.finalColorFormat;
			descriptor.width = data.cameraData.cameraTargetDescriptor.width / (int)controller.finalResolution;
			descriptor.height = data.cameraData.cameraTargetDescriptor.height / (int)controller.finalResolution;

			if (_oldfinalformat != controller.finalColorFormat && finalTexture != null) {
				finalTexture.Release();
				if (finalBlurTexture != null)
					finalBlurTexture.Release();

				_oldfinalformat = controller.finalColorFormat;

			}


			RenderingUtils.ReAllocateIfNeeded(ref finalTexture, descriptor, controller.filterMode, TextureWrapMode.Clamp, name: $"Shaped Clouds Final ({controller.name})");
			if (controller.finalBlur.enabled)
				RenderingUtils.ReAllocateIfNeeded(ref finalBlurTexture, descriptor, controller.filterMode, TextureWrapMode.Clamp, name: $"Shaped Clouds Final Blur ({controller.name})");

		}

		public void RenderBlur(CloudBlur blur, CommandBuffer cmd, RTHandle texture1, RTHandle texture2) {
			Blitter.BlitCameraTexture(cmd, texture1, texture2, blur.material, 0);
			Blitter.BlitCameraTexture(cmd, texture2, texture1, blur.material, 1);

		}

		public void RenderFilter(CloudsController controller, Camera camera, CommandBuffer cmd, RTHandle baseTexture) {
			var filter = controller.filter;

			filter.material.SetTexture("_MainTex", baseTexture);

			Blitter.BlitTexture(cmd, new(1,1,0,0), filter.material, 0);

		}

#if UNITY_6000_0_OR_NEWER
		[System.Obsolete]
#endif
		public void Dispose() {
			// Annoyingly, texture?.Release() wouldn't work.
			if (baseTexture != null)
				baseTexture.Release();
			if (blurTexture != null)
				blurTexture.Release();
			if (finalTexture != null)
				finalTexture.Release();
			if (finalBlurTexture != null)
				finalBlurTexture.Release();

		}


	}
#endif

}