using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Originally this file was a class that held all settings, but it changed later for a more modular system.
// Kept classes together since it was easier to handle at the time.
namespace ShapedClouds.Settings {
	/// <summary>
	/// Determines the scale of the temporary textures, compared to the render target.
	/// </summary>
	public enum CloudTextureResolution {
		Full = 1,
		Half = 2,
		Third = 3,
		Quarter = 4,
		Sixth = 6,
		Eighth = 8

	}

	/// <summary>
	/// Settings for layers of noise, used in <see cref="CloudFilter">CloudFilter</see> and <see cref="CloudRenderers.CloudBaseNoiseRenderer">CloudBaseNoiseRenderer</see>.
	/// </summary>
	[Serializable]
	public class CloudNoiseLayer {
		/// <summary>
		/// UV space offset for the clouds. Change this manually to move clouds around.
		/// </summary>
		[Tooltip("UV space offset for the clouds. Change this manually to move clouds around.")]
		public Vector2 offset;

		/// <summary>
		/// Scale of the cloud noise, higher values make the noise smaller and lower values make it bigger.
		/// </summary>
		[Tooltip("Scale of the cloud noise, higher values make the noise smaller and lower values make it bigger.")]
		public float scale;
		
		/// <summary>
		/// Intensity multiplier for this layer.
		/// </summary>
		[Tooltip("Intensity multiplier for this layer.")]
		public float intensity;

		/// <summary>
		/// World space velocity of the wind. Rotation is clockwise and 0 corresponds to North (Vector3.forward).
		/// </summary>
		[Compass]
		[Tooltip("World space velocity of the wind. Rotation is clockwise and 0 corresponds to North (Vector3.forward).")]
		public Vector2 wind;

		/// <summary>
		/// Clockwise rotation of the texture.
		/// </summary>
		/// 
		/// <remarks>
		/// Useful if you want to align the noise with the direction it's moving towards.
		/// If set to the same number as wind rotation, aligns the top part of the texture with the direction of the wind.
		/// </remarks>
		[Range(0, 360)]
		[Tooltip("Clockwise rotation of the texture. Useful if you want to align the noise with the direction it's moving towards. If set to the same number as wind rotation, aligns the top part of the texture with the direction of the wind.")]
		public float rotation = 0;

		private Vector4 v = Vector4.zero;

		public CloudNoiseLayer() {
			offset = Vector2.zero;
			wind = Vector2.zero;
			scale = 1;
			intensity = 1;

		}

		public CloudNoiseLayer(Vector2 wind, float scale, float intensity) {
			this.wind = wind;
			this.scale = scale;
			this.intensity = intensity;

			offset = Vector2.zero;

		}

		public CloudNoiseLayer(Vector2 offset, Vector2 wind, float scale, float intensity) {
			this.offset = offset;
			this.wind = wind;
			this.scale = scale;
			this.intensity = intensity;

		}

		public CloudNoiseLayer(Vector4 v) {
			wind = new(v.x, v.y);
			scale = v.z;
			intensity = v.w;

		}

		public static implicit operator CloudNoiseLayer(Vector4 v) => new(v);

		/// <summary>
		/// Returns the internal Vector4 of the layer, which should be input to the material.
		/// </summary>
		/// 
		/// <remarks>
		/// Only updated when <see cref="Update(CloudsController, float, float, float)">Update</see> is called.
		/// </remarks>
		/// <param name="cl">CloudNoiseLayer to get the vector from.</param>
		public static implicit operator Vector4(CloudNoiseLayer cl) => cl.v;


		public CloudNoiseLayer Update(CloudsController controller, float deltaTime, float scale = 1f, float addRotation = 0f) {
			float ts = deltaTime;

			if (controller.height <= 0 || ts == 0) {
				offset.x %= 1;
				offset.y %= 1;
				v.x = offset.x;
				v.y = offset.y;
				v.z = this.scale * scale * (1+controller.curvature);
				v.w = intensity;
				return this;

			}

			float c = Mathf.Cos((rotation + addRotation) * Mathf.Deg2Rad);
			float s = Mathf.Sin((rotation + addRotation) * Mathf.Deg2Rad);
			Vector2 w = new(wind.x * c - wind.y * s, wind.x * s + wind.y * c);

			offset -= w / controller.height * ts * this.scale * scale;
			offset.x %= 1;
			offset.y %= 1;
			v.x = offset.x;
			v.y = offset.y;
			v.z = this.scale * scale * (1+controller.curvature);
			v.w = intensity;

			return this;

		}

	}

