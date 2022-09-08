using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class InventorySlot : MonoBehaviour,
ISelectHandler, IDeselectHandler, ISubmitHandler,
IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] bool isActiveSlot = false; // 액티브 슬롯인지 여부
    [SerializeField, ReadOnly] private int slotIndex = -1; //해당 슬롯의 인덱스
    public Image slotBack; // 슬롯 배경 이미지
    public Image slotFrame; // 아이템 등급 표시 슬롯 프레임
    public Image slotIcon; // 아이템 아이콘
    public Image slotLevel; //레벨 텍스트 상자
    public Image shinyEffect; // 슬롯 반짝 빛나는 애니메이터 이펙트
    public GameObject newSign; // 새로운 언락 싸인
    public Button slotButton;
    public ToolTipTrigger slotTooltip;
    public Image failIndicator;
    public ShowMagicCooltime coolTimeIndicator;
    [SerializeField] private SlotInfo[] inventory;

    private void Awake()
    {
        // 액티브 슬롯일때
        if (isActiveSlot)
            // 오브젝트 순서에 인벤토리 개수만큼 추가
            slotIndex = 20 + transform.GetSiblingIndex();
        // 인벤토리 슬롯일때
        else
            // 슬롯 오브젝트 순서대로 인덱스 넣기
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

        // 해당 슬롯 UI 세팅
        Set_Slot();
    }

    public void Set_Slot()
    {
        // 슬롯 정보 찾기
        SlotInfo slotInfo = PlayerManager.Instance.inventory[slotIndex];

        // 마법 정보가 없거나 슬롯 비우기 활성화 되면 슬롯 초기화 후 넘기기
        if (slotInfo == null)
        {
            //프레임 색 초기화
            slotFrame.color = Color.white;

            //아이콘 및 레벨 비활성화
            slotIcon.enabled = false;
            slotLevel.gameObject.SetActive(false);
            // 툴팁 끄기
            slotTooltip.enabled = false;

            // 액티브 슬롯일때, 쿨타임 인디케이터 초기화
            if (isActiveSlot)
            {
                // 쿨타임 마법 정보 삭제
                coolTimeIndicator.magic = null;
            }

            return;
        }

        int grade = 0;
        Sprite iconSprite = null;
        int level = 0;

        // 각각 마법 및 아이템으로 형변환
        MagicInfo magic = slotInfo as MagicInfo;
        ItemInfo item = slotInfo as ItemInfo;

        // 아이템인지 마법인지 판단
        if (magic != null)
        {
            // 등급 초기화
            grade = magic.grade;
            // 레벨 초기화
            level = magic.magicLevel;
            // 아이콘 찾기
            iconSprite = MagicDB.Instance.GetMagicIcon(magic.id);

            // 레벨 활성화
            slotLevel.gameObject.SetActive(true);
            // 레벨 이미지 색에 등급색 넣기
            slotLevel.color = MagicDB.Instance.GradeColor[grade];
            //레벨 넣기
            slotLevel.GetComponentInChildren<TextMeshProUGUI>(true).text = "Lv. " + level.ToString();

            // 슬롯에 툴팁 정보 넣기
            slotTooltip.Magic = magic;
            slotTooltip.Item = null;
        }

        if (item != null)
        {
            // 등급 초기화
            grade = item.grade;
            // 아이콘 찾기
            iconSprite = ItemDB.Instance.GetItemIcon(item.id);

            // 레벨 비활성화
            slotLevel.gameObject.SetActive(false);

            // 슬롯에 툴팁 정보 넣기
            slotTooltip.Item = item;
            slotTooltip.Magic = null;
        }

        // 등급 프레임 색 넣기
        slotFrame.color = MagicDB.Instance.GradeColor[slotInfo.grade];
        //아이콘 활성화
        slotIcon.enabled = true;
        //아이콘 넣기
        slotIcon.sprite = iconSprite == null ? SystemManager.Instance.questionMark : iconSprite;
        //아이콘 색깔 초기화
        Color iconColor = slotIcon.color;
        iconColor.a = 1f;
        slotIcon.color = iconColor;
        // 툴팁 켜기
        slotTooltip.enabled = true;

        // 액티브 슬롯일때        
        if (isActiveSlot)
            // 쿨타임 보여줄 마법 정보 넣기
            coolTimeIndicator.magic = slotInfo as MagicInfo;
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
        // 현재 슬롯
        SlotInfo thisSlot = inventory[slotIndex];

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

            // 선택된 슬롯 정보 갱신
            PhoneMenu.Instance.nowSelectSlotInfo = thisSlot;

            // 해당 슬롯 아이템 삭제
            inventory[slotIndex] = null;

            // 폰 하단 버튼 상호작용 막기
            PhoneMenu.Instance.InteractToggleBtns(false);
        }
        // 아이템 들고 click 했을때
        else
        {
            // 선택된 슬롯 정보 인스턴싱
            SlotInfo nowSelectSlot = PhoneMenu.Instance.nowSelectSlotInfo;

            // 현재 슬롯이 액티브 슬롯일때
            if (isActiveSlot)
            {
                // 선택된 슬롯 정보를 각각 마법 및 아이템으로 형변환
                MagicInfo magic = nowSelectSlot as MagicInfo;
                ItemInfo item = nowSelectSlot as ItemInfo;

                // 마법이 아니거나, 액티브 마법이 아닐때
                if (magic == null || magic.castType != MagicDB.MagicType.active.ToString())
                {
                    // 마우스 아이콘 떨리기
                    PhoneMenu.Instance.ShakeMouseIcon();

                    // 현재 슬롯 빨갛게 인디케이터 점등
                    FailBlink(2);

                    // 리턴
                    return;
                }
            }

            // 현재 슬롯이 빈 슬롯 아닐때, 해당 슬롯과 선택된 슬롯 스왑
            if (thisSlot != null)
            {
                // 마우스 아이콘에 현재 슬롯 아이콘 넣기
                PhoneMenu.Instance.nowSelectIcon.sprite
                = thisSlot.slotType == SlotInfo.SlotType.Magic
                ? MagicDB.Instance.GetMagicIcon(thisSlot.id)
                : ItemDB.Instance.GetItemIcon(thisSlot.id);

                // 마우스의 슬롯 정보에 현재 슬롯 정보 넣기
                PhoneMenu.Instance.nowSelectSlotInfo = thisSlot;

                // 선택된 슬롯 정보를 현재 슬롯에 넣기
                inventory[slotIndex] = nowSelectSlot;
            }
            // 현재 슬롯이 빈 슬롯일때
            else
            {
                // 마우스 아이콘 끄기
                PhoneMenu.Instance.nowSelectIcon.enabled = false;

                // 현재 슬롯에 선택된 슬롯 아이템 넣기
                inventory[slotIndex] = nowSelectSlot;

                // 선택된 인덱스 초기화
                PhoneMenu.Instance.nowSelectIndex = -1;
            }

            // 현재 슬롯 shiny 이펙트 켜기
            PhoneMenu.Instance.invenSlots[slotIndex].shinyEffect.gameObject.SetActive(false);
            PhoneMenu.Instance.invenSlots[slotIndex].shinyEffect.gameObject.SetActive(true);

            // 폰 하단 버튼 상호작용 허용
            PhoneMenu.Instance.InteractToggleBtns(true);
        }

        // 현재 슬롯 UI 갱신
        Set_Slot();
    }

    public void FailBlink(int blinkNum)
    {
        // 기존 트윈 있다면 끄기
        failIndicator.DOKill();

        // 마스크 색깔 투명하게 초기화
        failIndicator.color = Color.clear;

        // 해당 슬롯 빨갛게 blinkNum 만큼 깜빡이기
        failIndicator.DOColor(new Color(1, 0, 0, 0.5f), 0.2f)
        .SetLoops(blinkNum * 2, LoopType.Yoyo)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            // 마스크 색깔 투명하게 초기화
            failIndicator.color = Color.clear;
        });
    }
}
