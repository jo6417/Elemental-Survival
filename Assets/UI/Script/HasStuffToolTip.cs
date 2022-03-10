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
                //비활성화된 오브젝트도 포함
                var obj = FindObjectOfType<HasStuffToolTip>(true);
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    print("new obj");
                    var newObj = new GameObject().AddComponent<HasStuffToolTip>();
                    instance = newObj;
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
    float halfCanvasWidth;
    RectTransform rect;
    
    private void Start() {
        halfCanvasWidth = GetComponentInParent<CanvasScaler>().referenceResolution.x * 0.5f;
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if(rect.anchoredPosition.x + rect.sizeDelta.x > halfCanvasWidth)
        {
            rect.pivot = new Vector2(1,0);
        }
        else{
            rect.pivot = new Vector2(0,0);
        }

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        transform.position = mousePos;

        //TODO 패널이 화면밖으로 안나가게 방지

    }

    //툴팁 켜기
    public void OpenTooltip(MagicInfo magic = null, ItemInfo item = null)
    {
        gameObject.SetActive(true);

        //마법 or 아이템 정보 넣기
        this.magic = magic;
        this.item = item;

        string name = "";
        string description = "";

        // 마법 정보가 있을때
        if (magic != null)
        {
            name = magic.magicName;
            description = magic.description;
        }

        // 아이템 정보가 있을때
        if (item != null)
        {
            name = item.itemName;
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

        gameObject.SetActive(false);
    }
}
