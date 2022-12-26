using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

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

    [ReadOnly] public bool loadDone = false; //로드 완료 여부
    public enum CastType { active, passive };

    public Dictionary<int, MagicInfo> magicDB = new Dictionary<int, MagicInfo>(); //마법 정보 DB
    public Dictionary<int, MagicInfo> activeMagicDB = new Dictionary<int, MagicInfo>(); //마법 정보 DB
    // public List<Sprite> magicIcon = null; //마법 아이콘 리스트
    public Dictionary<string, Sprite> magicIcon = new Dictionary<string, Sprite>();
    public Dictionary<string, GameObject> magicPrefab = new Dictionary<string, GameObject>(); //마법 프리팹 리스트

    public List<int> unlockMagics = new List<int>(); //합성 성공한 마법 리스트들, 로컬 세이브 데이터
    public List<int> savedMagics = new List<int>(); //이번 게임에서 한번이라도 소지했던 마법들

    [SerializeField, ReadOnly] Color[] gradeColor = new Color[7]; //마법 등급 색깔
    public Color[] GradeColor
    {
        get { return gradeColor; }
    }
    [SerializeField, ReadOnly] Color[] gradeHDRColor = new Color[7]; //마법 등급 HDR 색깔
    public Color[] GradeHDRColor
    {
        get { return gradeHDRColor; }
    }
    [SerializeField, ReadOnly] Color[] elementColors = new Color[6]; //원소 색깔
    [SerializeField] Color[] elementHDRColors = new Color[6]; //원소 HDR 색깔

    string[] elementNames = { "Earth", "Fire", "Life", "Lightning", "Water", "Wind" };
    public string[] ElementNames
    {
        get { return elementNames; }
    }

    void Awake()
    {
        #region FixedValue
        // 등급 색깔 고정
        // Color[] _gradeColor = {
        //     SystemManager.Instance.HexToRGBA("FFFFFF"),
        //     SystemManager.Instance.HexToRGBA("4FF84C"),
        //     SystemManager.Instance.HexToRGBA("3EC1FF"),
        //     SystemManager.Instance.HexToRGBA("CD45FF"),
        //     SystemManager.Instance.HexToRGBA("FF3310"),
        //     SystemManager.Instance.HexToRGBA("FF8C00"),
        //     SystemManager.Instance.HexToRGBA("FFFF00")};
        // gradeColor = _gradeColor;

        // // 등급 HDR 색깔 고정
        // Color[] _gradeHDRColor = {
        //     SystemManager.Instance.HexToRGBA("FFFFFF"),
        //     SystemManager.Instance.HexToRGBA("1EFF1E"),
        //     SystemManager.Instance.HexToRGBA("1E1EFF"),
        //     SystemManager.Instance.HexToRGBA("FF1EFF"),
        //     SystemManager.Instance.HexToRGBA("FF1E1E"),
        //     SystemManager.Instance.HexToRGBA("FF801E"),
        //     SystemManager.Instance.HexToRGBA("FFFF1E")};
        // gradeHDRColor = _gradeHDRColor;

        // // 원소젬 색깔 고정
        // Color[] _elementColor = {
        //     SystemManager.Instance.HexToRGBA("C88C5E"),
        //     SystemManager.Instance.HexToRGBA("FF5B5B"),
        //     SystemManager.Instance.HexToRGBA("5BFF64"),
        //     SystemManager.Instance.HexToRGBA("FFF45B"),
        //     SystemManager.Instance.HexToRGBA("739CFF"),
        //     SystemManager.Instance.HexToRGBA("5BFEFF")};
        // elementColors = _elementColor;

        // // 원소 이름
        // string[] name = { "Earth", "Fire", "Life", "Lightning", "Water", "Wind" };
        // ElementNames = name;
        #endregion

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

        // 웹에서 새로 데이터 받아서 웹 세이브데이터의 json 최신화
        yield return StartCoroutine(
            SaveManager.Instance.WebDataLoad(
                SystemManager.DBType.Magic,
                "https://script.googleusercontent.com/macros/echo?user_content_key=7V2ZVIq0mlz0OyEVM8ULXo0nlLHXKPuUIJxFTqfLhj4Jsbg3SVZjnSH4X9KTiksN02j7LG8xCj8EgELL1uGWpX0Tg3k2TlLvm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnD_xj3pGHBsYNBHTy1qMO9_iBmRB6zvsbPv4uu5dqbk-3wD3VcpY-YvftUimQsCyzKs3JAsCIlkQoFkByun7M-8F5ap6m-tpCA&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp"
        ));

        // 웹에서 가져온 마법DB 데이터를 로컬 마법DB 데이터에 덮어쓰기
        SaveManager.Instance.localSaveData.magicDBJson = SaveManager.Instance.webSaveData.magicDBJson;

        // 마법DB 수정된 로컬 데이터를 저장, 완료시까지 대기
        yield return StartCoroutine(SaveManager.Instance.Save());

        // 로컬 데이터에서 파싱해서 마법DB에 넣기, 완료시까지 대기
        yield return StartCoroutine(GetMagicDB());

        // DB 전부 Enum으로 바꿔서 저장
        yield return StartCoroutine(SaveManager.Instance.DBtoEnum());

        // 로컬 세이브에서 언락된 마법들 불러오기
        LoadUnlockMagics();

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
                magic["id"], magic["grade"], magic["magicName"], magic["element_A"], magic["element_B"], magic["castType"], magic["description"], magic["priceType"], magic["price"], magic["multiHit"],
                magic["power"], magic["speed"], magic["range"], magic["duration"], magic["critical"], magic["criticalPower"], magic["pierce"], magic["atkNum"], magic["coolTime"],
                magic["powerPerLev"], magic["speedPerLev"], magic["rangePerLev"], magic["durationPerLev"], magic["criticalPerLev"], magic["criticalPowerPerLev"], magic["piercePerLev"], magic["atkNumPerLev"], magic["coolTimePerLev"]
                ));
                yield return null;
            }

            // 모든 마법 딕셔너리를 액티브 쿨타임용 딕셔너리에 복사
            foreach (MagicInfo magic in magicDB.Values)
            {
                activeMagicDB[magic.id] = new MagicInfo(magic);
            }

            //모든 마법 초기화
            InitialMagic();
        }

        // 로컬 세이브에서 언락된 마법들 불러오기
        LoadUnlockMagics();

        this.loadDone = true;
        print("MagicDB Loaded!");
    }

    public void LoadUnlockMagics()
    {
        List<int> savedUnlockMagics = new List<int>();

        // 세이브 데이터에서 언락된 마법 받아오기
        savedUnlockMagics = SaveManager.Instance.localSaveData.unlockMagics.ToList();

        // 기본적으로 1등급 마법은 언락시키기
        for (int i = 0; i < magicDB.Count; i++)
        {
            MagicInfo magic = magicDB[i];

            // 1등급 마법이고, 세이브 데이터에 해당 마법이 없을때
            if (magic.grade == 1 && !savedUnlockMagics.Exists(x => x == magic.id))
            {
                // print(magic.id + " : " + magic.magicName + " : " + magic.grade);

                // 해당 마법 넣어주기
                savedUnlockMagics.Add(magic.id);
            }
        }

        // 정렬
        savedUnlockMagics.Sort();
        // 해금 마법 목록 초기화
        unlockMagics = savedUnlockMagics;
    }
    public MagicInfo GetMagicByName(string name)
    {
        MagicInfo magic = null;

        foreach (KeyValuePair<int, MagicInfo> value in magicDB)
        {
            // print(value.Value.id + " : " + value.Value.magicName.Replace(" ", "") + " : " + name.Replace(" ", ""));
            if (value.Value.name.Replace(" ", "") == name.Replace(" ", ""))
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

    public MagicInfo GetActiveMagicByID(int id)
    {
        if (activeMagicDB.TryGetValue(id, out MagicInfo value))
            return value;
        else
            return null;
    }

    public Sprite GetIcon(int id)
    {
        //아이콘의 이름
        string magicName = GetMagicByID(id).name.Replace(" ", "") + "_Icon";

        if (magicIcon.TryGetValue(magicName, out Sprite icon))
            return icon;
        else
            return null;
    }

    public GameObject GetMagicPrefab(int id)
    {
        //프리팹의 이름
        string magicName = GetMagicByID(id).name.Replace(" ", "") + "_Prefab";

        if (magicPrefab.TryGetValue(magicName, out GameObject prefab))
            return prefab;
        else
            return null;
    }

    public void InitialMagic()
    {
        foreach (KeyValuePair<int, MagicInfo> magic in magicDB)
        {
            magic.Value.magicLevel = 1;
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
        bool isExist = System.Array.Exists(ElementNames, x => x == element);

        return isExist;
    }

    public MagicInfo GetRandomMagic(int targetGrade = 0)
    {
        // 언락된 모든 마법 인덱스를 넣을 리스트
        List<int> randomMagicPool = new List<int>();

        // 언락된 마법 중 해당 등급 모두 넣기
        for (int i = 0; i < unlockMagics.Count; i++)
        {
            int grade = GetMagicByID(unlockMagics[i]).grade;

            // 0등급이면 넘기기
            if (grade == 0)
                continue;

            // 등급을 명시했을때
            if (targetGrade != 0)
            {
                // 해당 등급의 마법만 넣기
                if (grade == targetGrade)
                    randomMagicPool.Add(unlockMagics[i]);
            }
            // 등급을 명시하지 않았을때
            else
            {
                // 모든 마법 넣기
                randomMagicPool.Add(unlockMagics[i]);
            }
        }

        // 등급 지정을 해줬는데, 해당 등급의 마법이 하나도 없을때
        if (targetGrade != 0 && randomMagicPool.Count == 0)
        {
            return null;
        }
        else
        {
            // 뽑은 리스트에서 인덱스 랜덤 뽑기
            int index = Random.Range(0, randomMagicPool.Count);

            // 마법 ID로 마법정보 불러와서 리턴
            return GetMagicByID(randomMagicPool[index]);
        }
    }

    //랜덤 마법 리스트 뽑기
    public int[] RandomMagicList(int amount)
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

    public int ElementType(SlotInfo slotInfo)
    {
        // 원소 색깔 리스트에서 해당 색을 가진 인덱스 찾기
        int colorIndex = -1;
        colorIndex = System.Array.FindIndex(ElementNames, x => x == slotInfo.priceType);

        // 인덱스 못 찾았으면 인덱스 중 랜덤
        if (colorIndex == -1)
            colorIndex = Random.Range(0, 6);

        return colorIndex;
    }

    public Color GetElementColor(int colorIndex)
    {
        // 해당 인덱스로 원소 색깔 리턴
        return elementColors[colorIndex];
    }

    public Color GetHDRElementColor(int colorIndex)
    {
        // 해당 인덱스로 원소 색깔 리턴
        return elementHDRColors[colorIndex];
    }

    public float MagicPower(MagicInfo magic, MagicHolder.TargetType target = MagicHolder.TargetType.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float power = 0;

        //마법 파워 및 레벨당 증가량 계산
        power = magic.power + magic.powerPerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.TargetType.Enemy)
            //플레이어 자체 파워 증가량 계산
            power = power * PlayerManager.Instance.PlayerStat_Now.power;

        return power;
    }

    public float MagicSpeed(MagicInfo magic, bool bigNumFast, MagicHolder.TargetType target = MagicHolder.TargetType.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float speed = 0;

        //마법 속도 및 레벨당 증가량 계산
        speed = bigNumFast
        ? magic.speed + magic.speedPerLev * (level - 1)
        : magic.speed - magic.speedPerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.TargetType.Enemy)
            //플레이어 speed 스탯 곱하기
            speed = bigNumFast
            ? speed + speed * (PlayerManager.Instance.PlayerStat_Now.speed - 1)
            : speed - speed * (PlayerManager.Instance.PlayerStat_Now.speed - 1);

        //값 제한하기
        speed = Mathf.Clamp(speed, 0.01f, 100f);

        return speed;
    }

    public float MagicRange(MagicInfo magic, MagicHolder.TargetType target = MagicHolder.TargetType.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float range = 0;

        //마법 범위 및 레벨당 증가량 계산
        range = magic.range + magic.rangePerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.TargetType.Enemy)
            //플레이어 자체 마법 범위 증가량 계산
            range = range + range * (PlayerManager.Instance.PlayerStat_Now.range - 1);

        //값 제한하기
        range = Mathf.Clamp(range, 0.1f, 1000f);

        return range;
    }

    public float MagicDuration(MagicInfo magic, MagicHolder.TargetType target = MagicHolder.TargetType.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float duration = 0;

        //마법 지속시간 및 레벨당 증가량 계산
        duration = magic.duration + magic.durationPerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.TargetType.Enemy)
            //플레이어 자체 마법 지속시간 증가량 계산
            duration = duration * PlayerManager.Instance.PlayerStat_Now.duration;

        //값 제한하기
        duration = Mathf.Clamp(duration, 0.1f, 100f);

        return duration;
    }

    public bool MagicCritical(MagicInfo magic, MagicHolder.TargetType target = MagicHolder.TargetType.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        //크리티컬 성공 여부
        bool isCritical = false;

        //마법 크리티컬 확률 및 레벨당 증가량 계산
        float critical = magic.critical + magic.criticalPerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.TargetType.Enemy)
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

    public float MagicCriticalPower(MagicInfo magic, MagicHolder.TargetType target = MagicHolder.TargetType.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float criticalPower = 0;

        //마법 크리티컬 데미지 및 레벨당 증가량 계산
        criticalPower = magic.criticalPower + magic.criticalPowerPerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.TargetType.Enemy)
            //플레이어 자체 마법 크리티컬 데미지 증가량 계산
            criticalPower = criticalPower * PlayerManager.Instance.PlayerStat_Now.luck;

        return criticalPower;
    }

    public int MagicPierce(MagicInfo magic, MagicHolder.TargetType target = MagicHolder.TargetType.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        int pierce = 0;

        //마법 범위 및 레벨당 증가량 계산
        pierce = magic.pierce + Mathf.FloorToInt(magic.piercePerLev * (level - 1));

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.TargetType.Enemy)
            // 플레이어 관통 횟수 추가 계산
            pierce += PlayerManager.Instance.PlayerStat_Now.pierce;


        return pierce;
    }

    public int MagicAtkNum(MagicInfo magic, MagicHolder.TargetType target = MagicHolder.TargetType.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        int atkNum = 0;

        //마법 투사체 개수 및 레벨당 증가량 계산
        atkNum =
        magic.atkNum +
        Mathf.FloorToInt(magic.atkNumPerLev * (level - 1));

        // 플레이어가 쓰는 마법일때
        // if (target == MagicHolder.Target.Enemy)
        //     // 플레이어 투사체 개수 추가 계산
        //     atkNum += PlayerManager.Instance.PlayerStat_Now.atkNumNum;

        //최소값 1 제한
        atkNum = Mathf.Clamp(atkNum, 1, atkNum);

        return atkNum;
    }

    public float MagicCoolTime(MagicInfo magic, MagicHolder.TargetType target = MagicHolder.TargetType.Enemy)
    {
        // 마법 레벨 제한
        int level = Mathf.Clamp(magic.magicLevel, 1, magic.magicLevel);
        float coolTime = 0;

        //마법 쿨타임 및 레벨당 증가량 계산
        coolTime = magic.coolTime - magic.coolTimePerLev * (level - 1);

        // 플레이어가 쓰는 마법일때
        if (target == MagicHolder.TargetType.Enemy)
            //플레이어 자체 쿨타임 증가량 계산
            coolTime = coolTime - coolTime * (PlayerManager.Instance.PlayerStat_Now.coolTime - 1);

        //값 제한하기
        coolTime = Mathf.Clamp(coolTime, 0.01f, 10f);

        return coolTime;
    }
}
