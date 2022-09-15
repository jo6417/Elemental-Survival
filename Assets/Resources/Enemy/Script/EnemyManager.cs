using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using TMPro;
using System.Linq;

public class EnemyManager : Character
{
    [Header("Initial")]
    public EliteClass eliteClass = EliteClass.None; // 엘리트 여부
    public enum EliteClass { None, Power, Speed, Heal };
    public EnemyInfo enemy;
    public bool initialStart = false;
    public bool initialFinish = false;
    public bool lookLeft = false; //기본 스프라이트가 왼쪽을 바라보는지
    public EnemyHitCallback enemyHitCallback; //해당 몬스터 죽을때 실행될 함수들
    public delegate void EnemyHitCallback();

    [SerializeField]
    private List<int> defaultHasItem = new List<int>(); //가진 아이템 기본값
    public List<ItemInfo> nowHasItem = new List<ItemInfo>(); // 현재 가진 아이템
    GameObject targetObj; // 공격 목표
    public GameObject TargetObj
    {
        get
        {
            // 타겟이 없거나 비활성화 되어있으면
            if (targetObj == null || !targetObj.activeSelf)
            {
                // 새로운 타겟 찾기
                targetObj = SearchTarget();
            }

            return targetObj;
        }

        set
        {
            // 타겟 리셋 타임 재설정
            targetResetCount = targetResetTime;

            // 타겟 변경하기
            targetObj = value;

            // 타겟이 null이면
            if (targetObj == null)
            {
                // 목표 위치 리셋 타임 재설정
                // enemyAI.moveResetCount = 0f;

                // 주변 몬스터 타겟 찾아 넣기
                targetObj = SearchTarget();
            }
        }
    }

    [Header("Move")]
    public float targetResetTime = 3f; //타겟 재설정 시간
    public float targetResetCount = 0; //타겟 재설정 시간 카운트
    public Vector3 movePos; // 이동하려는 위치
    public Vector3 targetPos; // 추적한 타겟 위치
    public Vector3 targetDir; // 타겟 방향

    [Header("State")]
    public State nowState; //현재 상태
    public enum State { Idle, Hit, Dead, TimeStop, MagicStop }
    public Action nowAction = Action.Idle; //현재 행동
    public enum Action { Idle, Rest, Walk, Jump, Attack }
    public MoveType moveType;
    public enum MoveType
    {
        Walk, // 걸어서 등속도이동
        Jump, // 시간마다 점프
        Dash, // 범위안에 들어오면 대쉬
        Teleport, // 시간마다 플레이어 주변 위치로 텔레포트
        Follow // 플레이어와 일정거리 고정하며 따라다님
    };

    public float portalSize = 1f; //포탈 사이즈 지정값
    public bool afterEffect = false; // 상태이상 여부
    public bool isDead; //죽음 코루틴 진행중 여부
    public bool selfExplosion = false; //죽을때 자폭 여부
    public float attackRange; // 공격범위
    public bool changeGhost = false;
    float healCount; // Heal 엘리트몹 아군 힐 카운트
    private bool isGhost = false; // 마법으로 소환된 고스트 몬스터인지 여부
    public bool IsGhost
    {
        get
        {
            return isGhost;
        }
    }

    [Header("Refer")]
    public EnemyAI enemyAI;
    public EnemyAtkTrigger enemyAtkTrigger;
    public List<EnemyAttack> enemyAtkList = new List<EnemyAttack>(); // 공격 콜라이더 리스트
    public Transform spriteObj;
    public SpriteRenderer shadow; // 해당 몬스터 그림자
    public CircleCollider2D healRange; // Heal 엘리트 몬스터의 힐 범위
    public List<SpriteRenderer> spriteList = new List<SpriteRenderer>();
    public List<Material> originMatList = new List<Material>(); // 초기 머터리얼
    public List<Color> originMatColorList = new List<Color>(); // 초기 머터리얼 색
    public List<Color> originColorList = new List<Color>(); // 초기 스프라이트 색
    public List<Animator> animList = new List<Animator>(); // 초기 애니메이터
    public Collider2D physicsColl; // 물리용 콜라이더
    public List<EnemyHitBox> hitBoxList; // 히트박스 리스트
    public Sequence damageTextSeq; // 데미지 텍스트 시퀀스

