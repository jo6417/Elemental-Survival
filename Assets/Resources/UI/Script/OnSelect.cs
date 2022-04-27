using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public delegate void OnSelectCallBack();
public class OnSelect : MonoBehaviour, ISelectHandler
{
    [SerializeField]
    private bool isAutoClick = false; //선택만해도 클릭되게

    public OnSelectCallBack onSelect;
    private Button button;
    Vector3[] maskCorners = new Vector3[4];
    Vector3[] myCorners = new Vector3[4];

    private void Awake()
    {
        button = transform.GetComponent<Button>();
    }

    void ISelectHandler.OnSelect(BaseEventData eventData)
    {
        //선택되면 버튼 클릭 함수 호출
        if (isAutoClick)
            button.onClick.Invoke();

        //콜백에 들어있는 함수를 실행
        if (onSelect != null)
            onSelect();

        // 해당 버튼 네비 불러오기
        Navigation btnNav = button.navigation;
        if (btnNav.mode == Navigation.Mode.Explicit)
        {
            // 해당 버튼 위로 갈때 위에 있는 버튼으로 이동하게 지정
            btnNav.selectOnUp = button.FindSelectable(Vector3.up);
            button.navigation = btnNav;
        }

        //TODO 선택됬을때 해당 아이템 보이는 위치로 스크롤하기
        //TODO mask 1번 모서리가 transform 1번 모서리보다 아래에 있으면
        //TODO mask 3번 모서리가 transform 3번 모서리보다 위에 있으면
        //TODO 둘의 좌표 같아 질때까지 스크롤 올리기, 도중에 아이템MouseDown or Select or 들어오면 중지
    }

    IEnumerator ScrollMove()
    {
        while (maskCorners[3].y > myCorners[3].y)
        {
            //해당 아이템의 모서리 위치 불러오기
            transform.GetComponent<RectTransform>().GetWorldCorners(myCorners);

            //스크롤 내리기
            // scroll.verticalNormalizedPosition.

            yield return null;
        }
    }
}
