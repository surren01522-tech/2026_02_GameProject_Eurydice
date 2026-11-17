using UnityEngine;
using UnityEditor;

using ShapedClouds.CloudRenderers;

namespace ShapedClouds.Editor {
	public static class ShapedCloudsMenuItems {

		/// <summary>
		/// Creates a new GameObject with the given CloudRenderer and automatically adds it to the appropriate list in the ShapedClouds object.
		/// </summary>
		/// <param name="menuCommand"></param>
		/// <param name="name"></param>
		/// <param name="isFinal"></param>
		static GameObject CreateRenderer<T>(MenuCommand menuCommand, string name) where T : CloudRenderer {
			GameObject go = new(name);
			var parent = menuCommand.context as GameObject;
			GameObjectUtility.SetParentAndAlign(go, parent);
			go.AddComponent<T>();

			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;

			return go;

		}

		[MenuItem("GameObject/Shaped Clouds/Base Noise Renderer", false, 21)]
		static void CreateBaseNoise(MenuCommand menuCommand) {
			CreateRenderer<CloudBaseNoiseRenderer>(menuCommand, "Base Noise Renderer");

		}

		// [MenuItem("GameObject/Shaped Clouds/Cloud Filter", false, 21)]
		// static void CreateCloudFilter(MenuCommand menuCommand) {
		// 	CreateRenderer<CloudFilter>(menuCommand, "Cloud Filter");

		// }

		[MenuItem("GameObject/Shaped Clouds/Cloud Mesh Renderer", false, 21)]
		static void CreateCloudMesh(MenuCommand menuCommand) {
			CreateRenderer<CloudMeshRenderer>(menuCommand, "Cloud Mesh Renderer");

		}

		/// <summary>
		/// Menu item to create a Shaped Clouds gameobject, automatically creating and assigning two other components to render basic clouds.
		/// </summary>
		/// <param name="menuCommand"></param>
		[MenuItem("GameObject/Shaped Clouds/Clouds Controller", false, 10)]
		static void CreateShapedClouds(MenuCommand menuCommand) {
			GameObject go = new("Clouds Controller");
			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			go.AddComponent<CloudsController>();

			GameObject go2 = new("Base Noise");
			GameObjectUtility.SetParentAndAlign(go2, go);
			go2.AddComponent<CloudBaseNoiseRenderer>();

			// go2 = new("Cloud Filter");
			// GameObjectUtility.SetParentAndAlign(go2, go);
			// go2.AddComponent<CloudFilter>();

			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;

		}

	}

}