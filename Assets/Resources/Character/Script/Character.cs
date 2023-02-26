using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using TMPro;
using System.Linq;
using System.Text;

public enum CharacterState { Idle, Rest, Walk, Jump, Attack, Dead } // 캐릭터 상태 종류
public enum EliteClass { None, Power, Speed, Heal }; // 엘리트 몬스터 종류
public enum Debuff { Burn, Poison, Bleed, Slow, Shock, Stun, Stop, Flat, Freeze }; // 공격에 들어갈 디버프 종류

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
    public bool initialStart = true;
    public bool initialFinish = false;
    public EliteClass eliteClass = EliteClass.None; // 엘리트 여부    
    public bool lookLeft = false; //기본 스프라이트가 왼쪽을 바라보는지
    public CharacterStat characterStat = new CharacterStat(); // 해당 캐릭터 스탯

    [Header("State")]
    public List<Buff> buffList = new List<Buff>(); // 버프 리스트
    public bool isDead; //죽음 코루틴 진행중 여부
    public float deadDelay = 1f; // 죽는 트랜지션 진행 시간
    public bool changeGhost = false;
    public float portalSize = 1f; //포탈 사이즈 지정값
    public bool afterEffect = false; // 상태이상 여부
    public bool invinsible = false; //현재 무적 여부
    public float attackRange; // 공격범위
    public float healCount; // Heal 엘리트몹 아군 힐 카운트
    public float atkCoolCount = 0; // 공격 쿨타임 잔여시간

    public bool isGhost = false; // 마법으로 소환된 고스트 몬스터인지 여부
    public bool IsGhost
    {
        get
        {
            return isGhost;
        }
    }

    public CharacterState nowState = CharacterState.Idle; //현재 행동

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
    public float searchRange = 100f; // 타겟 추적 범위

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
    [SerializeField] GameObject debugText;
    [SerializeField] TextMeshProUGUI stateText;

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
        if (hitBoxList.Count == 0) hitBoxList = GetComponentsInChildren<HitBox>().ToList();
        // 공격 트리거 찾기
        if (enemyAtkTrigger == null) enemyAtkTrigger = GetComponentInChildren<EnemyAtkTrigger>();
        // 공격 오브젝트 모두 찾기
        if (enemyAtkList.Count == 0) enemyAtkList = GetComponentsInChildren<EnemyAttack>().ToList();

        // 스프라이트 설정 안했으면 디버그 메시지
        if (spriteList.Count == 0)
            Debug.Log("SpriteList is null");

        // 스프라이트에서 그림자는 빼기
        if (shadow != null)
            spriteList.Remove(shadow);

        // 버프 아이콘 부모 없으면 본인 오브젝트
        if (buffParent == null) buffParent = transform;

        // if (stateText == null) stateText = buffParent.GetComponentInChildren<TextMeshProUGUI>();
        // 디버그 텍스트 오브젝트 생성
        stateText = buffParent.GetComponentInChildren<TextMeshProUGUI>();
        if (stateText == null)
            stateText = LeanPool.Spawn(debugText, buffParent).GetComponentInChildren<TextMeshProUGUI>();

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
            hitBoxList[i].gameObject.SetActive(false);
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

            // // 초기화 스위치 켜질때까지 대기
            // yield return new WaitUntil(() => initialStart);

            // 고스트 여부 초기화
            isGhost = changeGhost;

            // 다음 리스폰할때 고스트 예약 초기화
            changeGhost = false;

            // 몬스터 정보 인스턴싱, 몬스터 오브젝트마다 따로 EnemyInfo 갖기
            enemy = new EnemyInfo(enemy);

            // 스탯 초기화
            enemyName = enemy.name;
            enemyType = enemy.enemyType;
            characterStat.hpMax = enemy.hpMax;
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
                    spriteList[0].material.SetColor("_OutLineColor", Color.clear);

                    // 스케일 초기화
                    transform.localScale = Vector2.one;

                    break;

                case 1:
                    //공격력 상승
                    powerNow = powerNow * 2f;

                    // 빨강 아웃라인
                    spriteList[0].material.SetColor("_OutLineColor", Color.red);

                    // 몬스터 스케일 상승
                    transform.localScale = Vector2.one * 1.5f;

                    break;

                case 2:
                    //속도 상승
                    speedNow = speedNow * 2.5f;
                    // 쿨타임 빠르게
                    cooltimeNow = cooltimeNow / 2f;

                    // 하늘색 아웃라인
                    spriteList[0].material.SetColor("_OutLineColor", Color.cyan);
                    break;

                case 3:
                    // 최대 체력 상승
                    characterStat.hpMax = characterStat.hpMax * 2f;

                    //힐 오브젝트 소환
                    healRange = LeanPool.Spawn(WorldSpawner.Instance.healRange, transform.position, Quaternion.identity, transform).GetComponent<CircleCollider2D>();
                    // 힐 오브젝트 크기 초기화
                    healRange.transform.localScale = Vector2.one * portalSize * 0.5f;
                    // 회복량 넣어주기
                    healRange.GetComponent<Attack>().power = -powerNow;

                    // 초록 아웃라인 머터리얼
                    spriteList[0].material.SetColor("_OutLineColor", Color.green);
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

        // 고스트 여부에 따라 타겟의 물리 레이어 산출
        int physicsLayer = IsGhost ? SystemManager.Instance.layerList.PlayerAttack_Layer : SystemManager.Instance.layerList.EnemyAttack_Layer;

        // 공격 트리거 레이어 바꾸기
        if (enemyAtkTrigger != null)
            enemyAtkTrigger.gameObject.layer = physicsLayer;
        // 공격 레이어 바꾸기
        for (int i = 0; i < enemyAtkList.Count; i++)
            enemyAtkList[i].gameObject.layer = physicsLayer;

        // 고스트일때
        if (IsGhost)
        {
            //체력 절반으로 초기화
            characterStat.hpNow = characterStat.hpMax / 2f;

            // rigid, sprite, 트윈, 애니메이션 상태 초기화
            for (int i = 0; i < spriteList.Count; i++)
            {
                // 고스트 색깔로 초기화
                spriteList[i].material.SetColor("_Tint", new Color(0, 1, 1, 0.5f));

                // 고스트색 아웃라인 넣기
                spriteList[i].material.SetColor("_OutLineColor", Color.cyan);
            }

            // 그림자 더 투명하게
            shadow.color = new Color(0, 0, 0, 0.25f);
        }
        else
        {
            // 맥스 체력으로 초기화
            characterStat.hpNow = characterStat.hpMax;

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
            nowState = CharacterState.Idle;

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
            hitBoxList[i].gameObject.SetActive(true);

        // 초기화 완료되면 초기화 스위치 끄기
        initialStart = false;
        // 초기화 완료
        initialFinish = true;
    }

    private void Update()
    {
        if (stateText != null)
        {
            if (SystemManager.Instance.showEnemyState)
            {
                // 상태 오브젝트 켜기
                stateText.transform.parent.gameObject.SetActive(true);
                // 각도 초기화
                stateText.transform.rotation = Quaternion.Euler(Vector3.zero);
                // 현재 상태 표시
                stateText.text = nowState.ToString();
            }
            else
                // 상태 오브젝트 끄기
                stateText.transform.parent.gameObject.SetActive(false);
        }

        // 공격중 아닐때 공격 쿨타임 차감
        if (nowState != CharacterState.Attack && atkCoolCount > 0)
            atkCoolCount -= Time.deltaTime;

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

        // 타겟 추적 카운트가 0이 되면 타겟 재설정
        if (targetResetCount <= 0)
        {
            // 타겟 재설정
            TargetObj = SearchTarget();
        }
        else
            // 타겟 재설정 카운트 차감
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

        // // 감전 디버프일때
        // if (DebuffList[(int)Debuff.Shock] != null
        // // 스턴 디버프일때
        // || DebuffList[(int)Debuff.Stun] != null
        // )
        //     // 행동불능이므로 false 리턴
        //     return false;

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

    public GameObject SearchTarget()
    {
        // 리턴할 타겟
        GameObject target = null;
        // 가장 가까운 적과의 거리
        float closeRange = float.PositiveInfinity;

        // 고스트 여부에 따라 타겟의 물리 레이어 산출
        int physicsLayer = IsGhost ? SystemManager.Instance.layerList.EnemyHit_Layer : SystemManager.Instance.layerList.PlayerHit_Layer;

        // 캐릭터 주변의 타겟 콜라이더 찾기
        List<Collider2D> targetCollList = Physics2D.OverlapCircleAll(transform.position, searchRange, 1 << physicsLayer).ToList();

        // 몬스터 본인 콜라이더는 전부 빼기
        for (int i = 0; i < hitBoxList.Count; i++)
            targetCollList.Remove(hitBoxList[i].GetComponent<Collider2D>());

        for (int i = 0; i < targetCollList.Count; i++)
        {
            // 히트박스 컴포넌트가 있을때
            if (targetCollList[i].TryGetComponent(out HitBox hitBox))
            {
                // 고스트 여부가 서로 같을때
                if (isGhost == hitBox.character.IsGhost)
                    // 다음으로 넘기기
                    continue;
            }
            else
                // 히트박스 컴포넌트가 없을때
                continue;

            // 해당 적이 이전 거리보다 짧으면
            if (Vector2.Distance(transform.position, targetCollList[i].transform.position) < closeRange)
                // 해당 적을 타겟으로 바꾸기
                target = targetCollList[i].gameObject;
        }

        // 아무것도 못찾으면
        if (target == null)
        {
            if (PlayerManager.Instance != null)
                // 플레이어를 리턴
                target = PlayerManager.Instance.gameObject;
            else
                // 메인 카메라를 리턴
                target = Camera.main.gameObject;
        }

        // 찾은 타겟 리턴
        return target;
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

    public float GetBuffedStat(string statName)
    {
        // 스탯이름으로 해당 스탯 값 불러오기
        var statField = characterStat.GetType().GetField(statName);
        var stat = statField.GetValue(characterStat);

        // 스탯이름 없으면 에러
        if (statField == null || stat == null)
        {
            Debug.Log(statName + " : 는 존재하지 않는 스탯");
            return -1;
        }

        // 명시적 변환하기
        float retrunStat = (float)stat;

        // 스탯에 해당하는 버프 모두 찾기
        List<Buff> _buffList = buffList.FindAll(x => x.statName == statField.Name);

        // 곱연산만 전부 연산
        for (int i = 0; i < _buffList.Count; i++)
            if (_buffList[i].isMultiple)
                retrunStat *= _buffList[i].amount;
        // 합연산만 전부 연산
        for (int i = 0; i < _buffList.Count; i++)
            if (!_buffList[i].isMultiple)
                retrunStat += _buffList[i].amount;

        return retrunStat;
    }

    public Buff SetBuff(string buffName, string statName, bool isMultiple, float amount, float duration,
                     bool dotHit, Transform _buffParent = null, GameObject buffEffect = null)
    {
        // 버프 아이콘
        Transform buffUI = null;

        // 스탯 이름이 있을때
        if (statName != "")
        {
            // 스탯이름으로 해당 스탯 값 불러오기
            var statField = characterStat.GetType().GetField(statName);
            var stat = statField.GetValue(characterStat);
            // 스탯이름 없으면 에러
            if (statField == null || stat == null)
            {
                Debug.Log(statName + " : 는 존재하지 않는 스탯");
                return null;
            }
        }

        // 버프 리스트에서 이름이 같은 버프 찾기
        Buff buff = buffList.Find(x => x.buffName == buffName);

        // 이미 같은 버프가 있으면
        if (buff != null)
        {
            // 잔여 버프시간 계산 (기존 duration에서 버프 시작시간과 현재 시간의 차이만큼 빼기)
            float remainTime = buff.duration - (Time.time - buff.startTime);
            // 더 큰 지속시간으로 교체
            buff.duration = Mathf.Max(remainTime, buff.duration);
        }
        // 없으면 버프 새로 생성
        else
        {
            // 버프 인스턴스 생성
            buff = new Buff();

            // 버프 이름 전달
            buff.buffName = buffName;
            // 스탯 이름 전달
            buff.statName = statName;
            // 버프 시작 시간 전달
            buff.startTime = Time.time;
            // 버프량 전달
            buff.amount = amount;
            // 연산 종류 전달
            buff.isMultiple = isMultiple;
            // 버프 코루틴 전달
            // buff.buffCoroutine = coroutine;
            // 버프 아이콘/이펙트
            buff.buffEffect = buffEffect;
            // 버프 아이콘/이펙트 부모
            buff.buffParent = _buffParent;
            // 버프 지속 시간 전달
            buff.duration = duration;

            // 이미 버프 중 아닐때
            if (buffEffect != null && !_buffParent.Find(buffEffect.name))
                // 아이콘/이펙트 붙이기
                buffUI = LeanPool.Spawn(buffEffect, _buffParent.position, Quaternion.identity, _buffParent).transform;

            //  몬스터 자체에 붙는 경우
            if (_buffParent == transform)
                // 몬스터 자체 사이즈와 맞추기
                buffUI.localScale = SystemManager.Instance.AntualSpriteScale(spriteList[0]) / 4f;
            else
                buffUI.localScale = Vector2.one;

            // 해당 버프 리스트에 넣기
            buffList.Add(buff);
        }

        // 도트 데미지일때
        if (dotHit)
        {
            // 이미 진행중인 도트뎀 코루틴 있으면 정지
            if (buff.buffCoroutine != null)
                StopCoroutine(buff.buffCoroutine);

            // 도트뎀 코루틴 실행
            buff.buffCoroutine = DotHit(buff, _buffParent, buffEffect);
            StartCoroutine(buff.buffCoroutine);
        }
        // 일반 버프일때
        else
            // 버프 없에고 아이콘,이펙트 제거
            StartCoroutine(StopBuff(buff, buff.duration, _buffParent, buffEffect));

        return buff;
    }

    public IEnumerator StopBuff(Buff buff, float duration, Transform buffParent = null, GameObject buffEffect = null)
    {
        // -1이면 자동 버프제거 중단
        if (duration == -1)
            yield break;

        Transform buffUI = null;

        // 수정된 duration 동안 대기
        yield return new WaitForSeconds(duration);

        // 이름으로 버프 찾기
        Buff findBuff = buffList.Find(x => x.buffName == buff.buffName);
        // 버프 찾았으면
        if (findBuff != null)
            // 해당 리스트에서 버프 없에기
            buffList.Remove(findBuff);

        // 아이콘/이펙트 찾기
        if (buffEffect != null && buffUI == null)
            buffUI = buffParent.Find(buffEffect.name);
        // 아이콘/이펙트 없에기
        if (buffUI != null)
            LeanPool.Despawn(buffUI);
    }

    public IEnumerator DotHit(Buff buff, Transform buffParent = null, GameObject buffEffect = null)
    {
        Transform buffUI = null;
        // 이미 버프 중 아닐때
        if (buffEffect != null && !buffParent.Find(buffEffect.name))
            // 아이콘/이펙트 붙이기
            buffUI = LeanPool.Spawn(buffEffect, buffParent.position, Quaternion.identity, buffParent).transform;

        // 도트 지속시간을 횟수로 환산
        int hitNum = (int)buff.duration;
        for (int i = 0; i < hitNum; i++)
            // 캐릭터 살아있을때
            if (!isDead)
            {
                // 도트 데미지 입히기
                hitBoxList[0].Damage(Mathf.Clamp(buff.amount, 1, float.PositiveInfinity), false);

                // 도트 딜레이 대기
                yield return new WaitForSeconds(1f);
            }

        // 버프 없에고 아이콘,이펙트 제거
        StartCoroutine(StopBuff(buff, buff.duration, buffParent, buffEffect));
    }
}

[System.Serializable]
public class CharacterStat
{
    public int powerSum; // 캐릭터 총 전투력
    public float hpMax = 100; // 최대 체력
    public float hpNow = 100; // 체력
    public float Level = 1; //레벨
    public float moveSpeed = 1; //이동속도
    public int atkNum = 0; // 공격 횟수
    public int pierce = 0; // 관통 횟수
    public float power = 1; //마법 공격력
    public float armor = 1; //방어력
    public float speed = 1; //마법 공격속도
    public float evade = 0f; // 회피율
    public float knockbackForce = 1; //넉백 파워
    public float coolTime = 1; //마법 쿨타임
    public float duration = 1; //마법 지속시간
    public float range = 1; //마법 범위
    public float scale = 1; //마법 사이즈
    public float luck = 1; //행운
    public float expGain = 1; //경험치 획득량
    public float getRage = 1; // 아이템 획득 범위

    //원소 공격력
    public float earth_atk = 1;
    public float fire_atk = 1;
    public float life_atk = 1;
    public float lightning_atk = 1;
    public float water_atk = 1;
    public float wind_atk = 1;

    public CharacterStat() { }
    public CharacterStat(CharacterStat characterStat)
    {
        this.powerSum = characterStat.powerSum; // 캐릭터 총 전투력
        this.hpMax = characterStat.hpMax; // 최대 체력
        this.hpNow = characterStat.hpNow; // 체력
        this.Level = characterStat.Level; //레벨
        this.moveSpeed = characterStat.moveSpeed; //이동속도
        this.atkNum = characterStat.atkNum; // 공격 횟수
        this.pierce = characterStat.pierce; // 관통 횟수
        this.power = characterStat.power; //마법 공격력
        this.armor = characterStat.armor; //방어력
        this.speed = characterStat.speed; //마법 공격속도
        this.evade = characterStat.evade; // 회피율
        this.knockbackForce = characterStat.knockbackForce; //넉백 파워
        this.coolTime = characterStat.coolTime; //마법 쿨타임
        this.duration = characterStat.duration; //마법 지속시간
        this.range = characterStat.range; //마법 범위
        this.luck = characterStat.luck; //행운
        this.expGain = characterStat.expGain; //경험치 획득량
        this.getRage = characterStat.getRage; // 아이템 획득 범위

        // 원소 공격력
        this.earth_atk = characterStat.earth_atk;
        this.fire_atk = characterStat.fire_atk;
        this.life_atk = characterStat.life_atk;
        this.lightning_atk = characterStat.lightning_atk;
        this.water_atk = characterStat.water_atk;
        this.wind_atk = characterStat.wind_atk;
    }

    // SetStat 없이 원본 스탯 변수 값 유지
    // GetStat 에서 buffList 참조해서 스탯 리턴

}

[System.Serializable]
public class Buff
{
    [Header("Refer")]
    public IEnumerator buffCoroutine; // 해당 버프 진행중인 코루틴
    public GameObject buffEffect; // 해당 버프 아이콘/이펙트 오브젝트
    public Transform buffParent; // 아이콘이 들어갈 부모 오브젝트
    [Header("State")]
    public string buffName; // 버프 이름
    public string statName; // 버프 스탯 종류
    public float startTime; // 해당 버프가 시작된 시간 (잔여 시간 측정에 사용)
    public float duration; // 해당 버프 지속 시간
    public float amount; // 버프량
    public bool isMultiple; // true 일때 곱연산, false 일때 합연산
}