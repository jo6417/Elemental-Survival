using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public enum ToolTipType { ProductTip, HasStuffTip };
    public ToolTipType toolTipType;
    private SlotInfo slotInfo;
    public SlotInfo _slotInfo
    {
        get { return slotInfo; }
        set
        {
            slotInfo = value;

            if (slotInfo != null)
            {
                magicName = _slotInfo.name;
            }
        }
    }

    public string magicName;
    public string itemName;

    // private void OnEnable()
    // {
    //     StartCoroutine(Init());
    // }

    // IEnumerator Init()
    // {
    //     yield return null;

    //     // 마법 아이템 정보 없으면 컴포넌트 끄기
    //     if (Magic == null && Item == null)
    //         this.enabled = false;
    // }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotInfo == null)
            return;

        // 상품 구매 버튼일때
        if (toolTipType == ToolTipType.ProductTip)
        {
            ProductToolTip.Instance.OpenTooltip(_slotInfo);
        }

        // 소지품 아이콘일때
        if (toolTipType == ToolTipType.HasStuffTip)
        {
            HasStuffToolTip.Instance.OpenTooltip(_slotInfo);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //마우스 잠겨있지 않으면
        if (Cursor.lockState == CursorLockMode.None)
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
