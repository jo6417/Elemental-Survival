using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum ToolTipType { ProductTip, HasStuffTip};
    public ToolTipType toolTipType;
    public MagicInfo magic;
    public ItemInfo item;
    public string magicName;
    public string itemName;

    private void Start() {
        if(magic != null)
        magicName = magic.magicName;

        if(item != null)
        itemName = item.itemName;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 상품 구매 버튼일때
        if (toolTipType == ToolTipType.ProductTip)
        {
            ProductToolTip.Instance.OpenTooltip(magic, item);
        }

        // 소지품 아이콘일때
        if (toolTipType == ToolTipType.HasStuffTip)
        {
            HasStuffToolTip.Instance.OpenTooltip(magic, item);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 상품 구매 버튼일때
        if (toolTipType == ToolTipType.ProductTip)
        {
            ProductToolTip.Instance.QuitTooltip();
        }

        // 소지품 아이콘일때
        if (toolTipType == ToolTipType.HasStuffTip)
        {
            HasStuffToolTip.Instance.QuitTooltip();
        }
    }
}
