using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class KingSlimeAI : MonoBehaviour
{
    [Header("Enemy")]
    public float babyCoolCount; // 새끼 슬라임 소환 쿨타임
    public float absorbCoolCount; //체력 흡수 쿨타임
    bool isInside; //슬라임 내부에 플레이어 있는지
    // public float atkRange = 20f;
    public float atkRatio = 0.5f; //이동 멈추고 공격할 확률
    EnemyManager enemyManager;
    public State state;
    State nextAction = State.Idle; //다음에 할 행동 예약
    public enum State { Idle, Move, Attack, Hit, Dead, TimeStop, SystemStop }

    [Header("Refer")]
    public Transform crownObj;
    // public GameObject slimeIcon; //새끼 슬라임 아이콘
    public GameObject slimePrefab; //새끼 슬라임 프리팹
    public AtkRangeTrigger babyTrigger; //새끼 슬라임 소환 범위

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

    private void Update()
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

        //점프 타입일때
        if (state == State.Idle)
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

        //보스안에 플레이어 들어왔을때
        if (isInside)
        {
            // 플레이어 이동속도 디버프
            PlayerManager.Instance.speedDebuff = 0.1f;
        }
        else
        {
            // 플레이어 이동속도 디버프 해제
            PlayerManager.Instance.speedDebuff = 1f;
        }

        // Idle 상태일때, 다음 행동도 Idle이면
        if (state == State.Idle && nextAction == State.Idle)
        {
            // 랜덤 숫자가 atkRatio 보다 적으면 
            if (Random.value < atkRatio)
            {
                // 다음 행동 Attack 으로 예약
                nextAction = State.Attack;
            }
        }

        // Idle 상태일때, 다음 행동이 Attack 이면
        if (state == State.Idle && nextAction == State.Attack)
        {
            //공격 패턴 선택
            ChooseAttack();
        }
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
        .Join(
            crownObj.DOLocalMove(new Vector2(0, -1), 0.2f)
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
            crownObj.DOLocalMove(new Vector2(0, 1), 0.2f)
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
                GameObject effect = LeanPool.Spawn(landEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
                //파티클 시간 끝나면 디스폰
                LeanPool.Despawn(effect, effect.GetComponent<ParticleSystem>().main.duration);
            }
        })
        .Join(
            //착지시 납작해지기
            enemyManager.spriteObj.DOScale(new Vector2(1.2f, 0.8f), 0.1f)
        )
        .Join(
            crownObj.DOLocalMove(new Vector2(0, -1), 0.2f)
        )
        .Append(
            enemyManager.spriteObj.DOScale(new Vector2(1f, 1f), 0.2f)
        )
        .Join(
            crownObj.DOLocalMove(new Vector2(0, 0), 0.2f)
        )
        .OnComplete(() =>
        {
            state = State.Idle;
        });
        jumpSeq.Restart();
    }

    void ChooseAttack()
    {
        // print("공격 패턴 선택");

        if (babyTrigger.atkTrigger)
        {
            //공격 상태로 전환
            state = State.Attack;

            StartCoroutine(BabySlimeSummon());
        }
    }

    IEnumerator BabySlimeSummon()
    {
        print("슬라임 소환 패턴");

        Sequence babySeq = DOTween.Sequence();
        babySeq
        .Append(
        // 납작해지기
        enemyManager.spriteObj.DOScale(new Vector2(1.2f, 0.8f), 0.2f)
        )
        .Join(
            crownObj.DOLocalMove(new Vector2(0, -1), 0.2f)
        )
        .Append(
        // 좌우로 부들부들 떨기
        transform.DOPunchPosition(Vector2.right, 2f)
        .SetEase(Ease.InOutFlash, 20, -1)
        )
        .AppendCallback(() =>
        {
            // 보잉거리는 anim 켜기
            enemyManager.anim.enabled = true;
            enemyManager.anim.speed = 1f;
        });

        // 애니메이터 활성화까지 대기
        yield return new WaitUntil(() => enemyManager.anim.enabled);

        // 남은 체력에 비례해서 소환 횟수 산출, 5~15마리
        int summonNum = 5 + Mathf.RoundToInt(10 * (enemyManager.hpMax - enemyManager.HpNow) / enemyManager.hpMax);
        for (int i = 0; i < summonNum; i++)
        {
            //슬라임 아이콘 소환
            GameObject babySlime = LeanPool.Spawn(slimePrefab, transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

            //컴포넌트 초기화
            // Collider2D babyColl = babySlime.GetComponent<Collider2D>();
            EnemyManager babyEnemyManager = babySlime.GetComponent<EnemyManager>();
            EnemyAI babyEnemyAI = babySlime.GetComponent<EnemyAI>();

            babyEnemyManager.coll.enabled = false;
            babyEnemyManager.enabled = false;
            babyEnemyAI.enabled = false;

            //소환 위치
            Vector2 summonPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * 20f;

            // babyTrigger 범위내 랜덤 포지션으로 슬라임 아이콘 dojump 시키기
            babySlime.transform.DOJump(summonPos, 5f, 1, 1f)
            .OnStart(() =>
            {
                print("dojump");
            })
            .OnComplete(() =>
            {
                //컴포넌트 활성화
                babyEnemyManager.coll.enabled = true;
                babyEnemyManager.enabled = true;
                babyEnemyAI.enabled = true;
            });

            //소환 딜레이
            yield return new WaitForSeconds(0.3f);
        }

        //애니메이션 끄기
        enemyManager.anim.enabled = false;

        // 천천히 추욱 쳐지기
        enemyManager.spriteObj.DOScale(new Vector2(1.2f, 0.7f), 2f)
        .OnStart(() => {
            crownObj.DOLocalMove(new Vector2(0, -2), 2f);
        })
        .OnComplete(() =>
        {
            // 스케일 복구
            enemyManager.spriteObj.DOScale(Vector2.one, 0.2f)
            .OnStart(() => {
            crownObj.DOLocalMove(new Vector2(0, 0), 0.2f);
        })
            .SetEase(Ease.InOutBack)
            .OnComplete(() =>
            {
                // 보잉거리는 anim 끄기
                enemyManager.anim.enabled = false;

                // Idle 상태로 전환
                state = State.Idle;

                //새끼 슬라임 소환 쿨타임
                babyCoolCount = 30f;
            });
        });

        yield return null;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        //플레이어가 충돌하고, 플레이어 대쉬 아닐때, 보스 점프 상태일때
        if (other.gameObject.CompareTag("Player") && !PlayerManager.Instance.isDash && state == State.Move)
        {
            //공격 상태로 전환
            state = State.Attack;

            // 애니메이터 활성화 (보잉거리는 애니메이션)
            enemyManager.anim.enabled = true;
            enemyManager.anim.speed = 1f;

            // 슬라임 콜라이더 trigger로 전환
            enemyManager.coll.isTrigger = true;

            // 플레이어를 슬라임 가운데로 이동 시키기
            PlayerManager.Instance.transform.DOMove(jumpLandPos, 0.5f);

            //플레이어가 슬라임 안에 있으면 true
            isInside = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        //플레이어 충돌, idle 상태일때, 쿨타임 됬을때
        if (other.gameObject.CompareTag("Player") && state == State.Attack && Time.time - absorbCoolCount > 1)
        {
            // 고정 데미지에 확률 계산
            float damage = Random.Range(enemyManager.power * 0.8f, enemyManager.power * 1.2f);

            // 플레이어 체력 깎기
            PlayerManager.Instance.Damage(damage);

            // 플레이어가 입은 데미지만큼 보스 회복
            enemyManager.Damage(-damage, false);

            // 쿨타임 갱신
            absorbCoolCount = Time.time;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        //플레이어가 나갈때, Attack 상태일때
        if (other.gameObject.CompareTag("Player") && state == State.Attack)
        {
            //플레이어 안에 있지 않음
            isInside = false;

            // 애니메이터 비활성화
            enemyManager.anim.enabled = false;

            // 스케일 복구
            transform.localScale = Vector2.one;

            // 콜라이더 trigger 비활성화
            enemyManager.coll.isTrigger = false;

            //Idle 상태로 전환
            state = State.Idle;
        }
    }
}
