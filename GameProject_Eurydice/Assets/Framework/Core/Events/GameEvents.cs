namespace GameFramework.Core
{
    // ===== 프레임워크 공용 이벤트 정의 =====
    // struct 기반: GC 할당 최소화. 프로젝트별 이벤트는 이 파일을 참고해 추가.

    /// <summary>범용 게임플레이 카운터 이벤트. 업적/퀘스트가 구독한다.</summary>
    public struct GameplayEvent
    {
        public string Key;      // 예: "enemy_kill", "item_collect", "stage_clear"
        public int Amount;      // 누적량 (기본 1)
        public string Param;    // 추가 파라미터 (예: 적 ID)

        public GameplayEvent(string key, int amount = 1, string param = null)
        {
            Key = key; Amount = amount; Param = param;
        }
    }

    public struct ItemAddedEvent    { public string ItemId; public int Amount; public int TotalCount; }
    public struct ItemRemovedEvent  { public string ItemId; public int Amount; public int TotalCount; }
    public struct InventoryChangedEvent { }

    public struct AchievementUnlockedEvent { public string AchievementId; }
    public struct AchievementProgressEvent { public string AchievementId; public int Current; public int Target; }

    public struct SceneLoadStartedEvent  { public string SceneName; }
    public struct SceneLoadProgressEvent { public float Progress; }
    public struct SceneLoadedEvent       { public string SceneName; }

    // ===== 입력 이벤트 (InputManager가 발행) =====
    public struct BackPressedEvent     { }   // ESC / 모바일 백버튼 / 패드 Start
    public struct JumpPressedEvent     { }
    public struct InteractPressedEvent { }

    public struct SaveCompletedEvent { public int Slot; }
    public struct LoadCompletedEvent { public int Slot; }
}
