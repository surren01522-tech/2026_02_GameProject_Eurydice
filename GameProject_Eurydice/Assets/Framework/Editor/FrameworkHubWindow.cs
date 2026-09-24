using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using GameFramework.Data;
using GameFramework.Gameplay;

namespace GameFramework.EditorTools
{
    /// <summary>
    /// 프레임워크 통합 관리 창.
    /// Tools > GameFramework > Framework Hub
    /// 탭: 업적 / 사운드 / 이펙트 / 세이브
    /// </summary>
    public class FrameworkHubWindow : EditorWindow
    {
        private enum Tab { Achievement, Sound, Effect, Save }
        private Tab _tab;
        private Vector2 _scroll;

        private AchievementDatabase _achvDb;
        private SoundLibrary _soundLib;
        private EffectLibrary _effectLib;

        private string _saveJsonPreview;
        private string _savePreviewPath;

        [MenuItem("Tools/GameFramework/Framework Hub %#g")] // Ctrl+Shift+G
        public static void Open()
        {
            var w = GetWindow<FrameworkHubWindow>("Framework Hub");
            w.minSize = new Vector2(560, 420);
        }

        private void OnEnable() => LoadAssets();

        private void LoadAssets()
        {
            _achvDb    = Resources.Load<AchievementDatabase>("AchievementDatabase");
            _soundLib  = Resources.Load<SoundLibrary>("SoundLibrary");
            _effectLib = Resources.Load<EffectLibrary>("EffectLibrary");
        }

        private void OnGUI()
        {
            DrawSetupBar();
            _tab = (Tab)GUILayout.Toolbar((int)_tab,
                new[] { "업적", "사운드", "이펙트", "세이브" }, GUILayout.Height(28));
            EditorGUILayout.Space(6);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case Tab.Achievement: DrawAchievements(); break;
                case Tab.Sound:       DrawSounds();       break;
                case Tab.Effect:      DrawEffects();      break;
                case Tab.Save:        DrawSaves();        break;
            }
            EditorGUILayout.EndScrollView();
        }

        // ===================== 셋업 바 =====================

