using UnityEngine;
using UnityEngine.UI;
using GameFramework.Data;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// 인벤토리 슬롯 하나의 표시. 그림 교체 포인트:
    /// - background : 슬롯 배경 스프라이트
    /// - icon       : 아이템 아이콘 (ItemData.icon에서 자동)
    /// - countText  : 수량 폰트/색
    /// </summary>
    public class InventorySlotView : MonoBehaviour
    {
        public Image background;
        public Image icon;
        public Text countText;

        public void Set(ItemData data, int count)
        {
            bool has = data != null && count > 0;

            icon.enabled = has && data.icon != null;
            icon.sprite = has ? data.icon : null;

            // 아이콘 스프라이트가 없으면 회색 박스로 표시 (플레이스홀더)
            if (has && data.icon == null)
            {
                icon.enabled = true;
                icon.color = new Color(0.55f, 0.55f, 0.6f);
            }
            else if (has)
            {
                icon.color = Color.white;
            }

            countText.enabled = has && count > 1;
            countText.text = has ? count.ToString() : "";
        }

        public void SetEmpty() => Set(null, 0);
    }
}
