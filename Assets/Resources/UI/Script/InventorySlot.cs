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
    [Header("State")]
    // [SerializeField, ReadOnly] private int slotIndex = -1; //해당 슬롯의 인덱스
    public SlotInfo slotInfo = null;
    [SerializeField] bool allowInit = true;
    public SlotType slotType;
    public enum SlotType { inventory, Merge, Quick };
    public System.Action setAction; // 해당 슬롯을 갱신 했을때 액션

    [Header("Refer")]
    [SerializeField] Transform shakeParent;
    public RectTransform rect;
    public Image slotBackEffect; // 슬롯 강조 배경 이펙트
    public Image slotFrame; // 아이템 등급 표시 슬롯 프레임
    public Image slotIcon; // 아이템 아이콘
    public Image slotLevel; //레벨 텍스트 상자
    public Image shinyEffect; // 슬롯 반짝 빛나는 애니메이터 이펙트
    public GameObject newSign; // 새로운 언락 싸인
    public Button slotButton;
    public ToolTipTrigger slotTooltip;
    public Image indicator;
    public GameObject soldOut; // 품절 표시
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
        slotTooltip = slotTooltip == null ? GetComponent<ToolTipTrigger>() : slotTooltip;
        // 버튼 컴포넌트 찾기
        slotButton = slotButton == null ? GetComponent<Button>() : slotButton;
        // Rect 찾기
        if (rect == null) rect = GetComponent<RectTransform>();

        // New 표시 끄기
        newSign.SetActive(false);

        // 버튼 onClick 콜백 함수 넣기
        slotButton.onClick.AddListener(() =>
        {
            ClickSlot(-1);
        });
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //버튼 상호작용 풀릴때까지 대기
        // yield return new WaitUntil(() => slotButton.interactable);
        yield return null;

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

        // 해당 슬롯 UI 세팅
        if (allowInit)
            Set_Slot();
    }

    public void Set_Slot(bool shiny = false)
    {
        // 갱신 액션 실행
        if (setAction != null)
            setAction.Invoke();

        // 마법 정보가 없을때
        if (slotInfo == null)
        {
            //프레임 색 초기화
            slotFrame.color = Color.white;

            //아이콘 및 레벨 비활성화
            slotIcon.enabled = false;
            slotLevel.gameObject.SetActive(false);
            // 툴팁 끄기
            slotTooltip.enabled = false;

            // 퀵슬롯일때, 쿨타임 인디케이터 초기화
            if (slotType == SlotType.Quick)
            {
                // 쿨타임 마법 정보 삭제
                coolTimeIndicator.magic = null;
            }

            // new 표시 끄기
            newSign.SetActive(false);

            return;
        }

        int grade = 0;
        Sprite iconSprite = null;
        int level = 0;
        Color frameColor = Color.white;

        // 각각 마법 및 아이템으로 형변환
        MagicInfo magic = slotInfo as MagicInfo;
        ItemInfo item = slotInfo as ItemInfo;

        // 아이템인지 마법인지 판단
        if (magic != null)
        {
            // 등급 초기화
            grade = magic.grade;
            // 레벨 초기화
            level = magic.MagicLevel;
            // 아이콘 찾기
            iconSprite = MagicDB.Instance.GetIcon(magic.id);

            // 레벨 활성화
            slotLevel.gameObject.SetActive(true);
            // 레벨 이미지 색에 등급색 넣기
            slotLevel.color = MagicDB.Instance.GradeColor[grade];
            //레벨 넣기
            slotLevel.GetComponentInChildren<TextMeshProUGUI>(true).text = "Lv." + level.ToString();
        }

        if (item != null)
        {
            // 등급 초기화
            grade = item.grade;
            // 아이콘 찾기
            iconSprite = ItemDB.Instance.GetIcon(item.id);

            // 레벨 비활성화
            slotLevel.gameObject.SetActive(false);
        }

        // 슬롯에 툴팁 정보 넣기
        if (magic != null)
            slotTooltip._slotInfo = magic;
        if (item != null)
            slotTooltip._slotInfo = item;

        // 등급 프레임 색
        frameColor = MagicDB.Instance.GradeColor[slotInfo.grade];

        // 등급 프레임 색 넣기
        slotFrame.color = frameColor;
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

        // 퀵슬롯일때
        if (slotType == SlotType.Quick)
        {
            MagicInfo coolMagic = slotInfo as MagicInfo;

            // 쿨타임 보여줄 마법 정보 넣기
            coolTimeIndicator.magic = MagicDB.Instance.GetQuickMagicByID(coolMagic.id);
        }

        // 슬롯 갱신 이펙트
        if (shiny)
        {
            // 현재 슬롯 shiny 이펙트 켜기
            shinyEffect.gameObject.SetActive(false);
            shinyEffect.gameObject.SetActive(true);
        }
    }

    // 슬롯에 마우스 오버시
    public void OnPointerEnter(PointerEventData eventData)
    {
        //해당 버튼 선택
        slotButton.Select();
    }

    public void OnSelect(BaseEventData eventData)
    {
        // 버튼이 상호작용 가능할때만
        if (slotButton.interactable)
        {
            // 버튼 선택 사운드 재생
            SoundManager.Instance.PlaySound("SelectButton");

            // 선택 슬롯 변수에 해당 슬롯 넣기
            PhoneMenu.Instance.nowSelectSlot = this;
        }
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
        {
            int secondInput = -1;

            // 쉬프트 누른채로 클릭했을때
            if (UIManager.Instance.UI_Input.UI.Shift.IsPressed())
            {
                // 머지 슬롯으로 변수 넣기
                secondInput = (int)SlotType.Quick;

                // 마법 넣기
                ClickSlot(secondInput);
            }

            // 컨트롤 누른채로 클릭했을때
            if (UIManager.Instance.UI_Input.UI.Ctrl.IsPressed())
            {
                // 퀵슬롯으로 변수 넣기
                secondInput = (int)SlotType.Merge;

                // 마법 넣기
                ClickSlot(secondInput);
            }
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
        // // 버튼이 상호작용 가능할때만
        // if (slotButton.interactable)
        //     // 슬롯 클릭하기
        //     ClickSlot();
    }

    public void ClickSlot(int secondInput = -1)
    {
        // 버튼 클릭 사운드 재생
        SoundManager.Instance.PlaySound("SubmitButton");

        // new 표시 끄기
        newSign.SetActive(false);

        // 선택된 슬롯 없을때
        if (PhoneMenu.Instance.nowHoldSlot == null)
        {
            // 해당 슬롯에 아이템 없으면 리턴
            if (slotInfo == null)
                return;

            // 머지 슬롯 단축키 누른채로 클릭했을때
            if (secondInput == 1)
            {
                // 합성슬롯 켜져있으면 리턴
                if (PhoneMenu.Instance.mergedSlot.transform.localScale.x > 0)
                    return;

                // 현재 슬롯이 액티브나 머지 슬롯일때
                if (slotType == SlotType.Quick
                || slotType == SlotType.Merge)
                {
                    //비어있는 인벤 슬롯 찾기
                    int emptyIndex = PhoneMenu.Instance.GetEmptySlot();

                    // 비어있는 인벤 슬롯 있을때
                    if (emptyIndex != -1)
                    {
                        // 현재 슬롯의 아이템 넣기
                        PhoneMenu.Instance.invenSlotList[emptyIndex].slotInfo = slotInfo;

                        // 현재 슬롯 정보 삭제
                        slotInfo = null;

                        // 각 슬롯 UI 갱신
                        PhoneMenu.Instance.invenSlotList[emptyIndex].Set_Slot(true);
                        Set_Slot(true);

                        return;
                    }
                    // 비어있는 인벤 슬롯 없을때
                    else
                    {
                        // 현재 슬롯 빨갛게 인디케이터 점등
                        BlinkSlot(4);
                        return;
                    }
                }

                // 비어있는 머지 슬롯
                InventorySlot emptyMergeSlot = null;

                // 좌측 슬롯이 비었을때
                if (PhoneMenu.Instance.L_MergeSlot.slotInfo == null)
                    emptyMergeSlot = PhoneMenu.Instance.L_MergeSlot;
                // 우측 슬롯이 비었을때
                else if (PhoneMenu.Instance.R_MergeSlot.slotInfo == null)
                    emptyMergeSlot = PhoneMenu.Instance.R_MergeSlot;

                // 비어있는 머지 슬롯이 있을때
                if (emptyMergeSlot != null)
                {
                    // 현재 슬롯의 아이템 넣기
                    emptyMergeSlot.slotInfo = slotInfo;

                    // 현재 슬롯 정보 삭제
                    slotInfo = null;

                    // 각 슬롯 UI 갱신
                    emptyMergeSlot.Set_Slot(true);
                    Set_Slot(true);

                    // 합성 가능 여부 체크하기
                    MergeCheck();

                    return;
                }
            }

            // 퀵슬롯 단축키 누른채로 클릭했을때
            if (secondInput == 2)
            {
                // 현재 슬롯이 액티브나 머지 슬롯일때
                if (slotType == SlotType.Quick
                || slotType == SlotType.Merge)
                {
                    //비어있는 인벤 슬롯 찾기
                    int emptyIndex = PhoneMenu.Instance.GetEmptySlot();

                    // 비어있는 인벤 슬롯 있을때
                    if (emptyIndex != -1)
                    {
                        // 현재 슬롯의 아이템 넣기
                        PhoneMenu.Instance.invenSlotList[emptyIndex].slotInfo = slotInfo;

                        // 현재 슬롯 정보 삭제
                        slotInfo = null;

                        // 각 슬롯 UI 갱신
                        PhoneMenu.Instance.invenSlotList[emptyIndex].Set_Slot(true);
                        Set_Slot(true);

                        return;
                    }
                    // 비어있는 인벤 슬롯 없을때
                    else
                    {
                        // 현재 슬롯 빨갛게 인디케이터 점등
                        BlinkSlot(4);
                        return;
                    }
                }

                // 슬롯정보가 마법이 아닐때, 액티브 마법이 아닐때
                MagicInfo magicInfo = slotInfo as MagicInfo;
                if (magicInfo == null)
                {
                    // 현재 슬롯 빨갛게 인디케이터 점등
                    BlinkSlot(4);

                    // 메시지
                    StartCoroutine(PhoneMenu.Instance.ChatAdd("퀵슬롯에는 마법만 장착 가능합니다."));

                    return;
                }

                // if (magicInfo.castType != MagicDB.CastType.active.ToString())
                // {
                //     // 현재 슬롯 빨갛게 인디케이터 점등
                //     BlinkSlot(4);

                //     // 메시지
                //     StartCoroutine(PhoneMenu.Instance.ChatAdd("액티브 마법만 장착 가능합니다."));

                //     return;
                // }

                // 비어있는 퀵슬롯
                InventorySlot emptyQuickSlot = null;

                // 퀵슬롯 전부 찾기
                InventorySlot[] quickSlots = UIManager.Instance.GetQuickSlots();
                // 비어있는 슬롯 찾기
                for (int i = 0; i < quickSlots.Length; i++)
                {
                    if (quickSlots[i].slotInfo == null)
                    {
                        emptyQuickSlot = quickSlots[i];
                        break;
                    }
                }

                // 비어있는 퀵슬롯이 있을때
                if (emptyQuickSlot != null)
                {
                    // 현재 슬롯의 아이템 넣기
                    emptyQuickSlot.slotInfo = slotInfo;

                    // 현재 슬롯 정보 삭제
                    slotInfo = null;

                    // 각 슬롯 UI 갱신
                    emptyQuickSlot.Set_Slot(true);
                    Set_Slot(true);

                    return;
                }
            }

            // 그냥 클릭했을때
            if (secondInput == -1)
            {
                // 마우스 아이콘에 해당 슬롯 아이콘 넣기
                UIManager.Instance.ToggleHoldSlot(true, slotIcon.sprite);

                // 마우스로 클릭했을때
                if (!UICursor.Instance.UI_Cursor.gameObject.activeSelf)
                    // 아이콘 마우스 위치로 이동
                    PhoneMenu.Instance.MousePos();
                // 키보드로 선택했을때
                else
                {
                    // UI 커서 자식으로 넣고 위치 초기화
                    UIManager.Instance.HoldIcon(UICursor.Instance.UI_Cursor);
                }

                // 현재 슬롯 기억하기
                PhoneMenu.Instance.nowHoldSlot = this;
                // 선택된 슬롯 정보 갱신
                PhoneMenu.Instance.nowHoldSlotInfo = slotInfo;

                // 해당 슬롯 아이템 삭제
                slotInfo = null;

                // // 키 입력 막기 변수 토글
                // PhoneMenu.Instance.InteractBtnsToggle(false);
                // // 메뉴 버튼 상호작용 토글
                // PhoneMenu.Instance.recipeBtn.interactable = false;
                // // 백 버튼 상호작용 토글
                // PhoneMenu.Instance.backBtn.interactable = false;
            }
        }
        // 아이템 들고 click 했을때
        else
        {
            // 선택된 슬롯 정보 인스턴싱
            SlotInfo selectSlotInfo = PhoneMenu.Instance.nowHoldSlotInfo;

            // 퀵슬롯일때
            if (slotType == SlotType.Quick)
            {
                // 선택된 슬롯 정보를 각각 마법 및 아이템으로 형변환
                MagicInfo magic = selectSlotInfo as MagicInfo;
                ItemInfo item = selectSlotInfo as ItemInfo;

                // 마법이 아닐때
                if (magic == null)
                {
                    // 마우스 아이콘 떨리기
                    PhoneMenu.Instance.ShakeMouseIcon();

                    // 현재 슬롯 빨갛게 인디케이터 점등
                    BlinkSlot(4);

                    // 메시지
                    StartCoroutine(PhoneMenu.Instance.ChatAdd("퀵슬롯에는 마법만 장착 가능합니다."));

                    // 리턴
                    return;
                }
            }

            // 현재 슬롯이 빈 슬롯 아닐때, 해당 슬롯과 선택된 슬롯 스왑
            if (slotInfo != null)
            {
                // 마우스 아이콘에 현재 슬롯 아이콘 넣기
                UIManager.Instance.nowHoldSlot.sprite
                = slotInfo as MagicInfo != null
                ? MagicDB.Instance.GetIcon(slotInfo.id)
                : ItemDB.Instance.GetIcon(slotInfo.id);

                // 마우스의 슬롯 정보에 현재 슬롯 정보 넣기
                PhoneMenu.Instance.nowHoldSlotInfo = slotInfo;

                // 선택된 슬롯 정보를 현재 슬롯에 넣기
                slotInfo = selectSlotInfo;
            }
            // 현재 슬롯이 빈 슬롯일때
            else
            {
                // 마우스 아이콘 끄기
                UIManager.Instance.ToggleHoldSlot(false);

                // 현재 슬롯에 선택된 슬롯 아이템 넣기
                slotInfo = selectSlotInfo;

                // 선택된 슬롯 초기화
                PhoneMenu.Instance.nowHoldSlot = null;
            }

            // 현재 슬롯 shiny 이펙트 켜기
            shinyEffect.gameObject.SetActive(false);
            shinyEffect.gameObject.SetActive(true);

            // 해당 슬롯이 Merge 슬롯일때
            if (slotType == SlotType.Merge)
            {
                // 합성 가능 여부 체크하기
                MergeCheck();
            }

            // // 키 입력 막기 변수 토글
            // PhoneMenu.Instance.InteractBtnsToggle(true);
            // // 메뉴 버튼 상호작용 토글
            // PhoneMenu.Instance.recipeBtn.interactable = true;
            // // 백 버튼 상호작용 토글
            // PhoneMenu.Instance.backBtn.interactable = true;
        }

        // 현재 슬롯 UI 갱신
        Set_Slot();

        // 합성 가능 여부 체크
        PhoneMenu.Instance.MergeNumCheck();
    }

    public void BlinkSlot(int blinkNum = -1, float blinkTime = 0.2f, Color color = default)
    {
        // 기존 트윈 있다면 끄기
        indicator.DOKill();

        // 인디케이터 색깔 투명하게 초기화
        indicator.color = Color.clear;

        // 색깔 입력 없으면 빨간색
        if (color == default)
            color = new Color(1, 0, 0, 0.5f);

        // 해당 슬롯 빨갛게 blinkNum 만큼 깜빡이기
        indicator.DOColor(color, blinkTime)
        .SetLoops(blinkNum, LoopType.Yoyo)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            // 인디케이터 색깔 투명하게 초기화
            indicator.color = Color.clear;
        });
    }

    public void ShakeIcon()
    {
        // shakeParent 현재 트윈 멈추기
        shakeParent.transform.DOPause();

        // 원래 위치 저장
        Vector2 originPos = shakeParent.transform.localPosition;

        // 해당 슬롯 흔들기
        shakeParent.transform.DOPunchPosition(Vector2.right * 30f, 1f, 10, 1)
        .SetEase(Ease.Linear)
        .OnPause(() =>
        {
            shakeParent.transform.localPosition = originPos;
        })
        .SetUpdate(true);
    }

    void MergeCheck()
    {
        // 양쪽 슬롯 다 들어있으면
        if (PhoneMenu.Instance.L_MergeSlot.slotInfo != null
        && PhoneMenu.Instance.R_MergeSlot.slotInfo != null)
        {
            // 양쪽 마법 정보 찾기
            MagicInfo L_Magic = PhoneMenu.Instance.L_MergeSlot.slotInfo as MagicInfo;
            MagicInfo R_Magic = PhoneMenu.Instance.R_MergeSlot.slotInfo as MagicInfo;

            // 양쪽 아이템 정보 찾기
            ItemInfo L_Item = PhoneMenu.Instance.L_MergeSlot.slotInfo as ItemInfo;
            ItemInfo R_Item = PhoneMenu.Instance.R_MergeSlot.slotInfo as ItemInfo;

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
                    mergeMagic.MagicLevel = L_Magic.MagicLevel + R_Magic.MagicLevel;
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

                    //! 구현되지 않아서 마법 프리팹 없을때 실패처리
                    if (mergeMagic != null && MagicDB.Instance.GetMagicPrefab(mergeMagic.id) == null)
                        mergeMagic = null;

                    //todo 밴 당한 마법일때 실패 처리
                    if (mergeMagic != null && MagicDB.Instance.banMagicList.Exists(x => x == mergeMagic.id))
                        mergeMagic = null;

                    // 합성 가능한 마법 없을때
                    if (mergeMagic == null)
                    {
                        // 합성 실패 트랜지션
                        StartCoroutine(PhoneMenu.Instance.MergeFail(PhoneMenu.Instance.L_MergeSlot, PhoneMenu.Instance.R_MergeSlot));

                        // 핸드폰 옆에 메시지 띄우기
                        StartCoroutine(PhoneMenu.Instance.ChatAdd("합성 가능한 마법이 없습니다."));
                    }
                    // 합성 가능한 마법 있을때
                    else
                    {
                        // 새로운 마법 정보로 인스턴싱
                        mergeMagic = new MagicInfo(mergeMagic);
                        // 레벨 합산해서 넣기
                        mergeMagic.MagicLevel = L_Magic.MagicLevel + R_Magic.MagicLevel;

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
                // 두 등급중에 최소 최대값 산출
                int min = Mathf.Min(L_Grade, R_Grade);
                int max = Mathf.Max(L_Grade, R_Grade);

                // 랜덤으로 뽑을 등급 숫자를 저장하는 리스트
                List<int> gradePool = new List<int>();
                // min 부터 max 까지 모든 등급 숫자를 리스트에 넣기
                for (int i = min; i < max + 1; i++)
                {
                    // 등급을 풀에 넣기
                    gradePool.Add(i);
                }

                // 등급 개수만큼 반복
                int poolCount = gradePool.Count;
                int get_Grade = -1;
                for (int i = 0; i < poolCount; i++)
                {
                    // 등급 풀에서 인덱스 하나 뽑기
                    int gradeIndex = Random.Range(0, gradePool.Count);
                    // 해당 인덱스로 등급 산출
                    get_Grade = gradePool[gradeIndex];

                    // print($"min : {min} / max : {max} / gradeIndex : {gradeIndex} / get_Grade : {get_Grade}");

                    // 언락된 마법 중 해당 등급의 마법이 하나라도 있으면
                    if (MagicDB.Instance.GetRandomMagic(get_Grade) != null)
                        // 해당 등급으로 결정하고 반복문 끝내기
                        break;
                    else
                    {
                        // 등급 변수 초기화
                        get_Grade = -1;

                        // 현재 뽑은 등급은 풀에서 삭제
                        gradePool.RemoveAt(gradeIndex);

                        // 랜덤 등급 풀에서 다시 뽑기
                        continue;
                    }
                }

                // 등급 풀을 전부 다 조회했는데 언락된 마법이 하나도 없을때
                while (get_Grade < 1)
                    // min 등급이 1등급 이상이라면
                    if (min > 1)
                    {
                        // min 등급을 1단계 낮추기
                        min--;

                        // min보다 1등급 낮춘 등급으로 다시 조회
                        MagicInfo getMagic = MagicDB.Instance.GetRandomMagic(min);
                        // 뽑기 가능한 마법이 있으면
                        if (getMagic != null)
                            // 현재 뽑은 등급을 넣기
                            get_Grade = min;
                    }
                    // 이하 모든 등급 조회했으면 탈출
                    else
                        break;

                // 등급 풀을 전부 다 조회했는데 언락된 마법이 하나도 없을때
                if (get_Grade == -1)
                {
                    // 핸드폰 옆에 메시지 띄우기
                    StartCoroutine(PhoneMenu.Instance.ChatAdd("뽑을 수 있는 마법이 없습니다."));

                    // 합성 실패 트랜지션 실행
                    StartCoroutine(PhoneMenu.Instance.MergeFail(PhoneMenu.Instance.L_MergeSlot, PhoneMenu.Instance.R_MergeSlot));

                    return;
                }

                StartCoroutine(PhoneMenu.Instance.MergeMagic(null, get_Grade));
            }
        }
    }
}
