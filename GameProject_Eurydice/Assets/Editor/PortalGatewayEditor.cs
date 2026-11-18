#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PortalGateway))]
public class PortalGatewayEditor : FoldGroupMonoBehaviourEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var portal = (PortalGateway)target;
        if (portal == null || portal.ShapeMode != PortalShapeMode.Custom) return;

        EditorGUILayout.Space(6f);
        int count = portal.CustomShapePoints != null ? portal.CustomShapePoints.Count : 0;
        EditorGUILayout.HelpBox($"커스텀 모양 편집 ({count}/{PortalGateway.MaxCustomShapePoints} 꼭짓점)\n씬 뷰: 꼭짓점 드래그(이동) | 변 중앙 녹색 점(추가) | Shift+클릭(삭제)", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("사각형 (4점)")) ApplyPreset(portal, 0);
        if (GUILayout.Button("원형 (8점)")) ApplyPreset(portal, 1);
        if (GUILayout.Button("아치형 (10점)")) ApplyPreset(portal, 2);
        EditorGUILayout.EndHorizontal();
    }

    private static void ApplyPreset(PortalGateway portal, int presetIndex)
    {
        Undo.RecordObject(portal, "Apply Portal Shape Preset");
        portal.ApplyCustomPreset(presetIndex);
        EditorUtility.SetDirty(portal);
        SceneView.RepaintAll();
    }

    private void OnSceneGUI()
    {
        var portal = (PortalGateway)target;
        if (portal == null || portal.ShapeMode != PortalShapeMode.Custom) return;

        serializedObject.Update();
        SerializedProperty pointsProp = serializedObject.FindProperty("customShapePoints");
        if (pointsProp == null || pointsProp.arraySize < 3) return;

        int count = Mathf.Min(pointsProp.arraySize, PortalGateway.MaxCustomShapePoints);
        Transform portalT = portal.transform;
        Vector3[] controlLoop = new Vector3[count + 1];
        for (int i = 0; i < count; i++)
        {
            Vector2 p = pointsProp.GetArrayElementAtIndex(i).vector2Value;
            controlLoop[i] = portalT.TransformPoint(new Vector3(p.x, p.y, 0f));
        }
        controlLoop[count] = controlLoop[0];

        var evalPts = portal.EvaluatedShapePoints;
        if (evalPts != null && evalPts.Count >= 3)
        {
            Handles.color = new Color(1f, 1f, 1f, 0.25f);
            Handles.DrawAAPolyLine(1.5f, controlLoop);

            Vector3[] smoothLoop = new Vector3[evalPts.Count + 1];
            for (int i = 0; i < evalPts.Count; i++)
                smoothLoop[i] = portalT.TransformPoint(new Vector3(evalPts[i].x, evalPts[i].y, 0f));
            smoothLoop[evalPts.Count] = smoothLoop[0];

            Handles.color = new Color(0.2f, 0.9f, 1f, 0.95f);
            Handles.DrawAAPolyLine(3f, smoothLoop);
        }
        else
        {
            Handles.color = new Color(0.2f, 0.9f, 1f, 0.95f);
            Handles.DrawAAPolyLine(3f, controlLoop);
        }

        bool deleteMode = Event.current.shift && count > 3;

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPt = controlLoop[i];
            float size = HandleUtility.GetHandleSize(worldPt) * 0.08f;

            if (deleteMode)
            {
                Handles.color = new Color(1f, 0.35f, 0.35f, 1f);
                if (Handles.Button(worldPt, Quaternion.identity, size, size * 1.25f, Handles.DotHandleCap))
                {
                    pointsProp.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    portal.RebuildCustomShapeMetrics();
                    portal.RefreshVisuals();
                    EditorUtility.SetDirty(portal);
                    return;
                }
            }
            else
            {
                Handles.color = Color.white;
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.Slider2D(worldPt, portalT.forward, portalT.right, portalT.up, size, Handles.DotHandleCap, 0f);
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 local = portalT.InverseTransformPoint(moved);
                    pointsProp.GetArrayElementAtIndex(i).vector2Value = new Vector2(local.x, local.y);
                    serializedObject.ApplyModifiedProperties();
                    portal.RebuildCustomShapeMetrics();
                    portal.RefreshVisuals();
                    EditorUtility.SetDirty(portal);
                    return;
                }
            }
        }

        if (!deleteMode && count < PortalGateway.MaxCustomShapePoints)
        {
            Handles.color = new Color(0.4f, 1f, 0.6f, 0.9f);
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                Vector3 worldMid = (controlLoop[i] + controlLoop[next]) * 0.5f;
                float midSize = HandleUtility.GetHandleSize(worldMid) * 0.055f;
                if (Handles.Button(worldMid, Quaternion.identity, midSize, midSize * 1.3f, Handles.DotHandleCap))
                {
                    Vector2 pA = pointsProp.GetArrayElementAtIndex(i).vector2Value;
                    Vector2 pB = pointsProp.GetArrayElementAtIndex(next).vector2Value;
                    int insertIdx = i + 1;
                    pointsProp.InsertArrayElementAtIndex(insertIdx);
                    pointsProp.GetArrayElementAtIndex(insertIdx).vector2Value = (pA + pB) * 0.5f;
                    serializedObject.ApplyModifiedProperties();
                    portal.RebuildCustomShapeMetrics();
                    portal.RefreshVisuals();
                    EditorUtility.SetDirty(portal);
                    return;
                }
            }
        }
    }
}
#endif
