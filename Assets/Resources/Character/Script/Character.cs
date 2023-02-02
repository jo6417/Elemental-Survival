using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using TMPro;
using System.Linq;
using System.Text;

public class Character : MonoBehaviour
{
    [Header("CallBack")]
    public InitCallback initCallback; // 캐릭터 생성시 실행될 콜백
    public delegate void InitCallback();
    public HitCallback hitCallback; // 캐릭터 피격시 실행될 콜백
    public delegate void HitCallback();
    public DeadCallback deadCallback; // 캐릭터 사망시 실행될 콜백
    public delegate void DeadCallback(Character character);

    [Header("Init")]
    public EnemyInfo enemy;
    public List<int> defaultHasItem = new List<int>(); //가진 아이템 기본값
    public List<ItemInfo> nowHasItem = new List<ItemInfo>(); // 현재 가진 아이템
    public bool usePortal = true; // 등장시 포탈 사용 여부
    public bool initialStart = false;
    public bool initialFinish = false;
    public EliteClass eliteClass = EliteClass.None; // 엘리트 여부
    public enum EliteClass { None, Power, Speed, Heal };
    public bool lookLeft = false; //기본 스프라이트가 왼쪽을 바라보는지

    [Header("Status")]
    public float hpNow;
    public float hpMax;
    public bool isDead; //죽음 코루틴 진행중 여부
    public float deadDelay = 1f; // 죽는 트랜지션 진행 시간
    public bool changeGhost = false;
    public float portalSize = 1f; //포탈 사이즈 지정값
    public bool afterEffect = false; // 상태이상 여부
    public bool invinsible = false; //현재 무적 여부
    public bool selfExplosion = false; //죽을때 자폭 여부
    public float attackRange; // 공격범위
    public float healCount; // Heal 엘리트몹 아군 힐 카운트

    [Header("State")]
    public bool isGhost = false; // 마법으로 소환된 고스트 몬스터인지 여부
    public bool IsGhost
    {
        get
        {
            return isGhost;
        }
    }

    public State nowState = State.Idle; //현재 행동
    public enum State { Idle, Rest, Walk, Jump, Attack, Dead }
    public MoveType moveType;
    public enum MoveType
    {
        Walk, // 걸어서 등속도이동
        Jump, // 시간마다 점프
        Dash, // 범위안에 들어오면 대쉬
        Custom // 자체 AI로 컨트롤
    };
    [SerializeField] GameObject targetObj; // 공격 목표
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
    public float targetResetTime = 0.5f; //타겟 재설정 시간
    public float targetResetCount = 0; //타겟 재설정 시간 카운트
    public Vector3 movePos; // 이동하려는 위치
    public Vector3 targetPos; // 추적한 타겟 위치
    public Vector3 targetDir; // 타겟 방향

    [Header("Refer")]
    public EnemyAI enemyAI;
    public EnemyAtkTrigger enemyAtkTrigger;
    public List<EnemyAttack> enemyAtkList = new List<EnemyAttack>(); // 공격 콜라이더 리스트
    public CircleCollider2D healRange; // Heal 엘리트 몬스터의 힐 범위
    public List<Animator> animList = new List<Animator>(); // 보유한 모든 애니메이터
    public Collider2D physicsColl; // 물리용 콜라이더
    public Rigidbody2D rigid;

    [Header("Buff")]
    public Transform buffParent; //버프 아이콘 들어가는 부모 오브젝트
    public IEnumerator hitCoroutine;
    public enum Debuff { Burn, Poison, Bleed, Slow, Shock, Stun, Stop, Flat, Freeze };
    public IEnumerator[] DebuffList = new IEnumerator[System.Enum.GetValues(typeof(Debuff)).Length];

    public float particleHitCount = 0; // 파티클 피격 카운트
    public float hitDelayCount = 0; // 피격 딜레이 카운트
    public float stopCount = 0; // 시간 정지 카운트
    public float stunCount = 0; // 스턴 카운트
    public float flatCount = 0; // 납작 디버프 카운트
    public float oppositeCount = 0; // 스포너 반대편 이동 카운트
    public float moveSpeedDebuff = 1f; // 속도 디버프

    [Header("Sprite")]
    public Transform spriteObj;
    public List<SpriteRenderer> spriteList = new List<SpriteRenderer>();
    public List<Material> originMatList = new List<Material>(); // 초기 머터리얼
    public List<Color> originMatColorList = new List<Color>(); // 초기 머터리얼 색
    public List<Color> originColorList = new List<Color>(); // 초기 스프라이트 색
    public List<HitBox> hitBoxList; // 보유한 모든 히트박스 리스트
    public GameObject hitEffect; // 피격시 피격지점에서 발생할 이펙트
    public SpriteRenderer shadow; // 해당 몬스터 그림자

