#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SeamlessWormhole.Editor
{
    /// <summary>
    /// 학생이 재사용 프리팹을 쉽게 연결할 수 있도록 단순한 Inspector를 제공합니다.
    /// </summary>
    [CustomEditor(typeof(SphericalWormholePortal))]
    internal sealed class SphericalWormholePortalEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "Destination Portal is the one source of truth: it decides both what this lens shows and where the player exits. No layer setup is required.",
                MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("destinationPortal"));

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Connect Both Ways"))
            {
                SphericalWormholePortal thisPortal = (SphericalWormholePortal)target;
                SerializedProperty destination = serializedObject.FindProperty("destinationPortal");
                SphericalWormholePortal other = destination.objectReferenceValue as SphericalWormholePortal;
                if (other == null)
                {
                    EditorUtility.DisplayDialog("Wormhole link", "First assign Destination Portal, then press this button.", "OK");
                }
                else
                {
                    Undo.RecordObjects(new Object[] { thisPortal, other }, "Connect wormholes both ways");
                    thisPortal.LinkTo(other);
                    other.LinkTo(thisPortal);
                    EditorUtility.SetDirty(thisPortal);
                    EditorUtility.SetDirty(other);
                }
            }

            EditorGUILayout.HelpBox(
                "Prefab use: drag two Wormhole Portal prefabs into the scene. Assign B to A's Destination Portal, then click Connect Both Ways once.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
