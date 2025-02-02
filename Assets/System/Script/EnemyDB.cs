using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyInfo
{
    public int id;
    public int grade;
    public string name;
    public string enemyType;
    public string elementType;
    public float spawnCool;
    public string description;

    [Header("Stat")]
    public float power;
    public float speed;
    public float range;
    public float cooltime;
    public float dropRate;
    public float hitDelay;
    public float hpMax;
    public float knockbackForce;

    [Header("Element")]
    // 원소 공격력 추가
    public float earth;
    public float fire;
    public float life;
    public float lightning;
    public float water;
    public float wind;

    public EnemyInfo() { }

    public EnemyInfo(int id, int grade, string enemyName, string enemyType, string elementType, float spawnCool, string description, float power, float speed, float range, float cooltime, float dropRate, float hitDelay, float hpMax, float knockbackForce, float earth, float fire, float life, float lightning, float water, float wind)
    {
        this.id = id;
        this.grade = grade;
        this.name = enemyName;
        this.enemyType = enemyType;
        this.elementType = elementType;
        this.spawnCool = spawnCool;
        this.description = description;
        this.power = power;
        this.speed = speed;
        this.range = range;
        this.cooltime = cooltime;
        this.dropRate = dropRate;
        this.hitDelay = hitDelay;
        this.hpMax = hpMax;
        this.knockbackForce = knockbackForce;

        this.earth = earth;
        this.fire = fire;
        this.life = life;
        this.lightning = lightning;
        this.water = water;
        this.wind = wind;
    }

    public EnemyInfo(EnemyInfo enemy)
    {
        this.id = enemy.id;
        this.grade = enemy.grade;
        this.name = enemy.name;
        this.enemyType = enemy.enemyType;
        this.elementType = enemy.elementType;
        this.spawnCool = enemy.spawnCool;
        this.description = enemy.description;
        this.power = enemy.power;
        this.speed = enemy.speed;
        this.range = enemy.range;
        this.cooltime = enemy.cooltime;
        this.dropRate = enemy.dropRate;
        this.hitDelay = enemy.hitDelay;
        this.hpMax = enemy.hpMax;
        this.knockbackForce = enemy.knockbackForce;

        this.earth = enemy.earth;
        this.fire = enemy.fire;
        this.life = enemy.life;
        this.lightning = enemy.lightning;
        this.water = enemy.water;
        this.wind = enemy.wind;
    }
}

