using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class MagicInfo
{
    public int magicLevel = 0; //현재 마법 레벨
    public bool isSummoned = false; //현재 소환 됬는지 여부

    [Header("Info")]
    public int id; //고유 아이디
    public int grade; //마법 등급
    public string magicName; //마법 이름
    public string element_A; //해당 마법을 만들 재료 A
    public string element_B; //해당 마법을 만들 재료 B
    public string castType; //시전 타입
    public string description; //마법 설명
    public string priceType; //마법 구매시 지불수단
    public int price; //마법 구매시 가격
    public bool onlyOne = false; //1이면 다중 발사 금지

    [Header("Spec")]
    public float power = 1; //데미지
    public float speed = 1; //투사체 속도 및 쿨타임
    public float range = 1; //범위
    public float duration = 1; //지속시간
    public float critical = 1f; //크리티컬 확률
    public float criticalDamage = 1f; //크리티컬 데미지 증가율
    public int pierce = 0; //관통 횟수 및 넉백 계수
    public int projectile = 0; //투사체 수
    public int coolTime = 0; //쿨타임

    [Header("LevUp")]
    public float powerPerLev;
    public float speedPerLev;
    public float rangePerLev;
    public float durationPerLev;
    public float criticalPerLev;
    public float criticalDamagePerLev;
    public float piercePerLev;
    public float projectilePerLev;
    public float coolTimePerLev;

    public MagicInfo(int id, int grade, string magicName, string element_A, string element_B, string castType, string description, string priceType, int price, bool onlyOne, float power, float speed, float range, float duration, float critical, float criticalDamage, int pierce, int projectile, int coolTime, float powerPerLev, float speedPerLev, float rangePerLev, float durationPerLev, float criticalPerLev, float criticalDamagePerLev, float piercePerLev, float projectilePerLev, float coolTimePerLev)
    {
        this.id = id;
        this.grade = grade;
        this.magicName = magicName;
        this.element_A = element_A;
        this.element_B = element_B;
        this.castType = castType;
        this.description = description;
        this.priceType = priceType;
        this.price = price;
        this.onlyOne = onlyOne;

        this.power = power;
        this.speed = speed;
        this.range = range;
        this.duration = duration;
        this.critical = critical;
        this.criticalDamage = criticalDamage;
        this.pierce = pierce;
        this.projectile = projectile;
        this.coolTime = coolTime;

        this.powerPerLev = powerPerLev;
        this.speedPerLev = speedPerLev;
        this.rangePerLev = rangePerLev;
        this.durationPerLev = durationPerLev;
        this.criticalPerLev = criticalPerLev;
        this.criticalDamagePerLev = criticalDamagePerLev;
        this.piercePerLev = piercePerLev;
        this.projectilePerLev = projectilePerLev;
        this.coolTimePerLev = coolTimePerLev;
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
        // 등급 색깔
        Color[] _gradeColor = {HexToRGBA("FFFFFF"), HexToRGBA("4FF84C"), HexToRGBA("3EC1FF"), HexToRGBA("CD45FF"),
        HexToRGBA("FF3310"), HexToRGBA("FFFF00")};
        gradeColor = _gradeColor;

        // 원소젬 색깔
        Color[] _elementColor = {HexToRGBA("C88C5E"), HexToRGBA("FF5B5B"), HexToRGBA("5BFF64"),
        HexToRGBA("FFF45B"), HexToRGBA("739CFF"), HexToRGBA("5BFEFF")};
        elementColor = _elementColor;

        // 원소 이름
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
                magicDB.Add(new MagicInfo(
                magic["id"], magic["grade"], magic["magicName"], magic["element_A"], magic["element_B"], magic["castType"], magic["description"], magic["priceType"], magic["price"], magic["onlyOne"], 
                magic["power"], magic["speed"], magic["range"], magic["duration"], magic["critical"], magic["criticalDamage"], magic["pierce"], magic["projectile"], magic["coolTime"], 
                magic["powerPerLev"], magic["speedPerLev"], magic["rangePerLev"], magic["durationPerLev"], magic["criticalPerLev"], magic["criticalDamagePerLev"], magic["piercePerLev"], magic["projectilePerLev"], magic["coolTimePerLev"]
                ));
            }

            //모든 마법 초기화
            InitialMagic();

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

        //! 테스트용 마법 추가
        // PlayerManager.Instance.GetMagic(MagicDB.Instance.GetMagicByID(15));
        // PlayerManager.Instance.GetMagic(MagicDB.Instance.GetMagicByID(3));
        // PlayerManager.Instance.GetMagic(MagicDB.Instance.GetMagicByID(4));

        // //플레이어 마법 시작
        // CastMagic.Instance.StartMagicCoroutine();

        yield return null;
    }
    
    public void InitialMagic(){
        foreach (var magic in magicDB)
        {
            magic.magicLevel = 0;
        }
    }

    public void ElementalSorting(List<string> elements, string element)
    {
        //첫번째 원소가 기본 원소일때
        if (isBasicElement(element))
        {
            //이 마법 원소에 해당 원소 없을때
            if (!elements.Exists(x => x == element))
                elements.Add(element);
        }
        //첫번째 원소가 기본 원소 아닐때
        else
        {
            if (MagicDB.Instance.magicDB.Exists(x => x.magicName == element))
            {
                // 원소 이름을 마법 이름에 넣어 마법 찾기
                MagicInfo magicInfo = MagicDB.Instance.magicDB.Find(x => x.magicName == element);
                // 해당 마법의 원소 두가지 다시 정렬하기
                ElementalSorting(elements, magicInfo.element_A);
                ElementalSorting(elements, magicInfo.element_B);
            }
        }
    }

    public bool isBasicElement(string element)
    {
        //기본 원소 이름과 일치하는 요소가 있는지 확인
        bool isExist = System.Array.Exists(MagicDB.Instance.elementNames, x => x == element);

        return isExist;
    }

    //랜덤 마법 뽑기
    public int[] RandomMagicIndex(List<MagicInfo> magicList, int amount)
    {
        //모든 마법 인덱스를 넣을 리스트
        List<int> magicIndex = new List<int>();

        //인덱스 모두 넣기
        for (int i = 0; i < magicList.Count; i++)
        {
            magicIndex.Add(i);
        }

        //랜덤 인덱스 3개를 넣을 배열
        int[] randomNum = new int[amount];

        for (int i = 0; i < amount; i++)
        {
            // 획득 가능한 마법 없을때
            if (magicIndex.Count == 0)
            {
                randomNum[i] = -1;
            }
            else
            {
                //인덱스 리스트에서 랜덤한 난수 생성
                int j = Random.Range(0, magicIndex.Count);
                int index = magicIndex[j];
                // print(magicIndex.Count + " : " + index);

                //랜덤 인덱스 숫자 넣기
                randomNum[i] = index;
                
                //! 테스트용 인덱스
                // randomNum[i] = 14;

                //이미 선택된 인덱스 제거
                magicIndex.RemoveAt(j);
            }
        }

        //인덱스 리스트 리턴
        return randomNum;
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

    public Color HexToRGBA(string hex)
    {
        Color color;
        ColorUtility.TryParseHtmlString("#" + hex, out color);

        return color;
    }

    public float MagicPower(MagicInfo magic)
    {
        float power = 0;

        //마법 파워 및 레벨당 증가량 계산
        power = magic.power + magic.powerPerLev * (magic.magicLevel - 1);
        //플레이어 자체 파워 증가량 계산
        power = power + power * (PlayerManager.Instance.power - 1);

        return power;
    }

    public float MagicSpeed(MagicInfo magic, bool bigNumFast)
    {
        float speed = 0;

        //마법 속도 및 레벨당 증가량 계산
        speed = bigNumFast ? magic.speed + magic.speedPerLev * (magic.magicLevel - 1) : magic.speed - magic.speedPerLev * (magic.magicLevel - 1);
        //플레이어 자체 마법 속도 증가량 계산
        speed = bigNumFast ? speed + speed * (PlayerManager.Instance.speed - 1) : speed - speed * (PlayerManager.Instance.speed - 1);
        //값 제한하기
        speed = Mathf.Clamp(speed, 0.01f, 100f);

        return speed;
    }

    public float MagicRange(MagicInfo magic)
    {
        float range = 0;

        //마법 범위 및 레벨당 증가량 계산
        range = magic.range + magic.rangePerLev * (magic.magicLevel - 1);
        //플레이어 자체 마법 범위 증가량 계산
        range = range + range * (PlayerManager.Instance.range - 1);
        //값 제한하기
        range = Mathf.Clamp(range, 0.1f, 10f);

        return range;
    }

    public float MagicDuration(MagicInfo magic)
    {
        float duration = 0;

        //마법 지속시간 및 레벨당 증가량 계산
        duration = magic.duration + magic.durationPerLev * (magic.magicLevel - 1);
        //플레이어 자체 마법 지속시간 증가량 계산
        duration = duration + duration * (PlayerManager.Instance.duration - 1);
        //값 제한하기
        duration = Mathf.Clamp(duration, 0.1f, 100f);

        return duration;
    }

    public float MagicCritical(MagicInfo magic)
    {
        float critical = 0;

        //마법 크리티컬 확률 및 레벨당 증가량 계산
        critical = magic.critical + magic.criticalPerLev * (magic.magicLevel - 1);
        //플레이어 자체 마법 크리티컬 확률 증가량 계산
        critical = critical + critical * (PlayerManager.Instance.luck - 1);
        //값 제한하기 0% ~ 100%
        critical = Mathf.Clamp(critical, 0f, 100f);

        return critical;
    }

    public float MagicCriticalDamage(MagicInfo magic)
    {
        float criticalDamage = 0;

        //마법 크리티컬 데미지 및 레벨당 증가량 계산
        criticalDamage = magic.criticalDamage + magic.criticalDamagePerLev * (magic.magicLevel - 1);
        //플레이어 자체 마법 크리티컬 데미지 증가량 계산
        criticalDamage = criticalDamage + criticalDamage * (PlayerManager.Instance.luck - 1);

        return criticalDamage;
    }

    public int MagicPierce(MagicInfo magic)
    {
        int pierce = 0;

        //마법 범위 및 레벨당 증가량 계산
        pierce = 
        magic.pierce + 
        Mathf.FloorToInt(magic.piercePerLev * (magic.magicLevel - 1)) + 
        PlayerManager.Instance.pierce;

        return pierce;
    }

    public int MagicProjectile(MagicInfo magic)
    {
        int projectile = 0;

        //마법 범위 및 레벨당 증가량 계산
        projectile = 
        magic.projectile + 
        Mathf.FloorToInt(magic.projectilePerLev * (magic.magicLevel - 1)) + 
        PlayerManager.Instance.projectileNum;

        return projectile;
    }

    public float MagicCoolTime(MagicInfo magic)
    {
        float coolTime = 0;

        //마법 쿨타임 및 레벨당 증가량 계산
        coolTime = magic.coolTime - magic.coolTimePerLev * (magic.magicLevel - 1);
        //플레이어 자체 쿨타임 증가량 계산
        coolTime = coolTime - coolTime * (PlayerManager.Instance.coolTime - 1);
        //값 제한하기
        coolTime = Mathf.Clamp(coolTime, 0.01f, 10f);

        return coolTime;
    }
}
