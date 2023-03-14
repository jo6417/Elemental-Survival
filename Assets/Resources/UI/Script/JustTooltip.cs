using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class JustTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] GameObject tooltip;

    private void OnEnable()
    {
        //끄는 것으로 초기화
        QuitTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //툴팁 활성화
        if (tooltip)
            tooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //툴팁 비활성화
        QuitTooltip();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // //툴팁 비활성화
        // QuitTooltip();
    }

    void QuitTooltip()
    {
        //툴팁 비활성화
        if (tooltip)
            tooltip.SetActive(false);
    }
}
