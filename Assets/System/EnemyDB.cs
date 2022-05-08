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
    public string enemyName;
    public string enemyType;
    public string description;

    [Header("Spec")]
    public float power;
    public float speed;
    public float range;
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

    public EnemyInfo(){}

    public EnemyInfo(int id, int grade, string enemyName, string enemyType, string description, float power, float speed, float range, float dropRate, float hitDelay, float hpMax, float knockbackForce, float earth, float fire, float life, float lightning, float water, float wind)
    {
        this.id = id;
        this.grade = grade;
        this.enemyName = enemyName;
        this.enemyType = enemyType;
        this.description = description;
        this.power = power;
        this.speed = speed;
        this.range = range;
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
        this.enemyName = enemy.enemyName;
        this.description = enemy.description;
        this.power = enemy.power;
        this.speed = enemy.speed;
        this.range = enemy.range;
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
                var obj = FindObjectOfType<EnemyDB>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<EnemyDB>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public Dictionary<int, EnemyInfo> enemyDB = new Dictionary<int, EnemyInfo>(); //마법 정보 DB
    public Dictionary<string, Sprite> enemyIcon = new Dictionary<string, Sprite>(); //마법 아이콘 리스트
    public Dictionary<string, GameObject> enemyPrefab = new Dictionary<string, GameObject>(); //마법 프리팹 리스트
    [HideInInspector]
    public bool loadDone = false; //로드 완료 여부

    void Awake()
    {
        //게임 시작과 함께 아이템DB 정보 로드하기
        // 아이템 DB, 아이콘, 프리팹 불러오기
        StartCoroutine(GetEnemyData());
    }

    //구글 스프레드 시트에서 Apps Script로 가공된 형태로 json 데이터 받아오기
    IEnumerator GetEnemyData()
    {
        //적 아이콘 모두 가져오기
        Sprite[] _enemyIcon = Resources.LoadAll<Sprite>("Enemy/Icon");
        foreach (var icon in _enemyIcon)
        {
            enemyIcon[icon.name] = icon;
        }

        //적 프리팹 모두 가져오기
        GameObject[] _enemyPrefab = Resources.LoadAll<GameObject>("Enemy/Prefab");
        foreach (var prefab in _enemyPrefab)
        {
            enemyPrefab[prefab.name] = prefab;
        }

        //Apps Script에서 가공된 json 데이터 문서 주소
        UnityWebRequest www = UnityWebRequest.Get("https://script.googleusercontent.com/macros/echo?user_content_key=6ZQ8sYLio20mP1B6THEMPzU6c7Ph6YYf0LUfc38pFGruRhf2CiPrtPUMnp3RV9wjWS5LUI11HGSiZodVQG0wgrSV-9f0c_yJm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnKa-POu7wcFnA3wlQMYgM526Nnu0gbFAmuRW8zSVEVAU9_HiX_KJ3qEm4imXtAtA2I-6ud_s58xOj3-tedHHV_AcI_N4bm379g&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp");
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

            foreach (var row in jsonObj["enemyData"])
            {
                //마법 한줄씩 데이터 파싱
                var enemy = JSON.Parse(row.ToString())[0];

                //받아온 데이터를 List<EnemyInfo>에 넣기
                enemyDB[enemy["id"]] = new EnemyInfo
                (enemy["id"], enemy["grade"], enemy["name"], enemy["enemyType"], enemy["description"], 
                enemy["power"], enemy["speed"], enemy["range"], enemy["dropRate"], enemy["hitDelay"], enemy["hpMax"], enemy["knockbackForce"], 
                enemy["earth"], enemy["fire"], enemy["life"], enemy["lightning"], enemy["water"], enemy["wind"]
                );
            }
            
        }

        loadDone = true;
        print("EnemyDB Loaded!");

        yield return null;
    }

    public EnemyInfo GetEnemyByID(int id)
    {
        if(enemyDB.TryGetValue(id, out EnemyInfo value))
        return value;
        else
        return null;
    }

    public EnemyInfo GetEnemyByName(string enemyName)
    {
        EnemyInfo enemy = null;

        foreach (KeyValuePair<int, EnemyInfo> value in enemyDB)
        {
            if (value.Value.enemyName.Replace(" ", "") == enemyName)
            {
                enemy = value.Value;
                break;
            }
        }
        return enemy;
    }

    public string GetNameByID(int id)
    {
        if(enemyDB.TryGetValue(id, out EnemyInfo value))
        return value.enemyName;
        else
        return null;
    }

    public Sprite GetIcon(int id)
    {
        //아이콘의 이름
        string enemyName = GetEnemyByID(id).enemyName.Replace(" ", "") + "_Icon";

        if(enemyIcon.TryGetValue(enemyName, out Sprite icon))
        return icon;
        else
        return null;
    }

    public GameObject GetPrefab(int id)
    {
        //프리팹의 이름
        EnemyInfo enemy = GetEnemyByID(id);
        string enemyName = enemy.enemyName.Replace(" ", "") + "_Prefab";

        if(enemyPrefab.TryGetValue(enemyName, out GameObject prefab))
        return prefab;
        else
        return null;
    }

    public int RandomEnemy(int maxGrade)
    {
        //TODO 등급별로 확률 지정할것
        // 랜덤 몬스터 등급 뽑기 1~6
        int randGrade = Random.Range(1, maxGrade + 1);

        //랜덤 등급의 모든 몬스터 임시 리스트에 추가
        List<int> tempList = new List<int>();
        foreach (KeyValuePair<int, EnemyInfo> info in enemyDB)
        {
            //해당 등급이고, 일반 몬스터면 리스트에 추가
            if (info.Value.grade == randGrade && info.Value.enemyType == "normal")
            {
                tempList.Add(info.Value.id);
            }
        }

        //임시리스트에서 랜덤 인덱스로 뽑기
        int id = -1;
        if(tempList.Count != 0)
        id = tempList[Random.Range(0, tempList.Count)];

        return id;
    }
}
