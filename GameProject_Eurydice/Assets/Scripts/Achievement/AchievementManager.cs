using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    public static event Action<AchievementData> OnAchievementUnlocked;
    public static event Action<AchievementData, float, float> OnProgressChanged;

    [FoldGroup("데이터베이스")]
    [SerializeField] private List<AchievementData> achievementDatabase = new();

    [FoldGroup("플레이 타임 추적")]
    [SerializeField] private bool autoTrackPlayTime = true;

    private readonly Dictionary<string, AchievementData> achievementLookup = new();
    private readonly Dictionary<AchievementType, List<AchievementData>> achievementGroups = new();
    private readonly Dictionary<string, float> progressMap = new();
    private readonly HashSet<string> unlockedIds = new();
    private readonly HashSet<string> clearedPuzzleIds = new();
    private readonly HashSet<string> clearedSocketIds = new();
    private readonly HashSet<string> clearedSocketGroupIds = new();

    private float totalPlayTime;
    private int clearedPuzzleCount;
    private int totalCollectedItemCount;
    private int socketedCount;
    private int clearedSocketGroupCount;
    private float playTimeSaveTimer;

    public IReadOnlyList<AchievementData> Achievements => achievementDatabase;
    public float TotalPlayTime => totalPlayTime;
    public int ClearedPuzzleCount => clearedPuzzleCount;
    public int TotalCollectedItemCount => totalCollectedItemCount;
    public int SocketedCount => socketedCount;
    public int ClearedSocketGroupCount => clearedSocketGroupCount;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitDatabase();
        LoadProgress();
    }

    private void OnEnable()
    {
        SaveManager.OnSaveDataDeleted += HandleSaveDataDeleted;
        PuzzleBase.OnAnyPuzzleCompleted += HandlePuzzleCompleted;
        InventoryManager.OnItemAdded += HandleItemAdded;
        ItemObject.OnAnyItemObjectPicked += HandleItemObjectPicked;
        ItemSocket.OnAnySocketPlaced += HandleSocketPlaced;
        ItemSocketGroup.OnAnySocketGroupAllPlaced += HandleSocketGroupAllPlaced;
    }

    private void OnDisable()
    {
        SaveManager.OnSaveDataDeleted -= HandleSaveDataDeleted;
        PuzzleBase.OnAnyPuzzleCompleted -= HandlePuzzleCompleted;
        InventoryManager.OnItemAdded -= HandleItemAdded;
        ItemObject.OnAnyItemObjectPicked -= HandleItemObjectPicked;
        ItemSocket.OnAnySocketPlaced -= HandleSocketPlaced;
        ItemSocketGroup.OnAnySocketGroupAllPlaced -= HandleSocketGroupAllPlaced;
    }

    private void Update()
    {
        if (!autoTrackPlayTime) return;

        totalPlayTime += Time.unscaledDeltaTime;
        playTimeSaveTimer += Time.unscaledDeltaTime;

        UpdateGroupProgress(AchievementType.PlayTime, totalPlayTime);

        if (playTimeSaveTimer >= 30f)
        {
            playTimeSaveTimer = 0f;
            SaveProgress();
        }
    }
    
    private void InitDatabase()
    {
        achievementLookup.Clear();
        achievementGroups.Clear();

        if (achievementDatabase.Count == 0)
        {
            var loaded = Resources.LoadAll<AchievementData>("Achievements");
            if (loaded != null && loaded.Length > 0)
                achievementDatabase.AddRange(loaded);
        }

        for (int i = 0; i < achievementDatabase.Count; i++)
        {
            var data = achievementDatabase[i];
            if (data == null) continue;

            string key = !string.IsNullOrEmpty(data.Id) ? data.Id : data.name;

            if (achievementLookup.ContainsKey(key))
            {
                Debug.LogError($"[AchievementManager] Duplicate ID '{key}' found in '{data.name}'.");
                continue;
            }

            achievementLookup[key] = data;

            if (!achievementGroups.TryGetValue(data.AchievementType, out var list))
            {
                list = new List<AchievementData>();
                achievementGroups[data.AchievementType] = list;
            }
            list.Add(data);
        }
    }

    private void RecordCountedAchievement(HashSet<string> set, string id, AchievementType countType, ref int counter)
    {
        if (string.IsNullOrEmpty(id) || !set.Add(id)) return;
        counter = set.Count;
        UpdateGroupProgress(countType, counter);
    }

    private void UnlockMatching(AchievementType type, Func<AchievementData, bool> match)
    {
        if (!achievementGroups.TryGetValue(type, out var list)) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (match(list[i]))
                Unlock(list[i].Id);
        }
    }

    private static bool MatchesItem(string targetId, ItemData item)
    {
        return item != null && (IsTargetMatch(targetId, item.id) || IsTargetMatch(targetId, item.name) || IsTargetMatch(targetId, item.itemName));
    }

    /// <summary>
    /// 퍼즐 클리어 시 해당 퍼즐 전용 업적과 퍼즐 카운트 업적을 갱신합니다.
    /// </summary>
    public void ClearPuzzle(PuzzleBase puzzle)
    {
        if (puzzle == null) return;
        string puzzleId = !string.IsNullOrEmpty(puzzle.UniqueId) ? puzzle.UniqueId : puzzle.gameObject.name;
        RecordCountedAchievement(clearedPuzzleIds, puzzleId, AchievementType.ClearPuzzleCount, ref clearedPuzzleCount);
        UnlockMatching(AchievementType.ClearPuzzle, d => IsTargetMatch(d.TargetId, puzzle.UniqueId) || IsTargetMatch(d.TargetId, puzzle.gameObject.name));
        SaveProgress();
    }

    /// <summary>
    /// 퍼즐 고유 ID 또는 이름으로 퍼즐 클리어 업적을 갱신합니다.
    /// </summary>
    public void ClearPuzzle(string puzzleId)
    {
        if (string.IsNullOrEmpty(puzzleId)) return;
        RecordCountedAchievement(clearedPuzzleIds, puzzleId, AchievementType.ClearPuzzleCount, ref clearedPuzzleCount);
        UnlockMatching(AchievementType.ClearPuzzle, d => IsTargetMatch(d.TargetId, puzzleId));
        SaveProgress();
    }

    /// <summary>
    /// 특정 챕터 클리어 업적을 해금합니다.
    /// </summary>
    public void ClearChapter(string chapterId)
    {
        if (string.IsNullOrEmpty(chapterId)) return;
        UnlockMatching(AchievementType.ClearChapter, d => IsTargetMatch(d.TargetId, chapterId));
        SaveProgress();
    }

    /// <summary>
    /// 아이템 획득 시 특정 아이템 획득 및 누적 수집 업적을 갱신합니다.
    /// </summary>
    public void CollectItem(ItemData item, int amount = 1, string objectUniqueId = null, string objectName = null)
    {
        if (amount <= 0) return;
        totalCollectedItemCount += amount;
        UpdateGroupProgress(AchievementType.CollectItemCount, totalCollectedItemCount);
        UnlockMatching(AchievementType.CollectItem, d => MatchesItem(d.TargetId, item) || IsTargetMatch(d.TargetId, objectUniqueId) || IsTargetMatch(d.TargetId, objectName));
        SaveProgress();
    }

    /// <summary>
    /// 특정 아이템 ID 문자열로 업적을 갱신합니다.
    /// </summary>
    public void CollectItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
        totalCollectedItemCount += amount;
        UpdateGroupProgress(AchievementType.CollectItemCount, totalCollectedItemCount);
        UnlockMatching(AchievementType.CollectItem, d => IsTargetMatch(d.TargetId, itemId));
        SaveProgress();
    }

    /// <summary>
    /// 아이템 소켓에 아이템 장착 시 특정 소켓 및 소켓 카운트 업적을 갱신합니다.
    /// </summary>
    public void SocketItem(ItemSocket socket, ItemData item)
    {
        string socketId = socket != null ? (!string.IsNullOrEmpty(socket.UniqueId) ? socket.UniqueId : socket.gameObject.name) : null;
        RecordCountedAchievement(clearedSocketIds, socketId, AchievementType.SocketItemCount, ref socketedCount);
        UnlockMatching(AchievementType.SocketItem, d =>
            (socket != null && (IsTargetMatch(d.TargetId, socket.UniqueId) || IsTargetMatch(d.TargetId, socket.gameObject.name) || MatchesItem(d.TargetId, socket.RequiredItem))) ||
            MatchesItem(d.TargetId, item));
        SaveProgress();
    }

    /// <summary>
    /// 소켓 그룹 내 모든 소켓 장착 완료 시 관련 업적을 갱신합니다.
    /// </summary>
    public void ClearSocketGroup(ItemSocketGroup group)
    {
        if (group == null) return;
        string groupId = !string.IsNullOrEmpty(group.UniqueId) ? group.UniqueId : group.gameObject.name;
        RecordCountedAchievement(clearedSocketGroupIds, groupId, AchievementType.ClearSocketGroupCount, ref clearedSocketGroupCount);
        UnlockMatching(AchievementType.ClearSocketGroup, d => IsTargetMatch(d.TargetId, group.UniqueId) || IsTargetMatch(d.TargetId, group.gameObject.name));
        SaveProgress();
    }

    /// <summary>
    /// 대상 문자열이 일치하는지 비교합니다 (대소문자/공백 무시).
    /// </summary>
    private static bool IsTargetMatch(string targetId, string candidate)
    {
        if (string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(candidate)) return false;
        return string.Equals(targetId.Trim(), candidate.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 특정 업적의 진행도를 증가시킵니다.
    /// </summary>
    public void AddProgress(string id, float amount = 1f)
    {
        if (string.IsNullOrEmpty(id) || IsUnlocked(id)) return;

        progressMap.TryGetValue(id, out float current);
        SetProgress(id, current + amount);
    }

    /// <summary>
    /// 특정 업적의 진행도를 설정하고 목표치 도달 시 해금합니다.
    /// </summary>
    public void SetProgress(string id, float value)
    {
        if (string.IsNullOrEmpty(id) || IsUnlocked(id)) return;
        if (!achievementLookup.TryGetValue(id, out var data)) return;

        progressMap[id] = value;
        OnProgressChanged?.Invoke(data, value, data.TargetValue);

        if (value >= data.TargetValue)
            Unlock(id);
    }
    
    public void Unlock(string id)
    {
        if (string.IsNullOrEmpty(id) || !unlockedIds.Add(id)) return;

        if (achievementLookup.TryGetValue(id, out var data))
        {
            progressMap[id] = data.TargetValue;
            OnAchievementUnlocked?.Invoke(data);
            Debug.Log($"[AchievementManager] Unlocked: {data.Title} ({data.Id})");
        }

        SaveProgress();
    }
    
    public bool IsUnlocked(string id)
    {
        return !string.IsNullOrEmpty(id) && unlockedIds.Contains(id);
    }

    /// <summary>
    /// 해당 업적의 현재 진행 수치와 목표 수치를 반환합니다.
    /// </summary>
    public (float current, float target) GetProgress(string id)
    {
        if (string.IsNullOrEmpty(id) || !achievementLookup.TryGetValue(id, out var data))
            return (0f, 1f);

        if (unlockedIds.Contains(id))
            return (data.TargetValue, data.TargetValue);

        progressMap.TryGetValue(id, out float current);
        return (current, data.TargetValue);
    }

    /// <summary>
    /// 동일 타입 업적 그룹의 진행도를 일괄 갱신합니다.
    /// </summary>
    private void UpdateGroupProgress(AchievementType type, float value)
    {
        if (!achievementGroups.TryGetValue(type, out var list)) return;

        for (int i = 0; i < list.Count; i++)
        {
            var data = list[i];
            if (IsUnlocked(data.Id)) continue;

            progressMap[data.Id] = value;
            OnProgressChanged?.Invoke(data, value, data.TargetValue);

            if (value >= data.TargetValue)
                Unlock(data.Id);
        }
    }
    
    private void HandlePuzzleCompleted(PuzzleBase puzzle)
    {
        if (puzzle != null)
            ClearPuzzle(puzzle);
    }
    
    private void HandleItemObjectPicked(ItemObject itemObject, ItemData item, int amount)
    {
        string uniqueId = itemObject != null ? itemObject.UniqueId : null;
        string objectName = itemObject != null ? itemObject.gameObject.name : null;
        CollectItem(item, amount, uniqueId, objectName);
    }

    private void HandleItemAdded(ItemData item, int amount)
    {
        if (item != null)
            CollectItem(item, amount);
    }

    private void HandleSocketPlaced(ItemSocket socket, ItemData item)
    {
        if (socket != null)
            SocketItem(socket, item);
    }

    private void HandleSocketGroupAllPlaced(ItemSocketGroup group)
    {
        if (group != null)
            ClearSocketGroup(group);
    }

    private void HandleSaveDataDeleted()
    {
        unlockedIds.Clear();
        progressMap.Clear();
        clearedPuzzleIds.Clear();
        clearedSocketIds.Clear();
        clearedSocketGroupIds.Clear();
        totalPlayTime = 0f;
        clearedPuzzleCount = 0;
        totalCollectedItemCount = 0;
        socketedCount = 0;
        clearedSocketGroupCount = 0;
    }
    public void SaveProgress()
    {
        var saveData = new AchievementSaveData
        {
            totalPlayTime = totalPlayTime,
            clearedPuzzleCount = clearedPuzzleCount,
            totalCollectedItemCount = totalCollectedItemCount,
            socketedCount = socketedCount,
            clearedSocketGroupCount = clearedSocketGroupCount,
            clearedPuzzleIds = new List<string>(clearedPuzzleIds),
            clearedSocketIds = new List<string>(clearedSocketIds),
            clearedSocketGroupIds = new List<string>(clearedSocketGroupIds),
            unlockedIds = new List<string>(unlockedIds)
        };

        foreach (var pair in progressMap)
            saveData.progresses.Add(new AchievementProgressSaveData(pair.Key, pair.Value));

        SaveManager.SaveAchievements(saveData);
    }

    public void LoadProgress()
    {
        var saveData = SaveManager.LoadAchievements();
        if (saveData == null) return;

        totalPlayTime = saveData.totalPlayTime;
        totalCollectedItemCount = saveData.totalCollectedItemCount;

        LoadIdSet(clearedPuzzleIds, saveData.clearedPuzzleIds);
        clearedPuzzleCount = Mathf.Max(saveData.clearedPuzzleCount, clearedPuzzleIds.Count);

        LoadIdSet(clearedSocketIds, saveData.clearedSocketIds);
        socketedCount = Mathf.Max(saveData.socketedCount, clearedSocketIds.Count);

        LoadIdSet(clearedSocketGroupIds, saveData.clearedSocketGroupIds);
        clearedSocketGroupCount = Mathf.Max(saveData.clearedSocketGroupCount, clearedSocketGroupIds.Count);

        LoadIdSet(unlockedIds, saveData.unlockedIds);

        progressMap.Clear();
        if (saveData.progresses != null)
        {
            for (int i = 0; i < saveData.progresses.Count; i++)
            {
                var entry = saveData.progresses[i];
                if (entry != null && !string.IsNullOrEmpty(entry.id))
                    progressMap[entry.id] = entry.current;
            }
        }
    }

    private static void LoadIdSet(HashSet<string> target, List<string> source)
    {
        target.Clear();
        if (source == null) return;
        for (int i = 0; i < source.Count; i++)
            target.Add(source[i]);
    }
}
