using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class MagicInfo
{
    //수정 가능한 변수들
    [Header("Configurable")]
    public int magicLevel = 0; //현재 마법 레벨
    public bool exist = false; //현재 소환 됬는지 여부
    public float coolCount = 0f; //현재 마법의 남은 쿨타임

    [Header("Info")]
    public int id; //고유 아이디
    public int grade; //마법 등급
    public string magicName; //마법 이름
    public string element_A; //해당 마법을 만들 재료 A
    public string element_B; //해당 마법을 만들 재료 B
    public string castType; //시전 타입
    public string description; //마법 설명
    public string priceType; //마법 구매시 화폐
    public bool multiHit; //다단 히트 여부
    public int price; //마법 구매시 가격
    // public bool onlyOne = false; //1이면 다중 발사 금지

    [Header("Spec")]
    public float power = 1; //데미지
    public float speed = 1; //투사체 속도 및 쿨타임
    public float range = 1; //범위
    public float duration = 1; //지속시간
    public float critical = 1f; //크리티컬 확률
    public float criticalPower = 1f; //크리티컬 데미지 증가율
    public int pierce = 0; //관통 횟수 및 넉백 계수
    public int projectile = 0; //투사체 수
    public int coolTime = 0; //쿨타임

    [Header("LevUp")]
    public float powerPerLev;
    public float speedPerLev;
    public float rangePerLev;
    public float durationPerLev;
    public float criticalPerLev;
    public float criticalPowerPerLev;
    public float piercePerLev;
    public float projectilePerLev;
    public float coolTimePerLev;

    public MagicInfo(MagicInfo magic)
    {
        this.id = magic.id;
        this.magicLevel = magic.magicLevel;
        this.grade = magic.grade;
        this.magicName = magic.magicName;
        this.element_A = magic.element_A;
        this.element_B = magic.element_B;
        this.castType = magic.castType;
        this.description = magic.description;
        this.priceType = magic.priceType;
        this.multiHit = magic.multiHit;
        this.price = magic.price;
        this.power = magic.power;
        this.speed = magic.speed;
        this.range = magic.range;
        this.duration = magic.duration;
        this.critical = magic.critical;
        this.criticalPower = magic.criticalPower;
        this.pierce = magic.pierce;
        this.projectile = magic.projectile;
        this.coolTime = magic.coolTime;
        this.powerPerLev = magic.powerPerLev;
        this.speedPerLev = magic.speedPerLev;
        this.rangePerLev = magic.rangePerLev;
        this.durationPerLev = magic.durationPerLev;
        this.criticalPerLev = magic.criticalPerLev;
        this.criticalPowerPerLev = magic.criticalPowerPerLev;
        this.piercePerLev = magic.piercePerLev;
        this.projectilePerLev = magic.projectilePerLev;
        this.coolTimePerLev = magic.coolTimePerLev;
    }

    public MagicInfo(int id, int grade, string magicName, string element_A, string element_B, string castType, string description, string priceType, bool multiHit, int price,
    float power, float speed, float range, float duration, float critical, float criticalPower, int pierce, int projectile, int coolTime,
    float powerPerLev, float speedPerLev, float rangePerLev, float durationPerLev, float criticalPerLev, float criticalPowerPerLev, float piercePerLev, float projectilePerLev, float coolTimePerLev)
    {
        this.id = id;
        this.grade = grade;
        this.magicName = magicName;
        this.element_A = element_A;
        this.element_B = element_B;
        this.castType = castType;
        this.description = description;
        this.priceType = priceType;
        this.multiHit = multiHit;
        this.price = price;

        this.power = power;
        this.speed = speed;
        this.range = range;
        this.duration = duration;
        this.critical = critical;
        this.criticalPower = criticalPower;
        this.pierce = pierce;
        this.projectile = projectile;
        this.coolTime = coolTime;

        this.powerPerLev = powerPerLev;
        this.speedPerLev = speedPerLev;
        this.rangePerLev = rangePerLev;
        this.durationPerLev = durationPerLev;
        this.criticalPerLev = criticalPerLev;
        this.criticalPowerPerLev = criticalPowerPerLev;
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

    public Dictionary<int, MagicInfo> magicDB = new Dictionary<int, MagicInfo>(); //마법 정보 DB
    // public List<Sprite> magicIcon = null; //마법 아이콘 리스트
    public Dictionary<string, Sprite> magicIcon = new Dictionary<string, Sprite>();
    public Dictionary<string, GameObject> magicPrefab = new Dictionary<string, GameObject>(); //마법 프리팹 리스트

    public List<int> unlockMagics = new List<int>(); //합성 성공한 마법 리스트들, 로컬 세이브 데이터
    public List<int> touchedMagics = new List<int>(); //이번 게임에서 한번이라도 소지했던 마법들

    [HideInInspector]
    public bool loadDone = false; //로드 완료 여부

    public Color[] gradeColor = new Color[7]; //마법 등급 색깔
    public Color[] elementColor = new Color[6]; //원소 색깔
    public string[] elementNames = new string[6]; //기본 원소 이름

    void Awake()
    {
        // 등급 색깔
        Color[] _gradeColor = {SystemManager.Instance.HexToRGBA("FFFFFF"), SystemManager.Instance.HexToRGBA("4FF84C"), SystemManager.Instance.HexToRGBA("3EC1FF"), SystemManager.Instance.HexToRGBA("CD45FF"),
        SystemManager.Instance.HexToRGBA("FF3310"), SystemManager.Instance.HexToRGBA("FF8C00"), SystemManager.Instance.HexToRGBA("FFFF00")};
        gradeColor = _gradeColor;

        // 원소젬 색깔
        Color[] _elementColor = {SystemManager.Instance.HexToRGBA("C88C5E"), SystemManager.Instance.HexToRGBA("FF5B5B"), SystemManager.Instance.HexToRGBA("5BFF64"),
        SystemManager.Instance.HexToRGBA("FFF45B"), SystemManager.Instance.HexToRGBA("739CFF"), SystemManager.Instance.HexToRGBA("5BFEFF")};
        elementColor = _elementColor;

        // 원소 이름
        string[] name = { "Earth", "Fire", "Life", "Lightning", "Water", "Wind" };
        elementNames = name;

        // 아이콘, 프리팹 불러오기
        GetMagicResources();
        // StartCoroutine(GetMagicData());

        //TODO 저장된 세이브 정보 불러오기
        SaveManager.Instance.LoadSet();
    }

    void GetMagicResources()
    {
        //마법 아이콘 전부 가져오기
        Sprite[] _magicIcon = Resources.LoadAll<Sprite>("Magic/Icon");
        foreach (var icon in _magicIcon)
        {
            magicIcon[icon.name] = icon;
        }

        //마법 프리팹 전부 가져오기
        GameObject[] _magicPrefab = Resources.LoadAll<GameObject>("Magic/Prefab");
        foreach (var prefab in _magicPrefab)
        {
            magicPrefab[prefab.name] = prefab;
        }
    }

    public void MagicDBSynchronize()
    {
        // 마법 DB 동기화 (웹 데이터 로컬에 저장 및 불러와서 magicDB에 넣기)
        StartCoroutine(MagicDBSync());
    }

    IEnumerator MagicDBSync()
    {
        // 버튼 동기화 아이콘 애니메이션 켜기
        Animator btnAnim = SystemManager.Instance.magicDBSyncBtn.GetComponentInChildren<Animator>();
        btnAnim.enabled = true;

        // 로컬 마법DB 데이터에 웹에서 가져온 마법DB 데이터를 덮어쓰기
        SaveManager.Instance.localSaveData.magicDBJson = SaveManager.Instance.webSaveData.magicDBJson;

        // 마법DB 수정된 로컬 데이터를 저장, 완료시까지 대기
        yield return StartCoroutine(SaveManager.Instance.Save());

        // 로컬 데이터에서 파싱해서 마법DB에 넣기, 완료시까지 대기
        yield return StartCoroutine(GetMagicDB());

        // 동기화 여부 다시 검사
        yield return StartCoroutine(
            SaveManager.Instance.DBSyncCheck(
                SystemManager.DBType.Magic,
                SystemManager.Instance.magicDBSyncBtn,
                "https://script.googleusercontent.com/macros/echo?user_content_key=7V2ZVIq0mlz0OyEVM8ULXo0nlLHXKPuUIJxFTqfLhj4Jsbg3SVZjnSH4X9KTiksN02j7LG8xCj8EgELL1uGWpX0Tg3k2TlLvm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnD_xj3pGHBsYNBHTy1qMO9_iBmRB6zvsbPv4uu5dqbk-3wD3VcpY-YvftUimQsCyzKs3JAsCIlkQoFkByun7M-8F5ap6m-tpCA&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp"
            ));

        // 아이콘 애니메이션 끄기
        btnAnim.enabled = false;
    }

    //구글 스프레드 시트에서 Apps Script로 가공된 형태로 json 데이터 받아오기
    public IEnumerator GetMagicDB()
    {
        // 웹에서 불러온 json string이 비어있지 않으면
        if (SaveManager.Instance.localSaveData.magicDBJson != "")
        {
            var jsonObj = JSON.Parse(SaveManager.Instance.localSaveData.magicDBJson);

            foreach (var row in jsonObj["magicData"])
            {
                //마법 한줄씩 데이터 파싱
                var magic = JSON.Parse(row.ToString())[0];

                //받아온 데이터를 id를 키값으로 MagicInfo 딕셔너리에 넣기
                magicDB[magic["id"]] = (new MagicInfo(
                magic["id"], magic["grade"], magic["magicName"], magic["element_A"], magic["element_B"], magic["castType"], magic["description"], magic["priceType"], magic["multiHit"], magic["price"],
                magic["power"], magic["speed"], magic["range"], magic["duration"], magic["critical"], magic["criticalPower"], magic["pierce"], magic["projectile"], magic["coolTime"],
                magic["powerPerLev"], magic["speedPerLev"], magic["rangePerLev"], magic["durationPerLev"], magic["criticalPerLev"], magic["criticalPowerPerLev"], magic["piercePerLev"], magic["projectilePerLev"], magic["coolTimePerLev"]
                ));
            }

            //모든 마법 초기화
            InitialMagic();
        }

        loadDone = true;
        print("MagicDB Loaded!");

        yield return null;
    }

    public MagicInfo GetMagicByName(string name)
    {
        MagicInfo magic = null;

        foreach (KeyValuePair<int, MagicInfo> value in magicDB)
        {
            // print(value.Value.id + " : " + value.Value.magicName.Replace(" ", "") + " : " + name.Replace(" ", ""));
            if (value.Value.magicName.Replace(" ", "") == name.Replace(" ", ""))
            {
                magic = value.Value;
                break;
            }
        }

        return magic;
    }

    public MagicInfo GetMagicByID(int id)
    {
        if (magicDB.TryGetValue(id, out MagicInfo value))
            return value;
        else
            return null;
    }

    public Sprite GetMagicIcon(int id)
    {
        //아이콘의 이름
        string magicName = GetMagicByID(id).magicName.Replace(" ", "") + "_Icon";

        if (magicIcon.TryGetValue(magicName, out Sprite icon))
            return icon;
        else
            return null;
    }

    public GameObject GetMagicPrefab(int id)
    {
        //프리팹의 이름
        string magicName = GetMagicByID(id).magicName.Replace(" ", "") + "_Prefab";

        if (magicPrefab.TryGetValue(magicName, out GameObject prefab))
            return prefab;
        else
            return null;
    }

    public void InitialMagic()
    {
        foreach (KeyValuePair<int, MagicInfo> magic in magicDB)
        {
            magic.Value.magicLevel = 0;
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
            // 원소 이름을 마법 이름에 넣어 마법 찾기
            MagicInfo magicInfo = GetMagicByName(element);

            if (magicInfo != null)
            {
                // 해당 마법의 원소 두가지 다시 정렬하기
                ElementalSorting(elements, magicInfo.element_A);
                ElementalSorting(elements, magicInfo.element_B);
            }
        }
    }

    public bool isBasicElement(string element)
    {
        //기본 원소 이름과 일치하는 요소가 있는지 확인
        bool isExist = System.Array.Exists(elementNames, x => x == element);

        return isExist;
    }

    public MagicInfo RandomMagic()
    {
        //모든 마법 인덱스를 넣을 리스트
        List<int> unlockIDs = new List<int>();

        //언락된 마법 인덱스 모두 넣기
        for (int i = 0; i < unlockMagics.Count; i++)
        {
            unlockIDs.Add(unlockMagics[i]);
        }

        //TODO 등급마다 확률 다르게
        //인덱스 리스트에서 랜덤으로 뽑기
        int j = Random.Range(0, unlockIDs.Count);

        //뽑은 인덱스로 마법 ID 찾기
        int magicID = unlockIDs[j];

        //마법 ID로 마법정보 불러와서 리턴
        return GetMagicByID(magicID);
    }

    //랜덤 마법 리스트 뽑기
    public int[] RandomMagicIndex(int amount)
    {
        //모든 마법 인덱스를 넣을 리스트
        List<int> magicIndex = new List<int>();

        //언락된 마법 인덱스 모두 넣기
        for (int i = 0; i < unlockMagics.Count; i++)
        {
            magicIndex.Add(unlockMagics[i]);
        }

        //뽑을 인덱스 최대 개수
        int numMax = unlockMagics.Count < amount ? unlockMagics.Count : amount;

        //랜덤 인덱스를 넣을 배열
        int[] randomNum = new int[numMax];

        for (int i = 0; i < numMax; i++)
        {
            //TODO 등급마다 확률 다르게
            //인덱스 리스트에서 랜덤한 난수 생성
            int j = Random.Range(0, magicIndex.Count);
            int index = magicIndex[j];
            // print(magicIndex.Count + " : " + index);

            //랜덤 인덱스 숫자 넣기
            randomNum[i] = index;

            //이미 선택된 인덱스 제거
            magicIndex.RemoveAt(j);
        }

        //인덱스 리스트 리턴
        return randomNum;
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

    public float MagicPower(MagicInfo magic, MagicHolder.Target target = MagicHolder.Target.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float power = 0;

        //마법 파워 및 레벨당 증가량 계산
        power = magic.power + magic.powerPerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.Target.Enemy)
            //플레이어 자체 파워 증가량 계산
            power = power + power * (PlayerManager.Instance.PlayerStat_Now.power - 1);

        return power;
    }

    public float MagicSpeed(MagicInfo magic, bool bigNumFast, MagicHolder.Target target = MagicHolder.Target.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float speed = 0;

        //마법 속도 및 레벨당 증가량 계산
        speed = bigNumFast
        ? magic.speed + magic.speedPerLev * (level - 1)
        : magic.speed - magic.speedPerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.Target.Enemy)
            //플레이어 speed 스탯 곱하기
            speed = bigNumFast
            ? speed + speed * (PlayerManager.Instance.PlayerStat_Now.speed - 1)
            : speed - speed * (PlayerManager.Instance.PlayerStat_Now.speed - 1);

        //값 제한하기
        speed = Mathf.Clamp(speed, 0.01f, 100f);

        return speed;
    }

    public float MagicRange(MagicInfo magic, MagicHolder.Target target = MagicHolder.Target.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float range = 0;

        //마법 범위 및 레벨당 증가량 계산
        range = magic.range + magic.rangePerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.Target.Enemy)
            //플레이어 자체 마법 범위 증가량 계산
            range = range + range * (PlayerManager.Instance.PlayerStat_Now.range - 1);

        //값 제한하기
        range = Mathf.Clamp(range, 0.1f, 1000f);

        return range;
    }

    public float MagicDuration(MagicInfo magic, MagicHolder.Target target = MagicHolder.Target.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float duration = 0;

        //마법 지속시간 및 레벨당 증가량 계산
        duration = magic.duration + magic.durationPerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.Target.Enemy)
            //플레이어 자체 마법 지속시간 증가량 계산
            duration = duration * PlayerManager.Instance.PlayerStat_Now.duration;

        //값 제한하기
        duration = Mathf.Clamp(duration, 0.1f, 100f);

        return duration;
    }

    public bool MagicCritical(MagicInfo magic, MagicHolder.Target target = MagicHolder.Target.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        //크리티컬 성공 여부
        bool isCritical = false;

        //마법 크리티컬 확률 및 레벨당 증가량 계산
        float critical = magic.critical + magic.criticalPerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.Target.Enemy)
            //플레이어 자체 마법 크리티컬 확률 증가량 계산
            critical = critical * PlayerManager.Instance.PlayerStat_Now.luck;

        //값 제한하기 0% ~ 100%
        critical = Mathf.Clamp(critical, 0f, 1f);

        //랜덤 확률 발생
        float rand = Random.value;

        //크리티컬 확률보다 랜덤 숫자가 더 적으면 크리티컬 성공
        if (rand <= critical)
            isCritical = true;

        // if (magic.magicName == "Life Mushroom")
        //     print(magic.magicName + " : " + critical);

        //크리티컬 성공여부 리턴
        return isCritical;
    }

    public float MagicCriticalPower(MagicInfo magic, MagicHolder.Target target = MagicHolder.Target.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float criticalPower = 0;

        //마법 크리티컬 데미지 및 레벨당 증가량 계산
        criticalPower = magic.criticalPower + magic.criticalPowerPerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.Target.Enemy)
            //플레이어 자체 마법 크리티컬 데미지 증가량 계산
            criticalPower = criticalPower * PlayerManager.Instance.PlayerStat_Now.luck;

        return criticalPower;
    }

    public int MagicPierce(MagicInfo magic, MagicHolder.Target target = MagicHolder.Target.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        int pierce = 0;

        //마법 범위 및 레벨당 증가량 계산
        pierce = magic.pierce + Mathf.FloorToInt(magic.piercePerLev * (level - 1));

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.Target.Enemy)
            // 플레이어 관통 횟수 추가 계산
            pierce += PlayerManager.Instance.PlayerStat_Now.pierce;


        return pierce;
    }

    public int MagicProjectile(MagicInfo magic, MagicHolder.Target target = MagicHolder.Target.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        int projectile = 0;

        //마법 투사체 개수 및 레벨당 증가량 계산
        projectile =
        magic.projectile +
        Mathf.FloorToInt(magic.projectilePerLev * (level - 1));

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.Target.Enemy)
            // 플레이어 투사체 개수 추가 계산
            projectile += PlayerManager.Instance.PlayerStat_Now.projectileNum;

        //최소값 1 제한
        projectile = Mathf.Clamp(projectile, 1, projectile);

        return projectile;
    }

    public float MagicCoolTime(MagicInfo magic, MagicHolder.Target target = MagicHolder.Target.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float coolTime = 0;

        //마법 쿨타임 및 레벨당 증가량 계산
        coolTime = magic.coolTime - magic.coolTimePerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.Target.Enemy)
            //플레이어 자체 쿨타임 증가량 계산
            coolTime = coolTime - coolTime * (PlayerManager.Instance.PlayerStat_Now.coolTime - 1);

        //값 제한하기
        coolTime = Mathf.Clamp(coolTime, 0.01f, 10f);

        return coolTime;
    }
}
