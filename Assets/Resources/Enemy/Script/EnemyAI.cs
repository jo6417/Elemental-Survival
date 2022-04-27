using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, SystemStop, Dead, Hit, TimeStop, Walk, Jump }
    public EnemyState enemyState;

    public EnemyInfo enemy;
    public enum MoveType { Walk, Jump, Dash };
    public MoveType moveType;
    public Rigidbody2D rigid;
    float speed;
    EnemyManager enemyManager;

    [Header("Refer")]
    SpriteRenderer sprite;
    Collider2D coll;

    [Header("Jump")]
    public GameObject landEffect;
    public Animator anim;
    public Transform shadow;
    Vector2 shadowPos; //그림자 초기 위치
    Sequence jumpSeq;
    public bool isJumping;

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        coll = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>();

        //그림자 초기 위치
        if (shadow != null)
            shadowPos = shadow.localPosition;
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
            shadow.localPosition = shadowPos;

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
            enemyState = EnemyState.Dead;

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
            enemyState = EnemyState.SystemStop;

            rigid.velocity = Vector2.zero; //이동 초기화
            anim.speed = 0f;
            transform.DOPause();
            return;
        }

        //시간 정지 디버프일때
        if (enemyManager.stopCount > 0)
        {
            enemyState = EnemyState.TimeStop;

            rigid.velocity = Vector2.zero; //이동 초기화
            rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            anim.speed = 0f;

            sprite.material = enemyManager.originMat;
            sprite.color = EnemySpawn.Instance.stopColor; //시간 멈춤 색깔
            transform.DOPause();

            enemyManager.stopCount -= Time.deltaTime * VarManager.Instance.timeScale;
            return;
        }

        //맞고 경직일때
        if (enemyManager.hitCount > 0)
        {
            enemyState = EnemyState.Hit;

            rigid.velocity = Vector2.zero; //이동 초기화

            // 머터리얼 및 색 변경
            sprite.material = EnemySpawn.Instance.hitMat;
            sprite.color = EnemySpawn.Instance.hitColor;

            enemyManager.hitCount -= Time.deltaTime * VarManager.Instance.timeScale;
            return;
        }

        enemyState = EnemyState.Idle;

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
            if (isJumping && jumpSeq.IsActive())
            {
                //전역 타임스케일 적용
                jumpSeq.timeScale = VarManager.Instance.timeScale;
                //시퀀스 재실행
                jumpSeq.Play();
                // transform.DOPlay();
                return;
            }

            // 점프중 아니고 일정 거리 내 들어오면 점프
            if (!isJumping && dis < 10f)
                Jump();
            else
                //멀면 걸어가기
                Walk();
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
        enemyState = EnemyState.Walk;

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
        enemyState = EnemyState.Jump;

        isJumping = true;

        // rigid.velocity = Vector2.zero;

        //현재 위치
        Vector2 startPos = transform.position;

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //움직일 거리, 플레이어 위치까지 갈수 있으면 플레이어 위치, 못가면 적 스피드
        float distance = dir.magnitude > enemy.range ? enemy.range : dir.magnitude;

        //점프 착지 위치
        Vector2 endPos = (Vector2)transform.position + dir.normalized * distance;

        //점프해서 올라갈 최고점 위치, 착지위치까지 거리 절반 + 위로 올라가기
        float height = 4f; //올라갈 높이
        Vector2 topPos = endPos + Vector2.up * height;

        Ease jumpEase = Ease.OutCirc;

        //점프 속도
        float speed = enemy.speed;
        speed = Mathf.Clamp(speed, 0.5f, 10f);

        float upTime = speed; //올라갈때 시간
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
            coll.isTrigger = true;

            //TODO 납작해졌다가 세로로 길어지는 애니메이션

            // 이펙트 끄기
            landEffect.SetActive(false);
        })
        .Append(
            //움직일 거리 절반 만큼 가면서 점프
            transform.DOMove(topPos, upTime)
            .SetEase(jumpEase)
        )
        .Join(
            //몬스터 올라간만큼 그림자 내리기
            shadow.DOLocalMove(shadowPos + Vector2.down * height, upTime)
            .SetEase(jumpEase)
        )
        .AppendCallback(() =>
        {
            //콜라이더 충돌 켜기
            coll.isTrigger = false;

            //현재위치 기준 착지위치 다시 계산
            endPos = (Vector2)transform.position + Vector2.down * height;
        })
        .Append(
            //착지위치까지 내려가기
            transform.DOMove(endPos, downTime)
        )
        .Join(
            //그림자 올리기
            shadow.DOLocalMove(shadowPos, downTime)
        )
        .AppendCallback(() =>
        {
            // 착지 이펙트 스폰
            GameObject effect = LeanPool.Spawn(landEffect, (Vector2)transform.position + Vector2.down * 0.8f, Quaternion.identity);
            //파티클 시간 끝나면 디스폰
            LeanPool.Despawn(effect, effect.GetComponent<ParticleSystem>().main.duration);
        })
        //점프 후 딜레이
        .SetDelay(1f)
        .OnComplete(() =>
        {
            isJumping = false;
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
