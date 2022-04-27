using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public enum ToolTipType { ProductTip, HasStuffTip };
    public ToolTipType toolTipType;
    public MagicInfo magic;
    public ItemInfo item;
    public string magicName;
    public string itemName;

    private void Start()
    {
        if (magic != null)
            magicName = magic.magicName;

        if (item != null)
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
        //마우스 잠겨있지 않으면
        if(Cursor.lockState == CursorLockMode.None)
        QuitTooltip();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        QuitTooltip();
    }

    void QuitTooltip()
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
