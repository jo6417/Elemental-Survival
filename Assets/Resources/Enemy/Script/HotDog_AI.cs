using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class HotDog_AI : MonoBehaviour
{
    [Header("Refer")]
    bool initialDone = false;
    AnimState animState;
    enum AnimState { isWalk, isRun, isBark, Jump, Bite, Charge, Eat, Launch };
    public EnemyManager enemyManager;
    EnemyInfo enemy;
    public EnemyAtkTrigger biteTrigger;
    public ParticleManager chargeEffect;
    public ParticleManager tailEffect; //꼬리 화염 파티클
    public SpriteRenderer eyeGlow; // 빛나는 눈 오브젝트
    public List<GameObject> glowObj = new List<GameObject>(); // 빛나는 오브젝트들

    [Header("Move")]
    public float farDistance = 15f;
    public float closeDistance = 8f;
    public ParticleManager breathEffect; //숨쉴때 입에서 나오는 불꽃
    public ParticleSystem handDust;
    public ParticleSystem footDust;
    public Vector2 moveToPos; // 목표 위치
    public float moveResetCount; // 목표위치 갱신 시간 카운트

    [Header("Stealth Atk")]
    public GameObject eyeTrailPrefab; // 눈에서 나오는 붉은 트레일
    public ParticleSystem groundSmoke; // 스텔스 할때 바닥에서 피어오르는 연기 이펙트
    public ParticleSystem eyeFlash; //눈이 번쩍하는 이펙트
    public ParticleSystem smokeEffect; // 안개 생성시 입에서 나오는 연기
    public EnemyAttack dashAtk;
    public SpriteRenderer shadowSprite; //그림자 스프라이트

    [Header("HellFire")]
    public ParticleSystem leakFireEffect; //차지 후 불꽃 새는 이펙트
    int jumpCount = -1;
    MagicInfo hellFireMagic;
    GameObject hellFirePrefab;
    [SerializeField]
    int hellfire_defaultNum = 10;
    int hellfire_Increment = 10;

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

    private void Awake()
    {
        enemyManager = enemyManager == null ? GetComponentInChildren<EnemyManager>() : enemyManager;
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        // 호흡 이펙트 끄기
        breathEffect.gameObject.SetActive(false);
        // 스모크 이펙트 끄기
        smokeEffect.gameObject.SetActive(false);
        // 바닥 연기 이펙트 끄기
        groundSmoke.gameObject.SetActive(false);
        // 눈 번쩍하는 이펙트 끄기
        eyeFlash.gameObject.SetActive(false);

        // 몸에서 HDR 빛나는 오브젝트 모두 켜기
        foreach (GameObject glow in glowObj)
        {
            glow.SetActive(true);
        }

        // 발 먼지 이펙트 끄기
        handDust.Stop();
        footDust.Stop();

        //대쉬 어택 콜라이더 끄기
        dashAtk.enabled = false;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        enemyManager.rigid.velocity = Vector2.zero; //속도 초기화

        //애니메이션 초기화
        enemyManager.animList[0].SetBool(AnimState.isWalk.ToString(), false);
        enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);
        enemyManager.animList[0].SetBool(AnimState.isBark.ToString(), false);

        // 상태값 Idle로 초기화
        enemyManager.nowAction = EnemyManager.Action.Idle;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 헬파이어 마법 데이터 찾기
        if (hellFireMagic == null)
        {
            // 찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            hellFireMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("HellFire"));

            // 강력한 데미지로 고정
            hellFireMagic.power = 10f;

            // 메테오 프리팹 찾기
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

        //죽을때 델리게이트에 함수 추가
        enemyManager.enemyDeadCallback += Dead;

        // 초기화 완료
        initialDone = true;
    }

    void Update()
    {
        if (enemyManager.enemy == null)
            return;

        // 상태 이상 있으면 리턴
        if (!enemyManager.ManageState())
            return;

        // 초기화 완료 안됬으면 리턴
        if (!initialDone)
            return;

        //행동 관리
        ManageAction();
    }

    void ManageAction()
    {
        // Idle 아니면 리턴
        if (enemyManager.nowAction != EnemyManager.Action.Idle)
            return;

        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // 플레이어 방향
        Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 플레이어와의 거리
        float playerDistance = playerDir.magnitude;

        // 물기 콜라이더에 플레이어 닿으면 bite 패턴
        if (biteTrigger.atkTrigger)
        {
            // 현재 액션 변경
            enemyManager.nowAction = EnemyManager.Action.Attack;

            // 호흡 이펙트 끄기
            breathEffect.SmoothDisable();

            // 물기 패턴 실행
            StartCoroutine(Bite());

            // 쿨타임 갱신
            coolCount = biteCooltime;

            return;
        }

        // 공격 범위내에 있고 공격 쿨타임 됬을때
        if (playerDistance <= farDistance && playerDistance >= closeDistance && coolCount <= 0)
        {
            //! 거리 확인용
            stateText.text = "Attack : " + playerDistance;

            // 속도 초기화
            enemyManager.rigid.velocity = Vector3.zero;

            // 현재 액션 변경
            enemyManager.nowAction = EnemyManager.Action.Attack;

            //공격 패턴 결정하기
            ChooseAttack();
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
        enemyManager.nowAction = EnemyManager.Action.Walk;

        // 움직이는 동안 공격 쿨타임 차감
        if (coolCount >= 0)
            coolCount -= Time.deltaTime;

        // 호흡 이펙트 켜기
        breathEffect.gameObject.SetActive(true);

        //애니메이터 켜기
        enemyManager.animList[0].enabled = true;

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
            enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);
            enemyManager.animList[0].SetBool(AnimState.isWalk.ToString(), true);
        }
        // 플레이어가 멀리 있을때
        else
        {
            // Run 애니메이션으로 전환
            enemyManager.animList[0].SetBool(AnimState.isWalk.ToString(), false);
            enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), true);

            // 속도 빠르게
            runSpeed = 1.5f;
        }

        // 목표 위치 갱신 시간 됬을때
        if (moveResetCount < Time.time)
        {
            moveResetCount = Time.time + 3f;

            // 방향에 거리 곱해서 목표 위치 벡터 갱신
            moveToPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * Random.Range(10, 20);

            // LeanPool.Spawn(SystemManager.Instance.markPrefab, moveToPos, Quaternion.identity);
            // print(moveToPos);
        }

        //움직일 방향, moveToPos를 목표로 움직이기
        Vector2 moveDir = moveToPos - (Vector2)transform.position;
        // 목표 위치까지 거리
        float moveDistance = moveDir.magnitude;

        // 목표 위치에 근접했을때
        if (moveDistance < 1f)
        {
            // 이동 멈추기
            enemyManager.rigid.velocity = Vector3.zero;

            // Idle 애니메이션 진행
            enemyManager.animList[0].SetBool(AnimState.isWalk.ToString(), false);
            enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);
        }
        else
        {
            // 이동 방향 쪽으로 쳐다보기
            if (moveDir.x > 0)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            //해당 방향으로 가속
            enemyManager.rigid.velocity = moveDir.normalized * enemyManager.speed * SystemManager.Instance.globalTimeScale * runSpeed;
            // print(enemyManager.rigid.velocity);
        }

        // 상태값 Idle로 초기화
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    void ChooseAttack()
    {
        // 현재 액션 변경
        enemyManager.nowAction = EnemyManager.Action.Attack;

        // Idle 애니메이션 재생
        enemyManager.animList[0].SetBool(AnimState.isWalk.ToString(), false);
        enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);

        // 발 먼지 이펙트 끄기
        handDust.Stop();
        footDust.Stop();

        // 호흡 이펙트 끄기
        breathEffect.SmoothDisable();

        // 랜덤 패턴 결정
        int randomNum = Random.Range(0, 3);
        print("randomNum : " + randomNum);

        //! 테스트를 위해 패턴 고정
        randomNum = 2;

        switch (randomNum)
        {
            case 0:
                // 근거리 및 중거리 헬파이어 패턴
                StartCoroutine(HellfireAtk());
                coolCount = hellfireCooltime;
                break;
            case 1:
                // 원거리 메테오 쿨타임 아닐때 meteor 패턴 코루틴
                StartCoroutine(MeteorAtk());
                coolCount = meteorCooltime;
                break;
            case 2:
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
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    void StartChargeEffect()
    {
        chargeEffect.gameObject.SetActive(true);
    }

    #region BiteAtk
    IEnumerator Bite()
    {
        yield return null;

        // 걷기 애니메이션 끄기
        enemyManager.animList[0].SetBool(AnimState.isWalk.ToString(), false);

        // 물기 애니메이션 재생
        enemyManager.animList[0].SetTrigger(AnimState.Bite.ToString());
    }

    void BiteEnd()
    {
        // 행동 초기화
        StartCoroutine(SetIdle(1f));
    }
    #endregion

    #region HellFire

    IEnumerator HellfireAtk()
    {
        // 차지 애니메이션 재생
        enemyManager.animList[0].SetTrigger(AnimState.Charge.ToString());
        // 차지 끝나면 에너지볼 먹는 애니메이션 재생
        enemyManager.animList[0].SetTrigger(AnimState.Eat.ToString());

        // 3연속 점프
        for (int i = 0; i < 3; i++)
        {
            jumpCount = i;

            // 점프 애니메이션 재생
            enemyManager.animList[0].SetTrigger(AnimState.Jump.ToString());

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
    }

    void SummonHellfire()
    {
        switch (jumpCount)
        {
            case 0:
                // 8방향 직선 연속 헬파이어 전개
                StartCoroutine(CastHellfire(jumpCount, 8, 10));
                break;
            case 1:
                // 원형 연속 헬파이어 전개
                StartCoroutine(CastHellfire(jumpCount, 16, 10));
                break;
            case 2:
                // 8방향 직선 및 유도 헬파이어 전개
                StartCoroutine(CastHellfire(jumpCount, 4, 50));
                break;
        }
    }

    IEnumerator CastHellfire(int _jumpCount, int summonNum, int loopNum)
    {
        // 각 헬파이어 위치 저장할 배열
        Vector3[] lastPos = new Vector3[summonNum];

        // 루프 개수만큼 반복
        for (int j = 0; j < loopNum; j++)
        {
            // 헬파이어 개수만큼 반복
            for (int i = 0; i < summonNum; i++)
            {
                // 유도 헬파이어일때, 5회차 이상이면 유도로 전환
                if (_jumpCount == 2 && j > 4)
                {
                    // 플레이어까지 방향 벡터 계산
                    Vector3 moveDir = PlayerManager.Instance.transform.position - lastPos[i];

                    // 플레이어 방향으로 0~5만큼 이동된 벡터 계산, 점점 거리 늘어남
                    moveDir = moveDir.normalized * 5f * (float)j / (float)loopNum;

                    // 5 보다 플레이어까지 거리가 가까울때 벡터 거리 제한
                    moveDir = Vector3.ClampMagnitude(moveDir, moveDir.magnitude);

                    // 마지막 저장된 위치에서 플레이어 위치까지 보정된 위치 저장
                    lastPos[i] = lastPos[i] + moveDir;
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
                magicHolder.SetTarget(MagicHolder.Target.Player);

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
    IEnumerator MeteorAtk()
    {
        yield return null;

        // 차지 애니메이션 재생
        enemyManager.animList[0].SetTrigger(AnimState.Charge.ToString());
        // 에너지볼 발사 애니메이션 재생
        enemyManager.animList[0].SetTrigger(AnimState.Launch.ToString());
    }

    // meteor 애니메이션 끝날때쯤 meteor 소환 함수
    public void CastMeteor()
    {
        StartCoroutine(SummonMeteor());
    }

    IEnumerator SummonMeteor()
    {
        //메테오 개수만큼 반복
        for (int i = 0; i < meteorNum; i++)
        {
            // 메테오 떨어질 위치
            Vector2 targetPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle * 20f;

            // 메테오 생성
            GameObject magicObj = LeanPool.Spawn(meteorPrefab, targetPos, Quaternion.identity, SystemManager.Instance.magicPool);

            // 메테오 스프라이트 빨갛게
            // magicObj.GetComponent<SpriteRenderer>().color = Color.red;

            MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
            // magic 데이터 넣기
            magicHolder.magic = meteorMagic;

            // 타겟을 플레이어로 전환
            magicHolder.SetTarget(MagicHolder.Target.Player);

            // 메테오 목표지점 targetPos 넣기
            magicHolder.targetPos = targetPos;

            yield return new WaitForSeconds(0.1f);
        }

        // Idle 애니메이션 재생
        // enemyManager.animList[0].SetBool(AnimState.Charge.ToString(), false);

        // 상태값 Idle로 초기화
        StartCoroutine(SetIdle(1f));
    }
    #endregion

    #region StealthAttack
    IEnumerator StealthAtk()
    {
        // 짖기 애니메이션 재생
        enemyManager.animList[0].SetBool(AnimState.isBark.ToString(), true);

        // 투명해질때까지 대기
        yield return new WaitUntil(() => enemyManager.spriteList[0].color == Color.clear);

        // 짖기 애니메이션 트리거 끄기
        enemyManager.animList[0].SetBool(AnimState.isBark.ToString(), false);

        // 돌진 횟수 계산 2~5회
        int atkNum = Random.Range(2, 6);

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
            foreach (SpriteRenderer sprite in enemyManager.spriteList)
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

            // 피격 콜라이더 켜기
            foreach (Collider2D coll in enemyManager.hitCollList)
            {
                coll.enabled = true;
            }
            // 대쉬 어택 콜라이더 켜기
            dashAtk.enabled = true;

            // 인디케이터 시간 대기
            yield return new WaitForSeconds(0.5f);

            // 눈 위치에 아키라 트레일 생성
            GameObject eyeTrail = LeanPool.Spawn(eyeTrailPrefab, eyeFlash.transform.position, Quaternion.identity, eyeFlash.transform.parent);

            // Run 애니메이션 재생
            enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), true);

            // 엔드 포지션으로 트윈
            transform.DOMove(dashEndPos, 1f)
            .OnComplete(() =>
            {
                // 아이 트레일 부모 바꾸기
                eyeTrail.transform.SetParent(SystemManager.Instance.effectPool);

                // 아이 아키라 트레일 1초후 디스폰
                LeanPool.Despawn(eyeTrail, 1f);
            });

            // 돌진시간 대기
            yield return new WaitForSeconds(0.8f);

            // 스프라이트 색 초기화
            foreach (SpriteRenderer sprite in enemyManager.spriteList)
            {
                sprite.DOColor(Color.clear, 0.2f);
            }
            //그림자 색 초기화
            shadowSprite.DOColor(Color.clear, 0.2f);

            // 눈빛 끄기
            eyeGlow.DOColor(Color.clear, 0.2f);

            // 피격 콜라이더 끄기
            foreach (Collider2D coll in enemyManager.hitCollList)
            {
                coll.enabled = false;
            }
            // 대쉬 어택 콜라이더 끄기
            dashAtk.enabled = false;

            //출발 방향 반대로 돌리기
            rightStart = !rightStart;

            // 애니메이션 idle
            enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);

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
        enemyManager.physicsColl.enabled = true;

        // 피격 콜라이더 켜기
        foreach (Collider2D coll in enemyManager.hitCollList)
        {
            coll.enabled = true;
        }

        // 애니메이션 idle
        enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);

        // 글로벌 라이트 초기화
        DOTween.To(x => SystemManager.Instance.globalLight.intensity = x, SystemManager.Instance.globalLight.intensity, SystemManager.Instance.globalLightDefault, 0.5f);

        // 몸에서 HDR 빛나는 오브젝트 모두 켜기
        foreach (GameObject glow in glowObj)
        {
            glow.SetActive(true);
        }

        // 스프라이트 색 초기화
        foreach (SpriteRenderer sprite in enemyManager.spriteList)
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
        enemyManager.physicsColl.enabled = false;

        // 피격 콜라이더 끄기
        foreach (Collider2D coll in enemyManager.hitCollList)
        {
            coll.enabled = false;
        }

        // 글로벌 라이트 어둡게
        DOTween.To(x => SystemManager.Instance.globalLight.intensity = x, SystemManager.Instance.globalLight.intensity, 0.1f, 1f);

        // 스프라이트 투명해지며 사라지기
        foreach (SpriteRenderer sprite in enemyManager.spriteList)
        {
            sprite.DOColor(Color.clear, 1f)
            .OnComplete(() =>
            {
                // 몸에서 HDR 빛나는 오브젝트 모두 끄기
                foreach (GameObject glow in glowObj)
                {
                    glow.SetActive(false);
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
        if (enemyManager.nowAction == EnemyManager.Action.Walk)
            handDust.Play();
    }

    void HandDustStop()
    {
        handDust.Stop();
    }

    void FootDustPlay()
    {
        if (enemyManager.nowAction == EnemyManager.Action.Walk)
            footDust.Play();
    }

    void FootDustStop()
    {
        footDust.Stop();
    }
    #endregion

    void Dead()
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
