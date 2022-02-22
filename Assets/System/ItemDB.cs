using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class ItemInfo
{
    public bool hasItem = false; //플레이어가 갖고 있는 아이템인지
    public int id; //고유 아이디
    public string itemName; //아이템 이름
    public string itemType; //아이템 타입 (Gem, Heart, Scroll, Artifact, etc)
    // 6원소 타입
    public bool earth;
    public bool fire;
    public bool life;
    public bool lightning;
    public bool water;
    public bool wind;    
    public string description; //아이템 설명

    public ItemInfo(int id, string itemName, string itemType, bool earth, bool fire, bool life, bool lightning, bool water, bool wind, string description)
    {
        this.id = id;
        this.itemName = itemName;
        this.itemType = itemType;

        this.earth = earth;
        this.fire = fire;
        this.life = life;
        this.lightning = lightning;
        this.water = water;
        this.wind = wind;
        
        this.description = description;
    }
}

public class ItemDB : MonoBehaviour
{
    #region Singleton
    private static ItemDB instance;
    public static ItemDB Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<ItemDB>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<ItemDB>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public List<ItemInfo> itemDB = null; //마법 정보 DB
    public List<Sprite> itemIcon = null; //마법 아이콘 리스트
    public List<GameObject> itemPrefab = null; //마법 프리팹 리스트
    [HideInInspector]
    public bool loadDone = false; //로드 완료 여부

    void Awake()
    {
        //게임 시작과 함께 아이템DB 정보 로드하기
        // 아이템 DB, 아이콘, 프리팹 불러오기
        itemDB = new List<ItemInfo>();
        StartCoroutine(GetItemData());
    }

    //구글 스프레드 시트에서 Apps Script로 가공된 형태로 json 데이터 받아오기
    IEnumerator GetItemData()
    {
        //마법 아이콘, 프리팹 가져오기
        itemIcon = Resources.LoadAll<Sprite>("Item/Icon").ToList();
        itemPrefab = Resources.LoadAll<GameObject>("Item/Prefab").ToList();

        //Apps Script에서 가공된 json 데이터 문서 주소
        UnityWebRequest www = UnityWebRequest.Get("https://script.googleusercontent.com/macros/echo?user_content_key=SFxUnXenFob7Vylyu7Y_v1klMlQl8nsSqvMYR4EBlwac7E1YN3SXAnzmp-rU-50oixSn5ncWtdnTdVhtI4nUZ9icvz8bgj6om5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnDd5HMKPhPTDYFVpd6ZAI5lT6Z1PRDVSUH9zEgYKrhfZq5_-qo0tdzwRz-NvpaavXaVjRCMLKUCBqV1xma9LvJ-ti_cY4IfTKw&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp");
        // 해당 주소에 요청
        yield return www.SendWebRequest();

        //에러 뜰 경우 에러 표시
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error : " + www.error);
        }
        else
        {
            //! 실제 퍼블리싱에서는 json 텍스트 문서로 대체 할것
            //사이트의 문서에서 json 텍스트 받아오기
            string jsonData = www.downloadHandler.text;

            var jsonObj = JSON.Parse(jsonData);

            foreach (var row in jsonObj["itemData"])
            {
                //마법 한줄씩 데이터 파싱
                var item = JSON.Parse(row.ToString())[0];

                //받아온 데이터를 List<ItemInfo>에 넣기
                itemDB.Add(new ItemInfo(item["id"], item["name"], item["itemType"], 
                item["earth"], item["fire"], item["life"], item["lightning"], item["water"], item["wind"], 
                item["description"]));
            }

            // foreach (var item in itemDB)
            // {
            //     print(
            //     " id : " + item.id + " / " +
            //     " name : " + item.itemName + " / "
            //     );
            // }
        }

        //TODO 모든 아이템 프리팹에 아이템 정보 넣어주기
        // foreach (var item in itemPrefab)
        // {
        //     item.GetComponent<ItemManager>().item = GetItemByName(item.name);
        // }

        loadDone = true;
        print("ItemDB Loaded!");

        yield return null;
    }

    public ItemInfo GetItemByID(int id)
    {
        ItemInfo item = itemDB.Find(x => x.id == id);
        return item;
    }

    public string GetTypeByID(int id)
    {
        string type = itemDB.Find(x => x.id == id).itemType;
        return type;
    }

    public ItemInfo GetItemByName(string name)
    {
        ItemInfo item = itemDB.Find(x => x.itemName.Replace(" ", "") == name.Replace(" ", ""));
        // print(item.itemName.Replace(" ", "") + " : " + name.Replace(" ", ""));
        return item;
    }
}
