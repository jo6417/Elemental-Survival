using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;
using Lean.Pool;

public class Bawi_AI : MonoBehaviour
{
    [Header("State")]
    float coolCount; //쿨타임 카운트
    public float stoneThrowCoolTime;
    // public float scatterCoolTime;
    // public float drillChaseCoolTime;
    // public float dashCoolTime;
    public float farDistance = 10f;
    // public float closeDistance = 5f;
    public Vector2 fistParentDefaultPos = new Vector2(5, 0); // fistParent 초기 위치
    public Vector2 drillParentDefaultPos = new Vector2(-5, 0); // drillParent 초기 위치
    public Vector2 fistPartDefaultPos = new Vector2(0, 5); // fistPart의 초기 위치
    public Vector2 drillPartDefaultPos = new Vector2(0, 5); // drillPart 초기 위치
    Vector3 playerDir;
    bool isFloating = true; //부유 상태 여부
    public float drillAimCount = 1f; //드릴 조준 시간

    [Header("Refer")]
    public TextMeshProUGUI stateText; //! 테스트 현재 상태
    public EnemyManager enemyManager;
    public GameObject smallLandDust; //착지할때 손 먼지 파티클

    [Header("Head")]
    public Transform headPart;
    public Collider2D headShadowColl; // 머리 충돌용 콜라이더
    public ParticleSystem headParticle; // 머리 부유 이펙트
    public GameObject bigLandDust; //착지할때 헤드 먼지 파티클
    public GameObject headDashDust; // 돌진시 땅에 남기는 먼지 이펙트

    [Header("Fist")]
    // public Transform fistParent; // 그림자까지 포함한 부모 오브젝트
    public SpriteRenderer fistPart;
    // public SpriteRenderer fistShadow; // 주먹 그림자
    public ParticleSystem fistParticle; // 주먹 부유 파티클
    public GameObject fistDust; //돌 부술때 먼지 파티클
    public Sprite emptyFistSprite;
    public Sprite openFistSprite;
    public Sprite grabFistSprite;
    public GameObject stonePrefab; // 공격시 던질 돌 프리팹
    public GameObject fistDashDust; // 돌진시 땅에 남기는 먼지 이펙트

    [Header("Drill")]
    public Transform drillParent; // 그림자까지 포함한 부모
    public Transform drillPart; // 드릴 고스트 및 부유 이펙트 포함한 부모
    public SpriteRenderer mainDrillSprite; // 드릴 스프라이트 자체
    public SpriteRenderer ghostDrillSprite; // 드릴 고스트 스프라이트
    public SpriteRenderer drillShadow; // 드릴 그림자
    public ParticleSystem drillParticle; // 드릴 부유 파티클
    public Animator mainDrillAnim; // 메인 드릴 회전 애니메이터
    public Animator ghostDrillAnim; // 고스트 드릴 회전 애니메이터
    public Collider2D drillGhostColl; // 고스트 드릴 콜라이더
    public ParticleSystem chargeGathering; // 드릴차지 기모으는 이펙트
    public ParticleSystem chargePulse; // 드릴차지 펄스 이펙트
    public GameObject drillDashDust; // 돌진시 땅에 남기는 먼지 이펙트

    private void Awake()
    {
        // enemyManager = GetComponent<EnemyManager>();

        // 초기 위치 저장
        // fistDefaultPos = fistPart.transform.localPosition;
        // drillDefaultPos = drillPart.transform.localPosition;
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        //애니메이션 스피드 초기화
        if (enemyManager.animList != null)
        {
            foreach (Animator anim in enemyManager.animList)
            {
                anim.speed = 1f;
            }
        }

        //속도 초기화
        enemyManager.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 콜라이더 충돌 초기화
        // enemyManager.coll.isTrigger = false;

        //부유 상태로 전환
        isFloating = true;

        // 각각 드릴 스프라이트 참조
        SpriteRenderer ghostDrill = mainDrillSprite.transform.Find("DrillGhost").GetComponent<SpriteRenderer>();

        // 고스트 드릴 오브젝트 비활성화
        ghostDrill.gameObject.SetActive(true);

        //드릴 스케일 및 위치 초기화
        ghostDrill.transform.localScale = Vector2.one;
        ghostDrill.transform.localPosition = Vector2.zero;

        // 각각 드릴 색 초기화
        mainDrillSprite.color = Color.white;
        ghostDrill.color = Color.clear;

        // 차지 파티클 끄기
        // chargeGathering.gameObject.SetActive(false);
    }

