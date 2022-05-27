using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Linq;
using System;

public class MergeSlot : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerEnterHandler, IPointerClickHandler
{
    int slotIndex; //해당 슬롯의 인덱스
    Image icon;
    TextMeshProUGUI level;
    Button button; //해당 슬롯의 버튼 컴포넌트
    public bool isStackSlot = false; //스택 슬롯인지 여부

    private void Awake()
    {
        // 버튼 컴포넌트 찾기
        button = transform.GetComponent<Button>();

        //스택 슬롯이면 리턴
        if (isStackSlot)
            return;

        //해당 슬롯의 인덱스 찾기
        slotIndex = transform.GetSiblingIndex();

        // 마법 아이콘 컴포넌트 찾기
        icon = transform.Find("Icon").GetComponentInChildren<Image>(true);
        // 마법 레벨 컴포넌트 찾기
        level = icon.transform.Find("Level").GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //해당 버튼 선택
        button.Select();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //마법 넣기
        PutMagic();
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
        PutMagic();
    }

    public void OnSelectSlot(bool isMouseSelect = false)
    {
        // print(slotIndex + " : OnSelect");

        MergeMenu.Instance.nowSelectSlot = this;

        // 스택 슬롯이면 리턴
        if (isStackSlot)
            return;
        // 슬롯의 마법이 null 아니면 리턴
        if (PlayerManager.Instance.hasMergeMagics[slotIndex] != null)
            return;
        // 마우스로 컨트롤 중인데 커서에 선택된 아이콘이 꺼져있으면 리턴
        if (!MergeMenu.Instance.selectedIcon.enabled && UIManager.Instance.isMouseMove)
            return;

        // 아이콘 활성화
        icon.enabled = true;
        // 아이콘 넣기
        icon.sprite = MergeMenu.Instance.selectedIcon.sprite == null ? SystemManager.Instance.questionMark : MergeMenu.Instance.selectedIcon.sprite;

        // 주변 슬롯이랑 조합 가능한지 확인
        MergeCheck();
    }

    void MergeCheck()
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
                    MagicInfo selectMagic = PlayerManager.Instance.hasStackMagics[0];
                    //현재 Select 슬롯 주변의 null이 아닌 마법
                    MagicInfo closeMagic = PlayerManager.Instance.hasMergeMagics[MergeMenu.Instance.closeSlots[i]];

                    //두 재료 모두 갖고 있는 마법 찾기
                    //변수 초기화
                    MagicInfo mixedMagic = null;
                    mixedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(x => x.element_A == selectMagic.magicName && x.element_B == closeMagic.magicName);
                    // null이면 재료 순서 바꿔서 재검사
                    if (mixedMagic == null)
                    {
                        mixedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(x => x.element_A == closeMagic.magicName && x.element_B == selectMagic.magicName);
                    }

                    // 해당 방향에 조합 가능 마법 있으면
                    if (mixedMagic != null)
                    {
                        // print(selectMagic.magicName + " + " + closeMagic.magicName + " = " + mixedMagic.magicName);

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
    }

    public void OnDeSelectSlot(bool isMouseSelect = false)
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
        // 마우스에 선택된 아이콘 꺼져있으면 리턴
        // if (!MergeMagic.Instance.selectedIcon.enabled && Cursor.lockState == CursorLockMode.None)
        //     return;

        // 아이콘 비활성화
        icon.enabled = false;

