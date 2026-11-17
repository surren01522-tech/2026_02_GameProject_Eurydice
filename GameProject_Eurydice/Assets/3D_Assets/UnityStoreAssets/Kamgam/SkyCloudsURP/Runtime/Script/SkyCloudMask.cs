using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Kamgam.SkyClouds
{
    [ExecuteAlways]
    public class SkyCloudMask : MonoBehaviour
    {
        [SerializeField]
        protected int _maskIndex = 1;
        protected string _maskShaderPropertyName = "_Mask1";

        public int MaskIndex
        {
            get => _maskIndex;
            set
            {
                if (value != _maskIndex)
                {
                    _maskIndex = value;
                    _maskShaderPropertyName = "_Mask" + _maskIndex;
                }
            }
        }

        public List<Material> SkyCouldMaterials = new List<Material>();
        public float Padding = 0.2f;

        public void Update()
        {
            var pos = transform.position;
            var radius = transform.localScale.x * 0.5f + Padding;

            setMask(pos, radius);
        }

        private void setMask(Vector3 pos, float radius)
        {
            if (SkyCouldMaterials != null)
            {
                foreach (var material in SkyCouldMaterials)
                {
                    if (material == null)
                        continue;

                    var sphereMask = new Vector4(
                        pos.x,
                        pos.y,
                        pos.z,
                        radius
                    );
                    material.SetVector(_maskShaderPropertyName, sphereMask);
                }
            }
        }

        public void OnDisable()
        {
            setMask(new Vector3(0, -999, 0), 0.001f);
        }

#if UNITY_EDITOR
        public void Reset()
        {
            SkyCouldMaterials.Clear();

            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in materialGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material.shader.name != "SkyClouds")
                    continue;

                if (material != null && material.name.StartsWith("SkyClouds"))
                {
                    SkyCouldMaterials.Add(material);
                }
            }

            EditorUtility.SetDirty(this);
        }

        public void OnValidate()
        {
            _maskShaderPropertyName = "_Mask" + _maskIndex;
        }
#endif
    }
}
