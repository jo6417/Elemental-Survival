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
    public Vector2 handDefaultPos; //손의 기본 위치
    public Vector2 drillDefaultPos; //드릴의 기본 위치
    Vector2 playerDir;

    [Header("Refer")]
    public TextMeshProUGUI stateText; //! 테스트 현재 상태
    public EnemyManager enemyManager;
    public Transform headPart;
    public SpriteRenderer leftHandPart;
    public Sprite emptyHandSprite;
    public Sprite openHandSprite;
    public Sprite grabHandSprite;
    public Transform drillHandPart;
    public GameObject stonePrefab; // 공격시 던질 돌 프리팹

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
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
    }

    void Update()
    {
        if (enemyManager.enemy == null)
            return;

        //상태 관리
        ManageState();

        //행동 관리
        ManageAction();
    }

    void ManageState()
    {
        //죽음 애니메이션 중일때
        if (enemyManager.isDead)
        {
            enemyManager.state = EnemyManager.State.Dead;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
            enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;

            if (enemyManager.animList.Count > 0)
            {
                foreach (Animator anim in enemyManager.animList)
                {
                    anim.speed = 0f;
                }
            }

            transform.DOPause();

            return;
        }

        //전역 타임스케일이 0 일때
        if (SystemManager.Instance.globalTimeScale == 0)
        {
            enemyManager.state = EnemyManager.State.MagicStop;

            // 애니메이션 멈추기
            if (enemyManager.animList.Count > 0)
            {
                foreach (Animator anim in enemyManager.animList)
                {
                    anim.speed = 0f;
                }
            }

            // 이동 멈추기
            enemyManager.rigid.velocity = Vector2.zero;

            transform.DOPause();
            return;
        }

        // 멈춤 디버프일때
        if (enemyManager.stopCount > 0)
        {
            enemyManager.state = EnemyManager.State.TimeStop;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
            enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            // 애니메이션 멈추기
            if (enemyManager.animList.Count > 0)
            {
                foreach (Animator anim in enemyManager.animList)
                {
                    anim.speed = 0f;
                }
            }

            foreach (SpriteRenderer sprite in enemyManager.spriteList)
            {
                sprite.material = enemyManager.originMat;
                sprite.color = SystemManager.Instance.stopColor; //시간 멈춤 색깔
            }
            transform.DOPause();

            enemyManager.stopCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;
            return;
        }

        //맞고 경직일때
        if (enemyManager.hitCount > 0)
        {
            enemyManager.state = EnemyManager.State.Hit;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화

            // 머터리얼 및 색 변경
            foreach (SpriteRenderer sprite in enemyManager.spriteList)
            {
                sprite.material = SystemManager.Instance.hitMat;
                sprite.color = SystemManager.Instance.hitColor;
            }

            enemyManager.hitCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;
            return;
        }

        //스폰 콜라이더에 닿아 반대편으로 보내질때 잠시대기
        if (enemyManager.oppositeCount > 0)
        {
            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화

            enemyManager.oppositeCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;
            return;
        }

        //모든 문제 없으면 idle 상태로 전환
        enemyManager.state = EnemyManager.State.Idle;

        // rigid, sprite, 트윈, 애니메이션 상태 초기화
        foreach (SpriteRenderer sprite in enemyManager.spriteList)
        {
            sprite.material = enemyManager.originMat;
            sprite.color = enemyManager.originColor;
        }
        transform.DOPlay();

        // 애니메이션 속도 초기화
        if (enemyManager.animList.Count > 0)
        {
            foreach (Animator anim in enemyManager.animList)
            {
                anim.speed = 1f;
            }
        }
    }

    void ManageAction()
    {
        // Idle 아니면 리턴
        if (enemyManager.nowAction != EnemyManager.Action.Idle)
            return;

        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // 플레이어와의 거리
        float distance = (PlayerManager.Instance.transform.position - transform.position).magnitude;

        stateText.text = " : " + distance;

        //TODO 먼 거리일때
        if (distance > farDistance)
        {
            stateText.text = "Far : " + distance;

            // 플레이어 따라가기
            Walk();
        }
        else
        {
            stateText.text = "Close : " + distance;

            // 양쪽 손 회전값 초기화
            leftHandPart.transform.rotation = Quaternion.Euler(0, 0, 90);
            drillHandPart.rotation = Quaternion.Euler(0, 0, 90);

            // 속도 초기화
            enemyManager.rigid.velocity = Vector3.zero;

            //공격 패턴 결정하기
            ChooseAttack();
        }
    }

    void Walk()
    {
        enemyManager.nowAction = EnemyManager.Action.Walk;

        // Idle 애니메이션으로 전환
        enemyManager.animList[0].SetBool("UseHand", false);
        enemyManager.animList[0].SetBool("UseDrill", false);

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        // 움직일 방향 2D 각도
        float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 가려는 방향으로 양쪽 손이 회전
        leftHandPart.transform.rotation = Quaternion.Euler(0, 0, rotation);
        drillHandPart.rotation = Quaternion.Euler(0, 0, rotation);

        //해당 방향으로 가속
        enemyManager.rigid.velocity = dir.normalized * enemyManager.speed * SystemManager.Instance.globalTimeScale;

        // 움직일 방향에따라 머리만 Y축 회전
        if (dir.x > 0)
        {
            headPart.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            headPart.rotation = Quaternion.Euler(0, 180, 0);
        }

        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    void ChooseAttack()
    {
        // 현재 액션 변경
        enemyManager.nowAction = EnemyManager.Action.Attack;

        // 랜덤 패턴 결정
        int randomNum = Random.Range(0, 5);

        // print("randomNum : " + randomNum);

        //! 테스트를 위해 패턴 고정
        randomNum = 1;

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
                break;
            case 4:
                //TODO 드릴 추적 패턴
                break;
        }
    }

    IEnumerator StoneThrow()
    {
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
            leftHandPart.sprite = openHandSprite;
        })
        .Append(
            // 바위 위치로 손이 이동
            leftHandPart.transform.parent.DOLocalMove(grabPos, grabTime)
        )
        .Join(
            // 아래로 손 내려가기
            leftHandPart.transform.DOLocalMove(Vector3.up * 2f, grabTime)
        )
        .Join(
            // 잡는 위치 방향으로 회전
            leftHandPart.transform.DORotate(new Vector3(0, 0, rotation), grabTime)
        )
        .AppendCallback(() =>
        {
            // 바위 집은 손으로 변경
            leftHandPart.sprite = grabHandSprite;
        })
        // 잠시 대기
        .AppendInterval(0.5f)
        .Append(
            // 던지기 로컬 위치로 이동
            leftHandPart.transform.parent.DOLocalMove(new Vector2(10f, 5f), 1f)
        )
        .Join(
            // 위로 손 올라가기
            leftHandPart.transform.DOLocalMove(Vector3.up * 5f, 1f)
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
            playerDir = PlayerManager.Instance.transform.position - leftHandPart.transform.position;

            // 플레이어 방향 2D 각도
            playerAngle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;

            stateText.text = "Targeting : " + followCount;

            // 플레이어 방향으로 손이 회전
            leftHandPart.transform.rotation = Quaternion.Euler(0, 0, playerAngle);

            //TODO 조준 레이저 라인 렌더링

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 돌 발사 위치
        Vector2 throwPos = leftHandPart.transform.position + Quaternion.AngleAxis(playerAngle, Vector3.forward) * Vector3.right * 7f;

        //던지기 시작 각도
        float startAngle = playerAngle + 89f;
        //던지기 종료 각도
        float endAngle = playerAngle - 89f;

        // 던지기 시작 위치
        Vector2 startPos = leftHandPart.transform.position + Quaternion.AngleAxis(startAngle, Vector3.forward) * Vector3.right * 7f;
        // 던지기 종료 위치
        Vector2 endPos = leftHandPart.transform.position + Quaternion.AngleAxis(endAngle, Vector3.forward) * Vector3.right * 7f;

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
            leftHandPart.transform.parent.DOMove(startPos, 0.5f)
        )
        .Join(
            //던지기 전 각도로 회전
            leftHandPart.transform.DORotate(new Vector3(0, 0, startAngle), 0.5f)
        )
        .AppendCallback(() =>
        {
            //돌 날리기
            StartCoroutine(ShotStone());
        })
        .Join(
            //포물선 그리며 손 휘두르기
            leftHandPart.transform.parent.DOPath(parabola, swingTime, PathType.CatmullRom, PathMode.TopDown2D, 10, Color.red)
        // .SetEase(Ease.OutBack)
        )
        .Join(
            // 던진 후 각도로 회전
            leftHandPart.transform.DORotate(new Vector3(0, 0, endAngle), swingTime)
        );

        // 던지기 시퀀스 끝날때까지 대기
        yield return new WaitUntil(() => !throwSeq.active);

        // 손 위치 기본 위치로 이동
        leftHandPart.transform.parent.DOLocalMove(handDefaultPos, 0.5f)
        .OnComplete(() =>
        {
            // Idle 액션으로 전환
            enemyManager.nowAction = EnemyManager.Action.Idle;

            // Idle 애니메이션으로 전환
            enemyManager.animList[0].SetBool("UseHand", false);
        });
    }

    IEnumerator ShotStone()
    {
        // 손을 절반 휘두르는 만큼 대기
        yield return new WaitForSeconds(0.1f);

        //중간에 돌 던지기
        GameObject stone = LeanPool.Spawn(stonePrefab, leftHandPart.transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

        // 타겟 및 타겟위치 설정하기
        MagicHolder magicHolder = stone.GetComponent<MagicHolder>();
        magicHolder.SetTarget(MagicHolder.Target.Player);
        magicHolder.targetPos = PlayerManager.Instance.transform.position;

        // 플레이어 방향 다시 계산
        playerDir = PlayerManager.Instance.transform.position - leftHandPart.transform.position;

        print("playerDir : " + playerDir);

        Rigidbody2D stoneRigid = stone.GetComponent<Rigidbody2D>();
        // 돌 회전시키기
        stoneRigid.angularVelocity = Random.value * -playerDir.x * 20f;
        //돌 날리기
        // stoneRigid.velocity = playerDir.normalized * 20f;

        // 손바닥으로 변경
        leftHandPart.sprite = openHandSprite;
    }

    IEnumerator DrillDash()
    {
        yield return null;

        //TODO 머리보다 높게 드릴 들기
        //TODO 드릴 스핀 애니메이션 재생

        //TODO 랜덤 차지 횟수 정하기
        //TODO 드릴을 살짝 올렸다 강하게 내려놓으며
        //TODO 기존 드릴 반투명하게
        //TODO 드릴 고스트 HDR 색 강해졌다가 돌아오기
        //TODO 드릴 고스트 활성화 및 doscale
        //TODO 드릴 고스트 콜라이더에 에너미어택 추가

        //TODO 잠깐동안 플레이어 조준, 보스 주위 고정된 거리로 원 그리면서
        //TODO 보스 자체가 플레이어쪽으로 빠르게 이동 Ease.InBack

        //TODO 드릴 고스트 투명해지다 사라지고 스케일 초기화
        //TODO 기존 드릴 색깔 복구
        //TODO 드릴 위치 및 각도 초기화

        //TODO Idle 애니메이션 재생
        //TODO Idle 상태로 전환
    }
}
