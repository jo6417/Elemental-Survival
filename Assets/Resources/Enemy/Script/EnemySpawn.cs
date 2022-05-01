using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class EnemySpawn : MonoBehaviour
{
    #region Singleton
    private static EnemySpawn instance;
    public static EnemySpawn Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<EnemySpawn>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<EnemySpawn>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public bool spawnSwitch;
    Collider2D col;
    public int MaxEnemyPower; //최대 몬스터 수
    public int NowEnemyPower; //현재 몬스터 수
    public float spawnCoolTime = 3f; //몬스터 스폰 쿨타임
    public float spawnCoolCount; //몬스터 스폰 쿨타임 카운트

    [Header("Refer")]
    public Transform enemyPool; //몬스터 소환될 부모 오브젝트
    public Transform effectPool; //이펙트 소환될 부모 오브젝트
    public GameObject dustPrefab; //먼지 이펙트 프리팹
    public GameObject mobPortal; //몬스터 등장할 포탈 프리팹
    public GameObject bloodPrefab; //혈흔 프리팹

    void Start()
    {
        col = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        //스폰 멈추기 스위치
        if (!spawnSwitch)
            return;

        //DB 로드 되어야 진행
        if (!EnemyDB.Instance.loadDone)
            return;

        // 쿨타임마다 실행하기
        if (spawnCoolCount <= 0)
        {
            // 1~5초 사이 랜덤 쿨타임, 쿨타임 최대치는 플레이어 전투력마다 0.05초씩 줄어듬, 100레벨시 1초
            float maxCoolTime = 5 - PlayerManager.Instance.PlayerStat_Now.playerPower * 0.05f;
            //1~5초 사이 값으로 범위 제한
            maxCoolTime = Mathf.Clamp(maxCoolTime, 1f, maxCoolTime * 2);
            // 플레이어 전투력에 따라 줄어드는 쿨타임 계산
            spawnCoolTime = Random.Range(1, maxCoolTime);

            //몬스터 스폰 쿨타임 초기화
            spawnCoolCount = spawnCoolTime;

            //몬스터 스폰 랜덤 횟수,  최대치는 플레이어 전투력마다 0.05씩 증가
            float maxSpawnNum = 5 + PlayerManager.Instance.PlayerStat_Now.playerPower * 0.05f;
            // 스폰 횟수 범위 제한
            maxSpawnNum = Mathf.Clamp(maxSpawnNum, maxSpawnNum, 10);

            int spawnNum = Random.Range(1, (int)maxSpawnNum); //1~3번 중 랜덤으로 반복
            // print(spawnCoolTime + " / " + spawnNum + " 번 스폰");

            for (int i = 0; i < spawnNum; i++)
            {
                //랜덤 몬스터 스폰
                StartCoroutine(SpawnMob());
            }
        }
        else
        {
            spawnCoolCount -= Time.deltaTime; //스폰 쿨타임 카운트하기
        }

        //! 테스트용 수동 스폰, 쿨타임 무시, 몬스터 총 전투력 무시
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //랜덤 몬스터 스폰
            StartCoroutine(SpawnMob(true));
        }
    }

    IEnumerator SpawnMob(bool ForceSpawn = false)
    {
        //총 누적시간 30초로 나눴을때의 몫
        float time = UIManager.Instance.time_current;
        int timePower = Mathf.FloorToInt(time / 30f);

        //몬스터 총 전투력 최대값 = 플레이어 전투력 + 누적 시간 계수
        MaxEnemyPower = PlayerManager.Instance.PlayerStat_Now.playerPower + timePower;

        //max 전투력 넘었으면 중단
        if (MaxEnemyPower <= NowEnemyPower)
            yield break;

        //몬스터DB에서 랜덤 id 뽑기
        int enemyId = -1;
        EnemyInfo enemy = null;
        GameObject enemyObj = null;

        //나올수 있는 몬스터의 최대 등급
        int maxGrade = MaxEnemyPower - NowEnemyPower;
        maxGrade = Mathf.Clamp(maxGrade, 1, 6); //최대 6까지

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

        //엘리트 출현 유무 (시간에 따라 엘리트 출현율 상승)
        bool isElite = Random.value < timePower / 100f; //3000초=50분 이상이면 100% 엘리트

        //! 테스트, 무조건 엘리트 몬스터 스폰
        isElite = true;

        //몬스터 총 전투력 올리기
        NowEnemyPower += enemy.grade;

        //포탈에서 몬스터 소환
        StartCoroutine(PortalSpawn(enemy, isElite));

        // print(enemy.enemyName + " : 스폰");
    }

    IEnumerator PortalSpawn(EnemyInfo enemy, bool isElite)
    {
        //몬스터 프리팹 찾기
        GameObject enemyPrefab = EnemyDB.Instance.GetPrefab(enemy.id);

        //몬스터 소환 완료 위치
        Vector2 spawnEndPos = BorderRandPos();

        // 몬스터 프리팹 소환 및 비활성화
        GameObject enemyObj = LeanPool.Spawn(enemyPrefab, spawnEndPos, Quaternion.identity, enemyPool);
        enemyObj.SetActive(false);

        //프리팹에서 스프라이트 컴포넌트 찾기
        SpriteRenderer enemySprite = enemyObj.GetComponentInChildren<SpriteRenderer>();

        //EnemyInfo 인스턴스 생성
        EnemyInfo enemyInfo = new EnemyInfo(enemy);

        EnemyManager enemyManager = enemyObj.GetComponent<EnemyManager>();

        // 몬스터 랜덤 스킬 뽑기
        if (isElite)
        {
            enemyManager.isElite = true;

            //엘리트 종류 뽑아서 매니저에 전달
            int eliteClass = Random.Range(1, 4);
            enemyManager.eliteClass = eliteClass;

            //엘리트 종류마다 색깔 및 능력치 적용
            switch (eliteClass)
            {
                case 1:
                    //체력 1.5배
                    enemyInfo.hpMax = enemyInfo.hpMax * 1.5f;
                    // 초록 아웃라인 머터리얼
                    enemySprite.material = VarManager.Instance.outLineMat;
                    enemySprite.material.color = Color.green;
                    break;

                case 2:
                    //공격력 1.5배
                    enemyInfo.power = enemyInfo.power * 1.5f;
                    // 빨강 아웃라인 머터리얼
                    enemySprite.material = VarManager.Instance.outLineMat;
                    enemySprite.material.color = Color.red;
                    break;

                case 3:
                    //속도 1.5배
                    enemyInfo.speed = enemyInfo.speed * 1.5f;
                    // 하늘색 아웃라인 머터리얼
                    enemySprite.material = VarManager.Instance.outLineMat;
                    enemySprite.material.color = Color.cyan;
                    break;

                case 4:
                    //쉴드
                    //TODO 포스쉴드 오브젝트 추가
                    break;
            }
        }
        else
        {
            //일반 스프라이트 머터리얼
            enemySprite.material = VarManager.Instance.spriteMat;
            enemyManager.isElite = false;
        }

        //EnemyInfo 정보 넣기
        enemyManager.enemy = enemyInfo;

        //포탈 소환 위치
        Vector2 portalPos = spawnEndPos + Vector2.down * enemySprite.bounds.size.y / 2;
        //몬스터 소환 시작 위치
        Vector2 spawnStartPos = spawnEndPos + Vector2.down * enemySprite.bounds.size.y * 1.5f;
        // print(enemy.enemyName + " : " + enemySprite.bounds.size.y);

        // print(transform.name + ":" + spawnStartPos + ":" + spawnEndPos);

        // 몬스터 발밑에서 포탈생성
        GameObject portal = LeanPool.Spawn(mobPortal, portalPos, Quaternion.identity, enemyPool);

        //아이콘 찾기
        GameObject iconObj = portal.transform.Find("PortalMask").Find("EnemyIcon").gameObject;
        //아이콘 시작위치로 이동 및 활성화
        iconObj.transform.position = spawnStartPos;
        // 떠오를 스프라이트에 몬스터 아이콘 넣기
        iconObj.GetComponent<SpriteRenderer>().sprite = enemySprite.sprite;
        iconObj.SetActive(false);

        //포탈 사이즈 줄이기
        portal.transform.localScale = Vector3.zero;

        //포탈 스폰 시퀀스
        Sequence portalSeq = DOTween.Sequence();
        portalSeq
        .Append(
            // 포탈을 몬스터 사이즈 맞게 키우기
            portal.transform.DOScale(new Vector2(enemyManager.portalSize, enemyManager.portalSize), 0.5f)
            .OnComplete(() =>
            {
                //아이콘 활성화
                iconObj.SetActive(true);
                //아이콘은 줄이기
                iconObj.transform.localScale = Vector2.one * 0.1f / enemyManager.portalSize;
            })
        )
        .Append(
            // 소환 완료 위치로 domove
            iconObj.transform.DOMove(spawnEndPos, 1f)
        )
        .AppendCallback(() =>
        {
            // 아이콘 사라지기
            iconObj.SetActive(false);
            // 몬스터 프리팹 활성화
            enemyObj.SetActive(true);
        })
        .Append(
            //포탈 사이즈 줄여 사라지기
            portal.transform.DOScale(Vector3.zero, 0.5f)
            .OnComplete(() =>
            {
                //포탈 디스폰
                LeanPool.Despawn(portal);
            })
        );

        //시간 멈추는동안 포탈 시퀀스 멈추기
        while (portalSeq.IsActive())
        {
            if(VarManager.Instance.timeScale == 0)
            portalSeq.Pause();
            else
            portalSeq.Play();

            yield return null;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 스폰 콜라이더 밖으로 나가면 콜라이더 내부 반대편으로 보내기
        if (other.CompareTag("Enemy") && other.gameObject.activeSelf)
        {
            EnemyManager manager = other.GetComponent<EnemyManager>();
            EnemyAI enemyAI = other.GetComponent<EnemyAI>();

            //죽은 몬스터는 미적용
            if (manager.isDead)
                return;

            //점프 몬스터는 점프 시퀀스 초기화
            if(enemyAI.moveType == EnemyAI.MoveType.Jump)
            enemyAI.jumpSeq.Pause();

            //이동 대기 카운트 초기화
            manager.oppositeCount = 0.5f;

            //원래 부모 기억
            Transform originParent = other.transform.parent;

            //몹 스포너로 부모 지정
            other.transform.parent = transform;
            // 내부 포지션 역전 및 거리 추가
            other.transform.localPosition *= -0.8f;

            other.transform.parent = originParent; //원래 부모로 복귀
        }
    }

    Vector2 InnerRandPos()
    {
        // 콜라이더 내 랜덤 위치
        float spawnPosX = Random.Range(col.bounds.min.x, col.bounds.max.x);
        float spawnPosY = Random.Range(col.bounds.min.y, col.bounds.max.y);

        return new Vector2(spawnPosX, spawnPosY);
    }

    //콜라이더 테두리 랜덤 위치
    Vector2 BorderRandPos()
    {
        float spawnPosX = Random.Range(col.bounds.min.x, col.bounds.max.x);
        float spawnPosY = Random.Range(col.bounds.min.y, col.bounds.max.y);
        int spawnSide = Random.Range(0, 4);

        // 스폰될 모서리 방향
        switch (spawnSide)
        {
            case 0:
                spawnPosY = col.bounds.max.y;
                break;

            case 1:
                spawnPosX = col.bounds.max.x;
                break;

            case 2:
                spawnPosY = col.bounds.min.y;
                break;

            case 3:
                spawnPosX = col.bounds.min.x;
                break;
        }

        return new Vector2(spawnPosX, spawnPosY);
    }
}
