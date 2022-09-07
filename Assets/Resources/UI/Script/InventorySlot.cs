using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour,
ISelectHandler, IDeselectHandler, ISubmitHandler,
IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField, ReadOnly] int slotIndex; //해당 슬롯의 인덱스
    public Image slotBack; // 슬롯 배경 이미지
    public Image slotFrame; // 아이템 등급 표시 슬롯 프레임
    public Image slotIcon; // 아이템 아이콘
    public Image slotLevel; //레벨 텍스트 상자
    public Image shinyEffect; // 슬롯 반짝 빛나는 애니메이터 이펙트
    public GameObject newSign; // 새로운 언락 싸인
    public Button slotButton;
    public ToolTipTrigger slotTooltip;
    [SerializeField] private SlotInfo[] inventory;

    private void Awake()
    {
        //해당 슬롯의 인덱스 찾기
        slotIndex = transform.GetSiblingIndex();

        // 마법 프레임 컴포넌트 찾기
        // frame = transform.Find("Frame").GetComponentInChildren<Image>(true);
        // // 마법 아이콘 컴포넌트 찾기
        // icon = transform.Find("Icon").GetComponentInChildren<Image>(true);
        // // 마법 레벨 컴포넌트 찾기
        // level = transform.Find("Level").GetComponent<Image>();
        // 툴팁 트리거 찾기
        slotTooltip = slotTooltip == null ? transform.GetComponent<ToolTipTrigger>() : slotTooltip;
        // 버튼 컴포넌트 찾기
        slotButton = slotButton == null ? transform.GetComponent<Button>() : slotButton;

        // 인벤토리 슬롯들 배열 참조
        inventory = PlayerManager.Instance.inventory;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //버튼 상호작용 풀릴때까지 대기
        yield return new WaitUntil(() => slotButton.interactable);

        // 버튼 이미지 컴포넌트 켜기
        slotButton.image.enabled = true;

        if (inventory[slotIndex] == null)
        {
            //마법정보 없을땐 툴팁 트리거 끄기
            slotTooltip.enabled = false;
        }
        else
        {
            //마법정보 있으면 툴팁 트리거 켜기
            slotTooltip.enabled = true;
        }

        // 마스크 이미지 숨기기
        shinyEffect.GetComponent<Mask>().showMaskGraphic = false;

        // New 표시 끄기
        newSign.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //해당 버튼 선택
        // button.Select();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스 나가면 Deselect 하기
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 마법 넣기
        ClickSlot();
    }

    public void OnSelect(BaseEventData eventData)
    {
        // 버튼이 상호작용 가능할때만
        if (slotButton.interactable)
        {
            // select 했을때 툴팁 띄우기 - 툴팁트리거에서 하기
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // 버튼이 상호작용 가능할때만
        if (slotButton.interactable)
        {
            // 툴팁 끄기 - 툴팁트리거에서 하기
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        // 슬롯 클릭하기
        ClickSlot();
    }

    void ClickSlot()
    {
        // 선택된 슬롯 없을때
        if (PhoneMenu.Instance.nowSelectIndex == -1)
        {
            // 해당 슬롯에 아이템 없으면 리턴
            if (inventory[slotIndex] == null)
                return;

            // 마우스 아이콘에 해당 슬롯 아이콘 넣기
            PhoneMenu.Instance.nowSelectIcon.enabled = true;
            PhoneMenu.Instance.nowSelectIcon.sprite = slotIcon.sprite;

            // 아이콘 마우스 위치로 이동
            PhoneMenu.Instance.MousePos();

            // 선택된 인덱스에 인덱스 넘겨주기
            PhoneMenu.Instance.nowSelectIndex = slotIndex;

            // 해당 슬롯 아이콘 끄기
            slotIcon.enabled = false;
        }
        // 아이템 들고 click 했을때
        else
        {
            // 마우스 아이콘 끄기
            PhoneMenu.Instance.nowSelectIcon.enabled = false;

            // 선택된 슬롯과 같은 슬롯일때
            if (PhoneMenu.Instance.nowSelectIndex == slotIndex)
            {
                // 선택된 인덱스 초기화
                PhoneMenu.Instance.nowSelectIndex = -1;

                // 해당 슬롯 아이콘 다시 켜기
                slotIcon.enabled = true;
            }
            // 선택된 슬롯과 다른 슬롯일때
            else
            {
                // 선택된 슬롯
                SlotInfo nowSelectSlot = inventory[PhoneMenu.Instance.nowSelectIndex];
                // 현재 슬롯
                SlotInfo thisSlot = inventory[slotIndex];

                // 현재 슬롯이 빈 슬롯 아닐때, 해당 슬롯과 선택된 슬롯 교체
                if (thisSlot != null)
                {
                    // 선택된 슬롯 아이템을 현재 슬롯에 넣기
                    inventory[slotIndex] = nowSelectSlot;

                    // 현재 슬롯 아이템을 선택된 슬롯에 넣기 
                    inventory[PhoneMenu.Instance.nowSelectIndex] = thisSlot;

                    //     print(slotIndex + ":" + inventory[slotIndex].name + " / "
                    // + PhoneMenu.Instance.nowSelectIndex + ":" + inventory[PhoneMenu.Instance.nowSelectIndex].name);
                }
                // 현재 슬롯이 빈 슬롯일때
                else
                {
                    // 현재 슬롯에 선택된 슬롯 아이템 넣기
                    inventory[slotIndex] = nowSelectSlot;

                    // 선택된 슬롯의 데이터 지우기
                    inventory[PhoneMenu.Instance.nowSelectIndex] = null;
                }

                // 바뀐 UI 갱신
                PhoneMenu.Instance.Set_InvenSlot(slotIndex);
                PhoneMenu.Instance.Set_InvenSlot(PhoneMenu.Instance.nowSelectIndex);

                // 양쪽 슬롯 shiny 이펙트 켜기
                PhoneMenu.Instance.invenSlots[slotIndex].shinyEffect.gameObject.SetActive(false);
                PhoneMenu.Instance.invenSlots[slotIndex].shinyEffect.gameObject.SetActive(true);
                PhoneMenu.Instance.invenSlots[PhoneMenu.Instance.nowSelectIndex].shinyEffect.gameObject.SetActive(false);
                PhoneMenu.Instance.invenSlots[PhoneMenu.Instance.nowSelectIndex].shinyEffect.gameObject.SetActive(true);

                // 선택된 인덱스 초기화
                PhoneMenu.Instance.nowSelectIndex = -1;
            }
        }
    }
}