    void Update()
    {
        if (enemyManager.enemy == null)
            return;

        // 상태 이상 있으면 리턴
        if (!enemyManager.ManageState())
            return;

        //행동 관리
        ManageAction();
    }
    void ManageAction()
    {
        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // 플레이어 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        // 플레이어와의 거리
        float distance = dir.magnitude;

        // 플레이어 방향 쳐다보기
        if (dir.x > 0)
        {
            headPart.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            headPart.rotation = Quaternion.Euler(0, 180, 0);
        }

        // Idle 아니면 리턴
        if (enemyManager.nowAction != EnemyManager.Action.Idle)
            return;

        // 먼 거리일때
        if (distance > farDistance)
        {
            //! 거리 확인용
            stateText.text = "Far : " + distance;

            // 플레이어 따라가기
            Walk();
        }
        // 가까울때
        else
        {
            //! 거리 확인용
            stateText.text = "Close : " + distance;

            // 양쪽 손 회전값 초기화
            fistPart.transform.rotation = Quaternion.Euler(0, 0, 90);
            drillPart.rotation = Quaternion.Euler(0, 0, 0);

            // 속도 초기화
            enemyManager.rigid.velocity = Vector3.zero;

            //공격 패턴 결정하기
            ChooseAttack();
        }
    }

