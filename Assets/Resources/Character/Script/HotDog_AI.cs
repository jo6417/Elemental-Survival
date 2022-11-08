using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class HotDog_AI : EnemyAI
{
    [Header("State")]
    [SerializeField]
    Patten patten = Patten.None;
    enum Patten { None, Hellfire, Meteor, Stealth };
    bool initDone = false;
    AnimState animState;
    enum AnimState { isWalk, isRun, isBark, Jump, Bite, ChargeBall, Eat, Launch, Change, BackStep };
    public EnemyAtkTrigger biteTrigger;

    [Header("Phase")]
    [SerializeField] Transform pushRange; // 페이즈 상승시 플레이어 밀어내는 범위
    [SerializeField] ParticleManager shield; // 쉴드 이펙트
    int nowPhase = 0;
    int nextPhase = 1;
    float damageMultiple = 1; // 페이즈에 따른 데미지 배율
    float speedMultiple = 1; // 페이즈에 따른 속도 배율
    float projectileMultiple = 1; // 페이즈에 따른 투사체 배율

    [Header("Effect Control")]
    public ParticleManager chargeEffect;
    public ParticleManager tailEffect; //꼬리 화염 파티클
    public SpriteRenderer eyeGlow; // 빛나는 눈 오브젝트
    public List<SpriteRenderer> glowObj = new List<SpriteRenderer>(); // 빛나는 오브젝트들
    public Material hdrMat; //몸에서 빛나는 부분들 머터리얼
    public Color[] phaseHDRColor = {
        new Color(1f, 1f, 1f, 0) * 10f,
        new Color(191f, 1f, 1f, 0) * 10f,
        new Color(191f, 20f, 0, 0) * 10f,
        new Color(0, 8f, 191f, 0) * 10f
        };
    public Color[] phaseColor = {
        new Color(1f, 1f, 1f, 1f),
        new Color(1f, 0, 0, 1f),
        new Color(1f, 1f, 0, 1f),
        new Color(0, 1f, 1f, 1f)
        };

    [Header("Move")]
    public float farDistance = 15f;
    public float closeDistance = 5f;
    public ParticleManager breathEffect; //숨쉴때 입에서 나오는 불꽃
    public ParticleSystem handDust;
    public ParticleSystem footDust;
    // public Vector2 moveToPos; // 목표 위치
    // public float moveResetCount; // 목표위치 갱신 시간 카운트

    [Header("Stealth Atk")]
    public GameObject eyeTrailPrefab; // 눈에서 나오는 붉은 트레일
    public ParticleSystem groundSmoke; // 스텔스 할때 바닥에서 피어오르는 연기 이펙트
    public ParticleSystem eyeFlash; //눈이 번쩍하는 이펙트
    public ParticleSystem smokeEffect; // 안개 생성시 입에서 나오는 연기
    public EnemyAttack dashAtk;
    public SpriteRenderer shadowSprite; //그림자 스프라이트
    MagicInfo flameMagic; // 플레임 마법 정보
    GameObject flamePrefab; // 플레임 마법 프리팹
    [SerializeField, ReadOnly] Vector2 flameLastPos;
    [SerializeField] float flameInverval = 1f;

    [Header("HellFire")]
    [SerializeField] Transform jawUp; // 윗턱 오브젝트
    public ParticleSystem mouthSparkEffect; // 에너지볼 먹을때 입에서 스파크 튀는 이펙트
    public ParticleSystem leakFireEffect; //차지 후 불꽃 새는 이펙트
    int jumpCount = -1;
    MagicInfo hellFireMagic;
    GameObject hellFirePrefab;

    [Header("Meteor")]
    public TextMeshProUGUI stateText; //! 테스트 현재 상태
    public float meteorRange;
    public int meteorNum;
    MagicInfo meteorMagic;
    GameObject meteorPrefab;

    [Header("Cooltime")]
    public float coolCount;
    public float biteCooltime = 1f;
    public float stealthCooltime = 3f;
    public float meteorCooltime = 5f;
    public float hellfireCooltime = 8f;

    [Header("Sound")]
    [SerializeField] string[] breathSounds = { "HotDog_Breath1", "HotDog_Breath2", "HotDog_Breath3" };
    int breath_lastIndex = -1;
    [SerializeField] string[] walkSounds = { };
    int walk_lastIndex = -1;
    [SerializeField] string[] runSounds = { };
    int run_lastIndex = -1;
    AudioSource laserSound;

    private void Awake()
    {
        character = character == null ? GetComponentInChildren<Character>() : character;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 초기화 안됨
        initDone = false;

        // 호흡 이펙트 끄기
        breathEffect.gameObject.SetActive(false);
        // 스모크 이펙트 끄기
        smokeEffect.gameObject.SetActive(false);
        // 바닥 연기 이펙트 끄기
        groundSmoke.gameObject.SetActive(false);
        // 눈 번쩍하는 이펙트 끄기
        eyeFlash.gameObject.SetActive(false);

        // 페이즈0 색으로 흰색 HDR 넣기
        hdrMat.color = phaseHDRColor[0];

        // 페이즈 변화시 밀어내기 이펙트 초기화
        pushRange.gameObject.SetActive(false);
        pushRange.GetComponentInChildren<Collider2D>().enabled = false; // 밀어내는 콜라이더 끄기
        pushRange.transform.GetChild(0).localScale = Vector2.zero; // 채우기 오브젝트 크기 초기화

        // 발 먼지 이펙트 끄기
        handDust.Stop();
        footDust.Stop();

        //대쉬 어택 콜라이더 끄기
        dashAtk.enabled = false;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => character.enemy != null);

        character.rigid.velocity = Vector2.zero; //속도 초기화

        //애니메이션 초기화
        character.animList[0].SetBool(AnimState.isWalk.ToString(), false);
        character.animList[0].SetBool(AnimState.isRun.ToString(), false);
        character.animList[0].SetBool(AnimState.isBark.ToString(), false);

        // 상태값 Idle로 초기화
        character.nowAction = Character.Action.Idle;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 플레임 마법 데이터 찾기
        if (flameMagic == null)
        {
            // 찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            flameMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("Flame"));

            // 데미지 고정
            flameMagic.power = 3f;

            // 플레임 프리팹 찾기
            flamePrefab = MagicDB.Instance.GetMagicPrefab(flameMagic.id);
        }

        // 헬파이어 마법 데이터 찾기
        if (hellFireMagic == null)
        {
            // 찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            hellFireMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("HellFire"));

            // 강력한 데미지로 고정
            hellFireMagic.power = 10f;

            // 헬파이어 프리팹 찾기
            hellFirePrefab = MagicDB.Instance.GetMagicPrefab(hellFireMagic.id);
        }

        // 메테오 마법 데이터 찾기
        if (meteorMagic == null)
        {
            // 찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            meteorMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("Meteor"));

            // 강력한 데미지로 고정
            meteorMagic.power = 20f;

            // 메테오 떨어지는 속도 초기화
            meteorMagic.speed = 1f;

            // 메테오 프리팹 찾기
            meteorPrefab = MagicDB.Instance.GetMagicPrefab(meteorMagic.id);
        }

        // 맞을때마다 Hit 함수 실행
        character.hitCallback += Hit;

        // 초기화 완료
        initDone = true;

        nowPhase = 0;
        nextPhase = 1;
        // 0번 페이즈 트랜지션
        StartCoroutine(PhaseChange());
    }

    IEnumerator PhaseChange()
    {
        // Idle 상태가 될때까지 대기
        yield return new WaitUntil(() => character.nowAction == Character.Action.Idle);

        // 현재 페이즈 컬러
        Color _nowColor = phaseColor[nowPhase];
        // 다음 페이즈 컬러
        Color _nextColor = phaseColor[nextPhase];

        // 무적 상태로 전환
        SwitchInvinsible(true);

        // 푸쉬 범위 나타내기
        pushRange.gameObject.SetActive(true);
        Collider2D pushColl = pushRange.GetComponentInChildren<Collider2D>();
        Transform pushRangeFill = pushRange.transform.GetChild(0); // 범위 채우는 오브젝트 찾기
        SpriteRenderer pushSprite = pushRange.GetComponent<SpriteRenderer>();
        SpriteRenderer pushFillSprite = pushRangeFill.GetComponent<SpriteRenderer>();

        // 밀어내는 콜라이더 끄기
        pushColl.enabled = false;

        pushRange.transform.localScale = Vector2.zero; // 범위 크기 제로로 초기화
        pushRangeFill.localScale = Vector2.zero; // 채우기 범위 크기 제로로 초기화

        pushSprite.color = _nowColor; // 범위 컬러를 현재 페이즈 컬러로
        pushFillSprite.color = _nextColor; // 채우기 컬러를 다음 페이즈 컬러로

        // 푸쉬 범위 스케일 키우기
        pushRange.transform.DOScale(Vector2.one * 5.5f, 1f)
        .SetEase(Ease.Linear);

        // 짖기 애니메이션 재생
        character.animList[0].SetBool(AnimState.isBark.ToString(), true);

        // 짖으며 범위 표시되는 시간 대기
        yield return new WaitForSeconds(1f);

        // 범위 오브젝트 투명해지며 끄기
        pushSprite.DOColor(Color.clear, 2f);

        // 밀어내기 콜라이더 켜기
        pushColl.enabled = true;
        // 채우기 범위 스케일 키우기
        pushRangeFill.DOScale(Vector2.one, 1f)
        .SetEase(Ease.Linear);

        // 범위 채우는 시간 대기
        yield return new WaitForSeconds(1.5f);

        // 하울링 사운드 재생
        SoundManager.Instance.PlaySound("HotDog_Howling", transform.position);

        // 범위 오브젝트 투명해지며 끄기
        pushFillSprite.DOColor(Color.clear, 0.5f)
        .OnComplete(() =>
        {
            // 범위 오브젝트 끄기
            pushRange.gameObject.SetActive(false);
            // 채우기 콜라이더 크기 초기화
            pushRangeFill.localScale = Vector2.zero;
        });

        Color changeColor = default;
        // 페이즈별로 머터리얼의 색 바꾸기
        switch (nextPhase)
        {
            case 1:
                // 빨간 HDR 넣기
                changeColor = phaseHDRColor[1];

                // 데미지 배율, 이속 배율, 투사체 개수 배율 적용
                damageMultiple = 1f;
                speedMultiple = 1f;
                projectileMultiple = 1f;

                break;
            case 2:
                // 노란 HDR 넣기
                changeColor = phaseHDRColor[2];

                // 데미지 배율, 이속 배율, 투사체 개수 배율 적용
                damageMultiple = 1.2f;
                speedMultiple = 1.2f;
                projectileMultiple = 1.2f;

                break;
            case 3:
                // 파란 HDR 넣기
                changeColor = phaseHDRColor[3];

                // 데미지 배율, 이속 배율, 투사체 개수 배율 적용
                damageMultiple = 1.5f;
                speedMultiple = 1.5f;
                projectileMultiple = 1.5f;

                break;
        }

        // 몬스터의 기본 정보값
        EnemyInfo originEnemy = EnemyDB.Instance.GetEnemyByID(character.enemy.id);

        // 데미지 갱신
        character.enemy.power = originEnemy.power * damageMultiple;

        // HDR 색으로 변화
        hdrMat.DOColor(changeColor, 0.5f)
        .OnUpdate(() =>
        {
            // OriginMat 에서 HDR 머터리얼 컬러 전부 교체해주기
            for (int i = 0; i < character.originMatList.Count; i++)
            {
                // originMatList에서 glowMat과 이름 같은 머터리얼 찾으면
                if (character.originMatList[i].name.Contains(hdrMat.name))
                {
                    character.originMatList[i] = hdrMat;
                }
            }

            // OriginMat 에서 HDR 머터리얼 컬러 전부 교체해주기
            for (int i = 0; i < character.originMatColorList.Count; i++)
            {
                character.originMatColorList[i] = hdrMat.color;
            }
        });

        // 쉴드 하위 파티클 모두 색 변경
        List<ParticleSystem> shieldParticles = shield.GetComponentsInChildren<ParticleSystem>().ToList();
        for (int i = 0; i < shieldParticles.Count; i++)
        {
            ParticleSystem.MainModule particleMain = shieldParticles[i].main;
            particleMain.startColor = phaseColor[nextPhase] / 2f;
        }
        shield.particle.Stop();
        shield.particle.Play();

        // 한번에 소환할 개수
        int summonNum = 20;
        // 소환 최소 범위
        float maxDistance = 18f;
        // 각 Flame 위치 저장할 배열
        Vector3[] lastPos = new Vector3[summonNum];

        // 범위만큼 주변에 Flame 원형으로 점점 크게 여러번 생성
        for (int j = 0; j < 5; j++)
        {
            // Flame 원형으로 생성
            for (int i = 0; i < 20; i++)
            {
                // 소환할 각도를 벡터로 바꾸기
                float angle = 360f * i / summonNum;
                Vector3 summonDir = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), Mathf.Cos(Mathf.Deg2Rad * angle), 0);
                // print(angle + " : " + summonDir);

                // 소환 반경 계산
                float radius = maxDistance / 5f * (j + 1);
                radius = Mathf.Clamp(radius, 1f, radius);

                // Flame 소환 위치, 현재 보스 위치에서 summonDir 각도로 범위만큼 곱하기 
                Vector3 targetPos = transform.position + summonDir * radius;

                // 각 Flame 소환 위치 저장
                lastPos[i] = targetPos;

                // Flame 생성
                GameObject magicObj = LeanPool.Spawn(flamePrefab, lastPos[i], Quaternion.identity, SystemManager.Instance.magicPool);

                // Flame 색깔 및 머터리얼 바꾸기
                Flame flame = magicObj.GetComponent<Flame>();
                flame.fireColor = new Color(1, 1, 1, 20f / 255f);
                flame.fireMaterial = hdrMat;

                // 매직홀더 찾기
                MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
                // magic 데이터 넣기
                magicHolder.magic = flameMagic;

                // 타겟을 플레이어로 전환
                magicHolder.SetTarget(MagicHolder.TargetType.Player);

                // Flame 목표지점 넣기
                magicHolder.targetPos = lastPos[i];
            }

            // 다음 사이즈 전개까지 대기
            yield return new WaitForSeconds(0.2f);
        }

        // 온몸에서 불꽃 뿜기 이펙트 재생
        LeakFire();

        // Flame 마법 전개, 색 변화 시간 대기
        yield return new WaitForSeconds(1f);

        // 짖기 애니메이션 끄기
        character.animList[0].SetBool(AnimState.isBark.ToString(), false);

        print(nowPhase + " -> " + nextPhase);

        // 무적 상태 해제
        SwitchInvinsible(false);

        // 쉴드 해제 시간 대기
        yield return new WaitForSeconds(2f);

        // 현재 페이즈 숫자 올리기        
        nowPhase = nextPhase;
    }

    void SwitchInvinsible(bool state)
    {
        // 무적 상태 갱신
        character.invinsible = state;

        // 무적임을 나타내기 위한 쉴드 토글
        if (state)
        {
            // 쉴드 하위 파티클 모두 색 변경
            List<ParticleSystem> shieldParticles = shield.GetComponentsInChildren<ParticleSystem>().ToList();
            for (int i = 0; i < shieldParticles.Count; i++)
            {
                ParticleSystem.MainModule particleMain = shieldParticles[i].main;
                particleMain.startColor = phaseColor[nowPhase] / 2f;
            }

            shield.particle.Play();
        }
        else
            shield.particle.Stop();
    }

    void Update()
    {
        // 이동 리셋 카운트 차감
        if (searchCoolCount > 0)
            searchCoolCount -= Time.deltaTime;

        if (character.enemy == null)
            return;

        // Idle 아니면 리턴
        if (character.nowAction != Character.Action.Idle)
            return;

        // 상태 이상 있으면 리턴
        if (!character.ManageState())
            return;

        // AI 초기화 완료 안됬으면 리턴
        if (!initDone)
            return;

        //행동 관리
        ManageAction();
    }

    void ManageAction()
    {
        // Idle 아니면 리턴
        // if (chracter.nowAction != Chracter.Action.Idle)
        //     return;

        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // 페이즈 올리는중이면 리턴
        if (nextPhase > nowPhase)
        {
            // 상태값 Idle로 초기화
            character.nowAction = Character.Action.Idle;

            // 속도 초기화
            character.rigid.velocity = Vector3.zero;

            // Idle 애니메이션 진행
            character.animList[0].SetBool(AnimState.isWalk.ToString(), false);
            character.animList[0].SetBool(AnimState.isRun.ToString(), false);

            return;
        }

        // 플레이어 방향
        Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 플레이어와의 거리
        float playerDistance = playerDir.magnitude;

        // 물기 콜라이더에 플레이어 닿으면 bite 패턴
        if (biteTrigger.atkTrigger)
        {
            //! 거리 확인용
            stateText.text = "Bite : " + playerDistance;

            // 속도 초기화
            character.rigid.velocity = Vector3.zero;

            // 현재 액션 변경
            character.nowAction = Character.Action.Attack;

            // 이동 애니메이션 종료
            character.animList[0].SetBool(AnimState.isWalk.ToString(), false);
            character.animList[0].SetBool(AnimState.isRun.ToString(), false);

            // 호흡 이펙트 끄기
            breathEffect.SmoothDisable();

            // 물기 패턴 실행
            Bite();

            // 쿨타임 갱신
            coolCount = biteCooltime;

            return;
        }

        // 공격 범위내에 있고 공격 쿨타임 됬을때
        if (playerDistance <= closeDistance && coolCount <= 0)
        {
            //! 거리 확인용
            stateText.text = "Attack : " + playerDistance;

            // 속도 초기화
            character.rigid.velocity = Vector3.zero;

            // 현재 액션 변경
            character.nowAction = Character.Action.Attack;

            //공격 패턴 결정하기
            StartCoroutine(ChooseAttack());
        }
        else
        {
            //! 거리 확인용
            stateText.text = "Move : " + playerDistance;

            // 공격 범위 내 위치로 이동
            Move();
        }
    }

    void Move()
    {
        character.nowAction = Character.Action.Walk;

        // 움직이는 동안 공격 쿨타임 차감
        if (coolCount >= 0)
            coolCount -= Time.deltaTime;

        // 호흡 이펙트 꺼져있으면
        if (!breathEffect.gameObject.activeSelf)
            // 호흡 사운드 재생
            SoundManager.Instance.PlaySoundPool(breathSounds.ToList(), mouthSparkEffect.transform.position, breath_lastIndex);

        // 호흡 이펙트 켜기
        breathEffect.gameObject.SetActive(true);

        //애니메이터 켜기
        character.animList[0].enabled = true;

        // 플레이어까지 방향 벡터
        Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;
        // 플레이어까지 거리
        float playerDistance = playerDir.magnitude;

        // 속도 초기화
        float runSpeed = 1f;

        // 플레이어 가까이 있을때
        if (playerDistance <= farDistance)
        {
            // Walk 애니메이션으로 전환
            character.animList[0].SetBool(AnimState.isRun.ToString(), false);
            character.animList[0].SetBool(AnimState.isWalk.ToString(), true);
        }
        // 플레이어가 멀리 있을때
        else
        {
            // Run 애니메이션으로 전환
            character.animList[0].SetBool(AnimState.isWalk.ToString(), false);
            character.animList[0].SetBool(AnimState.isRun.ToString(), true);

            // 속도 빠르게
            runSpeed = 2f;
        }

        // // 목표 위치 갱신 시간 됬을때
        // if (moveResetCount < Time.time)
        // {
        //     moveResetCount = Time.time + 3f;

        //     // 방향에 거리 곱해서 목표 위치 벡터 갱신
        //     moveToPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * Random.Range(closeDistance, farDistance);

        //     // print(moveToPos);
        // }

        // 이동할 방향 캐싱
        character.targetDir = character.targetPos - transform.position;
        // 목표 위치까지 거리
        float moveDistance = character.targetDir.magnitude;

        // 목표 위치에 근접했을때
        if (moveDistance < 1f)
        {
            // 이동 멈추기
            character.rigid.velocity = Vector3.zero;

            // Idle 애니메이션 진행
            character.animList[0].SetBool(AnimState.isWalk.ToString(), false);
            character.animList[0].SetBool(AnimState.isRun.ToString(), false);
        }
        else
        {
            // 이동 방향 쪽으로 쳐다보기
            if (character.targetDir.x > 0)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            //해당 방향으로 가속
            character.rigid.velocity =
            character.targetDir.normalized // 이동 방향
            * character.speedNow //몬스터 정보 속도
            * SystemManager.Instance.globalTimeScale //시간 비율 계산
            * runSpeed //달리기 속도 배율
            * speedMultiple; // 페이즈별로 속도 배율

            // print(chracter.rigid.velocity);
        }

        // 상태값 Idle로 초기화
        character.nowAction = Character.Action.Idle;
    }

    public void RunSound()
    {
        // 달리기 발소리 재생
        SoundManager.Instance.PlaySoundPool(runSounds.ToList(), transform.position, run_lastIndex);
    }

    public void WalkSound()
    {
        // 걷기 발소리 재생
        SoundManager.Instance.PlaySoundPool(walkSounds.ToList(), transform.position, walk_lastIndex);
    }

    IEnumerator ChooseAttack()
    {
        // 현재 액션 변경
        character.nowAction = Character.Action.Attack;

        // 이동 애니메이션 종료
        character.animList[0].SetBool(AnimState.isWalk.ToString(), false);
        character.animList[0].SetBool(AnimState.isRun.ToString(), false);

        // 백스텝 애니메이션 실행 후 대기
        character.animList[0].SetTrigger(AnimState.BackStep.ToString());

        // 플레이어까지 방향 벡터
        Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 플레이어 방향 쳐다보기
        if (playerDir.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // 백스텝 애니메이션 대기
        yield return new WaitForSeconds(1f);

        // 발 먼지 이펙트 끄기
        handDust.Stop();
        footDust.Stop();

        // 호흡 이펙트 끄기
        breathEffect.SmoothDisable();

        // 랜덤 패턴 결정
        int randomNum = Random.Range(0, 3);
        print("randomNum : " + randomNum);

        //! 테스트를 위해 패턴 고정
        if (patten != Patten.None)
            randomNum = (int)patten;

        switch (randomNum)
        {
            case (int)Patten.Hellfire:
                // 근거리 및 중거리 헬파이어 패턴
                StartCoroutine(HellfireAtk());
                coolCount = hellfireCooltime;
                break;
            case (int)Patten.Meteor:
                // 원거리 메테오 쿨타임 아닐때 meteor 패턴 코루틴
                MeteorAtk();
                coolCount = meteorCooltime;
                break;
            case (int)Patten.Stealth:
                // 원거리 스텔스 쿨타임 아닐때 stealthAtk 패턴 코루틴
                StartCoroutine(StealthAtk());
                coolCount = stealthCooltime;
                break;
        }
    }

    IEnumerator SetIdle(float endDelay)
    {
        // 호흡 이펙트 켜기
        breathEffect.gameObject.SetActive(true);

        yield return new WaitForSeconds(endDelay);

        // 상태값 Idle로 초기화
        character.nowAction = Character.Action.Idle;

        // 애니메이션 속도 초기화
        character.animList[0].speed = 1f;
    }

    void BackStepMove()
    {
        // 백스텝으로 이동할 위치
        float dirX = transform.rotation.eulerAngles.y == 180f ? -10f : 10f;
        float dirY = Random.Range(-1, 1) * Random.Range(-10f, 10f);
        Vector2 backStepPos = (Vector2)transform.position + new Vector2(dirX, dirY);

        // 마지막 플레이어 위치 반대 방향으로 이동
        character.rigid.DOMove(backStepPos, 0.5f);
    }

    void StartChargeEffect()
    {
        chargeEffect.gameObject.SetActive(true);
    }

    #region BiteAtk
    void Bite()
    {
        // 걷기 애니메이션 끄기
        character.animList[0].SetBool(AnimState.isWalk.ToString(), false);

        // 물기 애니메이션 재생
        character.animList[0].SetTrigger(AnimState.Bite.ToString());

        // 백스텝 애니메이션 실행 예약
        character.animList[0].SetTrigger(AnimState.BackStep.ToString());

        // 페이즈별로 물기 모션 속도 적용
        character.animList[0].speed = speedMultiple;
    }

    void BiteEnd()
    {
        // 애니메이터 속도 초기화
        character.animList[0].speed = 1f;

        // 행동 초기화
        StartCoroutine(SetIdle(2f));
    }
    #endregion

    #region HellFire

    IEnumerator HellfireAtk()
    {
        // 차지 애니메이션 재생
        character.animList[0].SetTrigger(AnimState.ChargeBall.ToString());
        // 차지 끝나면 에너지볼 먹는 애니메이션 재생
        character.animList[0].SetTrigger(AnimState.Eat.ToString());

        // 3연속 점프
        for (int i = 0; i < 3; i++)
        {
            jumpCount = i;

            // 점프 애니메이션 재생
            character.animList[0].SetTrigger(AnimState.Jump.ToString());

            // 점프 애니메이션 끝날때까지 대기
            yield return new WaitUntil(() => jumpCount == -1);

            // 점프 후딜레이 대기
            yield return new WaitForSeconds(0.5f);
        }

        // Idle 액션으로 전환
        StartCoroutine(SetIdle(1f));
    }

    void LeakFire()
    {
        // 관절에서 불꽃 새는 이펙트 켜기
        leakFireEffect.gameObject.SetActive(true);

        // 불꽃 호흡 이펙트 켜기
        breathEffect.gameObject.SetActive(true);

        // 불꽃 새는 사운드 재생
        SoundManager.Instance.PlaySound("HotDog_Leak", transform.position);
    }

    void FinishEat()
    {
        // 입에 스파크 이펙트 소환
        LeanPool.Spawn(mouthSparkEffect, breathEffect.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
    }

    void SummonHellfire()
    {
        switch (jumpCount)
        {
            case 0:
                // 직선 연속 헬파이어 전개
                StartCoroutine(CastHellfire(jumpCount, 6, 10));
                break;
            case 1:
                // 원형 연속 헬파이어 전개
                StartCoroutine(CastHellfire(jumpCount, 10, 10));
                break;
            case 2:
                // 직선 및 유도 헬파이어 전개
                StartCoroutine(CastHellfire(jumpCount, 1, 50));
                break;
        }
    }

    IEnumerator CastHellfire(int _jumpCount, int summonNum, int loopNum)
    {
        // 페이즈 배율 적용
        summonNum = (int)(summonNum * projectileMultiple);
        loopNum = (int)(loopNum * projectileMultiple);

        // 각 헬파이어 위치 저장할 배열
        Vector3[] lastPos = new Vector3[summonNum];

        // 루프 개수만큼 반복
        for (int j = 0; j < loopNum; j++)
        {
            // 헬파이어 개수만큼 반복
            for (int i = 0; i < summonNum; i++)
            {
                // 3번째 공격일때
                if (_jumpCount == 2)
                {
                    // 헬파이어 소환 위치, 플레이어 주변 범위내 랜덤
                    Vector3 targetPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * Random.Range(1f, 30f);

                    // 각 헬파이어의 소환 위치 저장
                    lastPos[i] = targetPos;
                }
                else
                {
                    // 소환할 각도를 벡터로 바꾸기
                    float angle = 360f * i / summonNum;
                    Vector3 summonDir = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), Mathf.Cos(Mathf.Deg2Rad * angle), 0);
                    // print(angle + " : " + summonDir);

                    // 헬파이어 소환 위치, 현재 보스 위치에서 summonDir 각도로 범위만큼 곱하기 
                    Vector3 targetPos = transform.position + summonDir * (5f + 3f * j);

                    // 각 헬파이어의 소환 위치 저장
                    lastPos[i] = targetPos;
                }

                // 헬파이어 생성
                GameObject magicObj = LeanPool.Spawn(hellFirePrefab, transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

                // 매직홀더 찾기
                MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
                // magic 데이터 넣기
                magicHolder.magic = hellFireMagic;

                // 타겟을 플레이어로 전환
                magicHolder.SetTarget(MagicHolder.TargetType.Player);

                // 헬파이어 목표지점 넣기
                magicHolder.targetPos = lastPos[i];

                // 타겟 오브젝트 넣기
                magicHolder.targetObj = PlayerManager.Instance.gameObject;
            }

            // 다음 사이즈 전개까지 대기
            yield return new WaitForSeconds(0.1f);
        }

        // 점프 카운트 초기화, 다음 점프 진행
        jumpCount = -1;
    }

    #endregion

    #region Meteor
    void MeteorAtk()
    {
        // 차지 애니메이션 재생
        character.animList[0].SetTrigger(AnimState.ChargeBall.ToString());
        // 에너지볼 발사 애니메이션 재생
        character.animList[0].SetTrigger(AnimState.Launch.ToString());
    }

    public void LaserSound(int playToggle)
    {
        if (playToggle == 0)
            // 레이저 사운드 반복 재생
            laserSound = SoundManager.Instance.PlaySound("HotDog_Ball_Laser", mouthSparkEffect.transform.position, 0.5f, 0, -1, true);
        else
            // 레이저 사운드 끄기
            SoundManager.Instance.StopSound(laserSound, 0.5f);
    }

    // meteor 애니메이션 끝날때쯤 meteor 소환 함수
    public void CastMeteor()
    {
        StartCoroutine(SummonMeteor());
    }

    IEnumerator SummonMeteor()
    {
        //메테오 개수만큼 반복
        for (int i = 0; i < meteorNum * projectileMultiple; i++)
        {
            // 메테오 떨어질 위치
            Vector2 targetPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle * meteorRange * projectileMultiple;

            // 메테오 생성
            GameObject magicObj = LeanPool.Spawn(meteorPrefab, targetPos, Quaternion.identity, SystemManager.Instance.magicPool);

            MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
            // magic 데이터 넣기
            magicHolder.magic = meteorMagic;

            // 타겟을 플레이어로 전환
            magicHolder.SetTarget(MagicHolder.TargetType.Player);

            // 메테오 목표지점 targetPos 넣기
            magicHolder.targetPos = targetPos;

            yield return new WaitForSeconds(0.1f);
        }

        // 상태값 Idle로 초기화
        StartCoroutine(SetIdle(1f));
    }
    #endregion

    #region StealthAttack
    IEnumerator StealthAtk()
    {
        // 짖기 애니메이션 재생
        character.animList[0].SetBool(AnimState.isBark.ToString(), true);

        // 히트박스 전부 끄기
        for (int i = 0; i < character.hitBoxList.Count; i++)
        {
            character.hitBoxList[i].enabled = false;
        }

        // 짖기 애니메이션 대기
        yield return new WaitForSeconds(2f);

        // 안개 생성
        MakeFog();

        // 스모크 사운드 재생
        SoundManager.Instance.PlaySound("HotDog_Stealth", 1f);

        // 투명해질때까지 대기
        yield return new WaitUntil(() => character.spriteList[0].color == Color.clear);

        // 스모크 사운드 정지
        SoundManager.Instance.StopSound("HotDog_Stealth", 1f);

        // 짖기 애니메이션 트리거 끄기
        character.animList[0].SetBool(AnimState.isBark.ToString(), false);

        // 돌진 횟수 계산 (2 ~ 4회)
        int atkNum = Random.Range(2, 5);
        // 투사체 배율 추가 계산 (2~4회) ~ (3~6회)
        atkNum = (int)(atkNum * projectileMultiple);

        //돌진 시작 방향 (좌 or 우)
        bool rightStart = true;
        // 짝수일때
        if (atkNum % 2 == 0)
            //오른쪽에서 시작
            rightStart = true;
        // 홀수일때
        else
            //왼쪽에서 시작
            rightStart = false;

        // 랜덤 횟수로 좌,우 번갈아 달려가며 공격
        for (int i = 0; i < atkNum; i++)
        {
            // 돌진 시작 위치 계산
            Vector3 dashStartPos = rightStart ? Camera.main.ViewportToWorldPoint(new Vector2(1, 0.5f)) : Camera.main.ViewportToWorldPoint(new Vector2(0, 0.5f));
            Vector3 dashEndPos = rightStart ? Camera.main.ViewportToWorldPoint(new Vector2(0, 0.5f)) : Camera.main.ViewportToWorldPoint(new Vector2(1, 0.5f));
            dashStartPos.z = 0f;
            dashEndPos.z = 0f;

            //돌진 시작 위치로 이동
            transform.position = dashStartPos;

            // 돌진 방향으로 회전
            if ((dashEndPos - dashStartPos).x > 0)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            // 스프라이트 색 초기화
            foreach (SpriteRenderer sprite in character.spriteList)
            {
                sprite.DOColor(Color.white, 0.2f);
            }
            //그림자 색 초기화
            shadowSprite.DOColor(new Color(0, 0, 0, 0.5f), 0.2f);

            // 눈이 빛나는 인디케이터 이펙트 보여주기
            eyeFlash.gameObject.SetActive(true);

            // 눈빛 켜기
            eyeGlow.gameObject.SetActive(true);
            eyeGlow.DOColor(Color.white, 0.2f);

            // 돌진 시작 경고음 재생
            SoundManager.Instance.PlaySound("HotDog_DashWarning");

            // 히트박스 전부 켜기
            for (int j = 0; j < character.hitBoxList.Count; j++)
            {
                character.hitBoxList[i].enabled = true;
            }

            // 대쉬 어택 콜라이더 켜기
            dashAtk.enabled = true;

            // 인디케이터 시간 대기
            yield return new WaitForSeconds(0.5f);

            // 눈 위치에 아키라 트레일 생성
            GameObject eyeTrail = LeanPool.Spawn(eyeTrailPrefab, eyeFlash.transform.position, Quaternion.identity, eyeFlash.transform.parent);

            // Run 애니메이션 재생
            character.animList[0].SetBool(AnimState.isRun.ToString(), true);

            // 돌진 시간 계산
            float moveSpeed = 1.5f / speedMultiple;

            // 플레임 소환 위치 초기화
            flameLastPos = transform.position;

            // 엔드 포지션으로 달리기
            transform.DOMove(dashEndPos, moveSpeed)
            .OnUpdate(() =>
            {
                //일정 거리마다 Flame 마법 생성
                if (Vector2.Distance(flameLastPos, transform.position) > flameInverval)
                {
                    // 마법 오브젝트 생성
                    GameObject magicObj = LeanPool.Spawn(flamePrefab, transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

                    // 매직홀더 찾기
                    MagicHolder flameHolder = magicObj.GetComponent<MagicHolder>();
                    // 마법 정보 넣기
                    flameHolder.magic = flameMagic;
                    // 마법 타겟 지정
                    flameHolder.SetTarget(MagicHolder.TargetType.Player);
                    // 타겟 위치 지정
                    flameHolder.targetPos = transform.position;

                    // 마지막 소환 위치 갱신
                    flameLastPos = magicObj.transform.position;
                }
            })
            .OnComplete(() =>
            {
                // 아이 트레일 부모 바꾸기
                eyeTrail.transform.SetParent(SystemManager.Instance.effectPool);

                // 아이 아키라 트레일 1초후 디스폰
                LeanPool.Despawn(eyeTrail, moveSpeed + 0.1f);
            });

            // 돌진시간 대기
            yield return new WaitForSeconds(moveSpeed - 0.5f);

            // 스프라이트 색 투명하게
            foreach (SpriteRenderer sprite in character.spriteList)
            {
                sprite.DOColor(Color.clear, 0.2f);
            }
            // 그림자 색 투명하게
            shadowSprite.DOColor(Color.clear, 0.2f);

            // 눈빛 끄기
            eyeGlow.DOColor(Color.clear, 0.2f);

            // 히트박스 전부 끄기
            for (int j = 0; j < character.hitBoxList.Count; j++)
            {
                character.hitBoxList[i].enabled = false;
            }

            // 대쉬 어택 콜라이더 끄기
            dashAtk.enabled = false;

            //출발 방향 반대로 돌리기
            rightStart = !rightStart;

            // 애니메이션 idle
            character.animList[0].SetBool(AnimState.isRun.ToString(), false);

            // 돌진 후딜레이 대기
            yield return new WaitForSeconds(0.5f);
        }

        // 플레이어 주변 랜덤 위치로 이동
        transform.position = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * 20f;

        // 보스를 부모로 지정
        groundSmoke.transform.SetParent(transform);
        // 바닥 연기 이펙트 끄기
        groundSmoke.gameObject.SetActive(false);

        // 꼬리 이펙트 켜기
        tailEffect.gameObject.SetActive(true);

        // 대쉬 어택 콜라이더 끄기
        dashAtk.enabled = false;
        // 충돌 콜라이더 켜기
        character.physicsColl.enabled = true;

        // 히트박스 전부 켜기
        for (int i = 0; i < character.hitBoxList.Count; i++)
        {
            character.hitBoxList[i].enabled = true;
        }

        // 애니메이션 idle
        character.animList[0].SetBool(AnimState.isRun.ToString(), false);

        // 글로벌 라이트 초기화
        DOTween.To(x => SystemManager.Instance.globalLight.intensity = x, SystemManager.Instance.globalLight.intensity, SystemManager.Instance.globalLightDefault, 0.5f);

        // 몸에서 HDR 빛나는 오브젝트 모두 켜기
        foreach (SpriteRenderer glow in glowObj)
        {
            glow.gameObject.SetActive(true);
        }

        // 스프라이트 색 초기화
        foreach (SpriteRenderer sprite in character.spriteList)
        {
            sprite.DOColor(Color.white, 0.5f);
        }
        //그림자 색 초기화
        shadowSprite.DOColor(new Color(0, 0, 0, 0.5f), 0.5f);

        // 상태값 Idle로 초기화
        StartCoroutine(SetIdle(0.5f));
    }

    public void MakeFog()
    {
        // 배경 어두워지고 보스 투명해지기
        StartCoroutine(Cloaking());
    }

    IEnumerator Cloaking()
    {
        // 스모크 이펙트 켜기
        smokeEffect.gameObject.SetActive(true);

        // 꼬리 이펙트 끄기
        tailEffect.SmoothDisable();

        yield return new WaitForSeconds(1f);

        // 플레이어를 부모로 지정
        groundSmoke.transform.SetParent(PlayerManager.Instance.transform);
        groundSmoke.transform.localPosition = Vector3.zero;
        // 바닥 연기 이펙트 켜기
        groundSmoke.gameObject.SetActive(true);

        //충돌 콜라이더 끄기
        character.physicsColl.enabled = false;

        // 글로벌 라이트 어둡게
        DOTween.To(x => SystemManager.Instance.globalLight.intensity = x, SystemManager.Instance.globalLight.intensity, 0.1f, 1f);

        // 스프라이트 투명해지며 사라지기
        for (int i = 0; i < character.spriteList.Count; i++)
        {
            character.spriteList[i].DOColor(Color.clear, 1f)
            .OnComplete(() =>
            {
                // 몸에서 HDR 빛나는 오브젝트 모두 끄기
                foreach (SpriteRenderer glow in glowObj)
                {
                    glow.gameObject.SetActive(false);
                }
            });
        }

        // 그림자도 투명하게
        shadowSprite.DOColor(Color.clear, 1f);

        // 투명도 절반까지 대기
        yield return new WaitForSeconds(0.5f);

        // 스모크 이펙트 끄기
        smokeEffect.GetComponent<ParticleManager>().SmoothDisable();
    }
    #endregion

    #region FootDust
    void HandDustPlay()
    {
        if (character.nowAction == Character.Action.Walk)
            handDust.Play();
    }

    void HandDustStop()
    {
        handDust.Stop();
    }

    void FootDustPlay()
    {
        if (character.nowAction == Character.Action.Walk)
            footDust.Play();
    }

    void FootDustStop()
    {
        footDust.Stop();
    }
    #endregion

    void Hit()
    {
        // 체력이 2/3 ~ 3/3 사이일때 1페이즈

        // 현재 1페이즈,체력이 2/3 이하일때, 2페이즈
        if (nowPhase == 1 && character.hpNow / character.hpMax <= 2f / 3f)
        {
            // 페이즈업 함수 실행 안됬을때
            if (nowPhase == nextPhase)
            {
                // 다음 페이스 숫자 올리기
                nextPhase = 2;

                // 다음 페이즈 예약
                StartCoroutine(PhaseChange());
            }
        }

        // 현재 2페이즈, 체력이 1/3 이하일때, 3페이즈
        if (nowPhase == 2 && character.hpNow / character.hpMax <= 1f / 3f)
        {
            // 페이즈업 함수 실행 안됬을때
            if (nowPhase == nextPhase)
            {
                // 다음 페이스 숫자 올리기
                nextPhase = 3;

                // 다음 페이즈 예약
                StartCoroutine(PhaseChange());
            }
        }

        // 체력이 0 이하일때, 죽었을때
        if (character.hpNow <= 0)
        {
            // 글로벌 라이트 초기화
            SystemManager.Instance.globalLight.intensity = SystemManager.Instance.globalLight.intensity;

            // 그림자 색 초기화
            shadowSprite.color = new Color(0, 0, 0, 0.5f);

            // 보스를 부모로 지정
            groundSmoke.transform.SetParent(transform);
            // 바닥 연기 이펙트 끄기
            groundSmoke.gameObject.SetActive(false);

            // 호흡 이펙트 켜기
            breathEffect.gameObject.SetActive(true);
            // 스모크 이펙트 끄기
            smokeEffect.gameObject.SetActive(false);
            // 바닥 연기 이펙트 끄기
            groundSmoke.gameObject.SetActive(false);
            // 눈 번쩍하는 이펙트 끄기
            eyeFlash.gameObject.SetActive(false);

            // 발 먼지 이펙트 끄기
            handDust.Stop();
            footDust.Stop();
        }
    }
}
