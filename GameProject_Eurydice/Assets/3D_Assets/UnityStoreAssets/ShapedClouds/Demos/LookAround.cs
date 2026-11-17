using UnityEngine;

namespace ShapedClouds.Demo {
	public class LookAround: MonoBehaviour {

		public Transform trans;
		public float maxX = 90f;
		public Vector2 lookRotation;

		public float sensitivity = 1f;


		void Start() {
			if (!trans)
				trans = transform;

			lookRotation = trans.rotation.eulerAngles;

		}

		void Update() {
			if (Time.timeScale > 0f && !Input.GetKey(KeyCode.LeftAlt)) {
				// Uses old input system.
				float yrot = Input.GetAxisRaw("Mouse X") * sensitivity;
				float xrot = Input.GetAxisRaw("Mouse Y") * sensitivity;

				lookRotation.y += yrot;
				lookRotation.x -= xrot;

				lookRotation.x = Mathf.Clamp(lookRotation.x, -maxX, maxX);

				// Makes sure the controller's direction is aligned with the view.
				trans.localRotation = Quaternion.Euler(lookRotation.x, lookRotation.y, 0);

				Cursor.lockState = CursorLockMode.Locked;

			} else
				Cursor.lockState = CursorLockMode.None;

		}


	}
	
}