	/// <summary>
	/// Settings for the cloud filter, responsible for rendering lighting and adding detail.
	/// </summary>
	[Serializable]
	public class CloudFilter {
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
		/// How dense clouds are, controls how opaque they are.
		/// </summary>
		[Tooltip("How dense clouds are, controls how opaque they are.")]
		public float density = 6f;

		/// <summary>
		/// Scale of the noise, affects all layers.
		/// </summary>
		[Tooltip("Scale of the noise, affects all layers.")]
		public float scale = 1f;
		
		/// <summary>
		/// How fast the wind moves the clouds, affects all layers.
		/// </summary>
		[Tooltip("How fast the wind moves the clouds, affects all layers.")]
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
		[Tooltip("The layers of noise."
		+ "\n\nThe maximum amount accepted by the default shader is 8. "
		+ "Each layer is an additional texture sample, 2 is often enough but higher numbers can hide individual layers better.")]
		public CloudNoiseLayer[] layers = {
			new(new(7.96421051f, 13.8770065f), 4f, .1f),
			new(new(9.628571f, 7.16175f), 8f, .075f)
		};


		/// <summary>
		/// How much light from the sun clouds will absorb.
		/// </summary>
		[Header("Lighting")]
		[Tooltip("How much light from the sun clouds will absorb.")]
		public float colorAbsorption = 4f;

		/// <summary>
		/// Color filter for clouds, multiplies color by RGB and transparency by A.
		/// </summary>
		[Tooltip("Color filter for clouds, multiplies color by RGB and transparency by A.")]
		[ColorUsage(true, true)]
		public Color color = new(1f, 1f, 1f);

		/// <summary>
		/// Color filter for the light coming from the Sun during sunset.
		/// </summary>
		/// 
		/// <remarks>
		/// The light still fades to black, there's no need for this color to be dark.
		/// </remarks>
		[Tooltip("Color filter for the light coming from the Sun during sunset."
		+ "\n\nThe light still fades to black, there's no need for this color to be dark.")]
		[ColorUsage(false, true)]
		public Color sunsetColor = new(1f, .3f, 0f);

		/// <summary>
		/// Light from the Moon, used during the night.
		/// </summary>
		/// 
		/// <remarks>
		/// The Moon is not simulated, this simply acts as the Sun during the night, and comes from the opposite direction.
		/// Not used when CloudFilter.useCustomMoonLight is enabled, as the color is taken from the light object instead.
		/// </remarks>
		[Tooltip("Light from the Moon, used during the night."
		+ "\n\nThe Moon is not simulated, this simply acts as the Sun during the night, and comes from the opposite direction. "
		+ "Not used when Use Custom Moon Light is enabled, as the color is taken from the light object instead.")]
		[ColorUsage(false, true)]
		public Color moonColor = new(0.01176f, 0.01668f, 0.03f);

		/// <summary>
		/// Ambient lighting during the day, as a percentage of the Sun.
		/// </summary>
		/// 
		/// <remarks>
		/// Ignored if <see cref="useCustomAmbientColor">useCustomAmbientColor</see> is enabled, which allows controlling the ambient color entirely through code.
		/// </remarks>
		[Tooltip("Ambient lighting during the day."
		+ "\n\nIgnored if Use Custom Ambient Color is enabled, which allows controlling the ambient color entirely through code.")]
		[Range(0, 1)]
		public float ambientDay = .3f;

		/// <summary>
		/// Ambient lighting during the night, as a percentage of the Moon.
		/// </summary>
		/// 
		/// <remarks>
		/// Ignored if <see cref="useCustomAmbientColor">useCustomAmbientColor</see> is enabled, which allows controlling the ambient color entirely through code.
		/// </remarks>
		[Tooltip("Ambient lighting during the night."
		+ "\n\nIgnored if Use Custom Ambient Color is enabled, which allows controlling the ambient color entirely through code.")]
		[Range(0, 1)]
		public float ambientNight = .1f;


		/// <summary>
		/// Shader used to render the clouds.
		/// </summary>
		[Header("Advanced")]
		[Tooltip("Shader used to render the clouds.")]
		public Shader shader;