    [Header("Stat")]
    public float powerNow;
    public float speedNow;
    public float rangeNow;
    public float cooltimeNow;

    [Header("Buff")]
    public Transform buffParent; //버프 아이콘 들어가는 부모 오브젝트
    public IEnumerator hitCoroutine;
    public IEnumerator burnCoroutine = null;
    public IEnumerator poisonCoroutine = null;
    public IEnumerator bleedCoroutine = null;
    public IEnumerator slowCoroutine = null;
    public IEnumerator shockCoroutine = null;
    public float particleHitCount = 0; // 파티클 피격 카운트
    public float hitDelayCount = 0; // 피격 딜레이 카운트
    public float stopCount = 0; // 시간 정지 카운트
    public float flatCount = 0; // 납작 디버프 카운트
    public float oppositeCount = 0; // 스포너 반대편 이동 카운트
    public float burnCoolCount; // 화상 도트뎀 남은시간
    public float poisonCoolCount; //독 도트뎀 남은시간
    public float bleedCoolCount; // 출혈 디버프 남은시간

    [Header("Debug")]
    [SerializeField]
    string enemyName;
    [SerializeField]
    string enemyType;
    [SerializeField]
    Vector3 velocity;

    void Awake()
    {
        enemyAI = enemyAI == null ? transform.GetComponent<EnemyAI>() : enemyAI;

        spriteObj = spriteObj == null ? transform : spriteObj;
        rigid = rigid == null ? spriteObj.GetComponentInChildren<Rigidbody2D>(true) : rigid;
        animList = animList.Count == 0 ? GetComponentsInChildren<Animator>().ToList() : animList;

        // 히트 박스 찾기
        hitBoxList = hitBoxList.Count == 0 ? GetComponentsInChildren<EnemyHitBox>().ToList() : hitBoxList;

        // 스프라이트 리스트에 아무것도 없으면 찾아 넣기
        spriteList = spriteList.Count == 0 ? GetComponentsInChildren<SpriteRenderer>().ToList() : spriteList;

        // 초기 스프라이트 정보 수집
        foreach (SpriteRenderer sprite in spriteList)
        {
            originColorList.Add(sprite.color);
            originMatList.Add(sprite.material);
            originMatColorList.Add(sprite.material.color);
        }

        // 버프 아이콘 부모 찾기
        buffParent = buffParent == null ? transform.Find("BuffParent") : buffParent;

        //초기 타겟은 플레이어
        TargetObj = PlayerManager.Instance.gameObject;

        // 공격 트리거 찾기
        enemyAtkTrigger = enemyAtkTrigger == null ? GetComponentInChildren<EnemyAtkTrigger>() : enemyAtkTrigger;
        // 공격 콜라이더 찾기
        enemyAtkList = enemyAtkList.Count == 0 ? GetComponentsInChildren<EnemyAttack>().ToList() : enemyAtkList;

        // 초기화 시작 및 완료 변수 초기화
        // initialStart = false;
        initialFinish = false;
    }

    private void OnEnable()
    {
        // 초기화 완료 취소
        initialFinish = false;

        StartCoroutine(Init());
    }

    private void OnDisable()
    {
        // 힐 범위 오브젝트가 있을때 디스폰
        if (healRange != null)
            LeanPool.Despawn(healRange.gameObject);
    }

    IEnumerator Init()
    {
        // 히트박스 전부 끄기
        for (int i = 0; i < hitBoxList.Count; i++)
        {
            hitBoxList[i].enabled = false;
        }

        // 물리 콜라이더 끄기
        physicsColl.enabled = false;

        //스케일 초기화
        transform.localScale = Vector3.one;

        // rigid 초기화
        rigid.velocity = Vector3.zero;
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 초기화 스위치 켜질때까지 대기
        yield return new WaitUntil(() => initialStart);

        // 고스트 여부 초기화
        isGhost = changeGhost;

        // 다음 리스폰할때 고스트 예약 초기화
        changeGhost = false;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => EnemyDB.Instance.loadDone);

