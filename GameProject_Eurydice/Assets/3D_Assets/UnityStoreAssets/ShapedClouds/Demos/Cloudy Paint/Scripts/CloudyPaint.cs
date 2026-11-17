using ShapedClouds.CloudRenderers;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShapedClouds.Demo {
	public class CloudyPaint: MonoBehaviour {

		private RenderTexture sourceTexture;
		private RenderTexture targetTexture;

		public new CloudMeshRenderer renderer;
		public ComputeShader computeShader;
		public new Collider collider;

		public float size = 50;
		public float strength = .1f;

		private Vector2 prevpos = new(.5f, .5f);


		private RenderTextureDescriptor _desc;
		void Start() {
			_desc = new() {
				enableRandomWrite = true,
				width = 2048,
				height = 1024,
				colorFormat = RenderTextureFormat.DefaultHDR,
				volumeDepth = 1,
				msaaSamples = 1,
				dimension = TextureDimension.Tex2D,
				useDynamicScale = true


			};
			sourceTexture = new RenderTexture(_desc);
			targetTexture = new RenderTexture(_desc);

		}

		// Update is called once per frame
		void Update() {
			renderer.texture = targetTexture;

			float s = 0;
			s += Input.GetMouseButton(0) ? 1 : 0;
			s += Input.GetMouseButton(1) ? -1 : 0;

			if (Input.GetKey(KeyCode.LeftShift))
				strength = Mathf.Clamp01(strength + Input.mouseScrollDelta.y / 10f);
			else
				size = Mathf.Max(0, size + Input.mouseScrollDelta.y * 10f);

			collider.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 10000f);

			Vector2 pos = hit.textureCoord;
			pos.x = pos.x * targetTexture.width / targetTexture.height;

			// Compute shader for the paint program.
			int kernel = computeShader.FindKernel("paint");
			computeShader.SetInt("height", targetTexture.height);

			computeShader.SetVector("pos1", prevpos);
			computeShader.SetVector("pos2", pos);

			computeShader.SetFloat("strength", s * strength);
			computeShader.SetFloat("size", size);

			computeShader.SetTexture(kernel, "source", sourceTexture);
			computeShader.SetTexture(kernel, "result", targetTexture);

			computeShader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out uint z);
			computeShader.Dispatch(kernel, targetTexture.width / (int)x, targetTexture.height / (int)y, (int)z);

			prevpos = pos;

			(targetTexture, sourceTexture) = (sourceTexture, targetTexture);

		}

		private void SwapTextures() {
			(targetTexture, sourceTexture) = (sourceTexture, targetTexture);
		}

		void Destroy() {
			sourceTexture.Release();
			targetTexture.Release();

		}

	}

}