using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;
using UnityEngine.Experimental;

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
                var obj = FindObjectOfType<WorldSpawner>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<WorldSpawner>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("Debug")]
    public bool spawnSwitch; //몬스터 스폰 ON/OFF
    public bool randomSpawn; //랜덤 몬스터 스폰 ON/OFF
    public bool dragSwitch; //몬스터 반대편 이동 ON/OFF
    public bool allEliteSwitch; // 모든 적이 엘리트

    [Header("State")]
    [SerializeField] int modifyEnemyPower; //! 전투력 임의 수정값
    [SerializeField, ReadOnly] int MaxEnemyPower; //최대 몬스터 전투력
    public int NowEnemyPower; //현재 몬스터 전투력
    [SerializeField] float enemySpawnCount; //몬스터 스폰 쿨타임 카운트
    [SerializeField] float itemboxSpawnCount; //아이템 박스 스폰 쿨타임 카운트
    [SerializeField] float lockerSpawnCount; //아이템 금고 스폰 쿨타임 카운트
    [SerializeField, ReadOnly] bool nowSpawning; //스폰중일때
    public List<Character> spawnAbleList = new List<Character>(); // 현재 맵에서 스폰 가능한 몹 리스트
    public List<Character> spawnEnemyList = new List<Character>(); //현재 스폰된 몬스터 리스트
    public List<GameObject> itemBoxList = new List<GameObject>(); // 현재 스폰된 아이템 박스 리스트
    public List<GameObject> lockerList = new List<GameObject>(); // 현재 스폰된 아이템 금고 리스트
    List<float> eliteWeight = new List<float>(); // 엘리트 몬스터 가중치 리스트

    [Header("Refer")]
    [SerializeField] GameObject itemBoxPrefab; // 파괴 가능한 아이템 박스
    [SerializeField] GameObject lockerPrefab; // 구매 가능한 아이템 금고
    public GameObject slotMachinePrefab; // 슬롯머신 프리팹
    [SerializeField] GameObject mobPortal; //몬스터 등장할 포탈 프리팹
    public GameObject healRange; // Heal 엘리트 몬스터의 힐 범위
    public GameObject hitEffect; // 몬스터 피격 이펙트
    public GameObject blockEffect; // 몬스터 무적시 피격 이펙트
    public GameObject dustPrefab; //먼지 이펙트 프리팹
    public GameObject bloodPrefab; //혈흔 프리팹
    public BoxCollider2D spawnColl; // 스포너 테두리 콜라이더

    private void Awake()
    {
        // 현재 스폰 몬스터 리스트 비우기
        spawnEnemyList.Clear();

        // 엘리트 종류 가중치
        eliteWeight.Add(0); // 엘리트 아닐 확률 가중치 = 0
        eliteWeight.Add(20); // Power 엘리트 가중치
        eliteWeight.Add(20); // Speed 엘리트 가중치
        eliteWeight.Add(10); // Heal 엘리트 가중치
    }

    void Start()
    {
        spawnColl = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        //DB 로드 되어야 진행
        if (!EnemyDB.Instance.loadDone)
            return;

        // 스폰 스위치 켜져있을때
        if (spawnSwitch)
            // 쿨타임마다 몬스터 스폰, 스폰중 아닐때
            if (enemySpawnCount <= 0 && !nowSpawning)
            {
                //몬스터 스폰 랜덤 횟수,  최대치는 플레이어 전투력마다 0.05씩 증가
                float maxSpawnNum = 5 + PlayerManager.Instance.PlayerStat_Now.playerPower * 0.05f;

                // 스폰 횟수 범위 제한
                maxSpawnNum = Mathf.Clamp(maxSpawnNum, maxSpawnNum, 10);

                // 1~3번 중 랜덤으로 반복
                int spawnNum = Random.Range(1, (int)maxSpawnNum);

                // print(spawnCoolCount + " / " + spawnNum + " 번 스폰");

                for (int i = 0; i < spawnNum; i++)
                {
                    //랜덤 몬스터 스폰
                    StartCoroutine(SpawnEnemy());
                }
            }
            else
                enemySpawnCount -= Time.deltaTime;

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

    IEnumerator SpawnEnemy(bool ForceSpawn = false)
    {
        //스폰 스위치 꺼졌으면 스폰 멈추기
        if (!spawnSwitch)
            yield break;

        // 30초 단위로 시간 계수 증가
        float time = SystemManager.Instance.time_current;
        float timePower = time / 30f;

        //몬스터 총 전투력 최대값 = 플레이어 전투력 + 누적 시간 계수
        MaxEnemyPower = PlayerManager.Instance.PlayerStat_Now.playerPower + Mathf.FloorToInt(timePower) + modifyEnemyPower;

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

        //todo 난이도 계수 곱하기 난이도 높을수록 출현율 증가 (난이도 시스템 구현 후)
        // 엘리트 출현 유무 (시간 및 총 전투력에 따라 엘리트 출현율 상승)
        bool isElite =
        Random.value < timePower / 100f // 30초마다 1%씩 출현율 상승 (3000초=50분 이상이면 100% 엘리트)
        + MaxEnemyPower / 10f / 100f; // 파워 10마다 1%씩 출현율 상승

        //! 테스트, 무조건 엘리트 몬스터 스폰
        if (allEliteSwitch)
            isElite = allEliteSwitch;

        //몬스터 총 전투력 올리기
        NowEnemyPower += enemy.grade;

        // 스폰 스위치 켜졌을때
        if (spawnSwitch)
            //포탈에서 몬스터 소환
            StartCoroutine(PortalSpawn(enemy, isElite));

        // 해당 몬스터의 쿨타임 넣기
        float spawnCoolTime = enemy.spawnCool;
        // 쿨타임에 50% 범위 내 랜덤성 부여
        spawnCoolTime = Random.Range(spawnCoolTime * 0.5f, spawnCoolTime * 1.5f);
        // 1~5초 사이 값으로 범위 제한
        spawnCoolTime = Mathf.Clamp(spawnCoolTime, 1f, 10f);

        // 쿨타임 갱신
        enemySpawnCount = spawnCoolTime;

        // print(enemy.enemyName + " : 스폰");

        // 스폰 끝
        nowSpawning = false;
    }

    public IEnumerator PortalSpawn(EnemyInfo enemy = null, bool isElite = false, Vector2 spawnEndPos = default, GameObject enemyPrefab = null, bool isBoss = false)
    {
        // enemyPrefab 변수 안들어왔으면
        if (enemyPrefab == null)
            // 몬스터 프리팹 찾기
            enemyPrefab = EnemyDB.Instance.GetPrefab(enemy.id);

        // spawnEndPos 소환 위치 변수 없으면 지정
        if (spawnEndPos == default)
            // 몬스터 소환 완료 위치
            spawnEndPos = BorderRandPos();

        // 몬스터 프리팹 소환
        GameObject enemyObj = LeanPool.Spawn(enemyPrefab, spawnEndPos, Quaternion.identity, SystemManager.Instance.enemyPool);
        // 소환된 몬스터 위치 이동
        enemyObj.transform.position = spawnEndPos;
        // 몬스터 비활성화
        enemyObj.SetActive(false);

        //프리팹에서 스프라이트 컴포넌트 찾기
        SpriteRenderer enemySprite = enemyObj.GetComponentInChildren<SpriteRenderer>();

        //EnemyInfo 인스턴스 생성
        EnemyInfo enemyInfo = new EnemyInfo(enemy);

        // 캐릭터 찾기
        Character character = enemyObj.GetComponentInChildren<Character>();

        // 보스일때
        if (isBoss)
            // 게이트에 캐릭터 변수 전달
            GatePortal.Instance.bossCharacter = character;

        //몬스터 리스트에 넣기
        spawnEnemyList.Add(character);

        // 엘리트 종류 기본값 None
        int eliteClass = 0;
        // 몬스터 랜덤 스킬 뽑기
        if (isElite)
        {
            // 엘리트 종류 가중치로 뽑기
            eliteClass = SystemManager.Instance.WeightRandom(eliteWeight);
        }
        else
        {
            //일반 스프라이트 머터리얼
            enemySprite.material = SystemManager.Instance.spriteLitMat;
        }
        // 엘리트 종류를 매니저에 전달
        character.eliteClass = (Character.EliteClass)eliteClass;

        //EnemyInfo 정보 넣기
        character.enemy = enemyInfo;

        //포탈 소환 위치
        Vector2 portalPos = spawnEndPos + Vector2.down * character.portalSize / 2;
        //몬스터 소환 시작 위치
        Vector2 spawnStartPos = spawnEndPos + Vector2.down * enemySprite.bounds.size.y * 1.5f;

        // print(transform.name + ":" + spawnStartPos + ":" + spawnEndPos);

        // 몬스터 발밑에서 포탈생성
        GameObject portal = LeanPool.Spawn(mobPortal, portalPos, Quaternion.identity, SystemManager.Instance.enemyPool);

        // 포탈 스프라이트 켜기
        portal.GetComponent<SpriteRenderer>().enabled = true;

        // 포탈에서 몬스터 올라오는 속도
        float portalUpSpeed = 1f;

        //아이콘 찾기
        GameObject iconObj = portal.transform.Find("PortalMask").Find("EnemyIcon").gameObject;

        //아이콘 시작위치로 이동 및 활성화
        iconObj.transform.position = spawnStartPos;

        // 아이콘 스프라이트 찾기
        SpriteRenderer iconSprite = iconObj.GetComponent<SpriteRenderer>();

        // 떠오를 스프라이트에 몬스터 아이콘 넣기
        iconSprite.sprite = EnemyDB.Instance.GetIcon(enemy.id);

        //아이콘 비활성화
        iconObj.SetActive(false);

        //포탈 사이즈 줄이기
        portal.transform.localScale = Vector3.zero;

        //포탈 스폰 시퀀스
        Sequence portalSeq = DOTween.Sequence();
        portalSeq
        .Append(
            // 포탈을 몬스터 사이즈 맞게 키우기
            portal.transform.DOScale(new Vector2(character.portalSize, character.portalSize), 0.5f)
            .OnComplete(() =>
            {
                //아이콘 활성화
                iconObj.SetActive(true);
                //아이콘은 줄이기
                iconObj.transform.localScale = Vector2.one * 0.1f / character.portalSize;
            })
        )
        .Append(
            // 소환 완료 위치로 domove
            iconObj.transform.DOMove(spawnEndPos, portalUpSpeed)
        )
        .AppendCallback(() =>
        {
            // 아이콘 사라지기
            iconObj.SetActive(false);
            // 몬스터 프리팹 활성화
            enemyObj.SetActive(true);

            // 소환된 몬스터 초기화 시작
            character.initialStart = true;

            // 몬스터 위치 가리킬 화살표 UI 소환
            StartCoroutine(PointEnemyDir(enemyObj));
        })
        .Append(
            //포탈 사이즈 줄여 사라지기
            portal.transform.DOScale(Vector3.zero, 0.5f)
            .SetAutoKill()
            .OnComplete(() =>
            {
                //포탈 디스폰
                LeanPool.Despawn(portal);
            })
        );

        yield return null;
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
        GameObject itembox = LeanPool.Spawn(itemBox, boxPos, Quaternion.identity, SystemManager.Instance.itemPool);

        // print($"loopNum : {loopNum}");

        return itembox;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 스폰 콜라이더 밖으로 나가면 콜라이더 내부 반대편으로 보내기, 콜라이더 꺼진 경우 아닐때만
        if (other.CompareTag(SystemManager.TagNameList.Enemy.ToString())
        && other.gameObject.activeSelf && dragSwitch && other.enabled)
        {
            Character manager = other.GetComponent<Character>();
            EnemyAI enemyAI = other.GetComponent<EnemyAI>();

            // 매니저가 없으면 몬스터 본체가 아니므로 리턴
            if (manager == null)
                return;

            //죽은 몬스터는 미적용
            if (manager.isDead)
                return;

            //이동 대기 카운트 초기화
            manager.oppositeCount = 0.5f;

            //원래 부모 기억
            Transform originParent = other.transform.parent;

            //몹 스포너로 부모 지정
            other.transform.parent = transform;
            // 내부 포지션 역전 및 거리 추가
            other.transform.localPosition *= -0.8f;
            //원래 부모로 복귀
            other.transform.parent = originParent;
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
                spawnPosY = Random.Range(spawnColl.bounds.max.y, spawnColl.bounds.max.y + edgeRadius);
                break;

            // 오른쪽
            case 1:
                spawnPosX = Random.Range(spawnColl.bounds.max.x, spawnColl.bounds.max.x + edgeRadius);
                break;

            // 아래
            case 2:
                spawnPosY = Random.Range(spawnColl.bounds.min.y, spawnColl.bounds.min.y - edgeRadius);
                break;

            // 왼쪽
            case 3:
                spawnPosX = Random.Range(spawnColl.bounds.min.x, spawnColl.bounds.min.x - edgeRadius);
                break;
        }

        return new Vector2(spawnPosX, spawnPosY);
    }

    IEnumerator PointEnemyDir(GameObject enemyObj)
    {
        // 오버레이 풀에서 화살표 UI 생성
        GameObject arrowUI = LeanPool.Spawn(UIManager.Instance.arrowPrefab, enemyObj.transform.position, Quaternion.identity, SystemManager.Instance.overlayPool);

        //몬스터 활성화 되어있으면
        while (enemyObj.activeSelf)
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
                    Vector2 dir = enemyObj.transform.position - PlayerManager.Instance.transform.position;
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
        yield return new WaitUntil(() => !enemyObj.activeSelf);

        //화살표 디스폰
        LeanPool.Despawn(arrowUI);
    }
}
