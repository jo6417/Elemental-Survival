using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using TMPro;

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

    // public bool isTooltipOn = true;
    public TextMeshProUGUI productName;
    public TextMeshProUGUI productDescript;
    public Sprite questionMark;

    [Header("Magic")]
    public MagicInfo magic;
    List<int> stats = new List<int>();
    public UIPolygon magicStatGraph;
    public GameObject recipeObj;
    public Image elementIcon_A;
    public Image elementIcon_B;
    public Image elementGrade_A;
    public Image elementGrade_B;

    [Header("Item")]
    public ItemInfo item;

    void Update()
    {
        FollowMouse();
    }

    void FollowMouse()
    {
        // Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0;
        transform.position = mousePos;
    }

    //툴팁 켜기
    public void OpenTooltip(MagicInfo magic = null, ItemInfo item = null)
    {
        FollowMouse();
        gameObject.SetActive(true);

        //마법 or 아이템 정보 넣기
        this.magic = magic;
        this.item = item;

        if (magic != null)
        {
            SetMagicInfo();
        }

        if (item != null)
        {
            SetItemInfo();
        }
    }

    //툴팁 끄기
    public void QuitTooltip()
    {
        // print("QuitTooltip");
        magic = null;
        item = null;

        gameObject.SetActive(false);
    }

    void SetMagicInfo()
    {
        if (magic == null)
        {
            Debug.Log("magic is null!");
            return;
        }

        //마법 이름, 설명 넣기
        productName.text = magic.magicName;
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
            return;
        }
        else
        {
            recipeObj.SetActive(true);
        }

        // 재료 A,B 아이콘 넣기, 미해금 마법이면 물음표 넣기
        elementIcon_A.sprite = isUnlock ? MagicDB.Instance.GetMagicIcon(magicA.id) : questionMark;
        elementIcon_B.sprite = isUnlock ? MagicDB.Instance.GetMagicIcon(magicB.id) : questionMark;

        // 재료 A,B 등급 넣기, 재료가 원소젬일때는 1등급 흰색
        elementGrade_A.color = MagicDB.Instance.gradeColor[magicA.grade];
        elementGrade_B.color = MagicDB.Instance.gradeColor[magicB.grade];
    }

    void SetItemInfo()
    {
        // 아이템 이름, 설명 넣기
        productName.text = item.itemName;
        productDescript.text = item.description;
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
