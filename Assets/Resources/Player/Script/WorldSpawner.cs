using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;
using UnityEngine.Experimental;
using UnityEngine.UI;

public class WorldSpawner : MonoBehaviour
{
    #region Singleton
    private static WorldSpawner instance;
    public static WorldSpawner Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<WorldSpawner>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = "WorldSpawner";
                    instance = obj.AddComponent<WorldSpawner>();
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("Debug")]
    public bool randomSpawn; //랜덤 몬스터 스폰 ON/OFF
    public bool allEliteSwitch; // 모든 적이 엘리트
    public bool spawnItem; // 아이템 스폰 여부

    [Header("State")]
    [SerializeField] int defaultEnemyPower; //! 전투력 임의 수정값
    public int nowDifficultGrade = 0; // 현재 난이도 등급
    [SerializeField] int MaxEnemyPower; //최대 몬스터 전투력
    public int NowEnemyPower; //현재 몬스터 전투력
    [SerializeField] float enemySpawnCount; //몬스터 스폰 쿨타임 카운트
    [SerializeField] float itemboxSpawnCount; //아이템 박스 스폰 쿨타임 카운트
    [SerializeField] float lockerSpawnCount; //아이템 금고 스폰 쿨타임 카운트
    [SerializeField, ReadOnly] bool nowSpawning; //스폰중일때
    [SerializeField] Transform targetObj;
    public float maxDistance = 80f; // 타겟과 몬스터 사이 최대 거리
    [SerializeField] float eliteRate; // 엘리트 계수
    public int[] outGemNum = new int[6]; //카메라 밖으로 나간 원소젬 개수
    public List<GameObject> outGem = new List<GameObject>(); //카메라 밖으로 나간 원소젬 리스트

    [Header("Gate")]
    public bool dragSwitch = true; // 몬스터 반대편 이동 여부
    public bool gateSpawn = false; // 게이트 주변에서 몬스터 생성 여부
    public float stageStartTime = 0f; // 스테이지 시작시간
#if UNITY_EDITOR
    [SerializeField]
