using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// public delegate void OnSelectCallBack();
public class ButtonEvents : MonoBehaviour, ISelectHandler
{
    public bool showUICursor = true; //ui커서 표시 여부
    [SerializeField]
    private bool isAutoClick = false; //선택만해도 클릭되게
    public OnSelectAutoScroll autoScroll = null;

    // public OnSelectCallBack onSelect;
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
        // if (onSelect != null)
        //     onSelect();

        // 해당 버튼 네비 불러오기
        Navigation btnNav = button.navigation;
        if (btnNav.mode == Navigation.Mode.Explicit)
        {
            // 해당 버튼 위로 갈때 위에 있는 버튼으로 이동하게 지정
            btnNav.selectOnUp = button.FindSelectable(Vector3.up);
            button.navigation = btnNav;
        }

        // 스크롤뷰 변수 있으면 신호 보내기
        if(autoScroll != null)
        {
            autoScroll.SetScrollItem(transform);
        }
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

    public void ButtonNavOff()
    {
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Explicit;
        button.navigation = nav;
    }

    public void ButtonNavAuto()
    {
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Automatic;
        button.navigation = nav;
    }
}
