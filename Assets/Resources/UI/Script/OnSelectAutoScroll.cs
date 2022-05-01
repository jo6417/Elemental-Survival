using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OnSelectAutoScroll : MonoBehaviour
{
    public bool isScroll;
    public Transform selectedItem = null; //스크롤에서 선택된 아이템
    public ScrollRect scroll;
    // public Transform contents; // 스크롤 전체 컨텐츠 담긴 오브젝트
    // public Transform viewport; // 스크롤 마스크 안에 보이는 영역 뷰포트
    float contentHeight; //컨텐츠 전체 높이
    Vector3[] contentsCorners = new Vector3[4]; // 컨텐츠의 모서리
    Vector3[] viewCorners = new Vector3[4]; // 뷰포트의 모서리
    Vector3[] itemCorners = new Vector3[4]; // 아이템의 모서리

    float scrollSpeed = 5f;

    private void Awake()
    {
        //4방향 코너 구하기, 좌측아래부터 시계방향
        scroll.viewport.GetComponent<RectTransform>().GetWorldCorners(viewCorners);
    }

    private void Update()
    {
        // 마우스 다운하면 스크롤 멈춤
        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
        {
            isScroll = false;

            //아이템 모서리 초기화
            for (int i = 0; i < itemCorners.Length; i++)
            {
                itemCorners[i] = Vector3.zero;
            }
        }

        // 선택된 아이템 있으면 해당 아이템이 완전히 보일때까지 스크롤
        if (isScroll)
        {
            AutoScroll();
        }
    }

    public void SetScrollItem(Transform item)
    {
        //아이템 변수 받기
        selectedItem = item;

        //컨텐츠 전체의 모서리 위치 가져오기
        scroll.content.GetComponent<RectTransform>().GetWorldCorners(contentsCorners);
        //컨텐츠 전체 높이 갱신
        contentHeight = Vector2.Distance(contentsCorners[0], contentsCorners[1]);

        isScroll = true;
    }

    void AutoScroll()
    {
        //아이템의 모서리 위치 가져오기
        selectedItem.GetComponent<RectTransform>().GetWorldCorners(itemCorners);

        // 뷰포트보다 아이템이 아래에 있을때
        if (itemCorners[0].y < viewCorners[0].y)
        {
            // 코너 사이의 거리를 구하기
            float dis = Vector2.Distance(itemCorners[0], viewCorners[0]);
            //전체 길이 비율로 값 산출
            float value = dis / contentHeight;

            scroll.verticalNormalizedPosition -= value * Time.unscaledDeltaTime * scrollSpeed;

            return;
        }
        // 뷰포트보다 아이템이 위에 있을때
        else if (itemCorners[1].y > viewCorners[1].y)
        {
            // 코너 사이의 거리를 구하기
            float dis = Vector2.Distance(itemCorners[1], viewCorners[1]);
            //전체 길이 비율로 값 산출
            float value = dis / contentHeight;

            scroll.verticalNormalizedPosition += value * Time.unscaledDeltaTime * scrollSpeed;

            return;
        }
        else
        {
            isScroll = false;
        }
    }
}
