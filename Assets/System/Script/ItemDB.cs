using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

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

    public enum ItemType { Gem, Heal, Shard, Artifact, Gadget, Magic }; // 아이템 타입 정의

    public Dictionary<int, ItemInfo> itemDB = new Dictionary<int, ItemInfo>(); //아이템 정보 DB
    // public List<ItemInfo> itemDB = new List<ItemInfo>(); //아이템 정보 DB
    public List<Sprite> itemIcon = null; //아이템 아이콘 리스트
    public List<GameObject> itemPrefab = null; //아이템 프리팹 리스트
    public int[] outGemNum = new int[6]; //카메라 밖으로 나간 원소젬 개수
    public List<GameObject> outGem = new List<GameObject>(); //카메라 밖으로 나간 원소젬 리스트
    [HideInInspector]
    public bool loadDone = false; //로드 완료 여부
    public GameObject magicItemPrefab; // 마법 슬롯 아이템 프리팹

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
                itemDB[item["id"]] = (new ItemInfo
                (item["id"], item["grade"], item["name"], item["itemType"], item["description"], item["priceType"], item["price"],
                item["projectileNum"], item["hpMax"], item["power"], item["armor"], item["speed"], item["moveSpeed"],
                item["evade"], item["coolTime"], item["duration"], item["range"], item["luck"], item["expRate"], item["getRange"],
                item["earth"], item["fire"], item["life"], item["lightning"], item["water"], item["wind"]
                ));
            }

        }

        //모든 아이템 초기화
        // InitialItems();

        loadDone = true;
        print("ItemDB Loaded!");

        yield return null;
    }

    // public void InitialItems()
    // {
    //     foreach (ItemInfo item in itemDB.Values)
    //     {
    //         item.amount = 0;
    //     }
    // }

    public ItemInfo GetRandomItem(int targetGrade = 0)
    {
        // 랜덤 아이템 id를 넣을 리스트
        List<int> randomItemPool = new List<int>();

        // 모든 아이템 중 해당 등급 모두 넣기
        for (int i = 0; i < itemDB.Count; i++)
        {
            int grade = itemDB[i].grade;

            // 0등급이면 넘기기
            if (grade == 0)
                continue;

            // 등급을 명시했을때
            if (targetGrade != 0)
            {
                // 해당 등급의 아이템만 넣기
                if (grade == targetGrade)
                    randomItemPool.Add(itemDB[i].id);
            }
            // 등급을 명시하지 않았을때
            else
            {
                // 모든 아이템 넣기
                randomItemPool.Add(itemDB[i].id);
            }
        }

        // 등급 지정을 해줬는데, 해당 등급의 아이템이 하나도 없을때
        if (targetGrade != 0 && randomItemPool.Count == 0)
        {
            return null;
        }
        // 등급 지정 안했거나, 랜덤풀에 아이템이 존재할때
        else
        {
            // 뽑은 리스트에서 인덱스 랜덤 뽑기
            int index = Random.Range(0, randomItemPool.Count);

            // 마법 ID로 마법정보 불러와서 리턴
            return GetItemByID(randomItemPool[index]);
        }
    }

    // 타입별로 랜덤 아이템 뽑기
    public ItemInfo GetRandomItem(ItemType itemType, int targetGrade = 0)
    {
        // 랜덤 아이템 id를 넣을 리스트
        List<int> randomItemPool = new List<int>();

        // 모든 아이템 중 해당 등급 모두 넣기
        for (int i = 0; i < itemDB.Count; i++)
        {
            int grade = itemDB[i].grade;

            // 일치하는 아이템 타입일때
            if (itemDB[i].itemType == itemType.ToString())
            {
                // 등급을 명시했을때
                if (targetGrade != 0)
                {
                    // 해당 등급의 아이템만 넣기
                    if (grade == targetGrade)
                        randomItemPool.Add(itemDB[i].id);
                }
                // 등급을 명시하지 않았을때
                else
                {
                    // 모든 아이템 넣기
                    randomItemPool.Add(itemDB[i].id);
                }
            }
        }

        // 등급 지정을 해줬는데, 해당 등급의 아이템이 하나도 없을때
        if (targetGrade != 0 && randomItemPool.Count == 0)
        {
            return null;
        }
        // 등급 지정 안했거나, 랜덤풀에 아이템이 존재할때
        else
        {
            // 뽑은 리스트에서 인덱스 랜덤 뽑기
            int index = Random.Range(0, randomItemPool.Count);

            // 마법 ID로 마법정보 불러와서 리턴
            return GetItemByID(randomItemPool[index]);
        }
    }

    //랜덤 아티팩트 뽑기
    // public int[] RandomItemIndex(int amount)
    // {
    //     //모든 아이템 인덱스를 넣을 리스트
    //     List<int> randomIndex = new List<int>();

    //     // 랜덤 아이템 풀
    //     randomIndex.Add(GetItemByName("Empty Scroll").id);
    //     randomIndex.Add(GetItemByName("Health Potion").id);
    //     randomIndex.Add(GetItemByName("Mana Shard").id);
    //     randomIndex.Add(GetItemByName("Random Box").id);

    //     //랜덤 인덱스를 넣을 배열
    //     int[] randomNum = new int[amount];

    //     for (int i = 0; i < amount; i++)
    //     {
    //         // 획득 가능한 아이템 없을때
    //         if (randomIndex.Count == 0)
    //         {
    //             randomNum[i] = -1;
    //         }
    //         else
    //         {
    //             //인덱스 리스트에서 랜덤한 난수 생성
    //             int j = Random.Range(0, randomIndex.Count);
    //             int itemID = randomIndex[j];

    //             //랜덤 인덱스 숫자 넣기
    //             randomNum[i] = itemID;
    //             //이미 선택된 인덱스 제거
    //             randomIndex.RemoveAt(j);
    //         }
    //     }

    //     //인덱스 리스트 리턴
    //     return randomNum;
    // }

    public List<ItemInfo> GetItemsByType(ItemType itemType)
    {
        // 해당 타입을 가진 아이템 모두 찾기
        List<ItemInfo> randomItemPool = itemDB.Values.ToList().FindAll(x => x.itemType == itemType.ToString());

        // 찾은 리스트 반환
        return randomItemPool;
    }

    public ItemInfo GetItemByID(int id)
    {
        if (itemDB.TryGetValue(id, out ItemInfo value))
            return value;
        else
            return null;
    }

    public string GetItemNameByID(int id)
    {
        string returnName = "";
        if (itemDB.TryGetValue(id, out ItemInfo value))
            returnName = value.name;

        return returnName;
    }

    public string GetTypeByID(int id)
    {
        string type = "";
        if (itemDB.TryGetValue(id, out ItemInfo value))
            type = value.itemType;

        return type;
    }

    public ItemInfo GetItemByName(string name)
    {
        ItemInfo item = null;

        foreach (KeyValuePair<int, ItemInfo> value in itemDB)
        {
            // print(value.Value.id + " : " + value.Value.magicName.Replace(" ", "") + " : " + name.Replace(" ", ""));
            if (value.Value.name.Replace(" ", "") == name.Replace(" ", ""))
            {
                item = value.Value;
                break;
            }
        }

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

    public IEnumerator ItemDrop(SlotInfo slotInfo, Vector2 dropPos, Vector2 dropDir = default)
    {
        // 인벤토리 빈칸 없으면 필드 드랍
        GameObject dropObj = null;

        MagicInfo magicInfo = slotInfo as MagicInfo;
        ItemInfo itemInfo = slotInfo as ItemInfo;

        // 마법일때
        if (magicInfo != null)
        {
            // 마법 슬롯 아이템 만들기
            dropObj = LeanPool.Spawn(ItemDB.Instance.magicItemPrefab, dropPos, Quaternion.identity, SystemManager.Instance.itemPool);

            // 아이템 프레임 색 넣기
            dropObj.transform.Find("Frame").GetComponent<SpriteRenderer>().color = MagicDB.Instance.GradeColor[slotInfo.grade];

            // 아이템 아이콘 넣기
            dropObj.transform.Find("Icon").GetComponent<SpriteRenderer>().sprite = MagicDB.Instance.GetMagicIcon(slotInfo.id);
        }

        // 아이템일때
        if (itemInfo != null)
        {
            dropObj = LeanPool.Spawn(ItemDB.Instance.GetItemPrefab(itemInfo.id), dropPos, Quaternion.identity, SystemManager.Instance.itemPool);
        }

        // 아이템 정보 넣기
        if (dropObj.TryGetComponent(out ItemManager itemManager))
        {
            itemManager.itemInfo = slotInfo as ItemInfo;
            itemManager.magicInfo = slotInfo as MagicInfo;
        }

        // 아이템 정보 삭제
        slotInfo = null;

        // 아이템 콜라이더 찾기
        Collider2D itemColl = dropObj.GetComponent<Collider2D>();
        // 아이템 rigid 찾기
        Rigidbody2D itemRigid = dropObj.GetComponent<Rigidbody2D>();

        // 콜라이더 끄기
        itemColl.enabled = false;

        Vector3 itemDir;
        // 드롭 방향 지정 했을때
        if (dropDir != default)
            itemDir = dropDir;
        // 드롭 방향 지정 없을때 
        else
            // 플레이어 반대 방향, 랜덤 각도 추가
            itemDir = dropObj.transform.position - PlayerManager.Instance.transform.position * Random.Range(10f, 20f);

        // 추가 각도를 벡터로 바꾸기
        float angle = Random.Range(-45, 45);
        Vector3 addDir = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), Mathf.Cos(Mathf.Deg2Rad * angle), 0) * Random.Range(10f, 20f);
        // print(addDir);

        // 플레이어 반대 방향, 랜덤 파워로 아이템 날리기
        itemRigid.velocity = itemDir + addDir;

        // 랜덤으로 방향 및 속도 결정
        float randomRotate = Random.Range(1f, 3f);
        // 아이템 랜덤 속도로 회전 시키기
        itemRigid.angularVelocity = randomRotate < 2f ? 90f * randomRotate : -90f * randomRotate;

        yield return new WaitForSeconds(1f);

        // 콜라이더 켜기
        itemColl.enabled = true;
    }
}
