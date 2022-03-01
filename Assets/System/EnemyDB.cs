using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyInfo
{
    public int id;
    public string name;
    public string description;

    [Header("Spec")]
    public float power;
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

    public EnemyInfo(int id, string name, string description, float power, float dropRate, float hitDelay, float hpMax, float knockbackForce, float earth, float fire, float life, float lightning, float water, float wind)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.power = power;
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

    public List<EnemyInfo> enemyDB = null; //마법 정보 DB
    public List<Sprite> enemyIcon = null; //마법 아이콘 리스트
    public List<GameObject> enemyPrefab = null; //마법 프리팹 리스트
    [HideInInspector]
    public bool loadDone = false; //로드 완료 여부

    void Awake()
    {
        //게임 시작과 함께 아이템DB 정보 로드하기
        // 아이템 DB, 아이콘, 프리팹 불러오기
        enemyDB = new List<EnemyInfo>();
        StartCoroutine(GetEnemyData());
    }

    //구글 스프레드 시트에서 Apps Script로 가공된 형태로 json 데이터 받아오기
    IEnumerator GetEnemyData()
    {
        //마법 아이콘, 프리팹 가져오기
        enemyIcon = Resources.LoadAll<Sprite>("Enemy/Icon").ToList();
        enemyPrefab = Resources.LoadAll<GameObject>("Enemy/Prefab").ToList();

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
                enemyDB.Add(new EnemyInfo
                (enemy["id"], enemy["name"], enemy["description"], 
                enemy["power"], enemy["dropRate"], enemy["hitDelay"], enemy["hpMax"], enemy["knockbackForce"], 
                enemy["earth"], enemy["fire"], enemy["life"], enemy["lightning"], enemy["water"], enemy["wind"]
                ));
            }

            // foreach (var enemy in enemyDB)
            // {
            //     print(
            //     " id : " + enemy.id + " / " +
            //     " name : " + enemy.name + " / " + 
            //     " description : " + enemy.description + " / " + 

            //     " power : " + enemy.power + " / " + 
            //     " dropRate : " + enemy.dropRate + " / " + 
            //     " hitDelay : " + enemy.hitDelay + " / " + 
            //     " hpMax : " + enemy.hpMax + " / " + 
            //     " knockbackForce : " + enemy.knockbackForce + " / " + 
                
            //     " earth : " + enemy.earth + " / " + 
            //     " fire : " + enemy.fire + " / " + 
            //     " life : " + enemy.life + " / " + 
            //     " lightning : " + enemy.lightning + " / " + 
            //     " water : " + enemy.water + " / " + 
            //     " wind : " + enemy.wind + " / "
            //     );
            // }
        }

        loadDone = true;
        print("EnemyDB Loaded!");

        yield return null;
    }

    public EnemyInfo GetEnemyByID(int id)
    {
        EnemyInfo enemy = enemyDB.Find(x => x.id == id);
        return enemy;
    }

    public string GetEnemyNameByID(int id)
    {
        EnemyInfo enemy = enemyDB.Find(x => x.id == id);
        return enemy.name;
    }

    public EnemyInfo GetEnemyByName(string name)
    {
        EnemyInfo enemy = enemyDB.Find(x => x.name.Replace(" ", "") == name.Replace(" ", ""));
        // print(enemy.enemyName.Replace(" ", "") + " : " + name.Replace(" ", ""));
        return enemy;
    }
}