    void Walk()
    {
        enemyManager.nowAction = EnemyManager.Action.Walk;

        //애니메이터 켜기
        enemyManager.animList[0].enabled = true;
        // Idle 애니메이션으로 전환
        enemyManager.animList[0].SetBool("UseHand", false);
        enemyManager.animList[0].SetBool("UseDrill", false);

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        // 움직일 방향 2D 각도
        float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 가려는 방향으로 양쪽 손이 회전
        fistPart.transform.rotation = Quaternion.Euler(0, 0, rotation);
        drillPart.rotation = Quaternion.Euler(0, 0, rotation - 90f);

        //해당 방향으로 가속
        enemyManager.rigid.velocity = dir.normalized * enemyManager.speed * SystemManager.Instance.globalTimeScale;

        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    public void Floating()
    {
        // 착지 상태일때 
        if (!isFloating)
        {
            // 충돌 콜라이더 끄기
            headShadowColl.enabled = false;

            // 부유 상태로 전환
            isFloating = true;

            // 부유 파티클 모두 켜기
            headParticle.Play();
            drillParticle.Play();
            fistParticle.Play();
        }
    }

    public void Landing()
    {
        // 부유 상태일때
        if (isFloating)
        {
            // 충돌 콜라이더 켜기
            headShadowColl.enabled = true;

            // 착지 상태로 전환
            isFloating = false;

            // 머리 먼지 파티클 생성
            LeanPool.Spawn(bigLandDust, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            if (enemyManager.animList[0].GetBool("UseDrill"))
                // 드릴 먼지 파티클 생성
                LeanPool.Spawn(smallLandDust, drillParent.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            if (enemyManager.animList[0].GetBool("UseHand"))
                // 주먹 먼지 파티클 생성
                LeanPool.Spawn(smallLandDust, fistPart.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
        }

        //애니메이터 끄기
        enemyManager.animList[0].enabled = false;
    }

    void ChooseAttack()
    {
        // 현재 액션 변경
        enemyManager.nowAction = EnemyManager.Action.Attack;

        // 랜덤 패턴 결정
        int randomNum = Random.Range(0, 5);

        // print("randomNum : " + randomNum);

        //! 테스트를 위해 패턴 고정
        randomNum = 2;

        switch (randomNum)
        {
            case 0:
                //TODO 주먹 내려찍기 패턴
                break;
            case 1:
                // 큰 바위 던지기 패턴
                StartCoroutine(StoneThrow(false));
                break;
            case 2:
                // 작은 돌 샷건 패턴
                StartCoroutine(StoneThrow(true));
                break;
            case 3:
                // 드릴 돌진 패턴
                StartCoroutine(DrillDash());
                break;
            case 4:
                //TODO 드릴 추적 패턴
                break;
        }
    }

    IEnumerator StoneThrow(bool isSmallStone = false)
    {
        // 머리 및 드릴 부유 이펙트 끄기
        headParticle.Stop();
        drillParticle.Stop();

        //주먹 공격 애니메이션으로 전환
        enemyManager.animList[0].SetBool("UseHand", true);

        //쿨타임 갱신
        coolCount = stoneThrowCoolTime;

        // 보스 주변 랜덤 위치로 바위 위치 지정
        Vector2 grabPos = Random.insideUnitCircle.normalized * 20f;
        // 돌 잡는 시간
        float grabTime = 1f;

        // 바위 방향
        Vector2 dir = grabPos - (Vector2)transform.position;
        // 바위 방향 바라볼 2D 각도
        float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 플레이어 방향
        float playerAngle = 0f;

        // 바위 집기 시퀀스
        Sequence grabSeq = DOTween.Sequence();
        grabSeq
        .AppendCallback(() =>
        {
            // 손바닥으로 변경
            fistPart.sprite = openFistSprite;
        })
        .Append(
            // 바위 위치로 손이 이동
            fistPart.transform.parent.DOLocalMove(grabPos, grabTime)
        )
        .Join(
            // 아래로 손 내려가기
            fistPart.transform.DOLocalMove(Vector3.up * 2f, grabTime)
        )
        .Join(
            // 잡는 위치 방향으로 회전
            fistPart.transform.DORotate(new Vector3(0, 0, rotation), grabTime)
        )
        .AppendCallback(() =>
        {
            // 바위 집은 손으로 변경
            fistPart.sprite = grabFistSprite;
        })
        // 잠시 대기
        .AppendInterval(0.5f)
        .Append(
            // 던지기 로컬 위치로 이동
            fistPart.transform.parent.DOLocalMove(new Vector2(10f, 5f), 1f)
        )
        .Join(
            // 위로 손 올라가기
            fistPart.transform.DOLocalMove(Vector3.up * 5f, 1f)
        );

        //시퀀스 끝날때까지 대기
        yield return new WaitUntil(() => !grabSeq.active);

        // 일정시간 손이 플레이어 바라보기
        float followCount = 1f;
        while (followCount > 0)
        {
            //시간 차감
            followCount -= Time.deltaTime;

            // 플레이어 방향
            playerDir = PlayerManager.Instance.transform.position - fistPart.transform.position;

            // 플레이어 방향 2D 각도
            playerAngle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;

            stateText.text = "Targeting : " + followCount;

            // 플레이어 방향으로 손이 회전
            fistPart.transform.rotation = Quaternion.Euler(0, 0, playerAngle);

            //TODO 조준 레이저 라인 렌더링

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 작은 돌이면 돌 부수는 트랜지션
        if (isSmallStone)
        {
            // 주먹 위치 떨림
            fistPart.transform.DOPunchPosition(Vector3.up, 0.5f, 50, 1);

            // 주먹에서 먼지 파티클 발생
            LeanPool.Spawn(fistDust, fistPart.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            yield return new WaitForSeconds(0.5f);
        }

        // 돌 발사 위치
        Vector2 throwPos = fistPart.transform.position + Quaternion.AngleAxis(playerAngle, Vector3.forward) * Vector3.right * 7f;

        //던지기 시작 각도
        float startAngle = playerAngle + 89f;
        //던지기 종료 각도
        float endAngle = playerAngle - 89f;

        // 던지기 시작 위치
        Vector2 startPos = fistPart.transform.position + Quaternion.AngleAxis(startAngle, Vector3.forward) * Vector3.right * 7f;
        // 던지기 종료 위치
        Vector2 endPos = fistPart.transform.position + Quaternion.AngleAxis(endAngle, Vector3.forward) * Vector3.right * 7f;

        // endPos가 더 높이 있을땐 startPos와 자리 바꾸기
        if (startPos.y < endPos.y)
        {
            Vector2 tempPos = startPos;

            startPos = endPos;
            endPos = tempPos;

            //던지기 시작 각도
            startAngle = playerAngle - 89f;
            //던지기 종료 각도
            endAngle = playerAngle + 89f;
        }

        //포물선 벡터
        Vector3[] parabola = { startPos, throwPos, endPos };

        float swingTime = 0.5f;

        // 던지기 시퀀스 재생
        Sequence throwSeq = DOTween.Sequence();
        throwSeq
        .Append(
            //던지기 시작할 위치로 이동
            fistPart.transform.parent.DOMove(startPos, 0.5f)
        )
        .Join(
            //던지기 전 각도로 회전
            fistPart.transform.DORotate(new Vector3(0, 0, startAngle), 0.5f)
        )
        .AppendCallback(() =>
        {
            //돌 날리기
            StartCoroutine(ShotStone(isSmallStone));
        })
        .Join(
            //포물선 그리며 손 휘두르기
            fistPart.transform.parent.DOPath(parabola, swingTime, PathType.CatmullRom, PathMode.TopDown2D, 10, Color.red)
        // .SetEase(Ease.OutBack)
        )
        .Join(
            // 던진 후 각도로 회전
            fistPart.transform.DORotate(new Vector3(0, 0, endAngle), swingTime)
        );

        // 던지기 시퀀스 끝날때까지 대기
        yield return new WaitUntil(() => !throwSeq.active);

        // 손 위치 기본 위치로 이동
        fistPart.transform.parent.DOLocalMove(fistPartDefaultPos, 0.5f)
        .OnComplete(() =>
        {
            // Idle 액션으로 전환
            enemyManager.nowAction = EnemyManager.Action.Idle;

            // 일반 주먹으로 변경
            fistPart.sprite = emptyFistSprite;
        });
    }

    IEnumerator ShotStone(bool isSmallStone)
    {
        // 손을 절반 휘두르는 만큼 대기
        yield return new WaitForSeconds(0.1f);

        //플레이어 위치
        Vector3 playerPos = PlayerManager.Instance.transform.position;

        //던질 돌 개수
        int stoneNum = 1;
        // 돌 사이즈
        Vector3 stoneScale = Vector3.one;

        // 작은돌 트리거면
        if (isSmallStone)
        {
            // 작은 돌 5개 던지기
            stoneNum = 5;
            // 돌 사이즈 작게
            stoneScale = Vector3.one * 0.5f;
        }
        else
        {
            // 큰돌 1개 던지기
            stoneNum = 1;
            // 돌 사이즈 크게
            stoneScale = Vector3.one;
        }

        for (int i = 0; i < stoneNum; i++)
        {
            //중간에 돌 던지기
            GameObject stone = LeanPool.Spawn(stonePrefab, fistPart.transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

            //돌 스케일 설정
            stone.transform.localScale = stoneScale;

            // 타겟 및 타겟위치 설정하기
            MagicHolder magicHolder = stone.GetComponent<MagicHolder>();
            magicHolder.SetTarget(MagicHolder.Target.Player);

            // 마법 랜덤 유지시간 추가
            magicHolder.addDuration = Random.Range(0.5f, 1f);

            // 작은 돌일때
            if (isSmallStone)
                //플레이어 주변에 던지기
                magicHolder.targetPos = playerPos + (Vector3)Random.insideUnitCircle * 10f;
            // 큰 돌일때
            else
                //플레이어 정확하게 조준하고 던지기
                magicHolder.targetPos = playerPos;

            // 플레이어 방향 다시 계산
            playerDir = PlayerManager.Instance.transform.position - fistPart.transform.position;

            Rigidbody2D stoneRigid = stone.GetComponent<Rigidbody2D>();
            // 돌 회전시키기
            stoneRigid.angularVelocity = Random.value * -playerDir.x * 20f;

            // 작은 돌일때
            if (isSmallStone)
            {
                //랜덤 대기시간
                float delay = Random.Range(0.01f, 0.05f);

                //일정 시간 대기
                yield return new WaitForSeconds(delay);
            }
        }

        // 손바닥으로 변경
        fistPart.sprite = openFistSprite;
    }

    IEnumerator DrillDash()
    {
        // 머리 및 주먹 부유 이펙트 끄기
        headParticle.Stop();
        fistParticle.Stop();

        //드릴 사용 애니메이션으로 전환
        enemyManager.animList[0].SetBool("UseDrill", true);

        // 드릴 스핀 애니메이션 재생
        mainDrillAnim.SetBool("Spin", true);
        ghostDrillAnim.SetBool("Spin", true);

        //착지 할때까지 대기
        yield return new WaitUntil(() => !isFloating);

        // 차지 이펙트 켜기
        chargeGathering.gameObject.SetActive(true);
        chargeGathering.Play();

        // 랜덤 차지 횟수 정하기
        int chargeNum = Random.Range(1, 4);

        // 드릴 머리보다 높게 들기
        Tween upTween = drillPart.DOLocalMove(drillPartDefaultPos + Vector2.up * 5f, 2f)
        .SetEase(Ease.InOutQuart);

        // 트윈 끝날때까지 대기
        yield return new WaitUntil(() => !upTween.active);

        // 차지 횟수만큼 차지 반복
        for (int i = 0; i < chargeNum; i++)
        {
            yield return StartCoroutine(DrillCharge(i, chargeNum - 1));
        }

        // 차지 이펙트 끄기
        StartCoroutine(chargeGathering.GetComponent<ParticleManager>().SmoothDisable());

        //조준 시간 갱신
        drillAimCount = 2f;

        // 보스 위치 떨림
        transform.DOShakePosition(drillAimCount, 0.3f, 50, 90f, false, false);

        // 드릴 높이 낮추기
        drillPart.DOLocalMove(Vector3.zero, 0.5f);

        // 드릴 타겟팅 지연시간
        float aimRate = 0.1f;

        // 조준 시간동안 플레이어 조준하기
        while (drillAimCount > 0)
        {
            //드릴이 공중에 떠있을때
            if (drillPart.localPosition.y > 0)
                aimRate = 0.5f;
            else
                aimRate = 0.1f;

            // 보스에서 플레이어까지 방향
            playerDir = PlayerManager.Instance.transform.position - transform.position;

            //드릴 위치
            Vector3 drillPos = transform.position + playerDir.normalized * 7f + Vector3.up * 2f;

            // 플레이어 방향으로 드릴 위치 이동
            drillParent.DOMove(drillPos, aimRate);

            // 플레이어 방향의 각도
            float rotation = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg - 90f;

            // 드릴이 플레이어 바라보기
            // drillPart.rotation = Quaternion.Euler(Vector3.forward * rotation);
            drillPart.DORotate(Vector3.forward * rotation, aimRate);

            //시간 차감
            stateText.text = "AimCount : " + drillAimCount;
            drillAimCount -= aimRate;
            yield return new WaitForSeconds(aimRate);
        }

        // 대쉬 위치
        Vector2 dashPos = transform.position + (PlayerManager.Instance.transform.position - transform.position).normalized * 30f;

        // 대쉬 선딜 대기
        yield return new WaitForSeconds(0.5f);

        // 대쉬 먼지 파티클 켜기
        headDashDust.SetActive(true);
        fistDashDust.SetActive(true);
        // drillDashDust.SetActive(true);

        // 플레이어 방향으로 돌진
        transform.DOMove(dashPos, 2f)
        .SetEase(Ease.OutExpo)
        .OnComplete(() =>
        {
            // 드릴 콜라이더 끄기
            drillGhostColl.enabled = false;

            // 대쉬 먼지 파티클 끄기
            headDashDust.SetActive(false);
            fistDashDust.SetActive(false);
            // drillDashDust.SetActive(false);
        });

        //돌진하는 동안 대기
        yield return new WaitForSeconds(2f);

        //초기화 시간
        float resetTime = 1f;

        // 메인 드릴 색깔 복구
        mainDrillSprite.DOColor(Color.white, resetTime);
        // 드릴 고스트 투명해지다 사라지고 스케일 초기화
        ghostDrillSprite.DOColor(new Color(1, 1, 1, 0f), resetTime)
        .OnComplete(() =>
        {
            // 고스트 드릴 스케일 초기화
            ghostDrillSprite.transform.localScale = Vector3.one;
        });

        // 드릴 위치 및 각도 초기화
        drillParent.DOLocalMove(drillParentDefaultPos, resetTime);
        drillPart.DOLocalMove(drillPartDefaultPos, resetTime);
        drillPart.DORotate(Vector3.zero, resetTime);

        // 초기화 시간 대기
        yield return new WaitForSeconds(resetTime);

        // Idle 애니메이션 재생
        enemyManager.animList[0].SetBool("UseDrill", false);
        // Idle 상태로 전환
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    IEnumerator DrillCharge(int nowCharge, int maxCharge)
    {
        // 현재 드릴 스케일 구하기
        Vector3 nowGhostScale = ghostDrillSprite.transform.localScale;
        //다음 드릴 스케일 지정
        Vector3 upGhostScale = new Vector3(nowGhostScale.x + 1, nowGhostScale.y + 1, 1);

        //드릴 차지 시간
        float reboundUpTime = 1f;

        // 드릴 차지 시퀀스 (1 ~ 3번 랜덤 차지)
        Sequence chargeSeq = DOTween.Sequence();
        chargeSeq
        .AppendCallback(() =>
        {
            // 고스트 드릴 오브젝트 활성화
            ghostDrillSprite.gameObject.SetActive(true);

            // 드릴 콜라이더 켜기
            drillGhostColl.enabled = true;
        })
        .Append(
            // 반동에 의해 살짝 내려감
            drillPart.DOLocalMove(drillPartDefaultPos + Vector2.up * 4.5f, 0.2f)
            .SetEase(Ease.OutBack)
        )
        .AppendCallback(() =>
        {
            if (nowCharge == maxCharge)
            {
                //차지 이펙트 끄기
                chargeGathering.Stop();
            }

            // 차지 펄스 이펙트 발생
            LeanPool.Spawn(chargePulse, mainDrillSprite.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
        })
        .Append(
            //원래 높이로 복구
            drillPart.DOLocalMove(drillPartDefaultPos + Vector2.up * 5f, reboundUpTime)
        )
        .Join(
            // 메인 드릴 반투명하게
            mainDrillSprite.DOColor(new Color(1, 1, 1, 0.5f), reboundUpTime)
        )
        .Join(
            // 고스트 드릴 매우 밝게 나타내기
            ghostDrillSprite.DOColor(new Color(1, 1, 1, 1f), reboundUpTime)
        )
        .Join(
            // 고스트 드릴 스케일 키우기
            ghostDrillSprite.transform.DOScale(upGhostScale, reboundUpTime)
        )
        .Join(
            // 그림자 크기 키우기
            drillShadow.transform.DOScale(new Vector3(3f, 1f, 0), reboundUpTime)
        )
        .Join(
            // 고스트 드릴 색 안정화
            ghostDrillSprite.DOColor(new Color(1, 1, 1, 0.5f), reboundUpTime)
        )
        // 차지 후 딜레이
        // .AppendInterval(1f)
        ;

        // 시퀀스 끝날때까지 대기
        yield return new WaitUntil(() => !chargeSeq.active);
    }

    void FistDrop()
    {
        //TODO 주먹 공격 애니메이션 켜기
        //TODO 주먹이 플레이어 위치 부드럽게 따라가기
        //TODO 랜덤 레벨로 주먹 커지기

        //TODO 주먹 로컬 위치 내리기
        //TODO 카메라 흔들기
        //TODO 착지 먼지 발생

        //TODO 주먹 단계별로 줄어들어 크기 초기화
        //TODO 주먹 위치 초기화
    }
}