    [Header("Stat")]
    public float powerNow;
    public float speedNow;
    public float rangeNow;
    public float cooltimeNow;

    [Header("Debug")]
    [SerializeField] string enemyName;
    [SerializeField] string enemyType;

    protected virtual void Awake()
    {
        StartCoroutine(AwakeInit());
    }

    IEnumerator AwakeInit()
    {
        enemyAI = enemyAI == null ? transform.GetComponent<EnemyAI>() : enemyAI;

        spriteObj = spriteObj == null ? transform : spriteObj;
        rigid = rigid == null ? spriteObj.GetComponentInChildren<Rigidbody2D>(true) : rigid;
        animList = animList.Count == 0 ? GetComponentsInChildren<Animator>().ToList() : animList;

        // 히트 박스 모두 찾기
        hitBoxList = hitBoxList.Count == 0 ? GetComponentsInChildren<HitBox>().ToList() : hitBoxList;

        // 스프라이트 설정 안했으면 디버그 메시지
        if (spriteList.Count == 0)
            Debug.Log("SpriteList is null");

        // 그림자는 빼기
        if (shadow != null)
            spriteList.Remove(shadow);

        // 버프 아이콘 부모 찾기, 없으면 본인 오브젝트
        buffParent = buffParent == null ? transform : buffParent;

        yield return null;
    }

    protected virtual void OnEnable()
    {
        StartCoroutine(Init());
    }

    public void FindEnemyInfo()
    {
        // 오브젝트 이름에서 _Prefab 앞쪽의 몬스터 이름 구하기
        int index = gameObject.name.IndexOf("_Prefab");

        // 이름 찾았으면 진행
        if (index != -1)
            // 몬스터 정보 찾기
            enemy = EnemyDB.Instance.GetEnemyByName(gameObject.name.Substring(0, index));
        // print(gameObject.name.Split('_')[0]);
    }

    IEnumerator Init()
    {
        // 초기화 완료 취소
        initialFinish = false;

        // 히트박스 전부 끄기
        for (int i = 0; i < hitBoxList.Count; i++)
        {
            // 모든 히트박스에 캐릭터 전달
            hitBoxList[i].character = this;
            // 히트박스 끄기
            hitBoxList[i].enabled = false;
        }

        // rigid 초기화
        if (rigid != null)
            rigid.velocity = Vector3.zero;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => EnemyDB.Instance.loadDone);

        if (enemy == null)
            FindEnemyInfo();

        if (enemy != null)
        {
            // 물리 콜라이더 끄기
            if (physicsColl != null)
                physicsColl.enabled = false;

            //스케일 초기화
            transform.localScale = Vector3.one;

            // 초기화 스위치 켜질때까지 대기
            yield return new WaitUntil(() => initialStart);

            // 고스트 여부 초기화
            isGhost = changeGhost;

            // 다음 리스폰할때 고스트 예약 초기화
            changeGhost = false;

            // 몬스터 정보 인스턴싱, 몬스터 오브젝트마다 따로 EnemyInfo 갖기
            enemy = new EnemyInfo(enemy);

            // 스탯 초기화
            enemyName = enemy.name;
            enemyType = enemy.enemyType;
            hpMax = enemy.hpMax;
            powerNow = enemy.power;
            speedNow = enemy.speed;
            rangeNow = enemy.range;
            cooltimeNow = enemy.cooltime;
        }

        // 스프라이트 머터리얼 고정
        foreach (SpriteRenderer sprite in spriteList)
        {
            // 캐릭터 머터리얼로 바꾸기
            // if (sprite.material != SystemManager.Instance.characterMat)
            //     sprite.material = SystemManager.Instance.characterMat;
        }