		/// <summary>
		/// Material created from the shader, used to render clouds.
		/// </summary>
		/// 
		/// <remarks>
		/// If using a custom shader that doesn't use the default shader's properties, you can directly change properties of the material here.
		/// Ideally, create a separate MonoBehaviour and have it modify this material. <para />
		/// 
		/// If replacing this material, you must also replace the shader so that they match.
		/// However, replacing materials through code is discouraged and, if doing so, the better way to do it is to replace the shader and let the controller create a new material.
		/// </remarks>
		[NonSerialized]
		public Material material;

		/// <summary>
		/// When true, uses the ambient color specified by <see cref="customAmbientColor">customAmbientColor</see>.
		/// </summary>
		[Tooltip("When true, uses the ambient color specified below.")]
		public bool useCustomAmbientColor;

		/// <summary>
		/// Only used when <see cref="useCustomAmbientColor">useCustomAmbientColor</see> is enabled, determines the color of clouds before lighting.
		/// </summary>
		[Tooltip("Only used when Use Custom Ambient Color is enabled, determines the color of clouds before lighting.")]
		[ColorUsage(false, true)]
		public Color customAmbientColor;

		/// <summary>
		/// When true, the light specified by <see cref="sun">sun</see> will determine the Sun light color and direction, instead of the main light of the scene.
		/// </summary>
		[Tooltip("When true, the light specified below will determine the Sun light color and direction, instead of the main light of the scene.")]
		public bool useCustomSunLight;

		/// <summary>
		/// Directional light to be used as the Sun, can be disabled if it doesn't affect anything else on the scene.
		/// </summary>
		/// 
		/// <remarks>
		/// Only used when <see cref="useCustomSunLight">useCustomSunLight</see> is enabled.
		/// </remarks>
		[Tooltip("Directional light to be used as the Sun, can be disabled if it doesn't affect anything else on the scene."
		+ "\n\nOnly used when Use Custom Sun Light is enabled.")]
		public Light sun;

		/// <summary>
		/// When true, the light specified by <see cref="moon">moon</see> will determine the Moon light color and direction.
		/// </summary>
		[Tooltip("When true, the light specified below will determine the Moon light color and direction.")]
		public bool useCustomMoonLight;

		/// <summary>
		/// Directional light to be used as the Moon, can be disabled if it doesn't affect anything else on the scene.
		/// </summary>
		/// 
		/// <remarks>
		/// Only used when <see cref="useCustomMoonLight">useCustomMoonLight</see> is enabled.
		/// </remarks>
		[Tooltip("Directional light to be used as the Moon, can be disabled if it doesn't affect anything else on the scene."
		+ "\n\nOnly used when Use Custom Moon Light is enabled.")]
		public Light moon;

		/// <summary>
		/// The distance clouds start to fade away as they go down the horizon.
		/// </summary>
		/// 
		/// <remarks>
		/// 0 is the horizon line and 1 is all the way down. Negative values fades clouds above the horizon.
		/// </remarks>
		[Tooltip("The distance clouds start to fade away as they go down the horizon."
		+ "\n\n0 is the horizon line and 1 is all the way down. Negative values fades clouds above the horizon.")]
		public float fadeDistance = 0f;

		/// <summary>
		/// The length of the fade effect.
		/// </summary>
		[Tooltip("The length of the fade effect.")]
		public float fadeLength = .25f;

		/// <summary>
		/// Controls how dense the cloud can be and still receive detail.
		/// </summary>
		/// 
		/// <remarks>
		/// When a cloud is dense enough, the filter won't apply noise to it, saving a texture sample for each noise layer.
		/// This variable controls at what point that happens, 0 being never and 1 being always.
		/// </remarks>
		[Tooltip("Controls how dense the cloud can be and still receive detail."
		+ "\n\nWhen a cloud is dense enough, the filter won't apply noise to it, saving a texture sample for each noise layer. "
		+ "This variable controls at what point that happens, 0 being never and 1 being always.")]
		[Range(0f, 1f)]
		public float detailLevel = .5f;
	
		public void CloudUpdate(CloudsController controller, float deltaTime) {
			foreach (CloudNoiseLayer l in layers)
				l.Update(controller, deltaTime * timeScale, scale, rotation - windRotation);

		}