        private void DrawSetupBar()
        {
            bool missing = _achvDb == null || _soundLib == null || _effectLib == null
                           || Resources.Load<ItemDatabase>("ItemDatabase") == null;
            if (!missing) return;

            EditorGUILayout.HelpBox("Resources 폴더에 라이브러리 에셋이 없습니다.", MessageType.Warning);
            if (GUILayout.Button("라이브러리 에셋 4종 자동 생성 (Resources/)", GUILayout.Height(26)))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                CreateIfMissing<SoundLibrary>("Assets/Resources/SoundLibrary.asset");
                CreateIfMissing<EffectLibrary>("Assets/Resources/EffectLibrary.asset");
                CreateIfMissing<ItemDatabase>("Assets/Resources/ItemDatabase.asset");
                CreateIfMissing<AchievementDatabase>("Assets/Resources/AchievementDatabase.asset");
                AssetDatabase.SaveAssets();
                LoadAssets();
            }
            EditorGUILayout.Space(4);
        }

        private static void CreateIfMissing<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        }

        // ===================== 업적 탭 =====================

        private void DrawAchievements()
        {
            if (_achvDb == null) { EditorGUILayout.HelpBox("AchievementDatabase 없음", MessageType.Info); return; }

            EditorGUILayout.LabelField($"업적 {_achvDb.achievements.Count}개", EditorStyles.boldLabel);

            for (int i = 0; i < _achvDb.achievements.Count; i++)
            {
                var a = _achvDb.achievements[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                _achvDb.achievements[i] = (AchievementData)EditorGUILayout.ObjectField(a, typeof(AchievementData), false);
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    Undo.RecordObject(_achvDb, "Remove Achievement");
                    _achvDb.achievements.RemoveAt(i);
                    EditorUtility.SetDirty(_achvDb);
                    EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (a != null)
                {
                    EditorGUILayout.LabelField(
                        $"  {a.displayName}  ·  키:{a.eventKey}" +
                        (string.IsNullOrEmpty(a.paramFilter) ? "" : $"({a.paramFilter})") +
                        $"  ·  목표 {a.targetCount}", EditorStyles.miniLabel);

                    // 플레이 모드: 실시간 진행도
                    if (Application.isPlaying && AchievementManager.HasInstance)
                    {
                        var (cur, target) = AchievementManager.Instance.GetProgressInfo(a.id);
                        bool done = AchievementManager.Instance.IsUnlocked(a.id);
                        var r = EditorGUILayout.GetControlRect(false, 16);
                        EditorGUI.ProgressBar(r, target > 0 ? (float)cur / target : 0f,
                            done ? "달성!" : $"{cur} / {target}");
                    }
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);
            if (GUILayout.Button("+ 새 업적 에셋 생성", GUILayout.Height(24)))
            {
                var asset = ScriptableObject.CreateInstance<AchievementData>();
                asset.id = $"achv_{_achvDb.achievements.Count + 1}";
                asset.displayName = "새 업적";
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/Achv_New.asset");
                AssetDatabase.CreateAsset(asset, path);
                Undo.RecordObject(_achvDb, "Add Achievement");
                _achvDb.achievements.Add(asset);
                EditorUtility.SetDirty(_achvDb);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(asset);
            }

            if (Application.isPlaying) Repaint(); // 진행도 실시간 갱신
        }

        // ===================== 사운드 탭 =====================

        private void DrawSounds()
        {
            if (_soundLib == null) { EditorGUILayout.HelpBox("SoundLibrary 없음", MessageType.Info); return; }

            EditorGUILayout.LabelField($"사운드 {_soundLib.sounds.Count}개", EditorStyles.boldLabel);

            for (int i = 0; i < _soundLib.sounds.Count; i++)
            {
                var s = _soundLib.sounds[i];
                EditorGUILayout.BeginHorizontal("box");

                s.id = EditorGUILayout.TextField(s.id, GUILayout.Width(130));
                s.clip = (AudioClip)EditorGUILayout.ObjectField(s.clip, typeof(AudioClip), false);
                s.volume = EditorGUILayout.Slider(s.volume, 0f, 1f, GUILayout.Width(110));

                using (new EditorGUI.DisabledScope(s.clip == null))
                {
                    if (GUILayout.Button("▶", GUILayout.Width(28))) PreviewClip(s.clip);
                }
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    Undo.RecordObject(_soundLib, "Remove Sound");
                    _soundLib.sounds.RemoveAt(i);
                    EditorUtility.SetDirty(_soundLib);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 항목 추가"))
            {
                Undo.RecordObject(_soundLib, "Add Sound");
                _soundLib.sounds.Add(new SoundEntry { id = $"sfx_{_soundLib.sounds.Count + 1}" });
                EditorUtility.SetDirty(_soundLib);
            }
            if (GUILayout.Button("■ 미리듣기 정지", GUILayout.Width(120))) StopPreview();
            EditorGUILayout.EndHorizontal();

            if (GUI.changed && _soundLib != null) EditorUtility.SetDirty(_soundLib);
        }

        // 에디터 전용 오디오 미리듣기 (내부 AudioUtil 리플렉션)
        private static void PreviewClip(AudioClip clip)
        {
            var util = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var m = util?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            if (m != null) m.Invoke(null, new object[] { clip, 0, false });
            else Debug.LogWarning("[Hub] 이 에디터 버전에서 미리듣기 API를 찾지 못했습니다.");
        }

        private static void StopPreview()
        {
            var util = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var m = util?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
            m?.Invoke(null, null);
        }

        // ===================== 이펙트 탭 =====================

        private void DrawEffects()
        {
            if (_effectLib == null) { EditorGUILayout.HelpBox("EffectLibrary 없음", MessageType.Info); return; }

            EditorGUILayout.LabelField($"이펙트 {_effectLib.effects.Count}개", EditorStyles.boldLabel);
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("미리보기(▶)는 플레이 모드에서 동작합니다 (원점에 재생).", MessageType.None);

            for (int i = 0; i < _effectLib.effects.Count; i++)
            {
                var e = _effectLib.effects[i];
                EditorGUILayout.BeginHorizontal("box");

                e.id = EditorGUILayout.TextField(e.id, GUILayout.Width(130));
                e.prefab = (GameObject)EditorGUILayout.ObjectField(e.prefab, typeof(GameObject), false);
                EditorGUILayout.LabelField("수명", GUILayout.Width(30));
                e.lifetime = EditorGUILayout.FloatField(e.lifetime, GUILayout.Width(46));

                using (new EditorGUI.DisabledScope(!Application.isPlaying || e.prefab == null))
                {
                    if (GUILayout.Button("▶", GUILayout.Width(28)))
                        Services.EffectManager.Instance.Play(e.id, Vector3.zero);
                }
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    Undo.RecordObject(_effectLib, "Remove Effect");
                    _effectLib.effects.RemoveAt(i);
                    EditorUtility.SetDirty(_effectLib);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ 항목 추가"))
            {
                Undo.RecordObject(_effectLib, "Add Effect");
                _effectLib.effects.Add(new EffectEntry { id = $"fx_{_effectLib.effects.Count + 1}", lifetime = -1f });
                EditorUtility.SetDirty(_effectLib);
            }

            if (GUI.changed && _effectLib != null) EditorUtility.SetDirty(_effectLib);
        }

        // ===================== 세이브 탭 =====================

        private void DrawSaves()
        {
            string dir = Application.persistentDataPath;
            var files = Directory.Exists(dir) ? Directory.GetFiles(dir, "save_*.dat") : Array.Empty<string>();

            EditorGUILayout.LabelField($"세이브 파일 {files.Length}개", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(dir, EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("폴더 열기", GUILayout.Width(100))) EditorUtility.RevealInFinder(dir);
            if (Application.isPlaying && GUILayout.Button("지금 저장 (슬롯 0)", GUILayout.Width(140)))
                Services.SaveManager.Instance.SaveGame(0);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            foreach (var f in files)
            {
                var info = new FileInfo(f);
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField($"{Path.GetFileName(f)}   {info.Length:N0} bytes   {info.LastWriteTime:yyyy-MM-dd HH:mm}");
                if (GUILayout.Button("보기", GUILayout.Width(50)))
                {
                    _savePreviewPath = f;
                    _saveJsonPreview = LoadPretty(f);
                }
                if (GUILayout.Button("삭제", GUILayout.Width(50)) &&
                    EditorUtility.DisplayDialog("세이브 삭제", $"{Path.GetFileName(f)} 를 삭제할까요?", "삭제", "취소"))
                {
                    File.Delete(f);
                    if (_savePreviewPath == f) _saveJsonPreview = null;
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(_saveJsonPreview))
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField($"미리보기: {Path.GetFileName(_savePreviewPath)}", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(_saveJsonPreview, GUILayout.ExpandHeight(true));
            }
        }

        private static string LoadPretty(string path)
        {
            string raw = File.ReadAllText(path);
            if (!raw.TrimStart().StartsWith("{"))
                return "(난독화된 파일입니다 — SaveManager의 useObfuscation을 끄고 저장하면 여기서 볼 수 있습니다)";
            return PrettyJson(raw);
        }

        // 의존성 없는 간단 JSON 들여쓰기 (내부 이스케이프된 json 문자열도 보기 좋게)
        private static string PrettyJson(string json)
        {
            var sb = new StringBuilder();
            int indent = 0; bool inStr = false;
            foreach (char c in json)
            {
                if (c == '"' ) { inStr = !inStr; sb.Append(c); continue; }
                if (inStr) { sb.Append(c); continue; }
                switch (c)
                {
                    case '{': case '[':
                        sb.Append(c); sb.Append('\n'); sb.Append(new string(' ', ++indent * 2)); break;
                    case '}': case ']':
                        sb.Append('\n'); sb.Append(new string(' ', --indent * 2)); sb.Append(c); break;
                    case ',':
                        sb.Append(c); sb.Append('\n'); sb.Append(new string(' ', indent * 2)); break;
                    case ':':
                        sb.Append(": "); break;
                    default:
                        sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}