#endif
    private float gateSpawnTime = 60f; // 스테이지 시작시 해당 시간 지나면 게이트포탈 근처에서 스폰
    public float GateSpawnTime { get { return gateSpawnTime; } set { gateSpawnTime = value; } }

    [Header("Pool")]
    public List<Character> spawnAbleList = new List<Character>(); // 현재 맵에서 스폰 가능한 몹 리스트
    public List<Character> spawnEnemyList = new List<Character>(); //현재 스폰된 몬스터 리스트
    public List<GameObject> itemBoxList = new List<GameObject>(); // 현재 스폰된 아이템 박스 리스트
    public List<GameObject> lockerList = new List<GameObject>(); // 현재 스폰된 아이템 금고 리스트
    [SerializeField] List<float> eliteWeight = new List<float>(); // 엘리트 몬스터 가중치 리스트

    [Header("Refer")]
    [SerializeField] GameObject itemBoxPrefab; // 파괴 가능한 아이템 박스
    [SerializeField] GameObject lockerPrefab; // 구매 가능한 아이템 금고
    public GameObject slotMachinePrefab; // 슬롯머신 프리팹
    // [SerializeField] GameObject spawnPortal; // 포탈 프리팹
    public GameObject spawnerPrefab; // 빔 프리팹
    public GameObject healRange; // Heal 엘리트 몬스터의 힐 범위
    public GameObject hitEffect; // 몬스터 피격 이펙트
    public GameObject blockEffect; // 몬스터 무적시 피격 이펙트
    public GameObject dustPrefab; //먼지 이펙트 프리팹
    public GameObject bloodPrefab; //혈흔 프리팹
    public BoxCollider2D spawnColl; // 스포너 테두리 콜라이더

    private void Awake()
    {
        // 다른 오브젝트가 이미 있을 때
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 현재 스폰 몬스터 리스트 비우기
        spawnEnemyList.Clear();

        // 엘리트 종류 가중치
        // eliteWeight.Add(0); // 엘리트 아닐 확률 가중치 = 0
        // eliteWeight.Add(20); // Power 엘리트 가중치
        // eliteWeight.Add(20); // Speed 엘리트 가중치
        // eliteWeight.Add(10); // Heal 엘리트 가중치

        // 플레이어 있으면
        if (PlayerManager.Instance != null)
            // 플레이어를 타겟으로 지정
            targetObj = PlayerManager.Instance.transform;
    }

    void Start()
    {
        spawnColl = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 몬스터 DB 초기화 대기
        yield return new WaitUntil(() => EnemyDB.Instance != null && EnemyDB.Instance.loadDone);

        // 몬스터 스폰 켜기
        SystemManager.Instance.spawnSwitch = true;
        // 몬스터 스폰 버튼 색깔 초기화
        SystemManager.Instance.ButtonToggle(ref SystemManager.Instance.spawnSwitch, SystemManager.Instance.spawnBtn, true);

        // 게이트 주변 스폰 끄기 (카메라 영역 바깥에서 스폰)
        gateSpawn = false;
        // 스테이지 시작시간 기록
        stageStartTime = Time.time;

#if !UNITY_EDITOR
        // 빌드상에서는 비우기
        spawnAbleList.Clear();
#endif

        // 스폰 가능 몬스터 풀이 비었으면
        if (spawnAbleList.Count == 0)
            // 현재 맵속성으로 몬스터 풀 만들기
            foreach (KeyValuePair<int, EnemyInfo> enemy in EnemyDB.Instance.enemyDB)
            {
                // 해당 몹의 원소 속성을 인덱스로 반환
                int enemyElement = System.Array.FindIndex(MagicDB.Instance.ElementNames, x => x == enemy.Value.elementType);

                // 현재 맵의 속성과 같은 원소속성의 일반몹이면
                if (enemyElement == (int)SystemManager.Instance.NowMapElement && enemy.Value.enemyType == EnemyDB.EnemyType.Normal.ToString())
                {
                    GameObject enemyObj = EnemyDB.Instance.GetPrefab(enemy.Value.id);
                    Character character = null;

                    if (enemyObj != null)
                        character = enemyObj.GetComponent<Character>();

                    if (character != null)
                        // 스폰 가능리스트에 포함
                        spawnAbleList.Add(character);
                }
            }

        print("GateSpawnTime : " + WorldSpawner.Instance.GateSpawnTime);
    }

    void Update()
    {
        // 초기화 안됬으면 리턴
        if (SystemManager.Instance == null || !SystemManager.Instance.loadDone)
            return;
        // 시간 멈췄으면 리턴
        if (Time.timeScale == 0f)
            return;

        // 스폰 스위치 켜져있을때
        if (SystemManager.Instance.spawnSwitch)
            // 쿨타임마다 몬스터 스폰, 스폰중 아닐때
            if (enemySpawnCount <= 0 && !nowSpawning)
            {
                //몬스터 스폰 랜덤 횟수,  최대치는 플레이어 전투력마다 0.05씩 증가
                float maxSpawnNum = 1;
                if (PlayerManager.Instance != null)
                    maxSpawnNum = 5 + PlayerManager.Instance.characterStat.powerSum * 0.05f;

                // 스폰 횟수 범위 제한
                maxSpawnNum = Mathf.Clamp(maxSpawnNum, maxSpawnNum, 10);

                // 1~3번 중 랜덤으로 반복
                int spawnNum = Random.Range(1, (int)maxSpawnNum);

                // print(spawnCoolCount + " / " + spawnNum + " 번 스폰");

                for (int i = 0; i < spawnNum; i++)
                {
                    //랜덤 몬스터 스폰
                    StartCoroutine(RandomSpawn());
                }
            }
            else
                enemySpawnCount -= Time.deltaTime;

        // 아이템 스폰일때
        if (spawnItem)
        {
            // 쿨타임마다 아이템 박스 스폰
            if (itemboxSpawnCount <= 0)
            {
                // 랜덤 스폰 확률
                float spawnRate = Random.value;
                // 아이템 박스 개수에 반비례해서 확률 스폰 (100% 확률에서 박스 1개당 확률 10% 차감)
                if (spawnRate <= 1f - itemBoxList.Count * 1f / 10f)
                {
                    // 아이템 박스 스폰, 최대 10개
                    GameObject itembox = SpawnItembox(itemBoxPrefab);

                    // 쿨타임 계산 (기본 시간 + 개당 n초 추가)
                    float boxCoolTime = 3f + itemBoxList.Count * 5f;
                    // 쿨타임 갱신
                    itemboxSpawnCount = boxCoolTime;
                }
            }
            else
                itemboxSpawnCount -= Time.deltaTime;

            // 쿨타임마다 아이템 금고 스폰
            if (lockerSpawnCount <= 0)
            {
                // 랜덤 스폰 확률
                float spawnRate = Random.value;
                // 아이템 박스 개수에 반비례해서 확률 스폰 (100% 확률에서 박스 1개당 확률 10% 차감)
                if (spawnRate <= 1f - lockerList.Count * 1f / 5f)
                {
                    // 아이템 금고 스폰, 최대 5개
                    GameObject itembox = SpawnItembox(lockerPrefab);

                    // 쿨타임 계산 (기본 시간 + 개당 n초 추가)
                    float boxCoolTime = 10f + lockerList.Count * 10f;
                    // 쿨타임 갱신
                    lockerSpawnCount = boxCoolTime;
                }
            }
            else
                lockerSpawnCount -= Time.deltaTime;
        }
    }

    public void RandomSpawn(bool isBoss = false)
    {
        // 랜덤 몬스터 찾기
        EnemyInfo enemy = EnemyDB.Instance.GetEnemyByID(EnemyDB.Instance.RandomEnemy(0, isBoss));

        // 몬스터 소환 위치
        Vector3 spawnPos = PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle.normalized * Random.Range(20f, 30f);

        //포탈에서 몬스터 소환
        EnemySpawn(enemy, spawnPos);
    }

    IEnumerator RandomSpawn()
    {
        //스폰 스위치 꺼졌으면 스폰 멈추기
        if (!SystemManager.Instance.spawnSwitch)
            yield break;

        float time = SystemManager.Instance.time_current;
        // 30초 단위로 시간 계수 증가
        float timePower = time / 30f;
        // 난이도 계수 반영 (최대 1을 넘지않게 최대 6까지 나오는 등급에 0.166 곱함)
        timePower = timePower * nowDifficultGrade * 0.166f;

        // 몬스터 총 전투력 최대값 = 기본 총 전투력 + 누적 시간 계수
        MaxEnemyPower = defaultEnemyPower + Mathf.FloorToInt(timePower);

        //max 전투력 넘었으면 중단
        if (MaxEnemyPower <= NowEnemyPower)
            yield break;

        // 스폰 시작
        nowSpawning = true;

        //몬스터DB에서 랜덤 id 뽑기
        int enemyId = -1;
        EnemyInfo enemy = null;
        GameObject enemyObj = null;

        //나올수 있는 몬스터의 최대 등급
        int maxGrade = MaxEnemyPower - NowEnemyPower;
        maxGrade = Mathf.Clamp(maxGrade, 1, 6); //최대 6까지

        //랜덤 스폰일때
        if (randomSpawn || spawnAbleList.Count == 0)
        {
            // 스폰 가능한 몬스터 찾을때까지 반복
            while (enemy == null || enemyObj == null)
            {
                //랜덤 id 찾기
                enemyId = EnemyDB.Instance.RandomEnemy(maxGrade);

                if (enemyId == -1)
                    continue;

                //enemy 찾기
                enemy = EnemyDB.Instance.GetEnemyByID(enemyId);

                // 몬스터 정보 없으면 넘기기
                if (enemy == null)
                    continue;

                //프리팹 찾기
                enemyObj = EnemyDB.Instance.GetPrefab(enemyId);

                yield return null;
            }
        }
        //랜덤 스폰 아닐때, spawnAbleList 에서 뽑아서 랜덤 스폰
        else
        {
            //프리팹 오브젝트 찾기
            enemyObj = spawnAbleList[Random.Range(0, spawnAbleList.Count)].gameObject;

            //몬스터 정보 찾기
            enemy = new EnemyInfo(EnemyDB.Instance.GetEnemyByName(enemyObj.name.Replace("_Prefab", "")));

            //몬스터 id 찾기
            enemyId = enemy.id;
        }

        // 엘리트 출현 유무 (시간 및 총 전투력에 따라 엘리트 출현율 상승)
        eliteRate = timePower / 100f; // 30초마다 1%씩 출현율 상승 (3000초=50분 이상이면 100% 엘리트)
        eliteRate = eliteRate / 2f; //todo 엘리트 너무 자주 떠서 확률 보정 테스트중

        //몬스터 총 전투력 올리기
        NowEnemyPower += enemy.grade;

        // 스폰 스위치 켜졌을때
        if (SystemManager.Instance.spawnSwitch)
            //포탈에서 몬스터 소환
            EnemySpawn(enemy);

        // 해당 몬스터의 쿨타임 넣기
        float spawnCoolTime = enemy.spawnCool;
        // 쿨타임에 50% 범위 내 랜덤성 부여
        spawnCoolTime = Random.Range(spawnCoolTime * 0.5f, spawnCoolTime * 1.5f);
        // 범위 제한
        spawnCoolTime = Mathf.Clamp(spawnCoolTime, 1f, 5f);

        // 쿨타임 갱신
        enemySpawnCount = spawnCoolTime;

        // print(enemy.enemyName + " : 스폰");

        // 스폰 끝
        nowSpawning = false;
    }

    public GameObject EnemySpawn(EnemyInfo enemy = null, Vector2 spawnPos = default, GameObject enemyPrefab = null, float spawnTime = 1f)
    {
        // enemyPrefab 변수 안들어왔으면
        if (enemyPrefab == null)
            // 몬스터 프리팹 찾기
            enemyPrefab = EnemyDB.Instance.GetPrefab(enemy.id);

        // spawnEndPos 소환 위치 변수 없으면 지정
        if (spawnPos == default)
        {
            if (gateSpawn)
                // 포탈 게이트 근처에서 스폰
                spawnPos = (Vector2)GatePortal.Instance.transform.position + Random.insideUnitCircle.normalized * Random.Range(15f, 20f);
            else
                // 화면 테두리 밖에서 스폰
                spawnPos = BorderRandPos();
        }

        // 몬스터 프리팹 소환
        GameObject enemyObj = LeanPool.Spawn(enemyPrefab, spawnPos, Quaternion.identity, ObjectPool.Instance.enemyPool);
        // 몬스터 끄기
        enemyObj.gameObject.SetActive(false);

        // 스폰 트랜지션 재생
        StartCoroutine(SpawnTransition(enemyObj, enemy, spawnPos, spawnTime));

        // 해당 몹 오브젝트 리턴
        return enemyObj;
    }

    public IEnumerator SpawnTransition(GameObject enemyObj, EnemyInfo enemy = null, Vector2 spawnPos = default, float spawnTime = 1f)
    {
        // 캐릭터 찾기
        Character character = enemyObj.GetComponentInChildren<Character>();

        // 소환 위치에 포탈 소환
        GameObject spawnBeam = LeanPool.Spawn(spawnerPrefab, spawnPos, Quaternion.identity, ObjectPool.Instance.enemyPool);
        Transform beam = spawnBeam.transform.Find("Beam");
        Transform portal = spawnBeam.transform.Find("Portal");

        // 포탈 및 빔 사이즈 초기화
        portal.localScale = Vector2.zero;
        beam.localScale = new Vector2(0, 1);

        // 포탈 확장
        portal.DOScale(Vector2.one * character.portalSize, 0.5f * spawnTime)
        .SetEase(Ease.InQuart);

        // 동시에 빔 확장
        beam.DOScale(new Vector2(character.portalSize * 0.5f, 1f), 0.5f * spawnTime)
        .SetEase(Ease.InQuart);

        // 확장 대기
        yield return new WaitForSeconds(0.5f * spawnTime);

        #region Enemy Init

        // 몬스터 리스트에 넣어서 기억하기
        spawnEnemyList.Add(character);

        // 일반 몹 일때
        if (enemy.enemyType == EnemyDB.EnemyType.Normal.ToString())
        {
            // 엘리트 종류 기본값 None
            int eliteClass = 0;
            // 엘리트 가중치 리스트 복사
            List<float> weightList = new List<float>(eliteWeight);
            // 일반몹 가중치는 시간에 따라 내리기
            weightList[0] = weightList[0] * (1f - eliteRate);
            // print(weightList[0]);
            // 엘리트 종류 가중치로 뽑기
            eliteClass = SystemManager.Instance.WeightRandom(eliteWeight);

            // 엘리트 종류를 매니저에 전달
            character.eliteClass = (EliteClass)eliteClass;
        }

        //EnemyInfo 인스턴스 생성
        EnemyInfo enemyInfo = new EnemyInfo(enemy);
        //EnemyInfo 정보 넣기
        character.enemy = enemyInfo;

        // 몬스터 켜기
        enemyObj.gameObject.SetActive(true);

        // 몬스터 하얗게 덮었다가 초기화
        foreach (SpriteRenderer sprite in character.spriteList)
        {
            // 하얗게 덮기
            sprite.material.SetColor("_Tint", Color.white);

            // 색깔 서서히 초기화
            sprite.material.DOColor(new Color(1, 1, 1, 0), "_Tint", 1f)
            .SetEase(Ease.OutQuad);
        }

        #endregion

        // 빔 축소
        beam.DOScale(new Vector2(0, 1), 0.3f * spawnTime)
        .SetEase(Ease.OutQuart);

        // 포탈 축소
        portal.DOScale(Vector2.zero, 0.3f * spawnTime)
        .SetEase(Ease.OutQuart);

        yield return new WaitForSeconds(0.3f * spawnTime);

        // 소환된 몬스터 초기화 시작
        character.initialStart = true;

        // 포탈 디스폰
        LeanPool.Despawn(spawnBeam);
    }

    public void EnemyDespawn(Character character)
    {
        // 몬스터 죽을때 함수 호출 (모든 몬스터 공통), ex) 체력 씨앗 드랍, 몬스터 아군 고스트 소환, 시체 폭발 등
        if (SystemManager.Instance.globalEnemyDeadCallback != null)
            SystemManager.Instance.globalEnemyDeadCallback(character);

        // 죽은 적을 리스트에서 제거
        spawnEnemyList.Remove(character);
    }

    GameObject SpawnItembox(GameObject itemBox)
    {
        // 박스 생성 위치
        Vector2 boxPos = default;
        bool isClose = true;
        int loopNum = 0;
        while (isClose)
        {
            // 변수 초기화
            isClose = false;

            // 생성 위치 뽑기
            boxPos = BorderRandPos();
            loopNum++;

            for (int i = 0; i < itemBoxList.Count; i++)
            {
                // 기존 박스와 거리 계산
                float distance = Vector2.Distance(itemBoxList[i].transform.position, boxPos);

                // 기존 박스와 거리가 가까우면
                if (distance <= 5f)
                {
                    isClose = true;
                    break;
                }
            }

            // 100번 이상 반복하면 탈출
            if (loopNum > 100)
                break;
        }

        // 아이템 박스 생성
        GameObject itembox = LeanPool.Spawn(itemBox, boxPos, Quaternion.identity, ObjectPool.Instance.itemPool);

        // print($"loopNum : {loopNum}");

        return itembox;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 스폰 콜라이더 밖으로 나가면 콜라이더 내부 반대편으로 보내기, 콜라이더 꺼진 경우 아닐때만
        if (other.CompareTag(TagNameList.Enemy.ToString())
        && other.gameObject.activeSelf && dragSwitch && other.enabled)
        {
            Character character = other.GetComponent<Character>();
            EnemyAI enemyAI = other.GetComponent<EnemyAI>();

            // 매니저가 없으면 몬스터 본체가 아니므로 리턴
            if (character == null)
                return;

            // 죽은 몬스터는 미적용
            if (character.isDead)
                return;

            // 이동 대기 카운트 초기화
            character.oppositeCount = 0.5f;

            // 테두리 랜덤 위치로 이동 시키기
            other.transform.position = BorderRandPos();

            // // 원래 부모 기억
            // Transform originParent = other.transform.parent;
            // //몹 스포너로 부모 지정
            // other.transform.parent = transform;
            // // 내부 포지션 역전 및 거리 추가
            // other.transform.localPosition *= -0.8f;
            // //원래 부모로 복귀
            // other.transform.parent = originParent;
        }
    }

    Vector2 InnerRandPos()
    {
        // 콜라이더 내 랜덤 위치
        float spawnPosX = Random.Range(spawnColl.bounds.min.x, spawnColl.bounds.max.x);
        float spawnPosY = Random.Range(spawnColl.bounds.min.y, spawnColl.bounds.max.y);

        return new Vector2(spawnPosX, spawnPosY);
    }

    //콜라이더 테두리 랜덤 위치
    public Vector2 BorderRandPos()
    {
        float edgeRadius = spawnColl.edgeRadius;
        float spawnPosX = Random.Range(spawnColl.bounds.min.x, spawnColl.bounds.max.x);
        float spawnPosY = Random.Range(spawnColl.bounds.min.y, spawnColl.bounds.max.y);
        int spawnSide = Random.Range(0, 4);

        // 스폰될 모서리 방향
        switch (spawnSide)
        {
            // 위
            case 0:
                spawnPosY = spawnColl.bounds.max.y;
                break;

            // 오른쪽
            case 1:
                spawnPosX = spawnColl.bounds.max.x;
                break;

            // 아래
            case 2:
                spawnPosY = spawnColl.bounds.min.y;
                break;

            // 왼쪽
            case 3:
                spawnPosX = spawnColl.bounds.min.x;
                break;
        }

        return new Vector2(spawnPosX, spawnPosY);
    }

    public IEnumerator PointEnemyDir(GameObject enemyObj)
    {
        // 오버레이 풀에서 화살표 UI 생성
        GameObject arrowUI = LeanPool.Spawn(UIManager.Instance.arrowPrefab, enemyObj.transform.position, Quaternion.identity, ObjectPool.Instance.overlayPool);

        //몬스터 활성화 되어있으면
        while (enemyObj && enemyObj.activeSelf)
        {
            // 몬스터가 화면 안에 있으면 화살표 비활성화, 밖에 있으면 활성화
            Vector3 arrowPos = Camera.main.WorldToViewportPoint(enemyObj.transform.position);
            if (arrowPos.x < 0f
            || arrowPos.x > 1f
            || arrowPos.y < 0f
            || arrowPos.y > 1f)
            {
                if (UIManager.Instance.enemyPointSwitch)
                {
                    arrowUI.SetActive(true);

                    // 화살표 위치가 화면 밖으로 벗어나지않게 제한
                    arrowPos.x = Mathf.Clamp(arrowPos.x, 0f, 1f);
                    arrowPos.y = Mathf.Clamp(arrowPos.y, 0f, 1f);
                    arrowUI.transform.position = Camera.main.ViewportToWorldPoint(arrowPos);

                    // 몬스터 방향 가리키기
                    Vector2 dir = enemyObj.transform.position - targetObj.position;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    arrowUI.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
                else
                    arrowUI.SetActive(false);
            }
            else
            {
                arrowUI.SetActive(false);
            }

            yield return null;
        }

        //몬스터 비활성화되면
        yield return new WaitUntil(() => !enemyObj || !enemyObj.activeSelf);

        //화살표 디스폰
        LeanPool.Despawn(arrowUI);
    }

    public IEnumerator AllKillEnemy()
    {
        // 인게임씬 아니면 리턴
        if (SystemManager.Instance.GetSceneName() != SceneName.InGameScene.ToString())
            yield break;

        // 소환된 모든 몬스터 죽이기
        foreach (Character character in spawnEnemyList)
        {
            StartCoroutine(character.hitBoxList[0].Dead(0));
        }
    }
}