		/// <summary>
		/// Utility method to convert a world space Vector3 direction into a windRotation that points there.
		/// </summary>
		/// <param name="direction">The world space direction to rotate wind towards.</param>
		public void SetWindDirection(Vector3 direction) {
			// It probably works.
			Vector2 v = new Vector2(direction.x, direction.z).normalized;
			windRotation = -Vector2.SignedAngle(new(direction.x, direction.z), Vector2.down) + 180;

		}

		private Vector4[] lv = new Vector4[0];
		private float[] lr = new float[0];
		public void Setup(CloudsController controller, Camera camera) {
			// Ensures material is there.
			if (material == null || material.shader != shader) {
				DestroyMaterial();
				material = new(shader);

			}

			material.SetTexture("_NoiseTexture", noiseTexture);

			material.SetFloat("_ColorAbsorption", colorAbsorption);
			material.SetFloat("_Density", density);

			material.SetColor("_Color", color);

			material.SetColor("_SunsetColor", sunsetColor);

			Color c;
			if (useCustomSunLight && sun != null) {
				c = sun.color.linear * sun.intensity;
				if (sun.useColorTemperature)
					c *= Mathf.CorrelatedColorTemperatureToRGB(sun.colorTemperature);

				material.SetColor("_SunColor", c);
				material.SetVector("_SunDirection", -sun.transform.forward);

			}

			material.SetKeyword(new(shader, "SC_CUSTOM_SUN"), useCustomSunLight && sun != null);

			if (useCustomMoonLight && moon != null) {
				c = moon.color.linear * moon.intensity;
				if (moon.useColorTemperature)
					c *= Mathf.CorrelatedColorTemperatureToRGB(moon.colorTemperature);
				material.SetVector("_MoonDirection", -moon.transform.forward);

			} else
				c = moonColor;

			material.SetColor("_MoonColor", c);
			material.SetKeyword(new(shader, "SC_CUSTOM_MOON"), useCustomMoonLight && moon != null);

			material.SetFloat("_AmbientDay", ambientDay);
			material.SetFloat("_AmbientNight", ambientNight);

			material.SetColor("_AmbientColor", customAmbientColor);
			material.SetKeyword(new(shader, "SC_CUSTOM_AMBIENT_COLOR"), useCustomAmbientColor);
			
			material.SetVector("_CameraOffset", camera.transform.position);
			material.SetFloat("_CloudHeight", controller.height);
			material.SetFloat("_Curvature", controller.curvature);

			material.SetFloat("_FadeDistance", fadeDistance);
			material.SetFloat("_FadeLength", fadeLength);
			material.SetFloat("_DetailLevel", detailLevel);

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

			material.SetVector("_CloudCameraOffset", camera.transform.position);
			material.SetFloat("_CloudHeight", controller.height);

			
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

	/// <summary>
	/// Settings for bidirectional blur effects for the base and final textures.
	/// </summary>
	[Serializable]
	public class CloudBlur {
		/// <summary>
		/// Enables or disables the bidirectional blur effect.
		/// </summary>
		[Tooltip("Enables or disables the bidirectional blur effect.")]
		public bool enabled = false;

		/// <summary>
		/// Amount of samples used in the blur effect.
		/// </summary>
		/// 
		/// <remarks>
		/// This does not include the middle sample, and goes in both directions.
		/// So 1 is actually 3 samples, 2 is actually 5 and so on.
		/// </remarks>
		[Range(1, 8)]
		[Tooltip("Amount of samples used in the blur effect.")]
		public int sampleCount = 1;
		
		/// <summary>
		/// Intensity of the blur effect, total distance in UV space the image will be blurred.
		/// </summary>
		[Range(0, 0.2f)]
		[Tooltip("Intensity of the blur effect, total distance in UV space the image will be blurred.")]
		public float intensity = 0;

		/// <summary>
		/// Shader used to blur the base texture, it should contain 2 passes, one horizontal blur and another vertical.
		/// </summary>
		[Header("Advanced")]
		[Tooltip("Shader used to blur the base texture, it should contain 2 passes, one horizontal blur and another vertical.")]
		public Shader shader;

		[NonSerialized]
		public Material material;

		public void Setup() {
			// Ensures material is there.
			if (material == null || material.shader != shader) {
				DestroyMaterial();
				material = new(shader);

			}

			material.SetFloat("_Samples", sampleCount);
			material.SetFloat("_Intensity", intensity);

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