        // 몬스터 정보 찾기
        enemy = EnemyDB.Instance.GetEnemyByName(transform.name.Split('_')[0]);

        // 몬스터 정보 인스턴싱, 몬스터 오브젝트마다 따로 EnemyInfo 갖기
        enemy = new EnemyInfo(enemy);

        // 스탯 초기화
        enemyName = enemy.enemyName;
        enemyType = enemy.enemyType;
        hpMax = enemy.hpMax;
        powerNow = enemy.power;
        speedNow = enemy.speed;
        rangeNow = enemy.range;
        cooltimeNow = enemy.cooltime;

        //엘리트 종류마다 색깔 및 능력치 적용
        switch ((int)eliteClass)
        {
            case 1:
                //체력 상승
                hpMax = hpMax * 2f;

                //힐 오브젝트 소환
                healRange = LeanPool.Spawn(EnemySpawn.Instance.healRange, transform.position, Quaternion.identity, transform).GetComponent<CircleCollider2D>();
                // 힐 오브젝트 크기 초기화
                healRange.transform.localScale = Vector2.one * portalSize;
                // enemyManager 넣어주기
                healRange.GetComponent<EnemyAttack>().enemyManager = this;

                // 초록 아웃라인 머터리얼
                spriteList[0].material = SystemManager.Instance.outLineMat;
                spriteList[0].material.color = Color.green;
                break;

            case 2:
                //공격력 상승
                powerNow = powerNow * 2f;
                // 빨강 아웃라인 머터리얼
                spriteList[0].material = SystemManager.Instance.outLineMat;
                spriteList[0].material.color = Color.red;
                break;

            case 3:
                //속도 상승
                speedNow = speedNow * 2f;
                // 쿨타임 빠르게
                cooltimeNow = cooltimeNow / 2f;

                // 하늘색 아웃라인 머터리얼
                spriteList[0].material = SystemManager.Instance.outLineMat;
                spriteList[0].material.color = Color.cyan;
                break;

            case 4:
                // 일정 범위 만큼 마법 차단하는 파란색 쉴드 생성
                // 해당 범위내 몬스터들은 무적, 맞으면 Miss
                // 콜라이더 stay 함수로 구현, 무적쉴드 충돌이면 무적 설정, 없으면 무적 해제
                //TODO 포스쉴드 프리팹 생성
                break;
        }

        //ItemDB 로드 될때까지 대기
        yield return new WaitUntil(() => ItemDB.Instance.loadDone);

        //보유 아이템 초기화
        nowHasItem.Clear();
        foreach (var itemId in defaultHasItem)
        {
            // id 할당을 위해 변수 선언
            int id = itemId;

            // -1이면 랜덤 원소젬 뽑기
            if (id == -1)
                id = Random.Range(0, 6);

            // item 인스턴스 생성 및 amount 초기화
            ItemInfo item = new ItemInfo(ItemDB.Instance.GetItemByID(id));
            item.amount = 1;

            //item 정보 넣기
            nowHasItem.Add(item);
        }

        hitDelayCount = 0; //데미지 카운트 초기화
        stopCount = 0; //시간 정지 카운트 초기화
        oppositeCount = 0; //반대편 전송 카운트 초기화

