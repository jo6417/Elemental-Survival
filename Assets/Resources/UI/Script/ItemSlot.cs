using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour,
ISelectHandler, IDeselectHandler, ISubmitHandler,
IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Button button;

    public void OnPointerEnter(PointerEventData eventData)
    {
        //해당 버튼 선택
        button.Select();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스 나가면 Deselect 하기
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 마법 넣기
        PutMagic();
    }

    public void OnSelect(BaseEventData eventData)
    {
        // 버튼이 상호작용 가능할때만
        if (button.interactable)
        {
            //todo select 했을때 - 툴팁 띄우기
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // 버튼이 상호작용 가능할때만
        if (button.interactable)
        {
            //todo 툴팁 끄기
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        // 마법 넣기
        PutMagic();
    }

    void PutMagic()
    {
        //todo click 했을때 - 해당 슬롯 아이콘 마우스에 넣기
        //todo 아이템 들고 click 했을때 해당 슬롯 아이템과 마우스 아이템 교체
        //todo 아이템 들고 빈 슬롯 click 했을때
        // 해당 슬롯에 마우스 아이템 놓기
        // 마우스에 있던 아이템의 원래 슬롯은 데이터 지우기
    }
}
