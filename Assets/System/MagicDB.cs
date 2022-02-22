using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class MagicInfo
{
    public bool hasMagic = false; //플레이어가 갖고 있는 마법인지
    public int id; //고유 아이디
    public int grade; //마법 등급
    public string magicName; //마법 이름
    public string element_A; //해당 마법을 만들 재료 A
    public string element_B; //해당 마법을 만들 재료 B
    public string description; //마법 설명

    [Header("Spec")]
    public float damage = 1; //데미지
    public float speed = 1; //투사체 속도
    public float range = 1; //범위
    public float coolTime = 1; //쿨타임
    public float criticalRate = 0; //크리티컬 확률
    public int pierceNum = 0; //관통 횟수

    [Header("Side Effect")]
    public int onlyOne = 0; //1이면 다중 발사 금지
    public float knockbackForce = 0; //넉백 파워

    public MagicInfo(int id, int grade, string magicName, string element_A, string element_B, string description,
    float damage, float speed, float range, float coolTime, float criticalRate, int pierceNum, int onlyOne, float knockbackForce)
    {
        this.id = id;
        this.grade = grade;
        this.magicName = magicName;
        this.element_A = element_A;
        this.element_B = element_B;
        this.description = description;

        this.damage = damage;
        this.speed = speed;
        this.range = range;
        this.coolTime = coolTime;
        this.criticalRate = criticalRate;
        this.pierceNum = pierceNum;
        this.onlyOne = onlyOne;
        this.knockbackForce = knockbackForce;
    }
}

public class MagicDB : MonoBehaviour
{
    #region Singleton
    private static MagicDB instance;
    public static MagicDB Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<MagicDB>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<MagicDB>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public List<MagicInfo> magicDB = null; //마법 정보 DB
    public List<Sprite> magicIcon = null; //마법 아이콘 리스트
    public List<GameObject> magicPrefab = null; //마법 프리팹 리스트
    [HideInInspector]
    public bool loadDone = false; //로드 완료 여부

    public Color[] gradeColor = new Color[6]; //마법 등급 색깔
    public Color[] elementColor = new Color[6]; //원소 색깔
    public string[] elementNames = new string[6]; //기본 원소 이름

    void Awake()
    {
        Color[] _gradeColor = {RGBAToHex("4FF84C"), RGBAToHex("3EC1FF"), RGBAToHex("CD45FF"),
        RGBAToHex("FFFF00"), RGBAToHex("FFAA00"), RGBAToHex("FF3310")};
        gradeColor = _gradeColor;
        Color[] _elementColor = {RGBAToHex("C88C5E"), RGBAToHex("FF5B5B"), RGBAToHex("5BFF64"),
        RGBAToHex("FFF45B"), RGBAToHex("739CFF"), RGBAToHex("5BFEFF")};
        elementColor = _elementColor;
        //원소 이름 모두 넣기
        string[] name = { "Earth", "Fire", "Life", "Lightning", "Water", "Wind" };
        elementNames = name;

        //게임 시작과 함께 마법DB 정보 로드하기
        // 마법 DB, 아이콘, 프리팹 불러오기
        magicDB = new List<MagicInfo>();
        StartCoroutine(GetMagicData());
    }

    //구글 스프레드 시트에서 Apps Script로 가공된 형태로 json 데이터 받아오기
    IEnumerator GetMagicData()
    {
        //마법 아이콘, 프리팹 가져오기
        magicIcon = Resources.LoadAll<Sprite>("Magic/Icon").ToList();
        magicPrefab = Resources.LoadAll<GameObject>("Magic/Prefab").ToList();

        //Apps Script에서 가공된 json 데이터 문서 주소
        UnityWebRequest www = UnityWebRequest.Get("https://script.googleusercontent.com/macros/echo?user_content_key=7V2ZVIq0mlz0OyEVM8ULXo0nlLHXKPuUIJxFTqfLhj4Jsbg3SVZjnSH4X9KTiksN02j7LG8xCj8EgELL1uGWpX0Tg3k2TlLvm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnD_xj3pGHBsYNBHTy1qMO9_iBmRB6zvsbPv4uu5dqbk-3wD3VcpY-YvftUimQsCyzKs3JAsCIlkQoFkByun7M-8F5ap6m-tpCA&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp");
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

            foreach (var row in jsonObj["magicData"])
            {
                //마법 한줄씩 데이터 파싱
                var magic = JSON.Parse(row.ToString())[0];

                //받아온 데이터를 List<MagicInfo>에 넣기
                magicDB.Add(new MagicInfo(magic["id"], magic["grade"], magic["magicName"], magic["element_A"], magic["element_B"], magic["description"],
                magic["damage"], magic["speed"], magic["range"], magic["coolTime"], magic["criticalRate"], magic["pierceNum"], 
                magic["onlyOne"], magic["knockbackForce"]));
            }

            // foreach (var item in magicDB)
            // {
            //     print(
            //     " id : " + item.id + " / " +
            //     " grade : " + item.grade + " / " +
            //     " magicName : " + item.magicName + " / " +
            //     " element_A : " + item.element_A + " / " +
            //     " element_B : " + item.element_B + " / " +
            //     " description : " + item.description);
            // }
        }

        loadDone = true;
        print("MagicDB Loaded!");

        yield return null;
    }

    public MagicInfo GetMagicByID(int id)
    {
        MagicInfo magic = magicDB.Find(x => x.id == id);
        return magic;
    }

    public MagicInfo GetMagicByName(string name)
    {
        MagicInfo magic = magicDB.Find(x => x.magicName == name);
        return magic;
    }

    public Color ElementColor(string element)
    {
        switch (element)
        {
            case "Earth":
                return elementColor[0];
            case "Fire":
                return elementColor[1];
            case "Life":
                return elementColor[2];
            case "Lightning":
                return elementColor[3];
            case "Water":
                return elementColor[4];
            case "Wind":
                return elementColor[5];

            default: return Color.white;
        }
    }

    Color RGBAToHex(string hex)
    {
        Color color;
        ColorUtility.TryParseHtmlString("#" + hex, out color);

        return color;
    }
}