public class EnemyDB : MonoBehaviour
{
    #region Singleton
    private static EnemyDB instance;
    public static EnemyDB Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<EnemyDB>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = "EnemyDB";
                    instance = obj.AddComponent<EnemyDB>();
                }
            }
            return instance;
        }
    }
    #endregion

    [ReadOnly] public bool initDone = false; //로드 완료 여부
    public enum EnemyType { Normal, Boss };

    public Dictionary<int, EnemyInfo> enemyDB = new Dictionary<int, EnemyInfo>(); //몬스터 정보 DB
    public Dictionary<string, Sprite> enemyIcon = new Dictionary<string, Sprite>(); //몬스터 아이콘 리스트
    public Dictionary<string, GameObject> enemyPrefab = new Dictionary<string, GameObject>(); //몬스터 프리팹 리스트

    void Awake()
    {
        // 다른 오브젝트가 이미 있을 때
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 몬스터 아이콘, 프리팹 불러오기
        GetEnemyResources();
    }

    void GetEnemyResources()
    {
        // 몬스터 아이콘 모두 가져오기
        Sprite[] _enemyIcon = Resources.LoadAll<Sprite>("Character/Icon");
        foreach (var icon in _enemyIcon)
        {
            enemyIcon[icon.name] = icon;
        }

        // 몬스터 프리팹 모두 가져오기
        GameObject[] _enemyPrefab = Resources.LoadAll<GameObject>("Character/Prefab");
        foreach (var prefab in _enemyPrefab)
        {
            enemyPrefab[prefab.name] = prefab;
        }
    }

    public void EnemyDBSynchronize(bool autoSave = true)
    {
        // 몬스터 DB 동기화 (웹 데이터 로컬에 저장 및 불러와서 DB에 넣기)
        StartCoroutine(EnemyDBSync(autoSave));
    }

    IEnumerator EnemyDBSync(bool autoSave = true)
    {
        // 버튼 동기화 아이콘 애니메이션 켜기
        Animator btnAnim = SystemManager.Instance.enemyDBSyncBtn.GetComponentInChildren<Animator>();
        btnAnim.enabled = true;

        // 웹에서 새로 데이터 받아서 웹 세이브데이터의 json 최신화
        yield return StartCoroutine(SaveManager.Instance.WebDataLoad(DBType.Enemy));

        // 로컬 세이브데이터에 웹에서 가져온 세이브데이터를 덮어쓰기
        SaveManager.Instance.localSaveData.enemyDBJson = SaveManager.Instance.webSaveData.enemyDBJson;

        // 로컬 세이브데이터에서 불러와 enemyDB에 넣기, 완료시까지 대기
        yield return StartCoroutine(GetEnemyDB());

        // 자동 저장일때
        if (autoSave)
        {
            // 수정된 로컬 세이브데이터를 저장, 완료시까지 대기
            yield return StartCoroutine(SaveManager.Instance.Save());

            // DB 전부 Enum으로 바꿔서 저장
            yield return StartCoroutine(SaveManager.Instance.DBtoEnum());

            // 동기화 여부 다시 검사
            yield return StartCoroutine(SaveManager.Instance.DBSyncCheck(DBType.Enemy, SystemManager.Instance.enemyDBSyncBtn));
        }

        // 아이콘 애니메이션 끄기
        btnAnim.enabled = false;
    }

    //구글 스프레드 시트에서 Apps Script로 가공된 형태로 json 데이터 받아오기
    public IEnumerator GetEnemyDB()
    {
        // 웹에서 불러온 json string이 비어있지 않으면
        if (SaveManager.Instance.localSaveData.enemyDBJson != "")
        {
            var jsonObj = JSON.Parse(SaveManager.Instance.localSaveData.enemyDBJson);

            foreach (var row in jsonObj["enemyData"])
            {
                //몬스터 한줄씩 데이터 파싱
                var enemy = JSON.Parse(row.ToString())[0];

                //받아온 데이터를 List<EnemyInfo>에 넣기
                enemyDB[enemy["id"]] = new EnemyInfo
                (enemy["id"], enemy["grade"], enemy["name"], enemy["enemyType"], enemy["elementType"], enemy["spawnCool"], enemy["description"],
                enemy["power"], enemy["speed"], enemy["range"], enemy["cooltime"], enemy["dropRate"], enemy["hitDelay"], enemy["hpMax"], enemy["knockbackForce"],
                enemy["earth"], enemy["fire"], enemy["life"], enemy["lightning"], enemy["water"], enemy["wind"]
                );

                yield return null;
            }

        }

        initDone = true;
        print("EnemyDB Loaded!");

        yield return null;
    }

    public EnemyInfo GetEnemyByID(int id)
    {
        if (enemyDB.TryGetValue(id, out EnemyInfo value))
            return value;
        else
            return null;
    }

    public EnemyInfo GetEnemyByName(string enemyName)
    {
        EnemyInfo enemy = null;

        foreach (KeyValuePair<int, EnemyInfo> value in enemyDB)
        {
            if (value.Value.name.Replace(" ", "") == enemyName.Replace(" ", ""))
            {
                enemy = value.Value;
                break;
            }
        }
        return enemy;
    }

    public string GetNameByID(int id)
    {
        if (enemyDB.TryGetValue(id, out EnemyInfo value))
            return value.name;
        else
            return null;
    }

    public Sprite GetIcon(int id)
    {
        //아이콘의 이름
        string enemyName = GetEnemyByID(id).name.Replace(" ", "") + "_Icon";

        if (enemyIcon.TryGetValue(enemyName, out Sprite icon))
            return icon;
        else
            return null;
    }

    public GameObject GetPrefab(int id)
    {
        //프리팹의 이름
        EnemyInfo enemy = GetEnemyByID(id);
        string enemyName = enemy.name.Replace(" ", "") + "_Prefab";

        if (enemyPrefab.TryGetValue(enemyName, out GameObject prefab))
            return prefab;
        else
            return null;
    }

    public int RandomEnemy(int maxGrade = 0, bool isBoss = false)
    {
        //TODO 등급별로 확률 지정할것
        // 랜덤 몬스터 등급 뽑기 1~6
        int randGrade = Random.Range(1, maxGrade + 1);

        //랜덤 등급의 모든 몬스터 임시 리스트에 추가
        List<int> tempList = new List<int>();
        foreach (KeyValuePair<int, EnemyInfo> info in enemyDB)
        {
            // 등급을 지정했을때
            if (maxGrade > 0)
                //해당 등급 아니면 넘기기
                if (info.Value.grade != randGrade)
                    continue;

            // 몬스터 타입이 일치할때
            if ((!isBoss && info.Value.enemyType == EnemyType.Normal.ToString())
            || (isBoss && info.Value.enemyType == EnemyType.Boss.ToString()))
                // 프리팹이 있을때
                if (GetPrefab(info.Value.id) != null)
                    tempList.Add(info.Value.id);
        }

        //임시리스트에서 랜덤 인덱스로 뽑기
        int id = -1;
        if (tempList.Count != 0)
            id = tempList[Random.Range(0, tempList.Count)];

        return id;
    }
}
