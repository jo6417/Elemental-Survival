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
    public float scatterCoolTime;
    public float drillChaseCoolTime;
    public float dashCoolTime;
    public float farDistance = 10f;
    public float closeDistance = 5f;
    public Vector2 fistDefaultPos; //손의 기본 위치
    public Vector2 drillDefaultPos; //드릴의 기본 위치
    Vector2 playerDir;
    public bool isFloating = true; //부유 상태 여부

    [Header("Refer")]
    public TextMeshProUGUI stateText; //! 테스트 현재 상태
    public EnemyManager enemyManager;
    public ParticleSystem elementSymbolEffect; //원소 문장 이펙트

    [Header("Head")]
    public Transform headPart;
    public ParticleSystem headParticle; // 머리 부유 파티클
    public GameObject dustParticle; //착지할때 먼지 파티클

    [Header("Fist")]
    public Transform fistParent; // 그림자까지 포함한 부모 오브젝트
    public SpriteRenderer fistPart;
    public SpriteRenderer fistShadow; // 주먹 그림자
    public ParticleSystem fistParticle; // 주먹 부유 파티클
    public Sprite emptyHandSprite;
    public Sprite openHandSprite;
    public Sprite grabHandSprite;
    public GameObject stonePrefab; // 공격시 던질 돌 프리팹

    [Header("Drill")]
    public Transform drillParent; // 그림자까지 포함한 부모
    public Transform drillPart; // 드릴 고스트 및 부유 이펙트 포함한 부모
    public SpriteRenderer drillSprite; // 드릴 스프라이트 자체
    public SpriteRenderer drillShadow; // 드릴 그림자
    public ParticleSystem drillParticle; // 드릴 부유 파티클
    public Animator mainDrillAnim; // 메인 드릴 회전 애니메이터
    public Animator ghostDrillAnim; // 고스트 드릴 회전 애니메이터
    public Collider2D drillGhostColl; // 고스트 드릴 콜라이더

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();

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
        SpriteRenderer ghostDrill = drillSprite.transform.Find("DrillGhost").GetComponent<SpriteRenderer>();

        // 고스트 드릴 오브젝트 비활성화
        ghostDrill.gameObject.SetActive(true);

        //드릴 스케일 및 위치 초기화
        ghostDrill.transform.localScale = Vector2.one;
        ghostDrill.transform.localPosition = Vector2.zero;

        // 각각 드릴 색 초기화
        drillSprite.color = Color.white;
        ghostDrill.color = Color.clear;
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
        drillPart.rotation = Quaternion.Euler(0, 0, rotation);

        //해당 방향으로 가속
        enemyManager.rigid.velocity = dir.normalized * enemyManager.speed * SystemManager.Instance.globalTimeScale;

        // 움직일 방향에따라 머리만 Y축 회전
        // if (dir.x > 0)
        // {
        //     headPart.rotation = Quaternion.Euler(0, 0, 0);
        // }
        // else
        // {
        //     headPart.rotation = Quaternion.Euler(0, 180, 0);
        // }

        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    public void Floating()
    {
        // 착지 상태일때 
        if (!isFloating)
        {
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
            // 착지 상태로 전환
            isFloating = false;

            //먼지 파티클 생성
            LeanPool.Spawn(dustParticle, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
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
        randomNum = 3;

        switch (randomNum)
        {
            case 0:
                //TODO 주먹 내려찍기 패턴
                break;
            case 1:
                // 큰 바위 던지기 패턴
                StartCoroutine(StoneThrow());
                break;
            case 2:
                //TODO 작은 돌 샷건 패턴
                break;
            case 3:
                //TODO 드릴 돌진 패턴, 1~3단계 랜덤 차지, 차지 레벨 높을수록 빠름
                StartCoroutine(DrillDash());
                break;
            case 4:
                //TODO 드릴 추적 패턴
                break;
        }
    }

    IEnumerator StoneThrow()
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
            fistPart.sprite = openHandSprite;
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
            fistPart.sprite = grabHandSprite;
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

        // LeanPool.Spawn(SystemManager.Instance.pointPrefab, startPos, Quaternion.identity);
        // LeanPool.Spawn(SystemManager.Instance.pointPrefab, throwPos, Quaternion.identity);
        // LeanPool.Spawn(SystemManager.Instance.pointPrefab, endPos, Quaternion.identity);

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
            StartCoroutine(ShotStone());
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
        fistPart.transform.parent.DOLocalMove(fistDefaultPos, 0.5f)
        .OnComplete(() =>
        {
            // Idle 액션으로 전환
            enemyManager.nowAction = EnemyManager.Action.Idle;
        });
    }

    IEnumerator ShotStone()
    {
        // 손을 절반 휘두르는 만큼 대기
        yield return new WaitForSeconds(0.1f);

        //중간에 돌 던지기
        GameObject stone = LeanPool.Spawn(stonePrefab, fistPart.transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

        // 타겟 및 타겟위치 설정하기
        MagicHolder magicHolder = stone.GetComponent<MagicHolder>();
        magicHolder.SetTarget(MagicHolder.Target.Player);
        magicHolder.targetPos = PlayerManager.Instance.transform.position;

        // 플레이어 방향 다시 계산
        playerDir = PlayerManager.Instance.transform.position - fistPart.transform.position;

        // print("playerDir : " + playerDir);

        Rigidbody2D stoneRigid = stone.GetComponent<Rigidbody2D>();
        // 돌 회전시키기
        stoneRigid.angularVelocity = Random.value * -playerDir.x * 20f;
        //돌 날리기
        // stoneRigid.velocity = playerDir.normalized * 20f;

        // 손바닥으로 변경
        fistPart.sprite = openHandSprite;
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

        // 랜덤 차지 횟수 정하기
        int chargeNum = Random.Range(1, 4);
        //! 차지횟수 고정
        chargeNum = 3;

        // 드릴 머리보다 높게 들기
        Tween upTween = drillPart.DOLocalMove(drillDefaultPos + Vector2.up * 5f, 2f)
        .SetEase(Ease.InOutQuart);

        // 트윈 끝날때까지 대기
        yield return new WaitUntil(() => !upTween.active);

        // 차지 횟수만큼 차지 반복
        for (int i = 0; i < chargeNum; i++)
        {
            yield return StartCoroutine(DrillCharge());
        }

        print("차지 끝, 돌진 시작");

        // 플레이어 방향
        playerDir = PlayerManager.Instance.transform.position - fistPart.transform.position;

        //TODO 잠깐동안 플레이어 조준, 보스 주위 고정된 거리로 원 그리면서
        //TODO 보스 자체가 플레이어쪽으로 빠르게 이동 Ease.InBack

        //TODO 드릴 고스트 투명해지다 사라지고 스케일 초기화
        //TODO 기존 드릴 색깔 복구
        //TODO 드릴 위치 및 각도 초기화

        //TODO Idle 애니메이션 재생
        //TODO Idle 상태로 전환
    }

    IEnumerator DrillCharge()
    {
        // 각각 드릴 스프라이트 참조
        SpriteRenderer mainDrill = drillSprite;
        SpriteRenderer ghostDrill = drillSprite.transform.Find("DrillGhost").GetComponent<SpriteRenderer>();

        // 현재 드릴 스케일 구하기
        Vector3 nowGhostScale = ghostDrill.transform.localScale;
        //다음 드릴 스케일 지정
        Vector3 upGhostScale = new Vector3(nowGhostScale.x + 1, nowGhostScale.y + 1, 1);

        print(nowGhostScale + " : " + upGhostScale);

        //드릴 차지 시간
        float chargeTime = 0.5f;

        //TODO 기 모으는 이펙트

        // 드릴 차지 시퀀스 (1 ~ 3번 랜덤 차지)
        Sequence chargeSeq = DOTween.Sequence();
        chargeSeq
        .AppendCallback(() =>
        {
            // 고스트 드릴 오브젝트 활성화
            ghostDrill.gameObject.SetActive(true);

            // 드릴 콜라이더 켜기
            drillGhostColl.enabled = true;
        })
        .Append(
            // 반동에 의해 살짝 내려감
            drillPart.DOLocalMove(drillDefaultPos + Vector2.up * 4.5f, 0.2f)
            .SetEase(Ease.OutBack)
        )
        .AppendCallback(() =>
        {
            // 원소 문장 이펙트 발생
            LeanPool.Spawn(elementSymbolEffect, drillSprite.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            //TODO 고스트 드릴 자체에서 이펙트 발생

        })
        .Append(
            //원래 높이로 복구
            drillPart.DOLocalMove(drillDefaultPos + Vector2.up * 5f, chargeTime)
        )
        .Join(
            // 메인 드릴 반투명하게
            mainDrill.DOColor(new Color(1, 1, 1, 0.5f), chargeTime)
        )
        .Join(
            // 고스트 드릴 매우 밝게 나타내기
            ghostDrill.DOColor(new Color(1, 1, 1, 1f), chargeTime)
        )
        .Join(
            // 고스트 드릴 스케일 키우기
            ghostDrill.transform.DOScale(upGhostScale, chargeTime)
        )
        .Join(
            // 그림자 크기 키우기
            drillShadow.transform.DOScale(new Vector3(3f, 1f, 0), chargeTime)
        )
        .Join(
            // 고스트 드릴 색 안정화
            ghostDrill.DOColor(new Color(1, 1, 1, 0.5f), chargeTime)
        );

        // 시퀀스 끝날때까지 대기
        yield return new WaitUntil(() => !chargeSeq.active);
    }
}
