using UnityEngine;
using UnityEngine.Rendering;

namespace ShapedClouds.CloudRenderers {

	/// <summary>
	/// Renders a mesh or a renderer to the base cloud texture.
	/// </summary>
	/// <remarks>
	/// Although it can render any renderer, it was made for skinned mesh renderers.
	/// Other uses are untested.
	/// </remarks>
	[ExecuteInEditMode]
	[AddComponentMenu("Shaped Clouds/Cloud Renderers/Cloud Mesh Renderer")]
	public class CloudMeshRenderer: CloudRenderer {
		public enum CloudMeshMaterial {
			SOLID = 0,
			VOLUME = 1,
			CUSTOM = 2

		}

		/// <summary>
		/// The mesh to render.
		/// </summary>
		[Tooltip("The mesh to render.")]
		public Mesh mesh;

		/// <summary>
		/// The material to use to render the cloud.
		/// </summary>
		[Header("Material")]
		[Tooltip("The material to use to render the cloud.")]
		public CloudMeshMaterial meshMaterial = CloudMeshMaterial.SOLID;

		// The actual material used.
		private Material _material;
		// To detect when meshMaterial changes so the correct material is loaded.
		private CloudMeshMaterial oldMeshMaterial;

		/// <summary>
		/// Texture used for the mesh, if using a volume material, this goes unused.
		/// </summary>
		[Header("Properties")]
		[Tooltip("Texture used for the mesh, if using a volume material, this goes unused.")]
		public Texture texture = null;
		
		/// <summary>
		/// The density of the mesh. For the volume material, this is the density per meter.
		/// </summary>
		[Tooltip("The density of the mesh. For the volume material, this is the density per meter.")]
		public float density = 1;
		
		/// <summary>
		/// If not null, renders the renderer.
		/// </summary>
		/// 
		/// <remarks>
		/// Meant for either a Mesh Renderer (will use the corresponding materials) or a Skinned Mesh Renderer (can render animations/characters).
		/// Ideally the renderer should not render to the normal scene, but still enabled so it's updated. <para />
		/// 
		/// There's a limitation regarding material properties, so the density and texture set in this component won't be applied when rendering a renderer.
		/// </remarks>
		[Header("Advanced")]
		[Tooltip("If not null, renders the renderer. Meant for either a Mesh Renderer (will use the corresponding materials) or a Skinned Mesh Renderer (can render animations/characters). Ideally the renderer should not render to the normal scene, but still enabled so it's updated.\n\nThere's a limitation regarding material properties, so the density and texture set in this component won't be applied when rendering a renderer.")]
		public Renderer targetRenderer;

		/// <summary>
		/// A custom material, it should output a density, preferably additive.
		/// </summary>
		/// 
		/// <remarks>
		/// It should have a _Density float property.
		/// Mesh Material must be set to CUSTOM.
		/// </remarks>
		[Tooltip("A custom material, it should output a density, preferably additive. It should also have a _Density float. Mesh Material must be set to CUSTOM.")]
		public Material customMaterial;

		public override void CloudUpdate(CloudsController sc, float deltaTime) { }

		public override void Render(CloudsController controller, Camera camera, CommandBuffer cmd) {
			if (_material == null || oldMeshMaterial != meshMaterial) {
				oldMeshMaterial = meshMaterial;
				switch (oldMeshMaterial) {
					case CloudMeshMaterial.SOLID:
						_material = Resources.Load<Material>("Shaped Clouds/Materials/Mesh/ShapedClouds_Solid");
						break;
					case CloudMeshMaterial.VOLUME:
						_material = Resources.Load<Material>("Shaped Clouds/Materials/Mesh/ShapedClouds_Volume");
						break;
					case CloudMeshMaterial.CUSTOM:
						_material = customMaterial;
						break;

				}

			}

			if (!isActiveAndEnabled || (camera.cullingMask & (1 << gameObject.layer)) == 0 || (mesh == null && targetRenderer == null) || _material == null || density == 0)
				return;

			cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);


			MaterialPropertyBlock mp = new();
			if (texture != null)
				mp.SetTexture("_MainTex", texture);

			mp.SetFloat("_Density", density);

			mp.SetVector("_CloudCameraOffset", camera.transform.position);
			mp.SetFloat("_CloudHeight", controller.height);

			if (targetRenderer == null)
				for (int i = 0; i < mesh.subMeshCount; i++)
					cmd.DrawMesh(mesh, Matrix4x4.Translate(transform.position) * Matrix4x4.Rotate(transform.rotation) * Matrix4x4.Scale(transform.lossyScale), _material, i, -1, mp);
			else
				for (int i = 0; i < targetRenderer.sharedMaterials.Length; i++) {
					cmd.DrawRenderer(targetRenderer, (meshMaterial == CloudMeshMaterial.CUSTOM && customMaterial) ? customMaterial : targetRenderer.sharedMaterials[i], i);
				}


		}
		
	}

}