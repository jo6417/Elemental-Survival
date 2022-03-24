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

    [Header("Magic")]
    public MagicInfo magic;
    List<int> stats = new List<int>();
    public UIPolygon magicStatGraph;
    public Image magicIcon;

    public GameObject magicElement;
    public Image elementIcon_A;
    public Image elementIcon_B;
    public Image elementGrade_A;
    public Image elementGrade_B;

    [Header("Item")]
    public ItemInfo item;
    public Image itemIcon;
    public Image itemFrame;

    void Update()
    {
        FollowMouse();
    }

    void FollowMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
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
            magicElement.SetActive(true);
            magicIcon.gameObject.SetActive(true);
            itemIcon.gameObject.SetActive(false);

            SetMagicInfo();
        }

        if (item != null)
        {
            magicElement.SetActive(false);
            magicIcon.gameObject.SetActive(false);
            itemIcon.gameObject.SetActive(true);

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

        //마법 재료 찾기
        MagicInfo magicA = MagicDB.Instance.GetMagicByName(magic.element_A);
        MagicInfo magicB = MagicDB.Instance.GetMagicByName(magic.element_B);

        //마법 이름, 설명 넣기
        productName.text = magic.magicName;
        productDescript.text = magic.description;

        // 아이콘 넣기
        magicIcon.sprite = MagicDB.Instance.magicIcon.Find(
            x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

        // 재료 A,B 아이콘 넣기
        // print(magic.element_A + " : " + magic.element_B);

        elementIcon_A.sprite = MagicDB.Instance.magicIcon.Find(
        x => x.name == magic.element_A.Replace(" ", "") + "_Icon");
        elementIcon_B.sprite = MagicDB.Instance.magicIcon.Find(
        x => x.name == magic.element_B.Replace(" ", "") + "_Icon");

        // 재료 A,B 등급 넣기, 재료가 원소젬일때는 1등급 흰색
        try
        {
            elementGrade_A.color = MagicDB.Instance.gradeColor[magicA.grade - 1];
        }
        catch (System.Exception)
        {
            elementGrade_A.color = Color.white;
        }

        try
        {
            elementGrade_B.color = MagicDB.Instance.gradeColor[magicB.grade - 1];
        }
        catch (System.Exception)
        {
            elementGrade_A.color = Color.white;
        }

        // // 마법 모든 스탯 가져오기
        // GetMagicStats();

        // // 마법 스펙 그래프에 반영하기
        // magicStatGraph.VerticesDistances = convertValue().ToArray();
        // magicStatGraph.OnRebuildRequested();

    }

    void SetItemInfo()
    {
        // 아이콘 넣기
        itemIcon.sprite = ItemDB.Instance.itemIcon.Find(
            x => x.name == item.itemName.Replace(" ", "") + "_Icon");

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