        // 몬스터 정보 있을때
        if (enemy != null)
            //엘리트 종류마다 색깔 및 능력치 적용
            switch ((int)eliteClass)
            {
                case 0:
                    // 아웃라인 지우기
                    foreach (SpriteRenderer sprite in spriteList)
                        sprite.material.SetColor("_OutLineColor", Color.clear);

                    // 스케일 초기화
                    transform.localScale = Vector2.one;

                    break;

                case 1:
                    //공격력 상승
                    powerNow = powerNow * 2f;

                    // 빨강 아웃라인
                    foreach (SpriteRenderer sprite in spriteList)
                        sprite.material.SetColor("_OutLineColor", Color.red);

                    // 몬스터 스케일 상승
                    transform.localScale = Vector2.one * 1.5f;

                    break;

                case 2:
                    //속도 상승
                    speedNow = speedNow * 2.5f;
                    // 쿨타임 빠르게
                    cooltimeNow = cooltimeNow / 2f;

                    // 하늘색 아웃라인
                    foreach (SpriteRenderer sprite in spriteList)
                        sprite.material.SetColor("_OutLineColor", Color.cyan);
                    break;

                case 3:
                    // 최대 체력 상승
                    hpMax = hpMax * 2f;

                    //힐 오브젝트 소환
                    healRange = LeanPool.Spawn(WorldSpawner.Instance.healRange, transform.position, Quaternion.identity, transform).GetComponent<CircleCollider2D>();
                    // 힐 오브젝트 크기 초기화
                    healRange.transform.localScale = Vector2.one * portalSize * 0.5f;
                    // 회복량 넣어주기
                    healRange.GetComponent<Attack>().fixedPower = -powerNow;

                    // 초록 아웃라인 머터리얼
                    foreach (SpriteRenderer sprite in spriteList)
                        sprite.material.SetColor("_OutLineColor", Color.green);
                    break;

                case 4:
                    // 일정 범위 만큼 마법 차단하는 파란색 쉴드 생성
                    // 해당 범위내 몬스터들은 무적(맞으면 Miss) 스위치 켜기, 범위 나가면 무적 끄기
                    // 이 엘리트 몹을 잡으면 쉴드 사라짐
                    // 콜라이더 stay 함수로 구현, 무적쉴드 충돌이면 무적 설정, 없으면 무적 해제
                    //TODO 포스쉴드 프리팹 생성
                    break;
            }

        yield return new WaitUntil(() => WorldSpawner.Instance != null);
        // 히트 이펙트가 없으면 기본 이펙트 가져오기
        if (hitEffect == null)
            hitEffect = WorldSpawner.Instance.hitEffect;

        //ItemDB 로드 될때까지 대기
        yield return new WaitUntil(() => ItemDB.Instance.loadDone);

        //보유 아이템 초기화
        nowHasItem.Clear();
        foreach (int itemId in defaultHasItem)
        {
            // id 할당을 위해 변수 선언
            int id = itemId;
            ItemInfo item = null;

            // -1이면 랜덤 원소젬 뽑기
            if (id == -1)
            {
                // 원소젬 전부 찾기
                List<ItemInfo> gems = ItemDB.Instance.GetItemsByType(ItemDB.ItemType.Gem);

                // 가중치 확률로 원소젬 인덱스 뽑기
                int gemIndex = SystemManager.Instance.WeightRandom(SystemManager.Instance.elementWeitght.ToList());
                if (gemIndex == -1)
                    gemIndex = 0;

                // gem 인스턴스 생성
                item = new ItemInfo(gems[gemIndex]);
            }
            else
            {
                // item 인스턴스 생성 및 amount 초기화
                item = new ItemInfo(ItemDB.Instance.GetItemByID(id));
            }

            //item 정보 넣기
            item.amount = 1;
            nowHasItem.Add(item);
        }

