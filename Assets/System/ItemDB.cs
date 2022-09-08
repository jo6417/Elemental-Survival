using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class ItemInfo : SlotInfo
{
    public int amount = 0; //몇개 갖고 있는지

    [Header("Info")]
    // public int id; //고유 아이디
    // public int grade; //아이템 등급
    // public string description; //아이템 설명
    // public int price; //아이템 가격
    // public string name; //아이템 이름
    public string itemType; //아이템 타입 (Gem, Heart, Scroll, Artifact, etc)
    public string priceType; //지불 원소 종류

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
        this.slotType = SlotType.Item;
        this.id = id;
        this.grade = grade;
        this.name = itemName;
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

    public ItemInfo(ItemInfo item)
    {
        this.slotType = SlotType.Item;
        this.id = item.id;
        this.grade = item.grade;
        this.name = item.name;
        this.itemType = item.itemType;
        this.description = item.description;
        this.priceType = item.priceType;
        this.price = item.price;
        this.projectileNum = item.projectileNum;
        this.hpMax = item.hpMax;
        this.power = item.power;
        this.armor = item.armor;
        this.moveSpeed = item.moveSpeed;
        this.rateFire = item.rateFire;
        this.coolTime = item.coolTime;
        this.duration = item.duration;
        this.range = item.range;
        this.luck = item.luck;
        this.expGain = item.expGain;
        this.moneyGain = item.moneyGain;
        this.earth = item.earth;
        this.fire = item.fire;
        this.life = item.life;
        this.lightning = item.lightning;
        this.water = item.water;
        this.wind = item.wind;
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

    public List<ItemInfo> itemDB = new List<ItemInfo>(); //아이템 정보 DB
    public List<Sprite> itemIcon = null; //아이템 아이콘 리스트
    public List<GameObject> itemPrefab = null; //아이템 프리팹 리스트
    public int[] outGemNum = new int[6]; //카메라 밖으로 나간 원소젬 개수
    public List<GameObject> outGem = new List<GameObject>(); //카메라 밖으로 나간 원소젬 리스트
    [HideInInspector]
    public bool loadDone = false; //로드 완료 여부

    void Awake()
    {
        // 아이콘, 프리팹 불러오기
        GetItemResources();
    }

    void GetItemResources()
    {
        //아이템 아이콘, 프리팹 가져오기
        itemIcon = Resources.LoadAll<Sprite>("Item/Icon").ToList();
        itemPrefab = Resources.LoadAll<GameObject>("Item/Prefab").ToList();
    }

    public void ItemDBSynchronize()
    {
        // 몬스터 DB 동기화 (웹 데이터 로컬에 저장 및 불러와서 DB에 넣기)
        StartCoroutine(ItemDBSync());
    }

    IEnumerator ItemDBSync()
    {
        // 버튼 동기화 아이콘 애니메이션 켜기
        Animator btnAnim = SystemManager.Instance.itemDBSyncBtn.GetComponentInChildren<Animator>();
        btnAnim.enabled = true;

        // 웹에서 새로 데이터 받아서 웹 세이브데이터의 json 최신화
        yield return StartCoroutine(
            SaveManager.Instance.WebDataLoad(
                SystemManager.DBType.Item,
                "https://script.googleusercontent.com/macros/echo?user_content_key=SFxUnXenFob7Vylyu7Y_v1klMlQl8nsSqvMYR4EBlwac7E1YN3SXAnzmp-rU-50oixSn5ncWtdnTdVhtI4nUZ9icvz8bgj6om5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnDd5HMKPhPTDYFVpd6ZAI5lT6Z1PRDVSUH9zEgYKrhfZq5_-qo0tdzwRz-NvpaavXaVjRCMLKUCBqV1xma9LvJ-ti_cY4IfTKw&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp"
        ));

        // 로컬 DB 데이터에 웹에서 가져온 DB 데이터를 덮어쓰기
        SaveManager.Instance.localSaveData.itemDBJson = SaveManager.Instance.webSaveData.itemDBJson;

        // DB 수정된 로컬 데이터를 저장, 완료시까지 대기
        yield return StartCoroutine(SaveManager.Instance.Save());

        // 로컬 데이터에서 파싱해서 DB에 넣기, 완료시까지 대기
        yield return StartCoroutine(GetItemDB());

        // 동기화 여부 다시 검사
        yield return StartCoroutine(
            SaveManager.Instance.DBSyncCheck(
                SystemManager.DBType.Item,
                SystemManager.Instance.itemDBSyncBtn,
                "https://script.googleusercontent.com/macros/echo?user_content_key=SFxUnXenFob7Vylyu7Y_v1klMlQl8nsSqvMYR4EBlwac7E1YN3SXAnzmp-rU-50oixSn5ncWtdnTdVhtI4nUZ9icvz8bgj6om5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnDd5HMKPhPTDYFVpd6ZAI5lT6Z1PRDVSUH9zEgYKrhfZq5_-qo0tdzwRz-NvpaavXaVjRCMLKUCBqV1xma9LvJ-ti_cY4IfTKw&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp"
            ));

        // 아이콘 애니메이션 끄기
        btnAnim.enabled = false;
    }

    //구글 스프레드 시트에서 Apps Script로 가공된 형태로 json 데이터 받아오기
    public IEnumerator GetItemDB()
    {
        // 웹에서 불러온 json string이 비어있지 않으면
        if (SaveManager.Instance.localSaveData.itemDBJson != "")
        {
            var jsonObj = JSON.Parse(SaveManager.Instance.localSaveData.itemDBJson);

            foreach (var row in jsonObj["itemData"])
            {
                //아이템 한줄씩 데이터 파싱
                var item = JSON.Parse(row.ToString())[0];

                //받아온 데이터를 List<ItemInfo>에 넣기
                itemDB.Add(new ItemInfo
                (item["id"], item["grade"], item["name"], item["itemType"], item["description"], item["priceType"], item["price"],
                item["projectileNum"], item["hpMax"], item["power"], item["armor"], item["moveSpeed"],
                item["rateFire"], item["coolTime"], item["duration"], item["range"], item["luck"], item["expGain"], item["moneyGain"],
                item["earth"], item["fire"], item["life"], item["lightning"], item["water"], item["wind"]
                ));
            }

        }

        //모든 아이템 초기화
        InitialItems();

        loadDone = true;
        print("ItemDB Loaded!");

        yield return null;
    }

    public void InitialItems()
    {
        foreach (var item in itemDB)
        {
            item.amount = 0;
        }
    }

    //랜덤 아티팩트 뽑기
    public int[] RandomItemIndex(int amount)
    {
        //모든 아이템 인덱스를 넣을 리스트
        List<int> randomIndex = new List<int>();

        // 랜덤 아이템 풀
        randomIndex.Add(GetItemByName("Empty Scroll").id);
        randomIndex.Add(GetItemByName("Health Potion").id);
        randomIndex.Add(GetItemByName("Mana Shard").id);
        randomIndex.Add(GetItemByName("Random Box").id);

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
        return item.name;
    }

    public string GetTypeByID(int id)
    {
        string type = itemDB.Find(x => x.id == id).itemType;
        return type;
    }

    public ItemInfo GetItemByName(string name)
    {
        ItemInfo item = itemDB.Find(x => x.name.Replace(" ", "") == name.Replace(" ", ""));
        // print(item.itemName.Replace(" ", "") + " : " + name.Replace(" ", ""));
        return item;
    }

    public Sprite GetItemIcon(int id)
    {
        //프리팹의 이름
        string itemName = GetItemByID(id).name.Replace(" ", "") + "_Icon";

        Sprite sprite = null;

        sprite = itemIcon.Find(x => x.name == itemName);
        return sprite;
    }

    public GameObject GetItemPrefab(int id)
    {
        //프리팹의 이름
        string itemName = GetItemByID(id).name.Replace(" ", "") + "_Prefab";

        GameObject prefab = null;

        prefab = itemPrefab.Find(x => x.name == itemName);
        return prefab;
    }
}
