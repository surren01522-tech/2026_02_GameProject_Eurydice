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
    /// </summary>
    public class FrameworkHubWindow : EditorWindow
    {
        private enum Tab
        {
            Item,
            Achievement,
            Augment,
            Trait,
            Sound,
            Effect,
            Save
        }

        private Tab _tab;
        private Vector2 _scroll;

        private ItemDatabase _itemDb;
        private AchievementDatabase _achievementDb;
        private AugmentDatabase _augmentDb;
        private TraitDatabase _traitDb;
        private SoundLibrary _soundLib;
        private EffectLibrary _effectLib;

        private string _saveJsonPreview;
        private string _savePreviewPath;

        [MenuItem("Tools/GameFramework/Framework Hub %#g")]
        public static void Open()
        {
            var window = GetWindow<FrameworkHubWindow>("Framework Hub");
            window.minSize = new Vector2(660, 460);
        }

        private void OnEnable() => LoadAssets();

        private void LoadAssets()
        {
            _itemDb = Resources.Load<ItemDatabase>("ItemDatabase");
            _achievementDb = Resources.Load<AchievementDatabase>("AchievementDatabase");
            _augmentDb = Resources.Load<AugmentDatabase>("AugmentDatabase");
            _traitDb = Resources.Load<TraitDatabase>("TraitDatabase");
            _soundLib = Resources.Load<SoundLibrary>("SoundLibrary");
            _effectLib = Resources.Load<EffectLibrary>("EffectLibrary");
        }

        private void OnGUI()
        {
            DrawSetupBar();
            _tab = (Tab)GUILayout.Toolbar(
                (int)_tab,
                new[] { "아이템", "업적", "증강", "특성", "사운드", "이펙트", "세이브" },
                GUILayout.Height(28));

            EditorGUILayout.Space(6);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case Tab.Item: DrawItems(); break;
                case Tab.Achievement: DrawAchievements(); break;
                case Tab.Augment: DrawAugments(); break;
                case Tab.Trait: DrawTraits(); break;
                case Tab.Sound: DrawSounds(); break;
                case Tab.Effect: DrawEffects(); break;
                case Tab.Save: DrawSaves(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSetupBar()
        {
            bool missing = _itemDb == null
                           || _achievementDb == null
                           || _augmentDb == null
                           || _traitDb == null
                           || _soundLib == null
                           || _effectLib == null;
            if (!missing)
                return;

            EditorGUILayout.HelpBox("Resources 폴더에 기본 데이터베이스/라이브러리 에셋이 없습니다.", MessageType.Warning);
            if (GUILayout.Button("기본 에셋 6종 자동 생성 (Resources/)", GUILayout.Height(26)))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                CreateIfMissing<ItemDatabase>("Assets/Resources/ItemDatabase.asset");
                CreateIfMissing<AchievementDatabase>("Assets/Resources/AchievementDatabase.asset");
                CreateIfMissing<AugmentDatabase>("Assets/Resources/AugmentDatabase.asset");
                CreateIfMissing<TraitDatabase>("Assets/Resources/TraitDatabase.asset");
                CreateIfMissing<SoundLibrary>("Assets/Resources/SoundLibrary.asset");
                CreateIfMissing<EffectLibrary>("Assets/Resources/EffectLibrary.asset");

                AssetDatabase.SaveAssets();
                LoadAssets();
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("UI 프리팹 6종 생성", GUILayout.Height(24)))
                UITemplateGenerator.CreateAllUI();
            if (GUILayout.Button("증강 팝업만 생성", GUILayout.Height(24)))
                UITemplateGenerator.CreateAugmentChoicePopup();
            if (GUILayout.Button("특성 팝업만 생성", GUILayout.Height(24)))
                UITemplateGenerator.CreateTraitPopup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
        }

        private static void CreateIfMissing<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        }

        private void DrawItems()
        {
            if (_itemDb == null)
            {
                EditorGUILayout.HelpBox("ItemDatabase 없음", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"아이템 {_itemDb.items.Count}개", EditorStyles.boldLabel);
            for (int i = 0; i < _itemDb.items.Count; i++)
            {
                var item = _itemDb.items[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                _itemDb.items[i] = (ItemData)EditorGUILayout.ObjectField(item, typeof(ItemData), false);
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    Undo.RecordObject(_itemDb, "Remove Item");
                    _itemDb.items.RemoveAt(i);
                    EditorUtility.SetDirty(_itemDb);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                if (item != null)
                    EditorGUILayout.LabelField(
                        $"{item.displayName} · ID:{item.id} · 타입:{item.type} · 최대 스택 {item.maxStack}",
                        EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 새 아이템 에셋 생성", GUILayout.Height(24)))
            {
                var asset = ScriptableObject.CreateInstance<ItemData>();
                asset.id = $"item_{_itemDb.items.Count + 1}";
                asset.displayName = "새 아이템";
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/Item_New.asset");
                AssetDatabase.CreateAsset(asset, path);

                Undo.RecordObject(_itemDb, "Add Item");
                _itemDb.items.Add(asset);
                EditorUtility.SetDirty(_itemDb);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(asset);
            }
        }

        private void DrawAchievements()
        {
            if (_achievementDb == null)
            {
                EditorGUILayout.HelpBox("AchievementDatabase 없음", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"업적 {_achievementDb.achievements.Count}개", EditorStyles.boldLabel);
            for (int i = 0; i < _achievementDb.achievements.Count; i++)
            {
                var achievement = _achievementDb.achievements[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                _achievementDb.achievements[i] = (AchievementData)EditorGUILayout.ObjectField(achievement, typeof(AchievementData), false);
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    Undo.RecordObject(_achievementDb, "Remove Achievement");
                    _achievementDb.achievements.RemoveAt(i);
                    EditorUtility.SetDirty(_achievementDb);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                if (achievement != null)
                {
                    EditorGUILayout.LabelField(
                        $"{achievement.displayName} · 키:{achievement.eventKey} · 목표 {achievement.targetCount}",
                        EditorStyles.miniLabel);

                    if (Application.isPlaying && AchievementManager.HasInstance)
                    {
                        var (cur, target) = AchievementManager.Instance.GetProgressInfo(achievement.id);
                        bool done = AchievementManager.Instance.IsUnlocked(achievement.id);
                        var rect = EditorGUILayout.GetControlRect(false, 16);
                        EditorGUI.ProgressBar(rect, target > 0 ? (float)cur / target : 0f, done ? "달성!" : $"{cur} / {target}");
                    }
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 새 업적 에셋 생성", GUILayout.Height(24)))
            {
                var asset = ScriptableObject.CreateInstance<AchievementData>();
                asset.id = $"achv_{_achievementDb.achievements.Count + 1}";
                asset.displayName = "새 업적";
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/Achv_New.asset");
                AssetDatabase.CreateAsset(asset, path);

                Undo.RecordObject(_achievementDb, "Add Achievement");
                _achievementDb.achievements.Add(asset);
                EditorUtility.SetDirty(_achievementDb);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(asset);
            }

            if (Application.isPlaying)
                Repaint();
        }

        private void DrawAugments()
        {
            if (_augmentDb == null)
            {
                EditorGUILayout.HelpBox("AugmentDatabase 없음", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"증강 {_augmentDb.augments.Count}개", EditorStyles.boldLabel);
            for (int i = 0; i < _augmentDb.augments.Count; i++)
            {
                var augment = _augmentDb.augments[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                _augmentDb.augments[i] = (AugmentData)EditorGUILayout.ObjectField(augment, typeof(AugmentData), false);
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    Undo.RecordObject(_augmentDb, "Remove Augment");
                    _augmentDb.augments.RemoveAt(i);
                    EditorUtility.SetDirty(_augmentDb);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                if (augment != null)
                {
                    EditorGUILayout.LabelField(
                        $"{augment.displayName} · 희귀도:{augment.rarity} · 고유:{augment.unique} · 최대 {augment.maxPickCount}",
                        EditorStyles.miniLabel);
                    DrawModifierPreview(augment.modifiers);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 새 증강 에셋 생성", GUILayout.Height(24)))
            {
                var asset = ScriptableObject.CreateInstance<AugmentData>();
                asset.id = $"augment_{_augmentDb.augments.Count + 1}";
                asset.displayName = "새 증강";
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/Augment_New.asset");
                AssetDatabase.CreateAsset(asset, path);

                Undo.RecordObject(_augmentDb, "Add Augment");
                _augmentDb.augments.Add(asset);
                EditorUtility.SetDirty(_augmentDb);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(asset);
            }
        }

        private void DrawTraits()
        {
            if (_traitDb == null)
            {
                EditorGUILayout.HelpBox("TraitDatabase 없음", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"특성 {_traitDb.traits.Count}개", EditorStyles.boldLabel);
            if (Application.isPlaying && TraitManager.HasInstance)
                EditorGUILayout.LabelField($"현재 포인트: {TraitManager.Instance.AvailablePoints}", EditorStyles.miniBoldLabel);

            for (int i = 0; i < _traitDb.traits.Count; i++)
            {
                var trait = _traitDb.traits[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                _traitDb.traits[i] = (TraitData)EditorGUILayout.ObjectField(trait, typeof(TraitData), false);
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    Undo.RecordObject(_traitDb, "Remove Trait");
                    _traitDb.traits.RemoveAt(i);
                    EditorUtility.SetDirty(_traitDb);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                if (trait != null)
                {
                    string extra = Application.isPlaying && TraitManager.HasInstance
                        ? $" · 현재 랭크 {TraitManager.Instance.GetRank(trait.id)}"
                        : string.Empty;
                    EditorGUILayout.LabelField(
                        $"{trait.displayName} · 비용:{trait.pointCost} · 최대 랭크 {trait.maxRank}{extra}",
                        EditorStyles.miniLabel);
                    DrawModifierPreview(trait.modifiersPerRank);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 새 특성 에셋 생성", GUILayout.Height(24)))
            {
                var asset = ScriptableObject.CreateInstance<TraitData>();
                asset.id = $"trait_{_traitDb.traits.Count + 1}";
                asset.displayName = "새 특성";
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/Trait_New.asset");
                AssetDatabase.CreateAsset(asset, path);

                Undo.RecordObject(_traitDb, "Add Trait");
                _traitDb.traits.Add(asset);
                EditorUtility.SetDirty(_traitDb);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(asset);
            }
        }

        private static void DrawModifierPreview(System.Collections.Generic.List<GameplayModifier> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0)
                return;

            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null)
                    continue;

                EditorGUILayout.LabelField(
                    $"  - {modifier.statKey} {modifier.operation} {modifier.value}",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawSounds()
        {
            if (_soundLib == null)
            {
                EditorGUILayout.HelpBox("SoundLibrary 없음", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"사운드 {_soundLib.sounds.Count}개", EditorStyles.boldLabel);
            for (int i = 0; i < _soundLib.sounds.Count; i++)
            {
                var sound = _soundLib.sounds[i];
                EditorGUILayout.BeginHorizontal("box");

                sound.id = EditorGUILayout.TextField(sound.id, GUILayout.Width(130));
                sound.clip = (AudioClip)EditorGUILayout.ObjectField(sound.clip, typeof(AudioClip), false);
                sound.volume = EditorGUILayout.Slider(sound.volume, 0f, 1f, GUILayout.Width(110));

                using (new EditorGUI.DisabledScope(sound.clip == null))
                {
                    if (GUILayout.Button("▶", GUILayout.Width(28)))
                        PreviewClip(sound.clip);
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

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 항목 추가"))
            {
                Undo.RecordObject(_soundLib, "Add Sound");
                _soundLib.sounds.Add(new SoundEntry { id = $"sfx_{_soundLib.sounds.Count + 1}" });
                EditorUtility.SetDirty(_soundLib);
            }

            if (GUILayout.Button("■ 미리듣기 정지", GUILayout.Width(120)))
                StopPreview();
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
                EditorUtility.SetDirty(_soundLib);
        }

        private static void PreviewClip(AudioClip clip)
        {
            var util = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var method = util?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            if (method != null)
                method.Invoke(null, new object[] { clip, 0, false });
            else
                Debug.LogWarning("[Hub] 이 에디터 버전에서 미리듣기 API를 찾지 못했습니다.");
        }

        private static void StopPreview()
        {
            var util = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var method = util?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
            method?.Invoke(null, null);
        }

        private void DrawEffects()
        {
            if (_effectLib == null)
            {
                EditorGUILayout.HelpBox("EffectLibrary 없음", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"이펙트 {_effectLib.effects.Count}개", EditorStyles.boldLabel);
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("미리보기(▶)는 플레이 모드에서 동작합니다 (원점 재생).", MessageType.None);

            for (int i = 0; i < _effectLib.effects.Count; i++)
            {
                var effect = _effectLib.effects[i];
                EditorGUILayout.BeginHorizontal("box");

                effect.id = EditorGUILayout.TextField(effect.id, GUILayout.Width(130));
                effect.prefab = (GameObject)EditorGUILayout.ObjectField(effect.prefab, typeof(GameObject), false);
                EditorGUILayout.LabelField("수명", GUILayout.Width(30));
                effect.lifetime = EditorGUILayout.FloatField(effect.lifetime, GUILayout.Width(46));

                using (new EditorGUI.DisabledScope(!Application.isPlaying || effect.prefab == null))
                {
                    if (GUILayout.Button("▶", GUILayout.Width(28)))
                        Services.EffectManager.Instance.Play(effect.id, Vector3.zero);
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

            if (GUILayout.Button("+ 항목 추가"))
            {
                Undo.RecordObject(_effectLib, "Add Effect");
                _effectLib.effects.Add(new EffectEntry { id = $"fx_{_effectLib.effects.Count + 1}", lifetime = -1f });
                EditorUtility.SetDirty(_effectLib);
            }

            if (GUI.changed)
                EditorUtility.SetDirty(_effectLib);
        }

        private void DrawSaves()
        {
            string dir = Application.persistentDataPath;
            var files = Directory.Exists(dir) ? Directory.GetFiles(dir, "save_*.dat") : Array.Empty<string>();

            EditorGUILayout.LabelField($"세이브 파일 {files.Length}개", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(dir, EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("폴더 열기", GUILayout.Width(100)))
                EditorUtility.RevealInFinder(dir);
            if (Application.isPlaying && GUILayout.Button("지금 저장 (슬롯 0)", GUILayout.Width(140)))
                Services.SaveManager.Instance.SaveGame(0);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField($"{Path.GetFileName(file)}   {info.Length:N0} bytes   {info.LastWriteTime:yyyy-MM-dd HH:mm}");
                if (GUILayout.Button("보기", GUILayout.Width(50)))
                {
                    _savePreviewPath = file;
                    _saveJsonPreview = LoadPretty(file);
                }

                if (GUILayout.Button("삭제", GUILayout.Width(50))
                    && EditorUtility.DisplayDialog("세이브 삭제", $"{Path.GetFileName(file)} 를 삭제할까요?", "삭제", "취소"))
                {
                    File.Delete(file);
                    if (_savePreviewPath == file)
                        _saveJsonPreview = null;
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

        private static string PrettyJson(string json)
        {
            var sb = new StringBuilder();
            int indent = 0;
            bool inString = false;

            foreach (char c in json)
            {
                if (c == '"')
                {
                    inString = !inString;
                    sb.Append(c);
                    continue;
                }

                if (inString)
                {
                    sb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '[':
                        sb.Append(c);
                        sb.Append('\n');
                        sb.Append(new string(' ', ++indent * 2));
                        break;
                    case '}':
                    case ']':
                        sb.Append('\n');
                        sb.Append(new string(' ', --indent * 2));
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        sb.Append('\n');
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
