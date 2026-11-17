using UnityEngine;
using UnityEngine.Rendering;

namespace ShapedClouds.CloudRenderers {

	/// <summary>
	/// Component responsible for rendering to the cloud textures.
	/// </summary>
	/// 
	/// <remarks>
	/// Must be a direct child of a <see cref="CloudsController">CloudsController</see>
	/// </remarks>
	public abstract class CloudRenderer: MonoBehaviour {
		/// <summary>
		/// Update the renderer, this should be dedicated to things like wind moving clouds or other effects that depend on time.
		/// </summary>
		/// 
		/// <param name="controller">The <see cref="CloudsController">CloudsController</see> component that called this method, useful because a renderer might want to know what certain properties are.</param>
		/// <param name="deltaTime">Scaled deltaTime, should be used instead of Time.deltaTime or similar methods.</param>
		public abstract void CloudUpdate(CloudsController controller, float deltaTime);

		/// <summary>
		/// Renders something using the command buffer.
		/// </summary>
		/// 
		/// <param name="controller">The CloudsController component that called this method, useful because a renderer might want to know what certain properties are.</param>
		/// <param name="camera">The Camera being currently rendered to, mainly there to filter based on the camera's layer culling mask.</param>
		/// <param name="cmd">The CommandBuffer to use.</param>
		public abstract void Render(CloudsController controller, Camera camera, CommandBuffer cmd);


	}

}