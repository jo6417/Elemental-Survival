using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class EnemyAI : MonoBehaviour
{
    [Header("Enemy")]
    public NowState nowState;
    public enum NowState { Idle, SystemStop, Dead, Hit, TimeStop, Walk, Jump }
    public MoveType moveType;
    public enum MoveType { Walk, Jump, Dash, Slide };
    private EnemyInfo enemy;
    private float speed;

    [Header("Refer")]
    private EnemyManager enemyManager;
    private Rigidbody2D rigid;
    private SpriteRenderer sprite;
    private Collider2D coll;
    private Animator anim;

    [Header("Jump")]
    [SerializeField]
    private bool nowJumping;
    [SerializeField]
    private float jumpStartDistance = 10f;
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

        //시간 멈춤 확인
        StartCoroutine(StopCheck());
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
            nowState = NowState.Dead;

            rigid.velocity = Vector2.zero; //이동 초기화
            rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            anim.speed = 0f;

            transform.DOPause();
            jumpSeq.Pause();

            return;
        }

        //전역 타임스케일이 0 일때
        if (VarManager.Instance.timeScale == 0)
        {
            nowState = NowState.SystemStop;

            rigid.velocity = Vector2.zero; //이동 초기화
            anim.speed = 0f;
            transform.DOPause();
            return;
        }

        //시간 정지 디버프일때
        if (enemyManager.stopCount > 0)
        {
            nowState = NowState.TimeStop;

            rigid.velocity = Vector2.zero; //이동 초기화
            rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            anim.speed = 0f;

            sprite.material = enemyManager.originMat;
            sprite.color = VarManager.Instance.stopColor; //시간 멈춤 색깔
            transform.DOPause();

            enemyManager.stopCount -= Time.deltaTime * VarManager.Instance.timeScale;
            return;
        }

        //맞고 경직일때
        if (enemyManager.hitCount > 0)
        {
            nowState = NowState.Hit;

            rigid.velocity = Vector2.zero; //이동 초기화

            // 머터리얼 및 색 변경
            sprite.material = VarManager.Instance.hitMat;
            sprite.color = VarManager.Instance.hitColor;

            enemyManager.hitCount -= Time.deltaTime * VarManager.Instance.timeScale;
            return;
        }

        nowState = NowState.Idle;

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

            enemyManager.oppositeCount -= Time.deltaTime * VarManager.Instance.timeScale;
            return;
        }

        rigid.velocity = Vector2.zero; //이동 초기화
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation; // 위치 고정 해제
        sprite.material = enemyManager.originMat;
        sprite.color = enemyManager.originColor;
        anim.speed = 1f; //애니메이션 속도 초기화
        transform.DOPlay();

        if (moveType == MoveType.Walk)
        {
            Walk();
        }

        if (moveType == MoveType.Jump)
        {
            //플레이어 사이 거리
            float dis = Vector2.Distance(transform.position, PlayerManager.Instance.transform.position);

            //점프중 멈췄다면 다시 재생
            if (nowJumping && jumpSeq.IsActive())
            {
                //전역 타임스케일 적용
                jumpSeq.timeScale = VarManager.Instance.timeScale;

                // 점프 일시정지였으면 시퀀스 재생
                if (!jumpSeq.IsPlaying())
                    jumpSeq.Play();

                return;
            }

            // 점프중 아니고 일정 거리 내 들어오면 점프
            if (!nowJumping && dis < jumpStartDistance)
                Jump();
            else
            {
                //멀면 걸어가기
                Walk();
            }
        }

        // // 전체 슬로우일때
        // if (VarManager.Instance.timeScale < 1)
        // {
        //     //시간 멈춤 색깔
        //     sprite.color = EnemySpawn.Instance.stopColor;

        //     // rigid.velocity = Vector2.zero;
        //     anim.speed = VarManager.Instance.timeScale;
        //     jumpSeq.timeScale = VarManager.Instance.timeScale;
        // }
    }

    void Walk()
    {
        nowState = NowState.Walk;

        //애니메이터 켜기
        if (!anim.enabled)
            anim.enabled = true;

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //해당 방향으로 가속
        rigid.velocity = dir.normalized * speed * VarManager.Instance.timeScale;

        //움직일 방향에따라 회전
        if (dir.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    void Jump()
    {
        nowState = NowState.Jump;

        nowJumping = true;

        //애니메이터 끄기
        if (anim.enabled)
            anim.enabled = false;

        // rigid.velocity = Vector2.zero;

        //현재 위치
        Vector2 startPos = transform.position;

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //움직일 거리, 플레이어 위치까지 갈수 있으면 플레이어 위치, 못가면 적 스피드
        float distance = dir.magnitude > enemy.range ? enemy.range : dir.magnitude;

        //점프 착지 위치
        Vector2 endPos = (Vector2)transform.position + dir.normalized * distance;

        //착지 위치에서 최상단 위치
        Vector2 topPos = endPos + Vector2.up * jumpHeight;

        Ease jumpEase = Ease.OutCirc;

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
            jumpSeq.timeScale = VarManager.Instance.timeScale;

            //시간 정지 디버프 중일때
            if (enemyManager.stopCount > 0)
            {
                //애니메이션 멈추기
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

            //TODO 납작해졌다가 세로로 길어지는 애니메이션

            // 이펙트 끄기
            if (landEffect)
                landEffect.SetActive(false);
        })
        .Append(
            transform.DOScale(new Vector2(1.2f, 0.8f), 0.2f)
        )
        .Append(
            //움직일 거리 절반 만큼 가면서 점프
            transform.DOMove(topPos, jumpUptime)
            .SetEase(jumpEase)
        )
        .Join(
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
            endPos = (Vector2)transform.position + Vector2.down * jumpHeight;
        })
        .Append(
            //착지위치까지 내려가기
            transform.DOMove(endPos, downTime)
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
            transform.DOScale(new Vector2(1.2f, 0.8f), 0.1f)
        )
        .AppendInterval(jumpDelay) //점프 후 딜레이
        .Join(
            transform.DOScale(new Vector2(1f, 1f), 0.2f)
        )
        .OnComplete(() =>
        {
            nowJumping = false;
        });
        jumpSeq.Restart();
    }

    IEnumerator StopCheck()
    {
        while (gameObject.activeSelf)
        {
            if (VarManager.Instance.playerTimeScale == 0)
            {
                //애니메이션 멈춤
                if (anim != null)
                    anim.speed = 0f;
            }
            else
            {
                //살아있을때, 정지 디버프 아닐때
                if (!enemyManager.isDead && enemyManager.stopCount <= 0)
                {
                    //애니메이션 재시작
                    if (anim != null)
                        anim.speed = 1f;
                }
            }
            yield return null;
        }
    }
}
