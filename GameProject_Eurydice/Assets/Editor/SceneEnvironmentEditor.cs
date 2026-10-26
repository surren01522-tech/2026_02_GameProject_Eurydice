#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(SceneEnvironment))]
public class SceneEnvironmentEditor : FoldGroupMonoBehaviourEditor
{
    public override void OnInspectorGUI()
    {
        var env = (SceneEnvironment)target;

        EditorGUILayout.Space(4f);
        if (!env.IsCaptured)
        {
            EditorGUILayout.HelpBox("아직 환경 데이터가 캡처되지 않았습니다. 아래 버튼으로 현재 씬의 RenderSettings, SH 환경광, 조명, 볼륨을 수집하세요.", MessageType.Warning);
        }

        GUI.enabled = !Application.isPlaying;
        if (GUILayout.Button("현재 씬 환경 캡처 (Capture Scene Environment)", GUILayout.Height(28f)))
        {
            CaptureSingleEnvironment(env);
        }
        GUI.enabled = true;

        EditorGUILayout.Space(4f);
        base.OnInspectorGUI();
    }

    private static void CaptureSingleEnvironment(SceneEnvironment env)
    {
        if (env == null) return;
        Scene targetScene = env.gameObject.scene;
        if (!targetScene.IsValid() || !targetScene.isLoaded) return;

        Scene prevActive = SceneManager.GetActiveScene();
        bool switched = false;
        if (prevActive != targetScene)
        {
            SceneManager.SetActiveScene(targetScene);
            switched = true;
        }

        try
        {
            Undo.RecordObject(env, "Capture Scene Environment");
            env.CaptureFromActiveSceneInEditor();
            Debug.Log("[SceneEnvironmentEditor] 환경 캡처 완료: " + targetScene.name);
        }
        finally
        {
            if (switched && prevActive.IsValid() && prevActive.isLoaded)
                SceneManager.SetActiveScene(prevActive);
        }
    }

    [MenuItem("Tools/Environment/현재 열린 씬들에 SceneEnvironment 구성 및 캡처")]
    public static void SetupAndCaptureOpenScenes()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[SceneEnvironmentEditor] 플레이 모드에서는 실행할 수 없습니다.");
            return;
        }

        Scene prevActive = SceneManager.GetActiveScene();
        int count = SceneManager.sceneCount;
        int capturedCount = 0;

        try
        {
            for (int i = 0; i < count; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (!s.IsValid() || !s.isLoaded) continue;

                SceneManager.SetActiveScene(s);
                SceneEnvironment env = FindOrCreateInScene(s);
                env.CaptureFromActiveSceneInEditor();
                capturedCount++;
            }
        }
        finally
        {
            if (prevActive.IsValid() && prevActive.isLoaded)
                SceneManager.SetActiveScene(prevActive);
        }

        Debug.Log("[SceneEnvironmentEditor] 열린 씬 일괄 캡처 완료: " + capturedCount + "개");
    }

    [MenuItem("Tools/Environment/심리스 씬 전체 일괄 캡처 및 저장")]
    public static void SetupAndSaveAllSeamlessScenes()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[SceneEnvironmentEditor] 플레이 모드에서는 실행할 수 없습니다.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

        var scenePaths = new List<string>
        {
            "Assets/Scenes/Chapter_0.unity",
            "Assets/Scenes/Play_test.unity"
        };

        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Seamless_Scenes" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.IsNullOrEmpty(path) && !scenePaths.Contains(path))
                scenePaths.Add(path);
        }

        int processed = 0;
        for (int i = 0; i < scenePaths.Count; i++)
        {
            string path = scenePaths[i];
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;

            Scene opened = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            SceneManager.SetActiveScene(opened);
            SceneEnvironment env = FindOrCreateInScene(opened);
            env.CaptureFromActiveSceneInEditor();
            EditorSceneManager.SaveScene(opened);
            processed++;
        }

        if (previousSetup != null && previousSetup.Length > 0)
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);

        Debug.Log("[SceneEnvironmentEditor] 심리스 씬 전체 캡처 및 저장 완료: " + processed + "개");
    }

    private static SceneEnvironment FindOrCreateInScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var existing = roots[i].GetComponentInChildren<SceneEnvironment>(true);
            if (existing != null) return existing;
        }

        var go = new GameObject("SceneEnvironment");
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create SceneEnvironment");
        return go.AddComponent<SceneEnvironment>();
    }
}
#endif
