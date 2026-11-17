using System;
using UnityEngine;

namespace ShapedClouds {
	/// <summary>
	/// Makes a Vector2 field look like a compass in the inspector window.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class CompassAttribute: PropertyAttribute {

	}

}