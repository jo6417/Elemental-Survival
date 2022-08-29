using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Linq;
using System;

public class MergeSlot : MonoBehaviour,
ISelectHandler, IDeselectHandler, ISubmitHandler,
IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField, ReadOnly] int slotIndex; //해당 슬롯의 인덱스
    [SerializeField] Image frame; // 마법 등급 표시 슬롯 프레임
    [SerializeField] Image icon; // 마법 아이콘
    [SerializeField] Image level; //레벨 텍스트 상자
    [SerializeField] Image shinyEffect; // 슬롯 반짝 빛나는 애니메이터 이펙트
    [SerializeField] GameObject newSign; // 새로운 언락 싸인
    Button button; //해당 슬롯의 버튼 컴포넌트
    public ToolTipTrigger tooltip;
    bool isMouseSelect = false; //마우스로 해당 슬롯 select 했는지
    MagicInfo[] mergeMagics;

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
        tooltip = transform.GetComponent<ToolTipTrigger>();
        // 버튼 컴포넌트 찾기
        button = transform.GetComponent<Button>();

        // merge 슬롯들 배열 참조
        mergeMagics = PlayerManager.Instance.hasMergeMagics;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //버튼 상호작용 풀릴때까지 대기
        yield return new WaitUntil(() => button.interactable);

        // 버튼 이미지 컴포넌트 켜기
        button.image.enabled = true;

        if (mergeMagics[slotIndex] == null)
        {
            //마법정보 없을땐 툴팁 트리거 끄기
            tooltip.enabled = false;
        }
        else
        {
            //마법정보 있으면 툴팁 트리거 켜기
            tooltip.enabled = true;
        }

        // 마스크 이미지 숨기기
        shinyEffect.GetComponent<Mask>().showMaskGraphic = false;

        // New 표시 끄기
        newSign.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //마우스로 해당 슬롯 선택함
        isMouseSelect = true;

        //해당 버튼 선택
        button.Select();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스로 선택 여부 초기화
        isMouseSelect = false;

        // 마우스 나가면 Deselect 하기
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnSelect(BaseEventData eventData)
    {
        OnSelectSlot();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        OnDeSelectSlot();
    }

    IEnumerator MergeCheck(bool isContinue = false)
    {
        // //이펙트 현재 슬롯 위치로 이동
        // MergeMenu.Instance.mergeSignal.position = transform.position;

        // //4방향 슬롯 불러오기
        // MergeMenu.Instance.closeSlots[0] = slotIndex - 4; //위
        // MergeMenu.Instance.closeSlots[1] = slotIndex + 4; //아래
        // MergeMenu.Instance.closeSlots[2] = slotIndex - 1; //왼쪽
        // MergeMenu.Instance.closeSlots[3] = slotIndex + 1; //오른쪽

        // // 좌측 끝 슬롯이면 왼쪽 인덱스 초기화
        // if (slotIndex % 4 == 0)
        // {
        //     MergeMenu.Instance.closeSlots[2] = -1; //왼쪽
        // }
        // // 우측 끝 슬롯이면 오른쪽 인덱스 초기화
        // if ((slotIndex + 1) % 4 == 0)
        // {
        //     MergeMenu.Instance.closeSlots[3] = -1; //오른쪽
        // }

        // for (int i = 0; i < MergeMenu.Instance.closeSlots.Length; i++)
        // {
        //     // 해당 방향의 전기 이펙트 끄기
        //     MergeMenu.Instance.mergeSignal.GetChild(i).gameObject.SetActive(false);

        //     // 해당 방향의 레시피 ID 초기화
        //     MergeMenu.Instance.mergeResultMagics[i] = -1;

        //     // 배열 범위 내 인덱스일때
        //     if (MergeMenu.Instance.closeSlots[i] >= 0 && MergeMenu.Instance.closeSlots[i] < mergeMagics.Length)
        //     {
        //         // 슬롯이 비어있지 않을때
        //         if (mergeMagics[MergeMenu.Instance.closeSlots[i]] != null)
        //         {
        //             //스택 0번의 현재 선택된 마법
        //             MagicInfo selectMagic = null;
        //             // 연속 합성일때는 본인 merge 슬롯에 있는 마법 넣고, 빈 슬롯일때는 선택된 스택 마법 넣기
        //             selectMagic = isContinue ? mergeMagics[slotIndex] : selectedMagic;
        //             //현재 Select 슬롯 주변의 null이 아닌 마법
        //             MagicInfo closeMagic = null;
        //             closeMagic = mergeMagics[MergeMenu.Instance.closeSlots[i]];

        //             //변수 초기화
        //             MagicInfo mixedMagic = null;

        //             // 서로 다른 마법일때
        //             if (selectMagic.id != closeMagic.id)
        //             {
        //                 //두 재료 모두 갖고 있는 마법 찾기
        //                 mixedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(x => x.element_A == selectMagic.magicName && x.element_B == closeMagic.magicName);

        //                 // null이면 재료 순서 바꿔서 재검사
        //                 if (mixedMagic == null)
        //                 {
        //                     mixedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(x => x.element_A == closeMagic.magicName && x.element_B == selectMagic.magicName);
        //                 }
        //             }
        //             // 서로 같은 마법일때
        //             else
        //             {
        //                 //같은 마법 넣어주기
        //                 mixedMagic = selectMagic;
        //             }

        //             //! 조합 가능성 출력
        //             // if (mixedMagic != null)
        //             //     print(i + " Slot : " + selectMagic.magicName + " + " + closeMagic.magicName + " = " + mixedMagic.magicName);

        //             // 같은 마법이거나 해당 마법과 조합 가능할때
        //             if (selectMagic.id == closeMagic.id || mixedMagic != null)
        //             {
        //                 //해당 방향의 슬롯 찾기
        //                 Transform closeIcon = MergeMenu.Instance.mergeSlots.GetChild(MergeMenu.Instance.closeSlots[i]).Find("Icon");
        //                 Vector2 moveDir = transform.position - closeIcon.position;

        //                 // 아이콘이 타겟 슬롯 방향으로 조금씩 움직이려는 트윈
        //                 closeIcon.DOLocalMove(moveDir.normalized * 20f, 0.5f)
        //                 .OnKill(() =>
        //                 {
        //                     closeIcon.localPosition = Vector2.zero;
        //                 })
        //                 .SetEase(Ease.InOutSine)
        //                 .SetLoops(-1, LoopType.Yoyo)
        //                 .SetUpdate(true);

        //                 //연속 합성 아닐때
        //                 if (!isContinue)
        //                     // 해당 방향의 전기 이펙트 켜기
        //                     MergeMenu.Instance.mergeSignal.GetChild(i).gameObject.SetActive(true);

        //                 // 해당 방향의 레시피 ID 넣기
        //                 MergeMenu.Instance.mergeResultMagics[i] = mixedMagic.id;

        //                 continue;
        //             }
        //         }
        //     }

        //     //모든 검사를 했음에도 조합 불가능하면 슬롯 인덱스 값 초기화
        //     MergeMenu.Instance.closeSlots[i] = -1;
        // }

        // // Merge 인디케이터 켜기
        // MergeMenu.Instance.mergeSignal.gameObject.SetActive(true);

        yield return null;
    }

    public void OnSelectSlot()
    {
        // print(slotIndex + " : OnSelect");

        // 스택 가운데 선택된 마법 참조
        Image selectedIcon = MergeMenu.Instance.selectedIcon;
        MagicInfo selectedMagic = MergeMenu.Instance.selectedMagic;

        // 스택 가운데 선택된 마법 입력
        if (PlayerManager.Instance.hasStackMagics.Count > 0)
            selectedMagic = PlayerManager.Instance.hasStackMagics[0];

        // 현재 선택된 슬롯 입력
        MergeMenu.Instance.nowSelectSlot = this;

        // 슬롯의 마법이 null 아니면 리턴
        if (mergeMagics[slotIndex] != null)
            return;
        // 마우스로 컨트롤 중이고, 마우스에 아이콘이 꺼져있으면 리턴
        if (isMouseSelect && !selectedIcon.enabled)
            return;

        // 프레임 색 넣기
        frame.color = MagicDB.Instance.GradeColor[selectedMagic.grade];
        // 아이콘 활성화
        icon.enabled = true;
        // 아이콘 넣기
        icon.sprite = selectedIcon.sprite == null ? SystemManager.Instance.questionMark : selectedIcon.sprite;

        // 주변 슬롯이랑 조합 가능한지 확인
        // StartCoroutine(MergeCheck());
    }

    public void OnDeSelectSlot()
    {
        // Merge 인디케이터 끄기
        MergeMenu.Instance.mergeSignal.gameObject.SetActive(false);

        // print(slotIndex + " : OnDeSelect");

        // 이 슬롯에 마법이 들어있으면 리턴
        if (mergeMagics[slotIndex] != null)
            return;

        // 프레임 색 초기화
        frame.color = Color.white;
        // 아이콘 비활성화
        icon.enabled = false;

        //선택 모드 아닐때만
        // if (!MergeMenu.Instance.mergeChooseMode)
        // {
        //     // 모든 주변 아이콘 무브 트윈 멈추기
        //     MergeMenu.Instance.IconMoveStop();
        // }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //마법 넣기
        StartCoroutine(PutMagic());
    }

    public void OnSubmit(BaseEventData eventData)
    {
        //마법 넣기
        StartCoroutine(PutMagic());
    }

    public IEnumerator PutMagic(bool isContinue = false)
    {
        #region MergeMode
        // 머지 선택모드 켜져있을때 눌렀으면
        // if (MergeMenu.Instance.mergeChooseMode)
        // {
        //     // 모든 주변 아이콘 무브 트윈 멈추기
        //     MergeMenu.Instance.IconMoveStop();

        //     //이 슬롯의 방향 인덱스 구하기
        //     int dirIndex = Array.IndexOf(MergeMenu.Instance.closeSlots, slotIndex);

        //     // 방향 인덱스 쪽에 있는 슬롯, 합성 대기중인 슬롯 들의 인덱스 넣어서 합성
        //     StartCoroutine(MergeMagic(dirIndex, MergeMenu.Instance.mergeWaitSlot.transform.GetSiblingIndex()));

        //     // 머지 선택 모드 종료
        //     MergeMenu.Instance.ChooseModeToggle();

        //     // Merge 인디케이터 끄기
        //     MergeMenu.Instance.mergeSignal.gameObject.SetActive(false);

        //     // 선택된 마법을 스택에서 빼기
        //     bool removed = PlayerManager.Instance.RemoveStack(0);

        //     // 마법이 완전히 삭제됬을때
        //     if (removed)
        //         // 스택 리스트 갱신
        //         MergeMenu.Instance.Scroll_Stack(-1);
        //     // 마법의 개수만 차감됬을때
        //     else
        //         // 스택 슬롯 전체 갱신
        //         MergeMenu.Instance.UpdateStacks();

        //     yield break;
        // }
        #endregion

        // 스택 가운데 선택된 마법 참조
        Image selectedIcon = MergeMenu.Instance.selectedIcon;

        // 새롭게 마법 정보 인스턴스 생성
        MagicInfo selectedMagic = new MagicInfo(MergeMenu.Instance.selectedMagic);

        // 스택 마법 들고있지 않으면 리턴
        if (isMouseSelect && !selectedIcon.enabled)
            yield break;

        // 모든 주변 아이콘 무브 트윈 멈추기
        // MergeMenu.Instance.IconMoveStop();

        // 현재 슬롯이 빈칸일때
        if (mergeMagics[slotIndex] == null)
        {
            // 마우스 아이콘 비활성화
            selectedIcon.enabled = false;

            // 선택된 Merge 슬롯에 프레임, 아이콘은 이미 적용됨

            // 선택된 Merge 슬롯에 레벨 및 색상 넣기
            level.gameObject.SetActive(true);
            level.color = MagicDB.Instance.GradeColor[selectedMagic.grade];
            level.GetComponentInChildren<TextMeshProUGUI>().text = "Lv. " + selectedMagic.magicLevel.ToString();

            // 선택된 Merge 슬롯에 마법 정보 넣기
            mergeMagics[slotIndex] = selectedMagic;

            // 툴팁 트리거에 마법 정보 넣기
            tooltip.Magic = selectedMagic;
            tooltip.enabled = true;

            // 선택된 스택의 마법 개수 차감
            PlayerManager.Instance.RemoveStack(0);
            // 스택 UI 갱신
            MergeMenu.Instance.Scroll_Stack(0);

            // 슬롯 샤이니 이펙트 켜기
            shinyEffect.gameObject.SetActive(false);
            shinyEffect.gameObject.SetActive(true);
        }
        // 현재 슬롯에 마법이 있을때
        else
        {
            // 두가지 마법 재료 참조
            MagicInfo elementA = selectedMagic;
            MagicInfo elementB = mergeMagics[slotIndex];

            // 스택의 마법과 현재 마법이 같은 마법일때
            if (elementA.id == elementB.id)
            {
                // 스택 마법 차감
                PlayerManager.Instance.RemoveStack(0);

                // 현재 슬롯 마법 레벨 업
                mergeMagics[slotIndex].magicLevel++;
                level.GetComponentInChildren<TextMeshProUGUI>().text = "Lv. " + mergeMagics[slotIndex].magicLevel.ToString();

                // 스택 UI 갱신
                MergeMenu.Instance.Scroll_Stack(0);

                // 슬롯 샤이니 이펙트 켜기
                shinyEffect.gameObject.SetActive(false);
                shinyEffect.gameObject.SetActive(true);
            }
            else
            {
                //두 재료 모두 갖고 있는 마법 찾기
                MagicInfo mergedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(x => x.element_A == elementA.magicName && x.element_B == elementB.magicName);

                // null이면 재료 순서 바꿔서 재검사
                if (mergedMagic == null)
                {
                    mergedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(x => x.element_A == elementB.magicName && x.element_B == elementA.magicName);
                }

                // 스택의 마법과 현재 마법이 합성 가능할때
                if (mergedMagic != null)
                {
                    // 합성된 마법 새로 인스턴스 생성
                    MagicInfo magic = new MagicInfo(mergedMagic);

                    // 두 마법의 레벨 합쳐서 넣기
                    magic.magicLevel = elementA.magicLevel + elementB.magicLevel;

                    // 현재 슬롯 합성된 마법으로 교체
                    StartCoroutine(MergeMagic(magic));
                }
                // 스택의 마법과 현재 마법이 합성 불가능
                else
                {
                    // 마우스의 아이콘 흔들기
                    MergeMenu.Instance.selectedIcon.transform.DOPunchPosition(Vector2.right * 30f, 0.8f, 10, 1)
                    .SetUpdate(true);

                    // 마스크 이미지 표시
                    shinyEffect.GetComponent<Mask>().showMaskGraphic = true;
                    // 해당 슬롯 빨갛게 2회 깜빡이기
                    shinyEffect.color = Color.clear;
                    shinyEffect.DOColor(new Color(1, 0, 0, 100f / 255f), 0.2f)
                    .SetLoops(4, LoopType.Yoyo)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        // 하얀색으로 초기화
                        shinyEffect.color = Color.white;

                        // 마스크 이미지 숨기기
                        shinyEffect.GetComponent<Mask>().showMaskGraphic = false;
                    });
                }
            }
        }

        #region MultiMerge

        // int ableDirIndex = -1; //합성 가능한 방향 인덱스
        // int ableNum = 0; //합성 가능한 개수

        // //합성 가능성 개수 검사
        // for (int i = 0; i < MergeMenu.Instance.closeSlots.Length; i++)
        // {
        //     //값이 -1이 아니면 합성 가능
        //     if (MergeMenu.Instance.closeSlots[i] != -1)
        //     {
        //         //합성 가능 방향 인덱스 넣기
        //         ableDirIndex = i;

        //         //합성 가능 개수 추가
        //         ableNum++;
        //     }
        // }

        // //합성 없이 그냥 마법 입력일때
        // if (ableNum == 0)
        // {
        //     // 슬롯 입력 후 반짝이는 이펙트 발생
        //     GameObject effect = transform.Find("ShinyMask").gameObject;
        //     effect.SetActive(false);
        //     effect.SetActive(true);
        // }

        // // closeSlots 배열에서 합성 가능성 하나면 - 가능성 슬롯 마법 삭제, 합성된 마법 이 슬롯에 배치
        // if (ableNum == 1)
        // {
        //     // 플레이어 조작 못하게 막기
        //     MergeMenu.Instance.loadingPanel.SetActive(true);

        //     // 실제 마법 합성하기
        //     StartCoroutine(MergeMagic(ableDirIndex, slotIndex));
        // }

        // // closeSlots 배열에서 합성 가능성 2개 이상이면 - 모든 슬롯 상호작용 금지 및 가능성 슬롯들만 반짝이기
        // if (ableNum > 1)
        // {
        //     // 합성 대기중 슬롯 변수에 이 슬롯 넣기
        //     MergeMenu.Instance.mergeWaitSlot = this;

        //     // 주변의 합성 가능한 슬롯 중 선택하기
        //     MergeMenu.Instance.ChooseModeToggle();

        //     yield break;
        // }

        // // Merge 인디케이터 끄기
        // MergeMenu.Instance.mergeSignal.gameObject.SetActive(false);

        // //마법 합성 로딩 끝날때까지 대기
        // yield return new WaitUntil(() => !MergeMenu.Instance.loadingPanel.activeSelf);

        // //TODO 연속 합성 아닐때만
        // if (!isContinue)
        // {
        //     // 선택된 마법을 스택에서 빼기
        //     bool removed = PlayerManager.Instance.RemoveStack(0);

        //     // 마법이 완전히 삭제됬을때
        //     if (removed)
        //         // 스택 리스트 갱신
        //         MergeMenu.Instance.Scroll_Stack(-1);
        //     // 마법의 개수만 차감됬을때
        //     else
        //         // 스택 슬롯 전체 갱신
        //         MergeMenu.Instance.UpdateStacks();
        // }

        //메인 UI에 스마트폰 알림 갱신
        // UIManager.Instance.PhoneNotice();

        #endregion
    }

    public IEnumerator MergeMagic(MagicInfo mergedMagic)
    {
        // 플레이어 조작 못하게 막기
        MergeMenu.Instance.loadingPanel.SetActive(true);

        // 현재 슬롯의 마법 데이터 교체
        mergeMagics[slotIndex] = mergedMagic;
        // 스택 마법 개수 차감
        PlayerManager.Instance.RemoveStack(0);
        // 스택 UI 갱신
        MergeMenu.Instance.Scroll_Stack(0);

        // 해금 리스트에 없는 마법일때 해금 시키기
        if (!MagicDB.Instance.unlockMagics.Exists(x => x == mergedMagic.id))
        {
            // 해금 리스트에 id 넣기
            MagicDB.Instance.unlockMagics.Add(mergedMagic.id);
            // 세이브
            StartCoroutine(SaveManager.Instance.Save());

            // 합성된 마법에 New 표시
            newSign.SetActive(true);
        }

        // 마스크 이미지 표시
        shinyEffect.GetComponent<Mask>().showMaskGraphic = true;
        // 하얀색으로 슬롯 가리기
        shinyEffect.color = new Color(1, 1, 1, 1f / 255f);
        shinyEffect.DOColor(Color.white, 0.5f)
        .SetUpdate(true);

        // 화이트 컬러 시간 대기
        yield return new WaitForSecondsRealtime(0.5f);

        // 합쳐진 마법 아이콘 넣기
        icon.sprite = MagicDB.Instance.GetMagicIcon(mergedMagic.id);
        // 프레임, 레벨 색깔 등급색으로 바꾸기
        frame.color = MagicDB.Instance.GradeColor[mergedMagic.grade];
        level.color = MagicDB.Instance.GradeColor[mergedMagic.grade];
        // 합쳐진 레벨 넣기
        level.GetComponentInChildren<TextMeshProUGUI>().text = "Lv. " + mergedMagic.magicLevel.ToString();

        // 툴팁 정보 넣기
        tooltip.Magic = mergedMagic;

        // 슬롯 가림막 제거
        shinyEffect.DOColor(new Color(1, 1, 1, 1f / 255f), 0.5f)
        .SetUpdate(true);

        // 가림막 제거 시간 대기
        yield return new WaitForSecondsRealtime(0.5f);

        // 마스크 이미지 숨기기
        shinyEffect.GetComponent<Mask>().showMaskGraphic = false;

        // 슬롯 샤이니 이펙트 켜기
        shinyEffect.gameObject.SetActive(false);
        shinyEffect.gameObject.SetActive(true);

        // 상호작용 막기 해제
        MergeMenu.Instance.loadingPanel.SetActive(false);
    }
}
