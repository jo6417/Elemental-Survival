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
    public EnemyInfo enemy;
    private float speed;
    public bool stopMove; //true가 되면 이동 하지 않음

    [Header("Refer")]
    private EnemyManager enemyManager;
    private Rigidbody2D rigid;
    private SpriteRenderer sprite;
    public Collider2D coll;
    public Animator anim;

    [Header("Jump")]
    [SerializeField]
    private bool nowJumping;
    [SerializeField]
    private float jumpHeight = 4f;
    [SerializeField]
    private float jumpUptime = 2f;
    [SerializeField]
    private float jumpDelay = 1f;
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
        enemyManager = GetComponent<EnemyManager>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        coll = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>();
        rigid = GetComponentInChildren<Rigidbody2D>();

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
        sprite.color = Color.white; //스프라이트 색깔 초기화
        rigid.velocity = Vector2.zero; //속도 초기화

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (enemy == null)
            enemy = enemyManager.enemy;

        transform.DOKill();
        jumpSeq.Kill();

        //애니메이션 스피드 초기화
        if (anim != null)
            anim.speed = 1f;

        // 위치 고정 해제
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //스피드 초기화
        speed = enemy.speed;

        //그림자 위치 초기화
        if (shadow)
            shadow.localPosition = Vector2.zero;

        // 콜라이더 충돌 초기화
        coll.isTrigger = false;
    }

    void Update()
    {
        if (enemy == null)
            return;

        //죽음 애니메이션 중일때
        if (enemyManager.isDead)
        {
            state = State.Dead;

            rigid.velocity = Vector2.zero; //이동 초기화
            rigid.constraints = RigidbodyConstraints2D.FreezeAll;

            if (anim != null)
                anim.speed = 0f;

            transform.DOPause();
            jumpSeq.Pause();

            return;
        }

        //전역 타임스케일이 0 일때
        if (SystemManager.Instance.timeScale == 0)
        {
            state = State.SystemStop;

            if (anim != null)
                anim.speed = 0f;

            rigid.velocity = Vector2.zero; //이동 초기화
            transform.DOPause();
            return;
        }

        //시간 정지 디버프일때
        if (enemyManager.stopCount > 0)
        {
            state = State.TimeStop;

            rigid.velocity = Vector2.zero; //이동 초기화
            rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            anim.speed = 0f;

            sprite.material = enemyManager.originMat;
            sprite.color = SystemManager.Instance.stopColor; //시간 멈춤 색깔
            transform.DOPause();

            enemyManager.stopCount -= Time.deltaTime * SystemManager.Instance.timeScale;
            return;
        }

        //맞고 경직일때
        if (enemyManager.hitCount > 0)
        {
            state = State.Hit;

            rigid.velocity = Vector2.zero; //이동 초기화

            // 머터리얼 및 색 변경
            sprite.material = SystemManager.Instance.hitMat;
            sprite.color = SystemManager.Instance.hitColor;

            enemyManager.hitCount -= Time.deltaTime * SystemManager.Instance.timeScale;
            return;
        }

        state = State.Idle;

        //스폰 콜라이더에 닿아 반대편으로 보내질때 잠시대기
        if (enemyManager.oppositeCount > 0)
        {
            rigid.velocity = Vector2.zero; //이동 초기화

            //점프시퀀스 초기화
            if (nowJumping && jumpSeq.IsActive())
            {
                jumpSeq.Pause();
                nowJumping = false;

                //그림자 위치 초기화
                if (shadow)
                    shadow.localPosition = Vector2.zero;
            }

            enemyManager.oppositeCount -= Time.deltaTime * SystemManager.Instance.timeScale;
            return;
        }

        rigid.velocity = Vector2.zero; //이동 초기화
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation; // 위치 고정 해제
        sprite.material = enemyManager.originMat;
        sprite.color = enemyManager.originColor;
        transform.DOPlay();
        if (anim != null)
            anim.speed = 1f; //애니메이션 속도 초기화

        // Idle 상태고 stopMove가 false일때
        if (state == State.Idle && !stopMove)
        {
            state = State.Move;

            if (moveType == MoveType.Walk)
            {
                Walk();
            }
            else if (moveType == MoveType.Jump)
            {
                //플레이어 사이 거리
                float dis = Vector2.Distance(transform.position, PlayerManager.Instance.transform.position);

                //점프중 멈췄다면 다시 재생
                if (nowJumping && jumpSeq.IsActive())
                {
                    //전역 타임스케일 적용
                    jumpSeq.timeScale = SystemManager.Instance.timeScale;

                    // 점프 일시정지였으면 시퀀스 재생
                    if (!jumpSeq.IsPlaying())
                        jumpSeq.Play();

                    return;
                }

                // 점프중 아니고 일정 거리 내 들어오면 점프
                if (!nowJumping)
                {
                    Jump();

                    // print("jump");
                }
            }
        }
    }

    void Walk()
    {
        //애니메이터 켜기
        if (anim != null && !anim.enabled)
            anim.enabled = true;

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //해당 방향으로 가속
        rigid.velocity = dir.normalized * speed * SystemManager.Instance.timeScale;

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
        nowJumping = true;

        //애니메이터 끄기
        if (anim != null && anim.enabled)
            anim.enabled = false;

        // rigid.velocity = Vector2.zero;

        //현재 위치
        Vector2 startPos = transform.position;

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //움직일 거리, 플레이어 위치까지 갈수 있으면 플레이어 위치, 못가면 적 스피드
        float distance = dir.magnitude > enemy.range ? enemy.range : dir.magnitude;

        //점프 착지 위치
        jumpLandPos = (Vector2)transform.position + dir.normalized * distance;

        //착지 위치에서 최상단 위치
        Vector2 topPos = jumpLandPos + Vector2.up * jumpHeight;

        Ease jumpEase = Ease.OutExpo;

        //점프 속도
        float speed = enemy.speed;
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
                if (anim != null)
                    anim.speed = 0f;
                //시퀀스 멈추기
                jumpSeq.Pause();
                // transform.DOPause();
            }
        })
        .OnStart(() =>
        {
            //콜라이더 충돌 끄기
            if (jumpCollisionOff)
                coll.isTrigger = true;

            // 이펙트 끄기
            if (landEffect)
                landEffect.SetActive(false);
        })
        .Append(
            // 점프 전 납작해지기
            transform.DOScale(new Vector2(1.2f, 0.8f), 0.2f)
        )
        .Append(
            //topPos로 가면서 점프
            transform.DOMove(topPos, jumpUptime)
            .SetEase(jumpEase)
        )
        .Join(
            //위로 길쭉해지기
            transform.DOScale(new Vector2(0.8f, 1.2f), 0.1f)
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
                coll.isTrigger = false;

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
            transform.DOScale(new Vector2(1.2f, 0.8f), 0.1f)
        )
        .AppendInterval(jumpDelay) //점프 후 딜레이
        .Join(
            transform.DOScale(new Vector2(1f, 1f), 0.2f)
        )
        .OnComplete(() =>
        {
            nowJumping = false;

            state = State.Idle;
        });
        jumpSeq.Restart();
    }
}
