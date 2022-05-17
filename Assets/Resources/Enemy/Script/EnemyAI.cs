using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class EnemyAI : MonoBehaviour
{
    [Header("Enemy")]
    public State state;
    public enum State { Idle, Move, Attack, Hit, Dead, TimeStop, SystemStop }
    public MoveType moveType;
    public enum MoveType
    {
        // 걸어서 등속도이동
        Walk,
        // 시간마다 점프
        Jump,
        // 시간마다 대쉬
        Dash,
        // 시간마다 플레이어 주변 위치로 텔레포트
        Teleport,
        // 플레이어와 일정거리 고정하며 따라다님
        Follow
    };

    [Header("Refer")]
    public EnemyManager enemyManager;

    [Header("Jump")]
    [SerializeField]
    private float jumpHeight = 4f;
    [SerializeField]
    private float jumpUptime = 2f;
    [SerializeField]
    private float jumpCoolTime = 1f;
    public float jumpCoolCount;
    [SerializeField]
    private bool jumpCollisionOff = false; //도약시 충돌 여부
    [SerializeField]
    private GameObject landEffect;
    [SerializeField]
    private Transform shadow;
    // private Vector2 shadowPos; //그림자 초기 위치
    public Sequence jumpSeq;
    public Vector2 jumpLandPos; //점프 착지 위치

    private void Awake()
    {
        enemyManager = enemyManager == null ? GetComponent<EnemyManager>() : enemyManager;

        //그림자 초기 위치
        // if (shadow != null)
        //     shadowPos = shadow.localPosition;
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
        if (enemyManager.anim != null)
            enemyManager.anim.speed = 1f;

        //속도 초기화
        enemyManager.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 콜라이더 충돌 초기화
        enemyManager.coll.isTrigger = false;

        //그림자 위치 초기화
        if (shadow)
            shadow.localPosition = Vector2.zero;
    }

    void Update()
    {
        if (enemyManager.enemy == null)
            return;

        //죽음 애니메이션 중일때
        if (enemyManager.isDead)
        {
            state = State.Dead;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
            enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;

            if (enemyManager.anim != null)
                enemyManager.anim.speed = 0f;

            transform.DOPause();
            jumpSeq.Pause();

            return;
        }

        //전역 타임스케일이 0 일때
        if (SystemManager.Instance.timeScale == 0)
        {
            state = State.SystemStop;

            if (enemyManager.anim != null)
                enemyManager.anim.speed = 0f;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
            transform.DOPause();
            return;
        }

        //시간 정지 디버프일때
        if (enemyManager.stopCount > 0)
        {
            state = State.TimeStop;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
            enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            enemyManager.anim.speed = 0f;

            enemyManager.sprite.material = enemyManager.originMat;
            enemyManager.sprite.color = SystemManager.Instance.stopColor; //시간 멈춤 색깔
            transform.DOPause();

            enemyManager.stopCount -= Time.deltaTime * SystemManager.Instance.timeScale;
            return;
        }

        //맞고 경직일때
        if (enemyManager.hitCount > 0)
        {
            state = State.Hit;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화

            // 머터리얼 및 색 변경
            enemyManager.sprite.material = SystemManager.Instance.hitMat;
            enemyManager.sprite.color = SystemManager.Instance.hitColor;

            enemyManager.hitCount -= Time.deltaTime * SystemManager.Instance.timeScale;
            return;
        }

        //스폰 콜라이더에 닿아 반대편으로 보내질때 잠시대기
        if (enemyManager.oppositeCount > 0)
        {
            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화

            //점프시퀀스 초기화
            if (state == State.Move && jumpSeq.IsActive())
            {
                jumpSeq.Pause();

                //그림자 위치 초기화
                if (shadow)
                    shadow.localPosition = Vector2.zero;
            }

            enemyManager.oppositeCount -= Time.deltaTime * SystemManager.Instance.timeScale;
            return;
        }

        enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
        enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation; // 위치 고정 해제
        enemyManager.sprite.material = enemyManager.originMat;
        enemyManager.sprite.color = enemyManager.originColor;
        transform.DOPlay();
        if (enemyManager.anim != null)
            enemyManager.anim.speed = 1f; //애니메이션 속도 초기화

        //걷는 타입일때
        if (moveType == MoveType.Walk && state == State.Idle)
        {
            Walk();
        }

        //점프 타입일때
        if (moveType == MoveType.Jump && state == State.Idle)
        {
            if (jumpCoolCount <= 0f)
            {
                //점프중 멈췄다면 다시 재생
                if (jumpSeq.IsActive())
                {
                    //전역 타임스케일 적용
                    jumpSeq.timeScale = SystemManager.Instance.timeScale;

                    // 점프 일시정지였으면 시퀀스 재생
                    if (!jumpSeq.IsPlaying())
                        jumpSeq.Play();

                    return;
                }

                // 점프중 아니고 일정 거리 내 들어오면 점프
                Jump();
            }
            else
            {
                jumpCoolCount -= Time.deltaTime;
            }
        }
    }

    void Walk()
    {
        state = State.Move;

        //애니메이터 켜기
        if (enemyManager.anim != null && !enemyManager.anim.enabled)
            enemyManager.anim.enabled = true;

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //해당 방향으로 가속
        enemyManager.rigid.velocity = dir.normalized * enemyManager.speed * SystemManager.Instance.timeScale;

        //움직일 방향에따라 회전
        if (dir.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        state = State.Idle;
    }

    void Jump()
    {
        // print("jump");

        //애니메이터 끄기
        if (enemyManager.anim != null && enemyManager.anim.enabled)
            enemyManager.anim.enabled = false;

        //현재 위치
        Vector2 startPos = transform.position;

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //움직일 거리, 플레이어 위치까지 갈수 있으면 플레이어 위치, 못가면 적 스피드
        float distance = dir.magnitude > enemyManager.range ? enemyManager.range : dir.magnitude;

        //점프 착지 위치
        jumpLandPos = (Vector2)transform.position + dir.normalized * distance;

        //착지 위치에서 최상단 위치
        Vector2 topPos = jumpLandPos + Vector2.up * jumpHeight;

        Ease jumpEase = Ease.OutExpo;

        //점프 속도
        float speed = enemyManager.speed;
        speed = Mathf.Clamp(speed, 0.5f, 10f);

        // float upTime = speed; //올라갈때 시간
        float downTime = speed * 0.1f; //내려갈때 시간

        //움직일 방향에따라 좌우반전
        if (dir.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        //점프 애니메이션
        jumpSeq = DOTween.Sequence();
        jumpSeq
        .OnUpdate(() =>
        {
            //전역 타임스케일 적용
            jumpSeq.timeScale = SystemManager.Instance.timeScale;

            //시간 정지 디버프 중일때
            if (enemyManager.stopCount > 0)
            {
                //애니메이션 멈추기
                if (enemyManager.anim != null)
                    enemyManager.anim.speed = 0f;
                //시퀀스 멈추기
                jumpSeq.Pause();
            }
        })
        .OnStart(() =>
        {
            state = State.Move;
            jumpCoolCount = jumpCoolTime;

            //콜라이더 충돌 끄기
            if (jumpCollisionOff)
                enemyManager.coll.enabled = false;

            // 이펙트 끄기
            if (landEffect)
                landEffect.SetActive(false);
        })
        .Append(
            // 점프 전 납작해지기
            enemyManager.spriteObj.DOScale(new Vector2(1.2f, 0.8f), 0.2f)
        )
        .Append(
            //topPos로 가면서 점프
            transform.DOMove(topPos, jumpUptime)
            .SetEase(jumpEase)
        )
        .Join(
            //위로 길쭉해지기
            enemyManager.spriteObj.DOScale(new Vector2(0.8f, 1.2f), 0.1f)
        )
        .Join(
            //몬스터 올라간만큼 그림자 내리기
            shadow.DOLocalMove(Vector2.down * jumpHeight, jumpUptime)
            .SetEase(jumpEase)
        )
        .AppendCallback(() =>
        {
            //콜라이더 충돌 켜기
            if (jumpCollisionOff)
                enemyManager.coll.enabled = true;

            //현재위치 기준 착지위치 다시 계산
            jumpLandPos = (Vector2)transform.position + Vector2.down * jumpHeight;
        })
        .Append(
            //착지위치까지 내려가기
            transform.DOMove(jumpLandPos, downTime)
        )
        .Join(
            //그림자 올리기
            shadow.DOLocalMove(Vector2.zero, downTime)
        )
        .AppendCallback(() =>
        {
            if (landEffect)
            {
                // 착지 이펙트 스폰
                GameObject effect = LeanPool.Spawn(landEffect, (Vector2)transform.position + Vector2.down * 0.8f, Quaternion.identity);
                //파티클 시간 끝나면 디스폰
                LeanPool.Despawn(effect, effect.GetComponent<ParticleSystem>().main.duration);
            }
        })
        .Join(
            //착지시 납작해지기
            enemyManager.spriteObj.DOScale(new Vector2(1.2f, 0.8f), 0.1f)
        )
        .Append(
            enemyManager.spriteObj.DOScale(new Vector2(1f, 1f), 0.2f)
        )
        .OnComplete(() =>
        {
            state = State.Idle;
        });
        jumpSeq.Restart();
    }
}
