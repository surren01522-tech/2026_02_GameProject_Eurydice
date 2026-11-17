using UnityEngine;

namespace ShapedClouds.Demo {
	[ExecuteInEditMode]
	public class ConstantRotation: MonoBehaviour {

		public Transform trans;

		public Vector3 rotation;


		void Reset() {
			if (!trans)
				trans = transform;

		}

		void Update() {
			if (!trans)
				return;
				
			Vector3 rot = trans.rotation.eulerAngles + rotation * Time.deltaTime;
			trans.rotation = Quaternion.Euler(rot);

		}


	}
	
}