using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Linq;

public class InventorySlot : MonoBehaviour,
ISelectHandler, IDeselectHandler, ISubmitHandler,
IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // [SerializeField, ReadOnly] private int slotIndex = -1; //해당 슬롯의 인덱스
    public SlotInfo slotInfo = null;

    public SlotType slotType;
    public enum SlotType { inventory, Merge, Active };
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

    private void Awake()
    {
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

        if (slotInfo == null)
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

    public void Set_Slot(bool shiny = false)
    {
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
            if (slotType == SlotType.Active)
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
        slotIcon.sprite = iconSprite;
        //아이콘 색깔 초기화
        Color iconColor = slotIcon.color;
        iconColor.a = 1f;
        slotIcon.color = iconColor;
        // 툴팁 켜기
        slotTooltip.enabled = true;

        // 액티브 슬롯일때
        if (slotType == SlotType.Active)
        {
            MagicInfo coolMagic = slotInfo as MagicInfo;

            // 쿨타임 보여줄 마법 정보 넣기
            coolTimeIndicator.magic = MagicDB.Instance.GetMagicByID(coolMagic.id);
        }

        // 슬롯 갱신 이펙트
        if (shiny)
        {
            // 현재 슬롯 shiny 이펙트 켜기
            shinyEffect.gameObject.SetActive(false);
            shinyEffect.gameObject.SetActive(true);
        }
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
        // 버튼이 상호작용 가능할때만
        if (slotButton.interactable)
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
        // 버튼이 상호작용 가능할때만
        if (slotButton.interactable)
            // 슬롯 클릭하기
            ClickSlot();
    }

    void ClickSlot()
    {
        // 선택된 슬롯 없을때
        if (PhoneMenu.Instance.nowSelectSlot == null)
        {
            // 해당 슬롯에 아이템 없으면 리턴
            if (slotInfo == null)
                return;

            // 마우스 아이콘에 해당 슬롯 아이콘 넣기
            PhoneMenu.Instance.nowSelectIcon.enabled = true;
            PhoneMenu.Instance.nowSelectIcon.sprite = slotIcon.sprite;

            // 아이콘 마우스 위치로 이동
            PhoneMenu.Instance.MousePos();

            // 현재 슬롯 기억하기
            PhoneMenu.Instance.nowSelectSlot = this;
            // 선택된 슬롯 정보 갱신
            PhoneMenu.Instance.nowSelectSlotInfo = slotInfo;

            // 해당 슬롯 아이템 삭제
            slotInfo = null;

            // 폰 하단 버튼 상호작용 막기
            PhoneMenu.Instance.InteractBtnsToggle(false);
        }
        // 아이템 들고 click 했을때
        else
        {
            // 선택된 슬롯 정보 인스턴싱
            SlotInfo selectSlotInfo = PhoneMenu.Instance.nowSelectSlotInfo;

            // 액티브 슬롯일때
            if (slotType == SlotType.Active)
            {
                // 선택된 슬롯 정보를 각각 마법 및 아이템으로 형변환
                MagicInfo magic = selectSlotInfo as MagicInfo;
                ItemInfo item = selectSlotInfo as ItemInfo;

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
            if (slotInfo != null)
            {
                // 마우스 아이콘에 현재 슬롯 아이콘 넣기
                PhoneMenu.Instance.nowSelectIcon.sprite
                = slotInfo as MagicInfo != null
                ? MagicDB.Instance.GetMagicIcon(slotInfo.id)
                : ItemDB.Instance.GetItemIcon(slotInfo.id);

                // 마우스의 슬롯 정보에 현재 슬롯 정보 넣기
                PhoneMenu.Instance.nowSelectSlotInfo = slotInfo;

                // 선택된 슬롯 정보를 현재 슬롯에 넣기
                slotInfo = selectSlotInfo;
            }
            // 현재 슬롯이 빈 슬롯일때
            else
            {
                // 마우스 아이콘 끄기
                PhoneMenu.Instance.nowSelectIcon.enabled = false;

                // 현재 슬롯에 선택된 슬롯 아이템 넣기
                slotInfo = selectSlotInfo;

                // 선택된 슬롯 초기화
                PhoneMenu.Instance.nowSelectSlot = null;
            }

            // 현재 슬롯 shiny 이펙트 켜기
            shinyEffect.gameObject.SetActive(false);
            shinyEffect.gameObject.SetActive(true);

            // 해당 슬롯이 Merge 슬롯일때
            if (slotType == SlotType.Merge)
            {
                // 양쪽 슬롯 찾기
                InventorySlot L_MergeSlot = PhoneMenu.Instance.L_MergeSlotRect.GetComponent<InventorySlot>();
                InventorySlot R_MergeSlot = PhoneMenu.Instance.R_MergeSlotRect.GetComponent<InventorySlot>();

                // 양쪽 슬롯 다 들어있으면
                if (L_MergeSlot.slotInfo != null
                && R_MergeSlot.slotInfo != null)
                {
                    // 양쪽 마법 정보 찾기
                    MagicInfo L_Magic = L_MergeSlot.slotInfo as MagicInfo;
                    MagicInfo R_Magic = R_MergeSlot.slotInfo as MagicInfo;

                    // 양쪽 아이템 정보 찾기
                    ItemInfo L_Item = L_MergeSlot.slotInfo as ItemInfo;
                    ItemInfo R_Item = R_MergeSlot.slotInfo as ItemInfo;

                    // 두 슬롯 모두 마법 들었을때
                    if (L_Magic != null
                    && R_Magic != null)
                    {
                        // 두 슬롯의 마법으로 합성 가능한지 판단
                        MagicInfo mergeMagic = null;

                        // 같은 마법일때
                        if (L_Magic.id == R_Magic.id)
                        {
                            // 기존 마법 정보로 새 마법 인스턴싱
                            mergeMagic = new MagicInfo(L_Magic);
                            // 레벨 합산해서 넣기
                            mergeMagic.magicLevel = L_Magic.magicLevel + R_Magic.magicLevel;
                            // 마법 합성 트랜지션
                            StartCoroutine(PhoneMenu.Instance.MergeMagic(mergeMagic));
                        }
                        // 다른 마법일때
                        else
                        {
                            //두 재료 모두 갖고 있는 마법 찾기
                            mergeMagic = MagicDB.Instance.magicDB.Values.ToList().Find(
                                y => y.element_A == L_Magic.name && y.element_B == R_Magic.name);

                            // 레시피 못찾으면 재료 순서 바꿔서 다시 찾기
                            if (mergeMagic == null)
                                mergeMagic = MagicDB.Instance.magicDB.Values.ToList().Find(
                                    y => y.element_A == R_Magic.name && y.element_B == L_Magic.name);

                            // 합성 가능한 마법 없을때
                            if (mergeMagic == null)
                            {
                                // 양쪽 슬롯 깜빡이기
                                L_MergeSlot.FailBlink();
                                R_MergeSlot.FailBlink();

                                // 양쪽 슬롯 아이콘 떨기
                                L_MergeSlot.ShakeIcon();
                                R_MergeSlot.ShakeIcon();
                            }
                            // 합성 가능한 마법 있을때
                            else
                            {
                                // 새로운 마법 정보로 인스턴싱
                                mergeMagic = new MagicInfo(mergeMagic);
                                // 레벨 합산해서 넣기
                                mergeMagic.magicLevel = L_Magic.magicLevel + R_Magic.magicLevel;

                                // 마법 합성 트랜지션
                                StartCoroutine(PhoneMenu.Instance.MergeMagic(mergeMagic));
                            }
                        }

                    }

                    // 슬롯 둘중 하나라도 샤드가 들었을때
                    if (L_Item != null && L_Item.itemType == ItemDB.ItemType.Shard.ToString()
                    || R_Item != null && R_Item.itemType == ItemDB.ItemType.Shard.ToString())
                    {
                        // 양쪽 등급 사이의 등급 산출
                        int L_Grade = L_Magic == null ? L_Item.grade : L_Magic.grade;
                        int R_Grade = R_Magic == null ? R_Item.grade : R_Magic.grade;
                        // 최소 최대값 산출
                        int min = Mathf.Min(L_Grade, R_Grade);
                        int max = Mathf.Max(L_Grade, R_Grade);
                        // 등급 사잇값 산출
                        int get_Grade = Random.Range(min, max + 1);

                        StartCoroutine(PhoneMenu.Instance.MergeMagic(null, get_Grade));
                    }
                }
            }

            // 폰 하단 버튼 상호작용 허용
            PhoneMenu.Instance.InteractBtnsToggle(true);
        }

        // 현재 슬롯 UI 갱신
        Set_Slot();
    }

    public void FailBlink(int blinkNum = 2)
    {
        // 기존 트윈 있다면 끄기
        failIndicator.DOKill();

        // 인디케이터 색깔 투명하게 초기화
        failIndicator.color = Color.clear;

        // 해당 슬롯 빨갛게 blinkNum 만큼 깜빡이기
        failIndicator.DOColor(new Color(1, 0, 0, 0.5f), 0.2f)
        .SetLoops(blinkNum * 2, LoopType.Yoyo)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            // 인디케이터 색깔 투명하게 초기화
            failIndicator.color = Color.clear;
        });
    }

    public void ShakeIcon()
    {
        // 아이콘 현재 트윈 멈추기
        slotIcon.transform.DOPause();

        // 원래 위치 저장
        Vector2 originPos = slotIcon.transform.localPosition;

        // 해당 슬롯 아이콘 흔들기
        slotIcon.transform.DOPunchPosition(Vector2.right * 30f, 1f, 10, 1)
        .SetEase(Ease.Linear)
        .OnPause(() =>
        {
            slotIcon.transform.localPosition = originPos;
        })
        .SetUpdate(true);
    }
}