        // 엘리트 몬스터일때
        if (eliteClass != EliteClass.None)
        {
            ItemInfo dropItem = null;

            // 각각 아이템 개별 확률 적용
            List<float> randomRate = new List<float>();
            randomRate.Add(40); // 원소젬 확률 가중치
            randomRate.Add(20); // 회복 아이템 확률 가중치
            randomRate.Add(20); // 자석 빔 확률 가중치
            randomRate.Add(10); // 슬롯머신 확률 가중치
            randomRate.Add(5); // 트럭 버튼 확률 가중치

            // 랜덤 아이템 뽑기 (몬스터 등급+0~2급 샤드, 체력회복템, 자석빔, 트럭 호출버튼)
            int randomItem = SystemManager.Instance.WeightRandom(randomRate);

            switch (randomItem)
            {
                // 샤드일때
                case 0:
                    // 랜덤 샤드 드랍 아이템에 등록
                    string itemName = "";

                    // 해당 몬스터 등급으로 뽑기 등급 산출
                    int grade = enemy.grade;
                    // 해당 몬스터 등급으로 랜덤 마법 뽑기
                    MagicInfo randomMagic = MagicDB.Instance.GetRandomMagic(grade);

                    // 뽑았는데 랜덤이면 하위 등급으로 다시 뽑기
                    while (randomMagic == null)
                    {
                        // 1등급 이하면 중단
                        if (grade <= 1)
                            break;
                        else
                            // 등급을 한단계 낮추기
                            grade--;

                        // 해당 등급으로 랜덤 마법 뽑기
                        randomMagic = MagicDB.Instance.GetRandomMagic(grade);

                        // if (randomMagic != null)
                        //     print(grade + " : " + randomMagic.name);
                    }

                    // 해당 등급의 마법을 뽑는데 성공했을때
                    if (randomMagic != null)
                    {
                        // 아이템 이름 짓기
                        itemName = "Magic Shard " + grade;

                        // 몬스터 등급에 해당하는 shard 찾기
                        dropItem = ItemDB.Instance.GetItemByName(itemName);
                    }
                    break;

                // 회복템일때
                case 1:
                    // 회복 아이템 찾기
                    dropItem = ItemDB.Instance.GetItemByName("Heart");
                    break;

                // 자석빔일때
                case 2:
                    dropItem = ItemDB.Instance.GetItemByName("Magnet");
                    break;

                // 슬롯머신일때
                case 3:
                    dropItem = ItemDB.Instance.GetItemByName("SlotMachine");
                    break;

                // 트럭 버튼일때
                case 4:
                    dropItem = ItemDB.Instance.GetItemByName("TruckButton");
                    break;
            }

            dropItem.amount = 1;
            // 드랍 아이템 정보 넣기
            nowHasItem.Add(dropItem);
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
                spriteList[i].material.SetColor("_Tint", new Color(0, 1, 1, 0.5f));

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

            for (int i = 0; i < spriteList.Count; i++)
            {
                // 고스트 여부에 따라 틴트 컬러 초기화
                spriteList[i].material.SetColor("_Tint", Color.clear);
            }

            // 그림자 색 초기화
            if (shadow)
                shadow.color = new Color(0, 0, 0, 0.5f);

            if (enemy != null)
            {
                // 물리 콜라이더 켜기
                if (physicsColl != null)
                    physicsColl.enabled = true;

                // 공격 트리거 레이어를 몬스터 공격으로 바꾸기
                if (enemyAtkTrigger)
                    enemyAtkTrigger.gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
                // 공격 레이어를 몬스터 공격으로 바꾸기
                for (int i = 0; i < enemyAtkList.Count; i++)
                {
                    enemyAtkList[i].gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
                }
            }
        }

        // 죽음 상태일때
        if (isDead)
        {
            //죽음 여부 초기화
            isDead = false;

            // idle 상태로 전환
            nowState = State.Idle;

            for (int i = 0; i < animList.Count; i++)
            {
                // 애니메이터 켜기
                animList[i].enabled = true;

                // 애니메이션 속도 초기화
                // 기본값 속도에 비례해서 현재 속도만큼 배율 넣기
                // animList[i].speed = 1f * speedNow / EnemyDB.Instance.GetEnemyByID(enemy.id).speed;
                if (speedNow != 0)
                    animList[i].speed = 1f * speedNow;
                else
                    animList[i].speed = 1f;
            }
        }

        if (enemy != null)
            //보스면 체력 UI 띄우기
            if (enemy.enemyType == EnemyDB.EnemyType.Boss.ToString())
            {
                StartCoroutine(UIManager.Instance.UpdateBossHp(this));
            }

        // 히트박스 전부 켜기
        for (int i = 0; i < hitBoxList.Count; i++)
        {
            hitBoxList[i].enabled = true;
        }

