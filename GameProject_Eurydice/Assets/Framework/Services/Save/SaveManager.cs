using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using GameFramework.Core;

namespace GameFramework.Services
{
    /// <summary>
    /// JSON 기반 세이브/로드 시스템. 슬롯 + 간단한 XOR 난독화 지원.
    /// 사용 예)
    ///   SaveManager.Instance.Register(mySystem);  // ISavable 구현체 등록
    ///   SaveManager.Instance.SaveGame(0);
    ///   SaveManager.Instance.LoadGame(0);
    /// </summary>
    public class SaveManager : MonoSingleton<SaveManager>
    {
        [SerializeField] private bool useObfuscation = false;
        [SerializeField] private string obfuscationKey = "ChangeThisKey!";

        private readonly List<ISavable> _savables = new();

        [Serializable] private class SaveFile { public List<Entry> entries = new(); public string savedAt; public int version = 1; }
        [Serializable] private class Entry { public string key; public string json; }

        public void Register(ISavable savable)
        {
            if (!_savables.Contains(savable)) _savables.Add(savable);
        }

        public void Unregister(ISavable savable) => _savables.Remove(savable);

        private string GetPath(int slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.dat");

        public bool HasSave(int slot = 0) => File.Exists(GetPath(slot));

        public void SaveGame(int slot = 0)
        {
            var file = new SaveFile { savedAt = DateTime.UtcNow.ToString("o") };
            foreach (var s in _savables)
            {
                try { file.entries.Add(new Entry { key = s.SaveKey, json = s.CaptureState() }); }
                catch (Exception e) { Debug.LogError($"[Save] {s.SaveKey} 캡처 실패: {e}"); }
            }

            string json = JsonUtility.ToJson(file);
            if (useObfuscation) json = Obfuscate(json);

            File.WriteAllText(GetPath(slot), json);
            EventBus.Publish(new SaveCompletedEvent { Slot = slot });
            Debug.Log($"[Save] 슬롯 {slot} 저장 완료");
        }

        public bool LoadGame(int slot = 0)
        {
            var path = GetPath(slot);
            if (!File.Exists(path)) { Debug.LogWarning($"[Save] 슬롯 {slot} 파일 없음"); return false; }

            try
            {
                string json = File.ReadAllText(path);
                if (useObfuscation) json = Obfuscate(json);

                var file = JsonUtility.FromJson<SaveFile>(json);
                var map = new Dictionary<string, string>();
                foreach (var e in file.entries) map[e.key] = e.json;

                foreach (var s in _savables)
                {
                    if (map.TryGetValue(s.SaveKey, out var data))
                    {
                        try { s.RestoreState(data); }
                        catch (Exception e) { Debug.LogError($"[Save] {s.SaveKey} 복원 실패: {e}"); }
                    }
                }

                EventBus.Publish(new LoadCompletedEvent { Slot = slot });
                Debug.Log($"[Save] 슬롯 {slot} 로드 완료");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 로드 실패: {e}");
                return false;
            }
        }

        public void DeleteSave(int slot = 0)
        {
            var path = GetPath(slot);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>XOR 난독화 (양방향 동일 연산). 치팅 완전 방어용은 아님.</summary>
        private string Obfuscate(string input)
        {
            var key = Encoding.UTF8.GetBytes(obfuscationKey);
            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
                sb.Append((char)(input[i] ^ key[i % key.Length]));
            return sb.ToString();
        }
    }
}