        // 고스트일때
        if (IsGhost)
        {
            //체력 절반으로 초기화
            hpNow = hpMax / 2f;

            // rigid, sprite, 트윈, 애니메이션 상태 초기화
            for (int i = 0; i < spriteList.Count; i++)
            {
                // 고스트 여부에 따라 색깔 초기화
                spriteList[i].color = new Color(0, 1, 1, 0.5f);

                // 고스트 여부에 따라 복구 머터리얼 바꾸기
                spriteList[i].material = SystemManager.Instance.outLineMat;
            }

            // 그림자 더 투명하게
            shadow.color = new Color(0, 0, 0, 0.25f);

            // 자폭형 몬스터 아닐때
            if (!selfExplosion)
            {
                // 공격 트리거 레이어를 플레이어 공격으로 바꾸기
                if (enemyAtkTrigger)
                    enemyAtkTrigger.gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
                // 공격 레이어를 플레이어 공격으로 바꾸기
                for (int i = 0; i < enemyAtkList.Count; i++)
                {
                    enemyAtkList[i].gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
                }
            }
        }
        else
        {
            // 맥스 체력으로 초기화
            hpNow = hpMax;

            // 물리 콜라이더 켜기
            physicsColl.enabled = true;

            // 엘리트 몬스터 아닐때
            if (eliteClass == EliteClass.None)
                // rigid, sprite, 트윈, 애니메이션 상태 초기화
                for (int i = 0; i < spriteList.Count; i++)
                {
                    // 고스트 여부에 따라 색깔 초기화
                    spriteList[i].color = originColorList[i];
                    spriteList[i].material = originMatList[i];
                }

            // 그림자 색 초기화
            if (shadow)
                shadow.color = new Color(0, 0, 0, 0.5f);

            // 공격 트리거 레이어를 몬스터 공격으로 바꾸기
            if (enemyAtkTrigger)
                enemyAtkTrigger.gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
            // 공격 레이어를 몬스터 공격으로 바꾸기
            for (int i = 0; i < enemyAtkList.Count; i++)
            {
                enemyAtkList[i].gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
            }
        }

        //죽음 여부 초기화
        isDead = false;

        // idle 상태로 전환
        nowState = State.Idle;

        // 히트박스 전부 켜기
        for (int i = 0; i < hitBoxList.Count; i++)
        {
            hitBoxList[i].enabled = true;
        }

        for (int i = 0; i < animList.Count; i++)
        {
            // 애니메이터 켜기
            animList[i].enabled = true;

            // 애니메이션 속도 초기화
            // 기본값 속도에 비례해서 현재 속도만큼 배율 넣기
            animList[i].speed = 1f * speedNow / EnemyDB.Instance.GetEnemyByID(enemy.id).speed;
        }

        //보스면 체력 UI 띄우기
        if (enemy.enemyType == EnemyDB.EnemyType.Boss.ToString())
        {
            StartCoroutine(UIManager.Instance.UpdateBossHp(this));
        }

        // Idle로 초기화
        nowAction = EnemyManager.Action.Idle;