        // 초기화 완료되면 초기화 스위치 끄기
        initialStart = false;
        // 초기화 완료
        initialFinish = true;
    }

    private void Update()
    {
        // 몬스터 정보 없으면 리턴
        if (enemy == null)
        {
            // FindEnemyInfo();
            return;
        }

        // 초기화 안됬으면 리턴
        if (!initialFinish)
        {
            // 이동 멈추기
            if (rigid != null)
                rigid.velocity = Vector2.zero;

            return;
        }

        // 고스트일때, 타겟이 비활성화되거나 리셋타임이 되면 타겟 재설정
        if (IsGhost && (targetResetCount <= 0 || TargetObj == null))
            // 타겟 리셋하기
            TargetObj = SearchTarget();

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

                // 힐 쿨타임을 몬스터 쿨타임 2배로 갱신
                float healCooltime = cooltimeNow * 2f;

                Transform healEffect = healRange.transform.GetChild(0);
                // 이펙트 오브젝트 켜기
                healEffect.gameObject.SetActive(true);
                // 사이즈 제로로 초기화
                healEffect.localScale = Vector2.zero;
                // 힐 이펙트 사이즈 키우기
                healEffect.DOScale(Vector2.one, healCooltime)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    // 이펙트 오브젝트 끄기
                    healEffect.gameObject.SetActive(false);
                });

                // 힐 쿨타임 갱신
                healCount = healCooltime;
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
        {
            FindEnemyInfo();
            return false;
        }

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
            // 애니메이션 멈추기
            if (animList.Count > 0)
            {
                foreach (Animator anim in animList)
                {
                    anim.speed = 0f;
                }
            }

            // 이동 멈추기
            if (rigid != null)
                rigid.velocity = Vector2.zero;

            transform.DOPause();

            // 행동불능이므로 false 리턴
            return false;
        }

        // 멈춤 디버프일때
        if (stopCount > 0)
        {
            // transform.DOPause();
            if (rigid != null)
                rigid.velocity = Vector2.zero; //이동 초기화

            // rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            // 애니메이션 멈추기
            if (animList.Count > 0)
            {
                foreach (Animator anim in animList)
                {
                    anim.speed = 0f;
                }
            }

            // // 히트 딜레이중 아닐때
            // if (hitDelayCount <= 0)
            //     //시간 멈춤 머터리얼 및 색으로 바꾸기
            //     for (int i = 0; i < spriteList.Count; i++)
            //     {
            //         spriteList[i].material.SetColor("_Tint", SystemManager.Instance.hitColor);
            //     }

            // 행동불능이므로 false 리턴
            return false;
        }

        //스폰 콜라이더에 닿아 반대편으로 보내질때 잠시대기
        if (oppositeCount > 0)
        {
            if (rigid != null)
                rigid.velocity = Vector2.zero; //이동 초기화

            // 행동불능이므로 false 리턴
            return false;
        }

        // 피격 했을때
        if (hitDelayCount > 0)
            return false;

        // 감전 디버프일때
        if (DebuffList[(int)Debuff.Shock] != null
        // 스턴 디버프일때
        || DebuffList[(int)Debuff.Stun] != null
        )
            // 행동불능이므로 false 리턴
            return false;

        // 애니메이션 속도 초기화
        if (animList.Count > 0)
        {
            foreach (Animator anim in animList)
            {
                anim.speed = 1f * speedNow / EnemyDB.Instance.GetEnemyByID(enemy.id).speed;
            }
        }

        // 상태 이상 없음
        afterEffect = true;

        // 행동가능이므로 true 리턴
        return true;
    }

    GameObject SearchTarget()
    {
        // 가장 가까운 적과의 거리
        float closeRange = float.PositiveInfinity;
        GameObject closeEnemy = null;

        // 고스트 아닐때
        if (!IsGhost)
            if (PlayerManager.Instance != null)
                // 플레이어를 리턴
                return PlayerManager.Instance.gameObject;
            else
                // 메인 카메라를 리턴
                return Camera.main.gameObject;


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
            if (enemyCollList[i].TryGetComponent(out HitBox hitBox))
            {
                // 찾은 몬스터도 고스트일때
                if (hitBox.character.IsGhost)
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
        if (closeEnemy != null && closeEnemy.GetComponent<HitBox>().character.isGhost)
            return null;

        return closeEnemy;
    }

    // 갖고있는 아이템 드랍
    public void DropItem()
    {
        //보유한 모든 아이템 드랍
        foreach (ItemInfo item in nowHasItem)
        {
            //해당 아이템의 amount 만큼 드랍
            for (int i = 0; i < item.amount; i++)
            {
                //아이템 프리팹 찾기
                GameObject prefab = ItemDB.Instance.GetItemPrefab(item.id);

                if (prefab == null)
                    print(item.name + " : not found");

                //아이템 오브젝트 소환
                GameObject itemObj = LeanPool.Spawn(prefab, transform.position, Quaternion.identity, ObjectPool.Instance.itemPool);

                //아이템 정보 넣기
                if (itemObj.TryGetComponent(out ItemManager itemManager))
                    itemManager.itemInfo = item;

                //아이템 리지드 찾기
                Rigidbody2D itemRigid = itemObj.GetComponent<Rigidbody2D>();

                // 랜덤 방향으로 아이템 날리기
                itemRigid.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * Random.Range(10f, 20f);

                // 아이템 랜덤 회전 시키기
                itemRigid.angularVelocity = Random.value < 0.5f ? 180f : -180f;
            }
        }
    }

    void GlobalSoundPlay(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName);
    }

    void SoundPlay(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName, transform.position);
    }

    // void SoundStop(string soundName)
    // {
    //     SoundManager.Instance.StopSound(soundName, 0.5f);
    // }
}
