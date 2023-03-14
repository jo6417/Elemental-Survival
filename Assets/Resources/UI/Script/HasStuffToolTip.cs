using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HasStuffToolTip : MonoBehaviour
{
    #region Singleton
    private static HasStuffToolTip instance;
    public static HasStuffToolTip Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<HasStuffToolTip>();
                if (instance == null)
                {
                    // GameObject obj = new GameObject();
                    // obj.name = "HasStuffToolTip";
                    // instance = obj.AddComponent<HasStuffToolTip>();
                }
            }
            return instance;
        }
    }
    #endregion

    public TextMeshProUGUI stuffName;
    public TextMeshProUGUI stuffDescription;
    public MagicInfo magic;
    public ItemInfo item;
    // float halfCanvasWidth;
    [SerializeField] CanvasGroup canvasGroup;

    [SerializeField, ReadOnly] RectTransform tooltipRect;
    [SerializeField, ReadOnly] Vector2 canvasRect;
    public NewInput Tooltip_Input; // 툴팁 인풋 받기

    private void Awake()
    {
        // 다른 오브젝트가 이미 있을 때
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        tooltipRect = GetComponent<RectTransform>();

        //처음엔 끄기
        // gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        Tooltip_Input = new NewInput();

        //마우스 클릭 입력
        Tooltip_Input.UI.Click.performed += val =>
        {
            QuitTooltip();
        };
        //마우스 위치 입력
        Tooltip_Input.UI.MousePosition.performed += val =>
        {
            if (canvasGroup.alpha > 0f)
                FollowMouse(Tooltip_Input.UI.MousePosition.ReadValue<Vector2>());
        };

        Tooltip_Input.Enable();
    }

    private void OnDisable()
    {
        if (Tooltip_Input != null)
            Tooltip_Input.Disable();
    }

    void FollowMouse(Vector3 nowMousePos)
    {
        //마우스 숨김 상태면 안따라감
        if (Cursor.lockState == CursorLockMode.Locked)
            return;

        // 화면 사이즈 계산
        canvasRect = SystemManager.Instance.ActualScreenSize();

        // 스크린 비율 계산
        Vector2 canvasSize = transform.parent.GetComponent<RectTransform>().sizeDelta;
        float screenRate = canvasSize.x / 1920f;
        // 스크린 비율만큼 툴팁 사이즈 계산
        transform.localScale = Vector2.one * screenRate;

        // 툴팁이 화면 밖으로 나간만큼 반대로 밀기
        #region TooltipOffset
        // 나간만큼 오프셋 조정
        Vector3 tooltipOffset = Vector2.zero;
        Vector3 tooltipSize = tooltipRect.sizeDelta * screenRate;
        // 툴팁이 화면 오른쪽 끝을 넘어가는 경우
        if (nowMousePos.x + tooltipSize.x > canvasRect.x)
            tooltipOffset.x = nowMousePos.x + tooltipSize.x - canvasRect.x;
        // 툴팁이 화면 위쪽 끝을 넘어가는 경우
        if (nowMousePos.y + tooltipSize.y > canvasRect.y)
            tooltipOffset.y = nowMousePos.y + tooltipSize.y - canvasRect.y;
        #endregion

        // 마우스 위치에 오프셋 추가해서 툴팁 위치 산출
        Vector3 tooltipPos = nowMousePos - tooltipOffset;
        // 월드 위치로 변경
        tooltipPos = Camera.main.ScreenToWorldPoint(tooltipPos);
        // 마우스 월드 위치로 이동
        transform.position = tooltipPos;

        // print(nowMousePos + " : " + transform.parent.GetComponent<RectTransform>().sizeDelta + " : " + canvasRect + " : " + tooltipOffset);

        // 로컬 z 축 0으로 초기화
        Vector3 localPos = transform.localPosition;
        localPos.z = 0;
        transform.localPosition = localPos;
    }

    //툴팁 켜기
    public void OpenTooltip(SlotInfo slotInfo = null)
    {
        // 마우스 위치 받기
        Vector2 mousePos = Tooltip_Input.UI.MousePosition.ReadValue<Vector2>();

        //마우스 위치로 이동 후 활성화
        FollowMouse(mousePos);
        gameObject.SetActive(true);
        canvasGroup.alpha = 0.7f;

        //마법 or 아이템 정보 넣기
        this.magic = slotInfo as MagicInfo;
        this.item = slotInfo as ItemInfo;

        string name = "";
        string description = "";

        // 마법 정보가 있을때
        if (slotInfo != null)
        {
            name = slotInfo.name;
            description = slotInfo.description;
        }

        // 아이템 정보가 있을때
        if (item != null)
        {
            name = item.name;
            description = item.description;
        }

        //이름, 설명 넣기
        stuffName.text = name;
        stuffDescription.text = description;
    }

    //툴팁 끄기
    public void QuitTooltip()
    {
        // print("QuitTooltip");
        magic = null;
        item = null;

        // gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
    }
}