        //선택 모드 아닐때만
        if (!MergeMenu.Instance.mergeChooseMode)
        {
            // 모든 주변 아이콘 무브 트윈 멈추기
            MergeMenu.Instance.IconMoveStop();
        }
    }

    public void PutMagic(bool isMouseSelect = false)
    {
        //버튼 상호작용 불가면 리턴
        if (!button.interactable)
            return;

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
        }

        // print(slotIndex + " : OnClick");

        // 스택 슬롯이면 리턴
        if (isStackSlot)
            return;
        // 슬롯의 마법이 null 아니면 리턴
        if (PlayerManager.Instance.hasMergeMagics[slotIndex] != null)
            return;
        //마우스 커서에 선택된 아이콘 꺼져있으면 리턴
        if (!MergeMenu.Instance.selectedIcon.enabled)
            return;

        // 모든 주변 아이콘 무브 트윈 멈추기
        MergeMenu.Instance.IconMoveStop();

        // 마우스 커서에 선택된 아이콘 비활성화
        MergeMenu.Instance.selectedIcon.enabled = false;
        // 선택했던 스택 슬롯 Image 초기화
        Image targetImage = MergeMenu.Instance.stackList[3].transform.Find("Icon").GetComponent<Image>();
        targetImage.enabled = true;

        // 선택된 Merge 슬롯에 아이콘은 이미 들어가 있으므로 스킵
        // 선택된 Merge 슬롯에 레벨 넣기
        level.enabled = true;
        level.text = "Lv. " + MergeMenu.Instance.selectedMagic.magicLevel.ToString();

        // 선택된 Merge 슬롯에 마법 정보 넣기
        PlayerManager.Instance.hasMergeMagics[slotIndex] = MergeMenu.Instance.selectedMagic;

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

        // closeSlots 배열에서 합성 가능성 하나면 - 가능성 슬롯 마법 삭제, 합성된 마법 이 슬롯에 배치
        if (ableNum == 1)
        {
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

            return;
        }

        // Merge 인디케이터 끄기
        MergeMenu.Instance.mergeSignal.gameObject.SetActive(false);

        // 선택된 마법을 스택에서 삭제
        PlayerManager.Instance.hasStackMagics.RemoveAt(0);
        // 스택 리스트 갱신
        MergeMenu.Instance.ScrollSlots(false);
    }

    public IEnumerator MergeMagic(int ableDirIndex, int selectedIndex)
    {
        //TODO 플레이어 조작 못하게 막기
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

        // 합성된 마법을 넣을 슬롯 찾기
        MergeSlot selectedSlot = MergeMenu.Instance.mergeSlots.GetChild(selectedIndex).GetComponent<MergeSlot>();

        //해당 방향의 슬롯에서 아이콘 찾기
        Transform closeIcon = MergeMenu.Instance.mergeSlots.GetChild(ableSlotIndex).Find("Icon");

        // 합성 가능한 아이콘이 날아가서 합쳐지는 트윈
        closeIcon.DOMove(selectedSlot.transform.position, 0.5f)
        .SetEase(Ease.InBack)
        .SetUpdate(true);

        //아이콘 이동 끝날때까지 대기
        yield return new WaitUntil(() => closeIcon.position == selectedSlot.transform.position);

        // 날아간 뒤 슬롯의 아이콘 및 레벨 끄기
        closeIcon.GetComponent<Image>().enabled = false;
        closeIcon.Find("Level").GetComponent<TextMeshProUGUI>().enabled = false;

        // 원래 자리로 돌아오기
        closeIcon.localPosition = Vector2.zero;

        // 이 슬롯에 합성된 마법 데이터 넣기
        PlayerManager.Instance.hasMergeMagics[selectedIndex]
        = MagicDB.Instance.GetMagicByID(MergeMenu.Instance.mergeResultMagics[ableDirIndex]);
        // 재료 레벨 합산해서 넣기
        PlayerManager.Instance.hasMergeMagics[selectedIndex].magicLevel
        = MergeMenu.Instance.selectedMagic.magicLevel + PlayerManager.Instance.hasMergeMagics[ableSlotIndex].magicLevel;

        print(MergeMenu.Instance.selectedMagic.magicName
        + " + " + PlayerManager.Instance.hasMergeMagics[ableSlotIndex].magicName +
        " = " + PlayerManager.Instance.hasMergeMagics[selectedIndex].magicName);

        // 슬롯에 합성된 마법 아이콘 넣기
        selectedSlot.icon.sprite = MagicDB.Instance.GetMagicIcon(MergeMenu.Instance.mergeResultMagics[ableDirIndex]) == null ? SystemManager.Instance.questionMark : MagicDB.Instance.GetMagicIcon(MergeMenu.Instance.mergeResultMagics[ableDirIndex]);
        // 슬롯에 합성된 마법 레벨 넣기
        selectedSlot.level.text = "Lv. " + PlayerManager.Instance.hasMergeMagics[selectedIndex].magicLevel.ToString();

        //TODO 합성 후 이펙트 발생
        GameObject effect = selectedSlot.transform.Find("ShinyMask").gameObject;
        effect.SetActive(false);
        effect.SetActive(true);
        //이펙트 시간 대기 후 비활성화
        yield return new WaitForSecondsRealtime(0.2f);

        // ableIndex 슬롯의 마법 데이터를 삭제
        PlayerManager.Instance.hasMergeMagics[ableSlotIndex] = null;

        // 상호작용 막기 해제
        MergeMenu.Instance.loadingPanel.SetActive(false);
    }
}
