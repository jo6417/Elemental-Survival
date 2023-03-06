using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using TMPro;
using UnityEngine.EventSystems;

public class ProductToolTip : MonoBehaviour
{

    #region Singleton
    private static ProductToolTip instance;
    public static ProductToolTip Instance
    {
        get
        {
            if (instance == null)
            {
                //비활성화된 오브젝트도 포함
                var obj = FindObjectOfType<ProductToolTip>(true);
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    print("new obj");
                    var newObj = new GameObject().AddComponent<ProductToolTip>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public enum ToolTipCorner { LeftUp, LeftDown, RightUp, RightDown };
    // bool isFollow = false; //마우스 따라가기 여부
    bool SetDone = false; //모든 정보 표시 완료 여부
    // public bool offCall = false; //툴팁 끄라는 명령

    [Header("Refer")]
    public TextMeshProUGUI productType;
    public TextMeshProUGUI productName;
    public TextMeshProUGUI productDescript;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField, ReadOnly] RectTransform tooltipRect;
    [SerializeField, ReadOnly] Vector2 canvasRect;

    [Header("Magic")]
    public MagicInfo magic;
    List<int> stats = new List<int>();
    // public UIPolygon magicStatGraph;
    public GameObject recipeObj;
    public Image GradeFrame;
    public Image elementIcon_A;
    public Image elementIcon_B;
    public Image elementGrade_A;
    public Image elementGrade_B;

    [Header("Item")]
    public ItemInfo item;

    [Header("Debug")]
    public string magicName;

    private void Awake()
    {
        tooltipRect = GetComponent<RectTransform>();

        //마우스 클릭 입력
        UIManager.Instance.UI_Input.UI.Click.performed += val =>
        {
            QuitTooltip();
        };
        //마우스 위치 입력
        UIManager.Instance.UI_Input.UI.MousePosition.performed += val =>
            FollowMouse(UIManager.Instance.GetMousePos(false));

        //처음엔 끄기
        // gameObject.SetActive(false);
        canvasGroup.alpha = 0;
    }

    void FollowMouse(Vector3 nowMousePos)
    {
        //마우스 숨김 상태면 안따라감
        if (Cursor.lockState == CursorLockMode.Locked)
            return;

        // 툴팁 비활성화면 안따라감
        if (!gameObject.activeSelf)
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
    public void OpenTooltip(
        SlotInfo slotInfo = null,
        ToolTipCorner toolTipCorner = ToolTipCorner.LeftDown,
        Vector2 position = default(Vector2))
    {
        //툴팁 고정 위치 들어왔으면 이동
        if (position != default(Vector2))
        {
            //입력된 위치로 이동
            transform.position = position;
        }
        else
        {
            FollowMouse(UIManager.Instance.GetMousePos(false));
        }

        //툴팁 켜기
        gameObject.SetActive(true);
        canvasGroup.alpha = 0.7f;

        if (!tooltipRect)
            tooltipRect = GetComponent<RectTransform>();

        //툴팁 피벗 바꾸기
        switch (toolTipCorner)
        {
            case ToolTipCorner.LeftUp:
                tooltipRect.pivot = Vector2.up;
                break;
            case ToolTipCorner.LeftDown:
                tooltipRect.pivot = Vector2.zero;
                break;
            case ToolTipCorner.RightUp:
                tooltipRect.pivot = Vector2.one;
                break;
            case ToolTipCorner.RightDown:
                tooltipRect.pivot = Vector2.right;
                break;
        }

        //마법 or 아이템 정보 넣기
        magic = slotInfo as MagicInfo;
        item = slotInfo as ItemInfo;

        if (magic != null)
        {
            SetDone = SetMagicInfo();
        }

        if (item != null)
        {
            SetDone = SetItemInfo();
        }
    }

    //툴팁 끄기
    public void QuitTooltip()
    {
        // print("QuitTooltip");
        magic = null;
        item = null;

        SetDone = false;

        // gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
    }

    bool SetMagicInfo()
    {
        if (magic == null)
        {
            Debug.Log("magic is null!");
            return false;
        }

        // GradeFrame.gameObject.SetActive(true);
        // 프레임 색깔에 등급 표시
        GradeFrame.color = MagicDB.Instance.GradeColor[magic.grade];

        // 마법 타입 표시
        productType.text = magic.castType;
        // 마법 타입에 따라 색 바꾸기
        // switch (magic.castType)
        // {
        //     case MagicDB.MagicType.passive.ToString():
        //         productType.color = Color.cyan;
        //         break;

        //     case MagicDB.MagicType.active.ToString():
        //         productType.color = Color.red;
        //         break;

        //     case MagicDB.MagicType.ultimate.ToString():
        //         productType.color = Color.magenta;
        //         break;
        // }

        //마법 이름, 설명 넣기
        productName.text = magic.name;
        productDescript.text = magic.description;

        //해당 마법 언락 여부
        bool isUnlock = MagicDB.Instance.unlockMagics.Exists(x => x == magic.id);

        //마법 재료 찾기
        MagicInfo magicA = MagicDB.Instance.GetMagicByName(magic.element_A);
        MagicInfo magicB = MagicDB.Instance.GetMagicByName(magic.element_B);

        //재료 null 이면 재료 표시 안함
        if (magicA == null || magicB == null)
        {
            recipeObj.SetActive(false);
        }
        else
        {
            // 재료 A,B 아이콘 넣기, 미해금 마법이면 물음표 넣기
            elementIcon_A.sprite = isUnlock ? MagicDB.Instance.GetIcon(magicA.id) : SystemManager.Instance.questionMark;
            elementIcon_B.sprite = isUnlock ? MagicDB.Instance.GetIcon(magicB.id) : SystemManager.Instance.questionMark;

            // 재료 A,B 등급 넣기, 재료가 원소젬일때는 1등급 흰색
            elementGrade_A.color = MagicDB.Instance.GradeColor[magicA.grade];
            elementGrade_B.color = MagicDB.Instance.GradeColor[magicB.grade];

            recipeObj.SetActive(true);
        }

        return true;
    }

    bool SetItemInfo()
    {
        // 마법 재료 오브젝트 끄기
        recipeObj.SetActive(false);

        //마법 등급 프레임 끄기
        // GradeFrame.gameObject.SetActive(false);
        // 프레임 색깔에 등급 표시
        GradeFrame.color = MagicDB.Instance.GradeColor[item.grade];

        // 아이템 타입 표시
        productType.text = item.itemType;

        // 아이템 이름, 설명 넣기
        productName.text = item.name;
        productDescript.text = item.description;

        return true;
    }

    List<float> convertValue()
    {
        // 스탯값을 레이더 그래프 값으로 변형
        List<float> radarValue = new List<float>();
        foreach (var stat in stats)
        {
            radarValue.Add(stat / 7f + 1f / 7f);
        }

        return radarValue;
    }

    void GetMagicStats()
    {
        // if (magic != null)
        //     stats.Clear();
        // stats.Add(magic.power);
        // stats.Add(magic.speed);
        // stats.Add(magic.range);
        // stats.Add(magic.critical);
        // stats.Add(magic.pierce);
        // stats.Add(magic.projectile);
        // stats.Add(magic.power); //마지막값은 첫값과 같게

        // print(magic.power
        //     + " : " + magic.speed
        //     + " : " + magic.range
        //     + " : " + magic.critical
        //     + " : " + magic.pierce
        //     + " : " + magic.projectile
        // );
    }
}
