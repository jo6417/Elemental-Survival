using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class EnemyAI : MonoBehaviour
{
    public EnemyInfo enemy;
    public enum MoveType { Walk, Jump, Dash };
    public MoveType moveType;
    public Rigidbody2D rigid;
    float speed;
    EnemyManager enemyManager;
    bool spawned = false; //스폰 완료 여부

    [Header("Refer")]
    SpriteRenderer sprite;
    Collider2D coll;
    public GameObject spawnPortal; //몬스터 등장할 포탈

    [Header("Jump")]
    public GameObject landEffect;
    public Animator anim;
    public Transform shadow;
    public float jumpCoolTime; //쿨타임이 0이 될때마다 점프
    float jumpCoolCount; //쿨타임 카운트하기
    Sequence jumpSeq;

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
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

        //enemy 못찾으면 코루틴 종료
        if (enemy == null)
            yield break;

        speed = enemy.speed;

        //그림자 위치 초기화
        if(shadow)
        shadow.localPosition = Vector2.up * 0.4f;
    }

    void Update()
    {
        if (enemy == null)
            return;

        if (moveType == MoveType.Walk)
            Walk();

        if (moveType == MoveType.Jump)
            JumpMove();
    }

    void Walk()
    {
        rigid.velocity = Vector2.zero; //이동 초기화

        if (enemyManager.hitCount <= 0)
        {
            //색깔 초기화
            sprite.color = Color.white;

            //움직일 방향
            Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

            //해당 방향으로 가속
            rigid.velocity = dir.normalized * speed;

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
        // 맞고나서 경직 시간일때
        else
        {
            rigid.velocity = Vector2.zero;

            // 기동 불가일때 색깔
            sprite.color = Color.gray;

            // 경직 시간 카운트
            enemyManager.hitCount -= Time.deltaTime;
        }
    }

    void JumpMove()
    {
        // 맞고나서 경직 시간일때
        if (enemyManager.hitCount > 0)
        {
            // 적 색깔 변화
            enemyManager.sprite.color = Color.gray;

            // 경직 시간 카운트
            enemyManager.hitCount -= Time.deltaTime;
        }
        // 플레이어쪽으로 점프
        else
        {
            enemyManager.sprite.color = Color.white;

            //점프 시간 됬을때
            if (jumpCoolCount < 0)
            {
                //점프 실행
                Jump();

                //쿨타임 초기화
                jumpCoolCount = jumpCoolTime;
            }
            else
            {
                rigid.velocity = Vector2.zero;

                //시간 멈췄을때는 리턴
                if (rigid.constraints == RigidbodyConstraints2D.FreezeAll)
                    return;

                //쿨타임 감소
                jumpCoolCount -= Time.deltaTime;

                //시퀀스 멈춰있으면 플레이하기
                if (jumpSeq != null && jumpSeq.active && !jumpSeq.IsPlaying())
                {
                    anim.speed = 1f;
                    jumpSeq.Play();
                }
            }
        }
    }

    void Jump()
    {
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
            //몬스터 죽으면 시퀀스 중단
            if (!gameObject.activeSelf)
                jumpSeq.Kill();

            if (rigid.constraints == RigidbodyConstraints2D.FreezeAll)
            {
                //애니메이션 멈추기
                anim.speed = 0f;
                //트윈 멈추기
                jumpSeq.Pause();
            }
        })
        .OnStart(() =>
        {
            //콜라이더 끄기
            coll.isTrigger = true;

            //TODO 세로로 길어지는 애니메이션

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
            shadow.DOLocalMove(Vector2.up * 0.4f + Vector2.down * height, upTime)
            .SetEase(jumpEase)
        )
        .Append(
            //착지위치까지 가기
            transform.DOMove(endPos, downTime)
        // .SetEase(jumpEase)
        )
        .Join(
            //그림자 올리기
            shadow.DOLocalMove(Vector2.up * 0.4f, downTime)
        // .SetEase(jumpEase)
        )
        .OnComplete(() =>
        {
            //콜라이더 켜기
            coll.isTrigger = false;

            //TODO 착지하며 보잉보잉하는 애니메이션
            // 이펙트 켜기
            landEffect.SetActive(true);
        });
    }
}
