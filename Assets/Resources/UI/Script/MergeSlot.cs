using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Linq;

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
        level = transform.Find("Level").GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //해당 버튼 선택
        button.Select();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //해당 버튼 클릭
        OnClickSlot();
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
        OnClickSlot();
    }

    public void OnSelectSlot(bool isMouseSelect = false)
    {
        // print(slotIndex + " : OnSelect");

        MergeMagic.Instance.nowSelectSlot = this;

        // 스택 슬롯이면 리턴
        if (isStackSlot)
            return;
        // 슬롯의 마법이 null 아니면 리턴
        if (PlayerManager.Instance.hasMergeMagics[slotIndex] != null)
            return;
        // 마우스로 컨트롤 중인데 커서에 선택된 아이콘이 꺼져있으면 리턴
        if (!MergeMagic.Instance.selectedIcon.enabled && UIManager.Instance.isMouseMove)
            return;

        // 아이콘 활성화
        icon.enabled = true;
        // 아이콘 넣기
        icon.sprite = MergeMagic.Instance.selectedIcon.sprite;

        // 주변 슬롯이랑 조합 가능한지 확인
        MergeCheck();
    }

    void MergeCheck()
    {
        //4방향 슬롯 불러오기
        MergeMagic.Instance.closeSlots[0] = slotIndex - 4;
        MergeMagic.Instance.closeSlots[1] = slotIndex + 4;
        MergeMagic.Instance.closeSlots[2] = slotIndex - 1;
        MergeMagic.Instance.closeSlots[3] = slotIndex + 1;

        foreach (var closeIndex in MergeMagic.Instance.closeSlots)
        {
            // 배열 범위 내 인덱스일때
            if (closeIndex >= 0 && closeIndex < PlayerManager.Instance.hasMergeMagics.Length)
            {
                // 슬롯이 비어있지 않을때
                if (PlayerManager.Instance.hasMergeMagics[closeIndex] != null)
                {
                    //스택 0번의 현재 선택된 마법
                    MagicInfo selectMagic = PlayerManager.Instance.hasStackMagics[0];
                    //현재 Select 슬롯 주변의 null이 아닌 마법
                    MagicInfo closeMagic = PlayerManager.Instance.hasMergeMagics[closeIndex];

                    //두 재료 모두 갖고 있는 마법 찾기
                    MagicInfo mixedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(x => x.element_A == selectMagic.magicName && x.element_B == closeMagic.magicName);

                    if(mixedMagic != null)
                    print(mixedMagic.magicName + " = " + selectMagic.magicName + " + " + closeMagic.magicName);

                    //TODO 주변 슬롯에 조합 가능 레시피 있으면 인디케이터 표시
                    //TODO 슬롯 사이 전기 이펙트, 아이콘이 타겟 슬롯 방향으로 조금씩 움직이려는 트윈
                }
            }

            // print(closeIndex);
        }
    }

    public void OnDeSelectSlot(bool isMouseSelect = false)
    {
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

        //TODO 인디케이터 없에기
    }

    public void OnClickSlot(bool isMouseSelect = false)
    {
        // print(slotIndex + " : OnClick");

        // 스택 슬롯이면 리턴
        if (isStackSlot)
            return;
        // 슬롯의 마법이 null 아니면 리턴
        if (PlayerManager.Instance.hasMergeMagics[slotIndex] != null)
            return;
        //마우스 커서에 선택된 아이콘 꺼져있으면 리턴
        if (!MergeMagic.Instance.selectedIcon.enabled && UIManager.Instance.isMouseMove)
            return;

        // 마우스 커서에 선택된 아이콘 비활성화
        MergeMagic.Instance.selectedIcon.enabled = false;
        // 선택했던 스택 슬롯 Image 초기화
        Image targetImage = MergeMagic.Instance.stackList[3].transform.Find("Icon").GetComponent<Image>();
        targetImage.enabled = true;

        // 해당 슬롯에 레벨 넣기
        level.enabled = true;
        level.text = "Lv. " + MergeMagic.Instance.selectedMagic.magicLevel.ToString();
        // 해당 슬롯에 실제 마법 데이터 넣기
        PlayerManager.Instance.hasMergeMagics[slotIndex] = MergeMagic.Instance.selectedMagic;

        // 선택되어있던 스택 슬롯 아이템 삭제
        PlayerManager.Instance.hasStackMagics.RemoveAt(0);
        // 스택 리스트 갱신
        MergeMagic.Instance.ScrollSlots(false);
    }
}
