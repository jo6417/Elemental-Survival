using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class JustTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public GameObject tooltip;
    bool tooltipFollow = false;

    private void OnEnable()
    {
        //끄는 것으로 초기화
        QuitTooltip();
    }

    private void Update()
    {
        //툴팁 마우스 따라다니기
        if (tooltipFollow)
            FollowMouse();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //툴팁 활성화
        tooltip.SetActive(true);

        //마우스 따라다니기 
        tooltipFollow = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //툴팁 비활성화
        QuitTooltip();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //툴팁 비활성화
        QuitTooltip();
    }

    void QuitTooltip()
    {
        //툴팁 비활성화
        tooltip.SetActive(false);

        //마우스 그만 따라다니기
        tooltipFollow = false;
    }

    void FollowMouse()
    {
        // Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0;
        tooltip.transform.position = mousePos;
    }
}
