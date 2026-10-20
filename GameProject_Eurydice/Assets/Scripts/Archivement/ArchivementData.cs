using UnityEngine;

public enum AchievementType
{
    PlayTime,
    ClearChapter,
    ClearPuzzle,
    ClearPuzzleCount,
    CollectItem,
    CollectItemCount,
    SocketItem,
    SocketItemCount,
    ClearSocketGroup,
    ClearSocketGroupCount
}

[CreateAssetMenu(fileName = "Achievement_", menuName = "Achievement Data")]
public class AchievementData : ScriptableObject
{
    [FoldGroup("기본 정보")]
    [SerializeField] private string id;
    [SerializeField] private string title;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private Sprite icon;

    [FoldGroup("달성 조건")]
    [SerializeField] private AchievementType achievementType;
    [SerializeField] private string targetId;
    [SerializeField] private float targetValue = 1f;

    [FoldGroup("표시 옵션")]
    [SerializeField] private bool isHidden = false;

    public string Id => id;
    public string Title => title;
    public string Description => description;
    public Sprite Icon => icon;
    public AchievementType AchievementType => achievementType;
    public string TargetId => targetId;
    public float TargetValue => targetValue;
    public bool IsHidden => isHidden;
}