        // 초기화 완료되면 초기화 스위치 끄기
        initialStart = false;
        // 초기화 완료
        initialFinish = true;
    }

    GameObject SearchTarget()
    {
        // 가장 가까운 적과의 거리
        float closeRange = float.PositiveInfinity;
        GameObject closeEnemy = null;

        //캐릭터 주변의 적들
        List<Collider2D> enemyCollList = Physics2D.OverlapCircleAll(transform.position, 50f, 1 << SystemManager.Instance.layerList.EnemyHit_Layer).ToList();

        // 몬스터 본인 콜라이더는 전부 빼기
        for (int i = 0; i < hitBoxList.Count; i++)
        {
            enemyCollList.Remove(hitBoxList[i].GetComponent<Collider2D>());
        }

        // 주변에 아무도 없으면 리턴 
        if (enemyCollList.Count == 0)
            return null;

        for (int i = 0; i < enemyCollList.Count; i++)
        {
            // 히트박스 컴포넌트가 있을때
            if (enemyCollList[i].TryGetComponent(out EnemyHitBox hitBox))
            {
                // 찾은 몬스터도 고스트일때
                if (hitBox.enemyManager.IsGhost)
                    // 다음으로 넘기기
                    continue;
            }
            else
                // 히트박스 컴포넌트가 없을때
                continue;

            // 해당 적이 이전 거리보다 짧으면
            if (Vector2.Distance(transform.position, enemyCollList[i].transform.position) < closeRange)
            {
                // 해당 적을 타겟으로 바꾸기
                closeEnemy = enemyCollList[i].gameObject;
            }
        }

        //리턴 직전에 고스트인지 재검사, 고스트면 리턴
        if (closeEnemy != null && closeEnemy.GetComponent<EnemyHitBox>().enemyManager.isGhost)
            return null;

        return closeEnemy;
    }

    private void Update()
    {
        // 초기화 안됬으면 리턴
        if (!initialFinish)
        {
            // 이동 멈추기
            rigid.velocity = Vector2.zero;

            return;
        }

        // 고스트일때, 타겟이 비활성화되거나 리셋타임이 되면 타겟 재설정
        if (IsGhost && (targetResetCount <= 0 || TargetObj == null))
            // 타겟 리셋하기
            TargetObj = null;

        // 타겟 리셋 카운트 차감
        if (targetResetCount > 0)
            targetResetCount -= Time.deltaTime;

        // 파티클 히트 딜레이 차감
        if (particleHitCount > 0)
        {
            // state = State.Hit;

            particleHitCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;
        }

        // 히트 딜레이 차감
        if (hitDelayCount > 0)
        {
            // state = State.Hit;

            hitDelayCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;
        }

        // flat 디버프 중일때 카운트 차감
        if (flatCount > 0)
            flatCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;

        // 멈춤 디버프 중일때 카운트 차감
        if (stopCount > 0)
            stopCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;

        // 반대편 보내질때 행동 멈추기
        if (oppositeCount > 0)
            oppositeCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;

        // 힐 오브젝트가 있을때
        if (healRange != null)
        {
            // 쿨타임 중일때
            if (healCount > 0)
            {
                // 힐 콜라이더 끄기
                healRange.enabled = false;

                // 힐 쿨타임 차감
                healCount -= Time.deltaTime;
            }
            // 쿨타임 끝났을때
            else
            {
                // 힐 콜라이더 켜기
                healRange.enabled = true;

                Transform healEffect = healRange.transform.GetChild(0);
                // 이펙트 오브젝트 켜기
                healEffect.gameObject.SetActive(true);
                // 사이즈 제로로 초기화
                healEffect.localScale = Vector2.zero;
                //todo 힐 이펙트 사이즈 키우기
                healEffect.DOScale(Vector2.one, cooltimeNow)
                .OnComplete(() =>
                {
                    // 이펙트 오브젝트 끄기
                    healEffect.gameObject.SetActive(false);
                });

                // 힐 쿨타임을 몬스터 쿨타임으로 갱신
                healCount = cooltimeNow;
            }
        }
    }

    public bool ManageState()
    {
        // 상태이상 여부 초기화
        afterEffect = false;

        // 초기화 안됬으면 리턴
        if (!initialFinish)
            return false;

        // 몬스터 정보 없으면 리턴
        if (enemy == null)
            return false;

        // 비활성화 되었으면 리턴
        if (gameObject == null || !gameObject)
            return false;

        // 죽는 중일때
        if (isDead)
        {
            // 행동불능이므로 false 리턴
            return false;
        }

        //전역 타임스케일이 0 일때
        if (SystemManager.Instance.globalTimeScale == 0)
        {
            nowState = State.MagicStop;

            // 애니메이션 멈추기
            if (animList.Count > 0)
            {
                foreach (Animator anim in animList)
                {
                    anim.speed = 0f;
                }
            }

            // 이동 멈추기
            rigid.velocity = Vector2.zero;

            transform.DOPause();

            // 행동불능이므로 false 리턴
            return false;
        }

        // 멈춤 디버프일때
        if (stopCount > 0)
        {
            nowState = State.TimeStop;

            rigid.velocity = Vector2.zero; //이동 초기화
            rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            // 애니메이션 멈추기
            if (animList.Count > 0)
            {
                foreach (Animator anim in animList)
                {
                    anim.speed = 0f;
                }
            }

            //시간 멈춤 머터리얼 및 색으로 바꾸기
            for (int i = 0; i < spriteList.Count; i++)
            {
                // spriteList[i].material = originMatList[i];
                spriteList[i].color = SystemManager.Instance.stopColor;
            }

            transform.DOPause();

            // 행동불능이므로 false 리턴
            return false;
        }

        //스폰 콜라이더에 닿아 반대편으로 보내질때 잠시대기
        if (oppositeCount > 0)
        {
            rigid.velocity = Vector2.zero; //이동 초기화

            // 행동불능이므로 false 리턴
            return false;
        }

        // 피격 했을때
        if (hitDelayCount > 0)
        {
            return false;
        }

        // 감전 디버프일때
        if (shockCoroutine != null)
        {
            // 행동불능이므로 false 리턴
            return false;
        }

        // 슬로우 디버프일때
        if (slowCoroutine != null)
        {
            // 행동가능이므로 true 리턴
            return true;
        }

        // 포이즌 디버프일때
        if (poisonCoroutine != null)
        {
            // 행동가능이므로 true 리턴
            return true;
        }

        // 모든 문제 없으면 idle 상태로 전환
        // state = State.Idle;

        // 고스트일때
        if (IsGhost)
            // rigid, sprite, 트윈, 애니메이션 상태 초기화
            for (int i = 0; i < spriteList.Count; i++)
            {
                // 고스트 여부에 따라 복구 색깔 및 머터리얼 바꾸기
                spriteList[i].material = SystemManager.Instance.outLineMat;
                spriteList[i].color = new Color(0, 1, 1, 0.5f);
            }
        // 고스트 아닐때
        else
            // rigid, sprite, 트윈, 애니메이션 상태 초기화
            for (int i = 0; i < spriteList.Count; i++)
            {
                // 고스트 여부에 따라 복구 색깔 및 머터리얼 바꾸기
                spriteList[i].color = originColorList[i];

                // 엘리트 아닐때
                if (eliteClass == EliteClass.None)
                    spriteList[i].material = originMatList[i];
            }

        // transform.DOPlay();

        // 애니메이션 속도 초기화
        if (animList.Count > 0)
        {
            foreach (Animator anim in animList)
            {
                anim.speed = 1f * speedNow / EnemyDB.Instance.GetEnemyByID(enemy.id).speed;
            }
        }

        // rigid 초기화
        // rigid.velocity = Vector3.zero;
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 상태 이상 없음
        afterEffect = true;

        // 행동가능이므로 true 리턴
        return true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isDead)
            return;

        if (other.transform.CompareTag(SystemManager.TagNameList.Magic.ToString()))
        {
            // 마법 정보 찾기
            MagicHolder magicHolder = other.GetComponent<MagicHolder>();
            MagicInfo magic = magicHolder.magic;

            //경직 풀기
            if (magicHolder.stopTime > 0)
            {
                // 카운터를 0으로 만들기
                // stopCount = 0;

                // 위치 고정 해제
                // enemyAI.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }

    // 갖고있는 아이템 드랍
    public void DropItem()
    {
        //아이템 없으면 원소젬 1개 추가, 최소 젬 1개라도 떨구게
        if (nowHasItem.Count == 0)
        {
            // 랜덤 원소젬 정보 넣기
            ItemInfo gem = ItemDB.Instance.GetItemByID(Random.Range(0, 6));
            gem.amount = 1;
            nowHasItem.Add(gem);
        }

        //보유한 모든 아이템 드랍
        foreach (ItemInfo item in nowHasItem)
        {
            // print(item.itemName + " : " + item.amount);
            //해당 아이템의 amount 만큼 드랍
            for (int i = 0; i < item.amount; i++)
            {
                //아이템 프리팹 찾기
                GameObject prefab = ItemDB.Instance.GetItemPrefab(item.id);

                if (prefab == null)
                    print(item.name + " : not found");

                //아이템 오브젝트 소환
                GameObject itemObj = LeanPool.Spawn(prefab, transform.position, Quaternion.identity, SystemManager.Instance.itemPool);

                //아이템 정보 넣기
                itemObj.GetComponent<ItemManager>().item = item;

                //아이템 리지드 찾기
                Rigidbody2D itemRigid = itemObj.GetComponent<Rigidbody2D>();

                // 랜덤 방향으로 아이템 날리기
                itemRigid.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * Random.Range(3f, 5f);

                // 아이템 랜덤 회전 시키기
                itemRigid.angularVelocity = Random.value < 0.5f ? 180f : -180f;
            }
        }

        // 엘리트 몬스터가 아닐때
        if (eliteClass == EliteClass.None)
        {
            // 드랍률에 따라 마법 드랍
            if (Random.value <= enemy.dropRate)
            {
                // 몬스터 등급에 해당하는 shard 찾기
                ItemInfo shardItem = ItemDB.Instance.GetItemByName("Magic Shard" + enemy.grade);
                // 아이템 프리팹 찾기
                GameObject shardPrefab = ItemDB.Instance.GetItemPrefab(shardItem.id);

                //아이템 오브젝트 소환
                GameObject shardObj = LeanPool.Spawn(shardPrefab, transform.position, Quaternion.identity, SystemManager.Instance.itemPool);

                // 아이템 매니저 찾기
                ItemManager itemManager = shardObj.GetComponent<ItemManager>();
                //아이템 정보 넣기
                itemManager.item = shardItem;

                //아이템 리지드 찾기
                Rigidbody2D itemRigid = shardObj.GetComponent<Rigidbody2D>();

                // 랜덤 방향, 랜덤 파워로 아이템 날리기
                itemRigid.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * Random.Range(3f, 5f);

                // 아이템 랜덤 회전 시키기
                float randomRotate = Random.Range(1f, 2f);
                itemRigid.angularVelocity = randomRotate < 1.5f ? 90f * randomRotate : -90f * randomRotate;
            }
        }
        // 엘리트 몬스터면 마법 확정 드랍
        else
        {
            ItemInfo itemInfo = null;
            GameObject itemObj = null;
            string itemName = "";

            // 랜덤 등급 뽑기
            int randomGrade = Random.Range(0, 7);
            // 뽑은 등급으로 랜덤 마법 뽑기
            MagicInfo randomMagic = MagicDB.Instance.RandomMagic(randomGrade);

            // 0등급일때 마법 이름으로 아이템 이름 짓기
            if (randomMagic.grade == 0)
            {
                // 아이템 이름 짓기
                itemName = randomMagic.name;
            }
            // 0등급 아닐때는 몬스터 등급으로 shard 이름 짓기
            else
            {
                // 소환할 shard 등급 랜덤하게 레벨업 (1~6 사이로 고정)
                int itemGrade = Mathf.Clamp(enemy.grade + Random.Range(0, 2), 1, 6);

                // 아이템 이름 짓기
                itemName = "Magic Shard" + itemGrade;
            }

            // 몬스터 등급에 해당하는 shard 찾기
            itemInfo = ItemDB.Instance.GetItemByName(itemName);

            // 아이템 프리팹 찾기
            GameObject itemPrefab = ItemDB.Instance.GetItemPrefab(itemInfo.id);

            //아이템 오브젝트 소환
            itemObj = LeanPool.Spawn(itemPrefab, transform.position, Quaternion.identity, SystemManager.Instance.itemPool);

            // 아이템 매니저 찾기
            ItemManager itemManager = itemObj.GetComponent<ItemManager>();
            //아이템 정보 넣기
            itemManager.item = itemInfo;

            //아이템 리지드 찾기
            Rigidbody2D itemRigid = itemObj.GetComponent<Rigidbody2D>();

            // 랜덤 방향, 랜덤 파워로 아이템 날리기
            itemRigid.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * Random.Range(3f, 5f);

            // 랜덤으로 방향 및 속도 결정
            float randomRotate = Random.Range(1f, 3f);
            // 아이템 랜덤 속도로 회전 시키기
            itemRigid.angularVelocity = randomRotate < 2f ? 90f * randomRotate : -90f * randomRotate;
        }
    }
}
