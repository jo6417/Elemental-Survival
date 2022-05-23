using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MergeSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        // TODO 빈슬롯일때만
        // TODO 아이콘 활성화 및 아이콘 넣기, 컬러 알파값 반투명하게
        // TODO 주변 슬롯에 조합 가능 레시피 있으면 인디케이터 표시
        //슬롯 2개 사이즈 직사각형 오브젝트 미리 만들어 비활성화 했던거 켜고 슬롯 사이로 이동, 슬롯 방향따라 회전
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //TODO 빈슬롯일때만
        //TODO 아이콘 비활성화
        //TODO 인디케이터 없에기
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //TODO 해당 슬롯에 실제 마법 데이터 및 아이콘 넣기
        //TODO 선택되어있던 Stack 슬롯 아이템 없에고 스택 리스트 갱신
    }
}
