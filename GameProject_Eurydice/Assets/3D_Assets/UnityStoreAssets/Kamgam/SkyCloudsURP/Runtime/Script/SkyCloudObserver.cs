using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Kamgam.SkyClouds
{
    /// <summary>
    /// The sky cloud observer implements two things:<br />
    /// * A mask (if available)<br />
    /// * Fading (if available)
    /// </summary>
    public class SkyCloudObserver : MonoBehaviour
    {
        public float FadeStartDistance = 2f;
        public float FadeEndDistance = 0.5f;

        protected SkyCloudMask _mask;
        public SkyCloudMask Mask
        {
            get
            {
                if (_mask == null)
                {
                    _mask = this.GetComponentInChildren<SkyCloudMask>(includeInactive: true);
                }
                return _mask;
            }
        }

        public float MaskScale
        {
            get => (Mask == null) ? 1f : Mask.transform.localScale.x;

            set
            {
                if (Mask != null)
                    Mask.transform.localScale = Vector3.one * value;
            }
        }

        public float MaskPadding
        {
            get => (Mask == null) ? 0f : Mask.Padding;

            set
            {
                if (Mask != null)
                    Mask.Padding = value;
            }
        }

        public Camera Camera
        {
            get => transform.GetComponentInParent<Camera>();
        }

        public Material FadeMaterial;

        [System.NonSerialized]
        private Vector3 _closestPosition;

        [System.NonSerialized]
        private SkyCloud _closestCloud;

        public void Start()
        {
            if (Mask != null)
            {
                Mask.Padding = Mask.Padding;
            }
        }

        public void Update()
        {
            // Cals distance and find closest cloud.
            float distance = SkyCloud.GetMinDistance(transform.position, out _closestPosition, out _closestCloud, distanceLimit: 10f);

            // Update properties based on closest cloud.
            if(_closestCloud != null && _closestCloud.gameObject != null)
            {
                FadeStartDistance = _closestCloud.ObserverFadeStartDistance;
                FadeEndDistance = _closestCloud.ObserverFadeEndDistance;

                if (_mask != null)
                {
                    MaskScale = _closestCloud.ObserverMaskScale;
                    MaskPadding = _closestCloud.ObserverMaskPadding;

                    // Handle dynamic cloud materials
                    var material = _closestCloud.CloudMaterial;
                    if (material != null && !Mask.SkyCouldMaterials.Contains(_closestCloud.CloudMaterial))
                    {
                        Mask.SkyCouldMaterials.Add(_closestCloud.CloudMaterial);
                    }
                }
            }

            // Update fade
            float alpha = Mathf.Clamp01((distance - FadeEndDistance) / FadeStartDistance);
            if (_closestCloud != null && _closestCloud.gameObject != null)
            {
                FadeMaterial.color = new Color(
                    _closestCloud.ObserverFadeColor.r,
                    _closestCloud.ObserverFadeColor.g,
                    _closestCloud.ObserverFadeColor.b,
                    1f - alpha);
            }
            else
            {
                FadeMaterial.color = new Color(1f, 1f, 1f, 1f - alpha);
            }

#if UNITY_EDITOR
            registerPlayModeCallback();
#endif
        }

#if UNITY_EDITOR

        private static bool registeredPlayModeCallback = false;

        private void registerPlayModeCallback()
        {
            if (!registeredPlayModeCallback)
            {
                registeredPlayModeCallback = true;
                EditorApplication.playModeStateChanged += onPlayModeChanged;
            }
        }

        private void onPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode || change == PlayModeStateChange.EnteredPlayMode)
            {
                // Init alpha with 0
                var col = FadeMaterial.color;
                col.a = 0f;
                FadeMaterial.color = col;
            }
        }

        //private void OnDrawGizmos()
        //{
        //    Gizmos.DrawSphere(_closestPosition, 0.1f);
        //}

        public void Reset()
        {
            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material SkyCloudIntersectionFade");
            if (materialGUIDs.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(materialGUIDs[0]);

                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && material.name.StartsWith("SkyCloud"))
                {
                    FadeMaterial = material;
                    // Init alpha with 0
                    var col = FadeMaterial.color;
                    col.a = 0f;
                    FadeMaterial.color = col;
                }

                EditorUtility.SetDirty(this);
            }
        }
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(SkyCloudObserver))]
    public class SkyCloudObserverEditor : UnityEditor.Editor
    {
        SkyCloudObserver obj;

        public void OnEnable()
        {
            obj = target as SkyCloudObserver;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Add IntersectionFade"))
            {
                var fade = obj.transform.Find("SkyClouds_IntersectionFade");
                if (fade == null)
                {
                    string[] fadeGUIDs = AssetDatabase.FindAssets("t:Prefab SkyClouds_IntersectionFade");
                    if (fadeGUIDs.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(fadeGUIDs[0]);
                        var fadePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        var fadeGO = PrefabUtility.InstantiatePrefab(fadePrefab, obj.transform) as GameObject;
                        fadeGO.transform.localPosition = new Vector3(0, 0, obj.Camera.nearClipPlane + 0.0001f);

                        EditorUtility.SetDirty(obj);
                    }
                }
            }

            if (GUILayout.Button("Add Mask"))
            {
                var mask = obj.transform.Find("SkyClouds_Mask0");
                if (mask == null)
                {
                    string[] maskGUIDs = AssetDatabase.FindAssets("t:Prefab SkyClouds_Mask0");
                    if (maskGUIDs.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(maskGUIDs[0]);
                        var maskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        var maskGO = PrefabUtility.InstantiatePrefab(maskPrefab, obj.transform) as GameObject;
                        maskGO.transform.localPosition = new Vector3(0, 0, 0);

                        maskGO.GetComponent<SkyCloudMask>().Reset();

                        EditorUtility.SetDirty(obj);
                    }
                }
            }
        }
    }
#endif
}
