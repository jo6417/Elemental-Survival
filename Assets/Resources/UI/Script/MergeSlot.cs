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
    int slotIndex; //해당 슬롯의 인덱스
    public Image frame;
    public Image icon;
    public TextMeshProUGUI level;
    Button button; //해당 슬롯의 버튼 컴포넌트
    public bool isStackSlot = false; //스택 슬롯인지 여부
    public ToolTipTrigger tooltip;
    bool isMouseSelect = false; //마우스로 해당 슬롯 select 했는지

    private void Awake()
    {
        // 버튼 컴포넌트 찾기
        button = transform.GetComponent<Button>();

        //스택 슬롯이면 리턴
        if (isStackSlot)
            return;

        //해당 슬롯의 인덱스 찾기
        slotIndex = transform.GetSiblingIndex();

        // 마법 프레임 컴포넌트 찾기
        frame = transform.Find("Frame").GetComponentInChildren<Image>(true);
        // 마법 아이콘 컴포넌트 찾기
        icon = transform.Find("Icon").GetComponentInChildren<Image>(true);
        // 마법 레벨 컴포넌트 찾기
        level = icon.transform.Find("Level").GetComponentInChildren<TextMeshProUGUI>(true);
        // 툴팁 트리거 찾기
        tooltip = transform.GetComponent<ToolTipTrigger>();
    }

    private void OnEnable()
    {
        //스택 슬롯이면 리턴
        if (isStackSlot)
            return;

        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //버튼 상호작용 풀릴때까지 대기
        yield return new WaitUntil(() => button.interactable);

        if (PlayerManager.Instance.hasMergeMagics[slotIndex] == null)
        {
            //마법정보 없을땐 툴팁 트리거 끄기
            tooltip.enabled = false;
        }
        else
        {
            //마법정보 있으면 툴팁 트리거 켜기
            tooltip.enabled = true;
        }
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

    public void OnPointerClick(PointerEventData eventData)
    {
        //마법 넣기
        StartCoroutine(PutMagic());
    }

    public void OnSelect(BaseEventData eventData)
    {
        OnSelectSlot();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        OnDeSelectSlot();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        //마법 넣기
        StartCoroutine(PutMagic());
    }

    public void OnSelectSlot()
    {
        // print(slotIndex + " : OnSelect");

        // 스택 가운데 선택된 마법 입력
        if (PlayerManager.Instance.hasStackMagics.Count > 0)
            MergeMenu.Instance.selectedMagic = PlayerManager.Instance.hasStackMagics[0];

        // 현재 선택된 슬롯 입력
        MergeMenu.Instance.nowSelectSlot = this;

        // 스택 슬롯이면 리턴
        if (isStackSlot)
            return;
        // 슬롯의 마법이 null 아니면 리턴
        if (PlayerManager.Instance.hasMergeMagics[slotIndex] != null)
            return;
        // 마우스로 컨트롤 중이고, 마우스에 아이콘이 꺼져있으면 리턴
        if (isMouseSelect && !MergeMenu.Instance.selectedIcon.enabled)
            return;

        // 프레임 색 넣기
        frame.color = MagicDB.Instance.gradeColor[MergeMenu.Instance.selectedMagic.grade];
        // 아이콘 활성화
        icon.enabled = true;
        // 아이콘 넣기
        icon.sprite = MergeMenu.Instance.selectedIcon.sprite == null ? SystemManager.Instance.questionMark : MergeMenu.Instance.selectedIcon.sprite;

        // 주변 슬롯이랑 조합 가능한지 확인
        StartCoroutine(MergeCheck());
    }

    IEnumerator MergeCheck(bool isContinue = false)
    {
        //이펙트 현재 슬롯 위치로 이동
        MergeMenu.Instance.mergeSignal.position = transform.position;

        //4방향 슬롯 불러오기
        MergeMenu.Instance.closeSlots[0] = slotIndex - 4; //위
        MergeMenu.Instance.closeSlots[1] = slotIndex + 4; //아래
        MergeMenu.Instance.closeSlots[2] = slotIndex - 1; //왼쪽
        MergeMenu.Instance.closeSlots[3] = slotIndex + 1; //오른쪽

        // 좌측 끝 슬롯이면 왼쪽 인덱스 초기화
        if (slotIndex % 4 == 0)
        {
            MergeMenu.Instance.closeSlots[2] = -1; //왼쪽
        }
        // 우측 끝 슬롯이면 오른쪽 인덱스 초기화
        if ((slotIndex + 1) % 4 == 0)
        {
            MergeMenu.Instance.closeSlots[3] = -1; //오른쪽
        }

        for (int i = 0; i < MergeMenu.Instance.closeSlots.Length; i++)
        {
            // 해당 방향의 전기 이펙트 끄기
            MergeMenu.Instance.mergeSignal.GetChild(i).gameObject.SetActive(false);

            // 해당 방향의 레시피 ID 초기화
            MergeMenu.Instance.mergeResultMagics[i] = -1;

            // 배열 범위 내 인덱스일때
            if (MergeMenu.Instance.closeSlots[i] >= 0 && MergeMenu.Instance.closeSlots[i] < PlayerManager.Instance.hasMergeMagics.Length)
            {
                // 슬롯이 비어있지 않을때
                if (PlayerManager.Instance.hasMergeMagics[MergeMenu.Instance.closeSlots[i]] != null)
                {
                    //스택 0번의 현재 선택된 마법
                    MagicInfo selectMagic = null;
                    // 연속 합성일때는 본인 merge 슬롯에 있는 마법 넣고, 빈 슬롯일때는 선택된 스택 마법 넣기
                    selectMagic = isContinue ? PlayerManager.Instance.hasMergeMagics[slotIndex] : MergeMenu.Instance.selectedMagic;
                    //현재 Select 슬롯 주변의 null이 아닌 마법
                    MagicInfo closeMagic = null;
                    closeMagic = PlayerManager.Instance.hasMergeMagics[MergeMenu.Instance.closeSlots[i]];

                    //변수 초기화
                    MagicInfo mixedMagic = null;

                    // 서로 다른 마법일때
                    if (selectMagic.id != closeMagic.id)
                    {
                        //두 재료 모두 갖고 있는 마법 찾기
                        mixedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(x => x.element_A == selectMagic.magicName && x.element_B == closeMagic.magicName);

                        // null이면 재료 순서 바꿔서 재검사
                        if (mixedMagic == null)
                        {
                            mixedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(x => x.element_A == closeMagic.magicName && x.element_B == selectMagic.magicName);
                        }
                    }
                    // 서로 같은 마법일때
                    else
                    {
                        //같은 마법 넣어주기
                        mixedMagic = selectMagic;
                    }

                    //! 조합 가능성 출력
                    // if (mixedMagic != null)
                    //     print(i + " Slot : " + selectMagic.magicName + " + " + closeMagic.magicName + " = " + mixedMagic.magicName);

                    // 같은 마법이거나 해당 마법과 조합 가능할때
                    if (selectMagic.id == closeMagic.id || mixedMagic != null)
                    {
                        //해당 방향의 슬롯 찾기
                        Transform closeIcon = MergeMenu.Instance.mergeSlots.GetChild(MergeMenu.Instance.closeSlots[i]).Find("Icon");
                        Vector2 moveDir = transform.position - closeIcon.position;

                        // 아이콘이 타겟 슬롯 방향으로 조금씩 움직이려는 트윈
                        closeIcon.DOLocalMove(moveDir.normalized * 20f, 0.5f)
                        .OnKill(() =>
                        {
                            closeIcon.localPosition = Vector2.zero;
                        })
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetUpdate(true);

                        //연속 합성 아닐때
                        if (!isContinue)
                            // 해당 방향의 전기 이펙트 켜기
                            MergeMenu.Instance.mergeSignal.GetChild(i).gameObject.SetActive(true);

                        // 해당 방향의 레시피 ID 넣기
                        MergeMenu.Instance.mergeResultMagics[i] = mixedMagic.id;

                        continue;
                    }
                }
            }

            //모든 검사를 했음에도 조합 불가능하면 슬롯 인덱스 값 초기화
            MergeMenu.Instance.closeSlots[i] = -1;
        }

        // Merge 인디케이터 켜기
        MergeMenu.Instance.mergeSignal.gameObject.SetActive(true);

        yield return null;
    }

    public void OnDeSelectSlot()
    {
        // Merge 인디케이터 끄기
        MergeMenu.Instance.mergeSignal.gameObject.SetActive(false);

        // print(slotIndex + " : OnDeSelect");

        // 스택 슬롯이면 리턴
        if (isStackSlot)
            return;
        // 슬롯의 마법이 null 아니면 리턴
        if (PlayerManager.Instance.hasMergeMagics[slotIndex] != null)
            return;
        // 마우스로 컨트롤 중이고, 마우스에 아이콘이 꺼져있으면 리턴
        // if (isMouseSelect && !MergeMenu.Instance.selectedIcon.enabled)
        //     return;

        // 프레임 색 초기화
        frame.color = Color.white;
        // 아이콘 비활성화
        icon.enabled = false;

        //선택 모드 아닐때만
        if (!MergeMenu.Instance.mergeChooseMode)
        {
            // 모든 주변 아이콘 무브 트윈 멈추기
            MergeMenu.Instance.IconMoveStop();
        }
    }

    public IEnumerator PutMagic(bool isContinue = false)
    {
        // 스택 슬롯이면 리턴
        if (isStackSlot)
            yield break;

        //버튼 상호작용 불가면 리턴
        if (!button.interactable)
            yield break;

        // 슬롯의 마법이 null 아니면 리턴
        if (PlayerManager.Instance.hasMergeMagics[slotIndex] != null)
            yield break;

        // 머지 선택모드 켜져있을때 눌렀으면
        if (MergeMenu.Instance.mergeChooseMode)
        {
            // 모든 주변 아이콘 무브 트윈 멈추기
            MergeMenu.Instance.IconMoveStop();

            //이 슬롯의 방향 인덱스 구하기
            int dirIndex = Array.IndexOf(MergeMenu.Instance.closeSlots, slotIndex);

            // 방향 인덱스 쪽에 있는 슬롯, 합성 대기중인 슬롯 들의 인덱스 넣어서 합성
            StartCoroutine(MergeMagic(dirIndex, MergeMenu.Instance.mergeWaitSlot.transform.GetSiblingIndex()));

            // 머지 선택 모드 종료
            MergeMenu.Instance.ChooseModeToggle();

            // Merge 인디케이터 끄기
            MergeMenu.Instance.mergeSignal.gameObject.SetActive(false);

            // 선택된 마법을 스택에서 삭제
            PlayerManager.Instance.hasStackMagics.RemoveAt(0);
            // 스택 리스트 갱신
            MergeMenu.Instance.ScrollSlots(false);

            yield break;
        }

        // 마우스로 컨트롤 중이고, 마우스에 아이콘이 꺼져있으면 리턴
        if (isMouseSelect && !MergeMenu.Instance.selectedIcon.enabled)
            yield break;

        // 모든 주변 아이콘 무브 트윈 멈추기
        MergeMenu.Instance.IconMoveStop();

        // 마우스 커서에 선택된 아이콘 비활성화
        MergeMenu.Instance.selectedIcon.enabled = false;

        // 선택된 마법 변수가 null이 아닐때
        if (MergeMenu.Instance.selectedMagic != null)
        {
            // 선택된 Merge 슬롯에 프레임, 아이콘은 이미 적용됨
            // 선택된 Merge 슬롯에 레벨 넣기
            level.enabled = true;
            level.text = "Lv. " + MergeMenu.Instance.selectedMagic.magicLevel.ToString();

            // 선택된 Merge 슬롯에 마법 정보 넣기
            PlayerManager.Instance.hasMergeMagics[slotIndex] = MergeMenu.Instance.selectedMagic;

            // 툴팁 트리거에 마법 정보 넣기
            tooltip.Magic = MergeMenu.Instance.selectedMagic;
            tooltip.enabled = true;
        }

        int ableDirIndex = -1; //합성 가능한 방향 인덱스
        int ableNum = 0; //합성 가능한 개수

        //합성 가능성 개수 검사
        for (int i = 0; i < MergeMenu.Instance.closeSlots.Length; i++)
        {
            //값이 -1이 아니면 합성 가능
            if (MergeMenu.Instance.closeSlots[i] != -1)
            {
                //합성 가능 방향 인덱스 넣기
                ableDirIndex = i;

                //합성 가능 개수 추가
                ableNum++;
            }
        }

        //합성 없이 그냥 마법 입력일때
        if (ableNum == 0)
        {
            // 슬롯 입력 후 반짝이는 이펙트 발생
            GameObject effect = transform.Find("ShinyMask").gameObject;
            effect.SetActive(false);
            effect.SetActive(true);
        }

        // closeSlots 배열에서 합성 가능성 하나면 - 가능성 슬롯 마법 삭제, 합성된 마법 이 슬롯에 배치
        if (ableNum == 1)
        {
            // 플레이어 조작 못하게 막기
            MergeMenu.Instance.loadingPanel.SetActive(true);

            // 실제 마법 합성하기
            StartCoroutine(MergeMagic(ableDirIndex, slotIndex));
        }

        // closeSlots 배열에서 합성 가능성 2개 이상이면 - 모든 슬롯 상호작용 금지 및 가능성 슬롯들만 반짝이기
        if (ableNum > 1)
        {
            // 합성 대기중 슬롯 변수에 이 슬롯 넣기
            MergeMenu.Instance.mergeWaitSlot = this;

            // 주변의 합성 가능한 슬롯 중 선택하기
            MergeMenu.Instance.ChooseModeToggle();

            yield break;
        }

        // Merge 인디케이터 끄기
        MergeMenu.Instance.mergeSignal.gameObject.SetActive(false);

        //마법 합성 로딩 끝날때까지 대기
        yield return new WaitUntil(() => !MergeMenu.Instance.loadingPanel.activeSelf);

        //TODO 연속 합성 아닐때만
        if (!isContinue)
        {
            // 선택된 마법을 스택에서 삭제
            PlayerManager.Instance.hasStackMagics.RemoveAt(0);
            // 스택 리스트 갱신
            MergeMenu.Instance.ScrollSlots(false);
        }

        //메인 UI에 스마트폰 알림 갱신
        UIManager.Instance.PhoneNotice();
    }

    public IEnumerator MergeMagic(int ableDirIndex, int mergeIndex)
    {
        // 플레이어 조작 못하게 막기
        MergeMenu.Instance.loadingPanel.SetActive(true);

        // 모든 주변 아이콘 무브 트윈 멈추기
        // 아이콘이 타겟 슬롯 방향으로 조금씩 움직이려는 트윈 종료
        foreach (int index in MergeMenu.Instance.closeSlots)
        {
            // 배열 범위 내 인덱스일때
            if (index >= 0 && index < PlayerManager.Instance.hasMergeMagics.Length)
            {
                //해당 방향의 슬롯 찾기
                Transform dirIcon = MergeMenu.Instance.mergeSlots.GetChild(index).Find("Icon");
                dirIcon.DOKill();
            }

            yield return null;
        }

        // 합성 가능한 슬롯 방향 인덱스로 슬롯 인덱스 구하기
        int ableSlotIndex = MergeMenu.Instance.closeSlots[ableDirIndex];

        // 해당 방향 인덱스는 초기화
        MergeMenu.Instance.closeSlots[ableDirIndex] = -1;
        // print(ableSlotIndex + " : " + MergeMenu.Instance.closeSlots[ableDirIndex]);

        // 합성된 마법을 넣을 슬롯 찾기
        MergeSlot mergedSlot = MergeMenu.Instance.mergeSlots.GetChild(mergeIndex).GetComponent<MergeSlot>();

        //해당 방향의 슬롯 찾기
        Transform closeSlot = MergeMenu.Instance.mergeSlots.GetChild(ableSlotIndex);
        //프레임 찾기
        Transform closeFrame = closeSlot.Find("Frame");
        //아이콘 찾기
        Transform closeIcon = closeSlot.Find("Icon");

        // 이동할 아이콘에 스프라이트 넣기
        MergeMenu.Instance.mergeIcon.GetComponent<Image>().sprite = closeIcon.GetComponent<Image>().sprite;
        // mergeIcon 슬롯에 위치 시키기
        MergeMenu.Instance.mergeIcon.position = closeIcon.position;
        // mergeIcon이 날아가서 합쳐지는 트윈
        MergeMenu.Instance.mergeIcon.DOMove(mergedSlot.transform.position, 0.5f)
        .OnStart(() =>
        {
            //시작할때 활성화
            MergeMenu.Instance.mergeIcon.gameObject.SetActive(true);
        })
        .SetEase(Ease.InBack)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            //끝나면 비활성화
            MergeMenu.Instance.mergeIcon.gameObject.SetActive(false);
        });

        // 원래 슬롯 자리 초기화
        closeFrame.GetComponent<Image>().color = Color.white; //프레임 색 초기화
        closeIcon.GetComponent<Image>().enabled = false; //아이콘 비활성화
        closeIcon.Find("Level").GetComponent<TextMeshProUGUI>().enabled = false; //레벨 비활성화
        closeSlot.GetComponent<ToolTipTrigger>().enabled = false; //툴팁 트리거 끄기

        //아이콘 이동 끝날때까지 대기
        yield return new WaitUntil(() => MergeMenu.Instance.mergeIcon.position == mergedSlot.transform.position);

        // 원래 자리로 돌아오기
        // closeIcon.localPosition = Vector2.zero;

        // 합성된 마법 새로 인스턴스 생성
        MagicInfo mergedMagic = new MagicInfo(MagicDB.Instance.GetMagicByID(MergeMenu.Instance.mergeResultMagics[ableDirIndex]));

        // 합치기 전에 미리 레벨 합산해놓기
        int totalLevel = PlayerManager.Instance.hasMergeMagics[mergeIndex].magicLevel + PlayerManager.Instance.hasMergeMagics[ableSlotIndex].magicLevel;

        //! 디버그 확인용
        // print(PlayerManager.Instance.hasMergeMagics[mergeIndex].magicName
        // + " + " + PlayerManager.Instance.hasMergeMagics[ableSlotIndex].magicName +
        // " = " + mergedMagic.magicName + " Lv. " + totalLevel);

        // 슬롯에 합성된 마법 데이터 넣기
        PlayerManager.Instance.hasMergeMagics[mergeIndex] = mergedMagic;

        // 신규 데이터에 합산된 레벨 넣기
        PlayerManager.Instance.hasMergeMagics[mergeIndex].magicLevel = totalLevel;

        // 다 사용한 변수 초기화
        MergeMenu.Instance.selectedMagic = null;

        // 슬롯에 합성된 마법 등급 프레임 넣기
        mergedSlot.frame.color = MagicDB.Instance.gradeColor[PlayerManager.Instance.hasMergeMagics[mergeIndex].grade];
        // 슬롯에 합성된 마법 아이콘 넣기
        mergedSlot.icon.sprite =
        MagicDB.Instance.GetMagicIcon(mergedMagic.id) == null
        ? SystemManager.Instance.questionMark
        : MagicDB.Instance.GetMagicIcon(mergedMagic.id);
        // 슬롯에 합성된 마법 레벨 넣기
        mergedSlot.level.text = "Lv. " + mergedMagic.magicLevel.ToString();
        // 슬롯에 툴팁 넣기
        ToolTipTrigger tooltip = mergedSlot.GetComponent<ToolTipTrigger>();
        tooltip.enabled = true;
        tooltip.Magic = mergedMagic;

        // 합성 후 이펙트 발생
        GameObject effect = mergedSlot.transform.Find("ShinyMask").gameObject;
        effect.SetActive(false);
        effect.SetActive(true);

        //이펙트 시간 대기
        yield return new WaitForSecondsRealtime(0.2f);

        // 날아가서 합쳐진 슬롯의 마법 데이터를 삭제
        PlayerManager.Instance.hasMergeMagics[ableSlotIndex] = null;

        // 상호작용 막기 해제
        MergeMenu.Instance.loadingPanel.SetActive(false);

        //TODO 조합 가능한 주변 슬롯 다시 찾기
        yield return StartCoroutine(mergedSlot.MergeCheck(true));

        //TODO 합쳐진 슬롯 클릭 이벤트
        StartCoroutine(mergedSlot.PutMagic(true));
    }
}
