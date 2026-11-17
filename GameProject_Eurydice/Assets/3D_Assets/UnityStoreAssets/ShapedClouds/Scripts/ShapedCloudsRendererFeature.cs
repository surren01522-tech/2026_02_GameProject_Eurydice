using UnityEngine.Rendering.Universal;

namespace ShapedClouds {

	/// <summary>
	/// Allows more fine control over when passes are rendered, by injecting them with a renderer feature instead of through a regular script.
	/// </summary>
	/// 
	/// <remarks>
	/// Simply goes through the <see cref="CloudsController.lateList">late list</see> of active <see cref="CloudsController">CloudsController</see> objects and enqueues their passes. <para />
	/// 
	/// Note that, due to the nature of how the system works, adding a second of this renderer feature won't do anything.
	/// The pass will be enqueued a second time for each CloudsController, but the amount of <see cref="CloudsController">CloudsController</see> objects in the list won't change.
	/// When the pass renders, it takes the first item of the late list and then removes it.
	/// If the list is empty, it simply doesn't do anything, so any extra passes will be skipped. <para />
	/// 
	/// As a result, adding more than a single of this feature won't do anything, and only the first one is effective.
	/// </remarks>
	public class ShapedCloudsRendererFeature: ScriptableRendererFeature {

		public override void Create() {
			// Nothing to do here.

		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
			foreach (var c in CloudsController.lateList)
				renderer.EnqueuePass(c.GetPass());

		}

	}

}