namespace GameFramework.Services
{
    /// <summary>
    /// 저장이 필요한 시스템이 구현하는 인터페이스.
    /// SaveManager.Register(this) 로 등록하면 저장/로드에 자동 포함된다.
    /// </summary>
    public interface ISavable
    {
        /// <summary>저장 데이터의 고유 키 (예: "inventory", "achievements")</summary>
        string SaveKey { get; }

        /// <summary>현재 상태를 JSON 문자열로 반환</summary>
        string CaptureState();

        /// <summary>JSON 문자열로부터 상태 복원</summary>
        void RestoreState(string json);
    }
}
