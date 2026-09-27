using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    /// <summary>
    /// 패널 활성화.
    /// </summary>
    public virtual void Open()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 패널 비활성화.
    /// </summary>
    public virtual void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 뒤로 가기(ESC) 입력 처리. 자체 처리를 완료한 경우 true, 상위 매니저가 닫아야 할 경우 false 반환
    /// </summary>
    public virtual bool OnBackPressed()
    {
        return false;
    }
}
