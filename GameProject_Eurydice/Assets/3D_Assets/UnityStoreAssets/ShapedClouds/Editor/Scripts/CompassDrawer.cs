using UnityEditor;
using UnityEngine;

namespace ShapedClouds.Editor {

	/// <summary>
	/// Used to draw the compass for easier wind direction control.
	/// </summary>
	[CustomPropertyDrawer(typeof(CompassAttribute))]
	public class CompassDrawer: PropertyDrawer {
		public Texture rose;
		public Texture needle;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			return 48;

		}

		public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label) {
			var rect = EditorGUI.PrefixLabel(position, label);

			rect.height = 48;
			var controlRect = rect;
			controlRect.width = 48;

			var fieldRect = rect;
			fieldRect.x += 52;
			fieldRect.width = rect.xMax - fieldRect.x;
			fieldRect.height = EditorGUIUtility.singleLineHeight;

			Vector2 direction = prop.vector2Value.normalized;
			float angle = Vector2.SignedAngle(direction, Vector2.down) + 180;
			float magnitude = prop.vector2Value.magnitude;

			angle = DrawControl(controlRect, angle);

			float lw = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 70;

			angle = EditorGUI.Slider(fieldRect, "Angle", angle, 0, 360f);

			fieldRect.y += EditorGUIUtility.singleLineHeight + 2;

			magnitude = EditorGUI.FloatField(fieldRect, "Magnitude", magnitude);

			EditorGUIUtility.labelWidth = lw;

			angle *= Mathf.Deg2Rad;
			direction.x = Mathf.Sin(angle);
			direction.y = Mathf.Cos(angle);

			if (prop.vector2Value != direction * magnitude) {
				prop.vector2Value = direction * magnitude;
				prop.serializedObject.ApplyModifiedProperties();

			}

		}

		private float DrawControl(Rect rect, float previousValue) {
			if (rose == null)
				rose = Resources.Load<Texture>("Shaped Clouds/Textures/ShapedCloudsEditor_Compass_Rose");
			if (needle == null)
				needle = Resources.Load<Texture>("Shaped Clouds/Textures/ShapedCloudsEditor_Compass_Needle");

			int id = GUIUtility.GetControlID(FocusType.Passive, rect);
			float value = previousValue;

			if (Event.current != null) {
				Vector2 v;
				switch (Event.current.type) {
					case EventType.MouseDown:
						if (!rect.Contains(Event.current.mousePosition))
							break;

						GUIUtility.hotControl = id;

						v = (Event.current.mousePosition - rect.center).normalized * new Vector2(1, -1);
						value = Vector2.SignedAngle(v, Vector2.down) + 180;

						if (value != previousValue)
							GUI.changed = true;

						break;

					case EventType.MouseUp:
						if (GUIUtility.hotControl != id)
							break;

						GUIUtility.hotControl = 0;
						break;

					case EventType.MouseDrag:
						if (GUIUtility.hotControl != id)
							break;

						v = (Event.current.mousePosition - rect.center).normalized * new Vector2(1, -1);
						value = Vector2.SignedAngle(v, Vector2.down) + 180;

						if (value != previousValue)
							GUI.changed = true;
						break;

				}

			}

			Matrix4x4 m = GUI.matrix;
			GUI.DrawTexture(rect, rose);

			GUIUtility.RotateAroundPivot(value, rect.center);
			GUI.DrawTexture(rect, needle);
			GUI.matrix = m;

			return value;

		}

	}

}