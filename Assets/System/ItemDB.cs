using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class ItemInfo
{
    public int hasNum = 0; //몇개 갖고 있는지

    public int id; //고유 아이디
    public int grade; //아이템 등급
    public string itemName; //아이템 이름
    public string itemType; //아이템 타입 (Gem, Heart, Scroll, Artifact, etc)
    public string description; //아이템 설명
    public string priceType; //지불 원소 종류
    public int price; //아이템 가격
    
    [Header("Buff")] // 능력치 추가 계수 (곱연산 기본값 : 1 / 합연산 기본값 : 0)
    public int projectileNum = 0; // 투사체 개수
    public float hpMax = 1; //최대 체력
    public float power = 1; //마법 공격력
    public float armor = 1; //방어력
    public float moveSpeed = 1; //이동 속도
    public float rateFire = 1; //마법 공격속도
    public float coolTime = 1; //마법 쿨타임
    public float duration = 1; //마법 지속시간
    public float range = 1; //마법 범위
    public float luck = 1; //행운
    public float expGain = 1; //경험치 획득량
    public float moneyGain = 1; //원소젬 획득량

    // 원소 공격력 추가
    public float earth;
    public float fire;
    public float life;
    public float lightning;
    public float water;
    public float wind;

    public ItemInfo(int id, int grade, string itemName, string itemType, string description, string priceType, int price, int projectileNum, float hpMax, float power, float armor, float moveSpeed, float rateFire, float coolTime, float duration, float range, float luck, float expGain, float moneyGain, float earth, float fire, float life, float lightning, float water, float wind)
    {
        this.id = id;
        this.grade = grade;
        this.itemName = itemName;
        this.itemType = itemType;
        this.description = description;
        this.priceType = priceType;
        this.price = price;

        this.projectileNum = projectileNum;
        this.hpMax = hpMax;
        this.power = power;
        this.armor = armor;
        this.moveSpeed = moveSpeed;
        this.rateFire = rateFire;
        this.coolTime = coolTime;
        this.duration = duration;
        this.range = range;
        this.luck = luck;
        this.expGain = expGain;
        this.moneyGain = moneyGain;

        this.earth = earth;
        this.fire = fire;
        this.life = life;
        this.lightning = lightning;
        this.water = water;
        this.wind = wind;
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
                itemDB.Add(new ItemInfo
                (item["id"], item["grade"], item["name"], item["itemType"], item["description"], item["priceType"], item["price"], 
                item["projectileNum"], item["hpMax"], item["power"], item["armor"], item["moveSpeed"], 
                item["rateFire"], item["coolTime"], item["duration"], item["range"], item["luck"], item["expGain"], item["moneyGain"], 
                item["earth"], item["fire"], item["life"], item["lightning"], item["water"], item["wind"]
                ));
            }

            // foreach (var item in itemDB)
            // {
            //     print(
            //     " id : " + item.id + " / " +
            //     " name : " + item.itemName + " / " + 
            //     " earth : " + item.earth + " / " + 
            //     " fire : " + item.fire + " / " + 
            //     " life : " + item.life + " / " + 
            //     " lightning : " + item.lightning + " / " + 
            //     " water : " + item.water + " / " + 
            //     " wind : " + item.wind + " / "
            //     );
            // }
        }

        //모든 아이템 초기화
        InitialItems();

        //TODO 모든 아이템 프리팹에 아이템 정보 넣어주기
        // foreach (var item in itemPrefab)
        // {
        //     item.GetComponent<ItemManager>().item = GetItemByName(item.name);
        // }

        loadDone = true;
        print("ItemDB Loaded!");

        yield return null;
    }

    public void InitialItems(){
        foreach (var item in itemDB)
        {
            item.hasNum = 0;
        }
    }

    //랜덤 아티팩트 뽑기
    public int[] RandomArtifactIndex(List<ItemInfo> dbList, int amount)
    {
        //모든 아이템 인덱스를 넣을 리스트
        List<int> randomIndex = new List<int>();

        //아이템 id 모두 넣기
        for (int i = 0; i < dbList.Count; i++)
        {
            randomIndex.Add(dbList[i].id);
        }

        //랜덤 인덱스를 넣을 배열
        int[] randomNum = new int[amount];

        for (int i = 0; i < amount; i++)
        {
            // 획득 가능한 아이템 없을때
            if (randomIndex.Count == 0)
            {
                randomNum[i] = -1;
            }
            else
            {
                //인덱스 리스트에서 랜덤한 난수 생성
                int j = Random.Range(0, randomIndex.Count);
                int itemID = randomIndex[j];
                // print(magicIndex.Count + " : " + index);

                //랜덤 인덱스 숫자 넣기
                randomNum[i] = itemID;
                //이미 선택된 인덱스 제거
                randomIndex.RemoveAt(j);
            }
        }

        //인덱스 리스트 리턴
        return randomNum;
    }

    public ItemInfo GetItemByID(int id)
    {
        ItemInfo item = itemDB.Find(x => x.id == id);
        return item;
    }

    public string GetItemNameByID(int id)
    {
        ItemInfo item = itemDB.Find(x => x.id == id);
        return item.itemName;
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
