using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

using ShapedClouds.Settings;


namespace ShapedClouds.CloudRenderers {

	/// <summary>
	/// Renders cloud noise to the base texture.
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("Shaped Clouds/Cloud Renderers/Base Noise Renderer")]
	public class CloudBaseNoiseRenderer: CloudRenderer {


		/// <summary>
		/// The noise texture to use.
		/// </summary>
		/// 
		/// <remarks>
		/// If null, uses the material default instead.
		/// Only the R channel is used, textures should NOT be set as sRGB (color) in import settings.
		/// Higher resolutions are better depending on how low the scale of the noise will be.
		/// </remarks>
		[Header("Material Properties")]
		[Tooltip("The noise texture to use."
		+ "\n\nOnly the R channel is used, textures should NOT be set as sRGB (color) in import settings. "
		+ "Higher resolutions are better depending on how low the scale of the noise will be.")]
		public Texture noiseTexture;

		/// <summary>
		/// Multiplier for how much density the base noise will have.
		/// </summary>
		[Tooltip("Multiplier for how much density the base noise will have.")]
		public float density = 2f;
		
		/// <summary>
		/// How much the sky is filled with clouds.
		/// </summary>
		[Tooltip("How much the sky is filled with clouds.")]
		[Range(0.0f, 1.0f)]
		public float cloudiness = .5f;

		/// <summary>
		/// Scale of the noise, affects all layers.
		/// </summary>
		[Tooltip("Scale of the noise, affects all layers.")]
		public float scale = 1f;
		
		/// <summary>
		/// How fast the wind affects the clouds, affects all layers.
		/// </summary>
		[Tooltip("How fast the wind affects the clouds, affects all layers.")]
		public float timeScale = 1f;

		/// <summary>
		/// Rotates the noise textures clockwise, affects all layers. Doesn't affect wind direction.
		/// </summary>
		[Tooltip("Rotates the noise textures clockwise, affects all layers. Doesn't affect wind direction.")]
		public float rotation;
		
		/// <summary>
		/// Rotates the direction of the wind clockwise, affects all layers. Doesn't affect noise texture rotation.
		/// </summary>
		[Tooltip("Rotates the direction of the wind clockwise, affects all layers. Doesn't affect noise texture rotation.")]
		public float windRotation;

		/// <summary>
		/// The layers of noise.
		/// </summary>
		/// 
		/// <remarks>
		/// The maximum amount accepted by the default shader is 8.
		/// Each layer is an additional texture sample, 2 is often enough but higher numbers can hide individual layers better.
		/// </remarks>
		[Tooltip("The layers of noise. The maximum amount accepted by the default shader is 8. Each layer is an additional texture sample, 2 is often enough but higher numbers can hide individual layers better.")]
		public CloudNoiseLayer[] layers = {
			new(new(6.458498f, 23.1146679f), .1f, .6f),
			new(new(18.85098f, 6.68136072f), .14f, .4f)
		};

		/// <summary>
		/// Shader used to render the clouds, most users should keep it as the default.
		/// </summary>
		[Header("Advanced")]
		[Tooltip("Shader used to render the clouds, most users should keep it as the default.")]
		public Shader shader;

		/// <summary>
		/// Material created from the shader, used to render the base noise.
		/// </summary>
		/// 
		/// <remarks>
		/// If using a custom shader that doesn't use the default shader's properties, you can directly change properties of the material here.
		/// Ideally, create a separate MonoBehaviour and have it modify this material. <para />
		/// 
		/// If replacing this material, you must also replace the shader so that they match.
		/// However, replacing materials through code is discouraged and, if doing so, the better way to do it is to replace the shader and let the controller create a new material.
		/// </remarks>
		[System.NonSerialized]
		public Material material;

		/// <summary>
		/// Base amount of cloudiness.
		/// </summary>
		/// 
		/// <remarks>
		/// Adjust this value if a cloudiness of 0 doesn't correspond to a clear sky (happens depending on layers and textures used).
		/// </remarks>
		[Tooltip("Base amount of cloudiness. Adjust this value if a cloudiness of 0 doesn't correspond to a clear sky (happens depending on layers and textures used).")]
		public float cloudinessBase = -1f;
		
		/// <summary>
		/// Cloudiness multiplier.
		/// </summary>
		/// 
		/// <remarks>
		/// Adjust this value if a cloudiness of 1 doesn't correspond to a fully covered sky (happens depending on layers and textures used).
		/// </remarks>
		[Tooltip("Cloudiness multiplier. Adjust this value if a cloudiness of 1 doesn't correspond to a fully covered sky (happens depending on layers and textures used).")]
		public float cloudinessMult = 1f;

		void Reset() {
			layers = new CloudNoiseLayer[] {
				new(new(Random.value, Random.value), new(6.458498f, 23.1146679f), .1f, .6f),
				new(new(Random.value, Random.value), new(18.85098f, 6.68136072f), .14f, .4f)
			};


			noiseTexture = Resources.Load<Texture>("Shaped Clouds/Textures/ShapedClouds_SimpleClouds");
			shader = Resources.Load<Shader>("Shaped Clouds/Shaders/ShapedClouds_BaseNoiseShader");
			material = null;

		}

		void OnDestroy() {
			DestroyMaterial();

		}

		public override void CloudUpdate(CloudsController sc, float deltaTime) {
			foreach (CloudNoiseLayer l in layers)
				l.Update(sc, deltaTime * timeScale, scale, rotation - windRotation);

		}


		private Vector4[] lv = new Vector4[0];
		private float[] lr = new float[0];
		public override void Render(CloudsController controller, Camera camera, CommandBuffer cmd) {
			if (!isActiveAndEnabled || (camera.cullingMask & (1 << gameObject.layer)) == 0 || shader == null)
				return;

			if (material == null || material.shader != shader) {
				DestroyMaterial();
				material = new(shader);

			}
			
			material.SetTexture("_NoiseTexture", noiseTexture);

			material.SetFloat("_Density", density);
			material.SetFloat("_Cloudiness", cloudinessBase + cloudiness * cloudinessMult);

			material.SetVector("_CameraOffset", camera.transform.position);
			material.SetFloat("_CloudHeight", controller.height);
			material.SetFloat("_Curvature", controller.curvature);


			// Layer arrays, one for offset, scale and intensity, another holding floats for rotation.
			if (layers.Length > 0) {
				if (lv.Length != layers.Length)
					lv = new Vector4[8];

				for (int i = 0; i < Mathf.Min(layers.Length, lv.Length); i++)
					lv[i] = layers[i];

				material.SetVectorArray("_Layer", lv);

				if (lr.Length != layers.Length)
					lr = new float[8];
				for (int i = 0; i < Mathf.Min(layers.Length, lr.Length); i++)
					lr[i] = (layers[i].rotation + rotation) * Mathf.Deg2Rad;
				material.SetFloatArray("_LayerRotation", lr);

			}

			material.SetInteger("_LayerCount", layers.Length);

			if (camera.orthographic) {
				float oldSize = camera.orthographicSize;
				float oldNear = camera.nearClipPlane;

				Vector2 fovVector = new(Mathf.Sin(controller.orthographicFOV/2 * Mathf.Deg2Rad), Mathf.Cos(controller.orthographicFOV/2 * Mathf.Deg2Rad));
				camera.orthographicSize = 1f;
				camera.nearClipPlane = fovVector.y / Mathf.Max(fovVector.x, 0.001f);

				material.SetMatrix("_OrthographicMatrix", (GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix).inverse);

				camera.orthographicSize = oldSize;
				camera.nearClipPlane = oldNear;

			}

			material.SetKeyword(new(shader, "SC_ORTHOGRAPHIC"), camera.orthographic);


			Blitter.BlitTexture(cmd, new(1,1,0,0), material, 0);
			
		}

		/// <summary>
		/// Utility method to convert a world space Vector3 direction into a windRotation that points there.
		/// </summary>
		/// <param name="direction">The world space direction to rotate wind towards.</param>
		public void SetWindDirection(Vector3 direction) {
			Vector2 v = new Vector2(direction.x, direction.z).normalized;
			windRotation = Vector2.SignedAngle(new(direction.x, direction.z), Vector2.down) + 180;

		}

		public void DestroyMaterial() {
			// Avoids a warning.
			if (material != null
#if UNITY_EDITOR
			&& !EditorUtility.IsPersistent(material)
#endif
			)
				GameObject.DestroyImmediate(material);

			// Even if the material wasn't destroyed (because it was persistent), set it to null so it can be considered gone.
			material = null;

		}

	}

}