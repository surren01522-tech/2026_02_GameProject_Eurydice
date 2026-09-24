using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>모든 UI 패널/팝업의 베이스. 프리팹 루트에 부착.</summary>
    public abstract class UIPanel : MonoBehaviour
    {
        [Tooltip("true면 UIManager의 팝업 스택에 쌓여 뒤로가기(ESC)로 닫힌다.")]
        public bool isPopup = true;

        public virtual void OnShow(object args) { }
        public virtual void OnHide() { }

        /// <summary>뒤로가기로 닫히기 전에 호출. false 반환 시 닫기 취소.</summary>
        public virtual bool OnBackRequested() => true;

        public void Close() => UIManager.Instance.Hide(this);
    }
}
