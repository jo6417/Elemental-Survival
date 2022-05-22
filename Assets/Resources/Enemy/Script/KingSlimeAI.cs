using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class KingSlimeAI : MonoBehaviour
{
    [Header("Enemy")]
    float absorbCoolCount; //체력 흡수 쿨타임
    float babyAtkCoolCount; // 새끼 슬라임 소환 쿨타임
    float poisonAtkCoolCount; // 독 뿜기 쿨타임
    public bool isInside; //슬라임 내부에 플레이어 있는지
    public float poisonAtkRange = 20f; //독 뿜기 공격 가능 범위
    public float atkRatio = 0.5f; //이동 멈추고 공격할 확률
    EnemyManager enemyManager;

    [Header("Refer")]
    public Transform crownObj;
    public GameObject slimePrefab; //새끼 슬라임 프리팹
    public EnemyAtkTrigger babyTrigger; //새끼 슬라임 소환 범위
    public ParticleSystem poisonAtkParticle; //독 뿜기 파티클
    public SpriteRenderer spriteFill; // 독 뿜기 기모을때 차오르는 스프라이트

    [Header("Jump")]
    [SerializeField]
    private float jumpHeight = 4f;
    [SerializeField]
    private float jumpUptime = 2f;
    [SerializeField]
    private bool jumpCollisionOff = false; //도약시 충돌 여부
    [SerializeField]
    private GameObject landEffect;
    [SerializeField]
    private Transform shadow;
    public Sequence jumpSeq;
    public Vector2 jumpLandPos; //점프 착지 위치

    private void Awake()
    {
        enemyManager = GetComponentInChildren<EnemyManager>();
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        // 콜라이더 충돌 초기화
        enemyManager.coll.enabled = false;
        enemyManager.coll.isTrigger = false;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        //애니메이션 스피드 초기화
        if (enemyManager.anim != null)
            enemyManager.anim.speed = 1f;

        //속도 초기화
        enemyManager.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //그림자 위치 초기화
        if (shadow)
            shadow.localPosition = Vector2.zero;

        //몸체 머터리얼 컬러 초기화
        Color bodyColor = Color.green;
        enemyManager.sprite.material.SetColor("_TexColor", bodyColor);
        //아웃라인 컬러 초기화
        Color outLineColor = Color.cyan * 5f;
        enemyManager.sprite.material.SetColor("_Color", outLineColor);
        //독 기모으기 스프라이트 초기화
        spriteFill.material.SetFloat("_FillRate", 0);
    }

    private void Update()
    {
        if (enemyManager.enemy == null)
            return;

        // state 확인
        if (ManageState())
        {
            // State에 아무 이상 없으면 행동 시작
            ManageAction();
        }

        // 플레이어 흡수중일때
        if (isInside)
        {
            // 플레이어 이동속도 디버프
            PlayerManager.Instance.speedDebuff = 0.1f;

            // 흡수 쿨타임 됬을때
            if (absorbCoolCount <= 0f)
            {
                // 고정 데미지에 확률 계산
                float damage = Random.Range(enemyManager.power * 0.8f, enemyManager.power * 1.2f);

                // 플레이어 체력 깎기
                PlayerManager.Instance.Damage(damage);

                // 플레이어가 입은 데미지만큼 보스 회복
                enemyManager.Damage(-damage, false);

                // 쿨타임 갱신
                absorbCoolCount = 1f;
            }

            // 체력 흡수 패턴 쿨타임 차감
            absorbCoolCount -= Time.deltaTime;
        }
        else
        {
            // 플레이어 이동속도 디버프 해제
            PlayerManager.Instance.speedDebuff = 1f;
        }

        // 공격 중이 아닐때
        if (enemyManager.nowAction != EnemyManager.Action.Attack)
        {
            // 새끼 슬라임 소환 쿨타임 차감
            babyAtkCoolCount -= Time.deltaTime;

            // 독방울 패턴 쿨타임 차감
            poisonAtkCoolCount -= Time.deltaTime;
        }
    }

    bool ManageState()
    {
        //죽음 애니메이션 중일때
        if (enemyManager.isDead)
        {
            enemyManager.state = EnemyManager.State.Dead;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
            enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;

            if (enemyManager.anim != null)
                enemyManager.anim.speed = 0f;

            transform.DOPause();
            jumpSeq.Pause();

            return false;
        }

        //전역 타임스케일이 0 일때
        if (SystemManager.Instance.timeScale == 0)
        {
            enemyManager.state = EnemyManager.State.SystemStop;

            if (enemyManager.anim != null)
                enemyManager.anim.speed = 0f;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
            transform.DOPause();
            return false;
        }

        //시간 정지 디버프일때
        if (enemyManager.stopCount > 0)
        {
            enemyManager.state = EnemyManager.State.TimeStop;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
            enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            enemyManager.anim.speed = 0f;

            enemyManager.sprite.material = enemyManager.originMat;
            enemyManager.sprite.color = SystemManager.Instance.stopColor; //시간 멈춤 색깔
            transform.DOPause();

            enemyManager.stopCount -= Time.deltaTime * SystemManager.Instance.timeScale;
            return false;
        }

        //맞고 경직일때
        if (enemyManager.hitCount > 0)
        {
            enemyManager.state = EnemyManager.State.Hit;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화

            // 머터리얼 및 색 변경
            enemyManager.sprite.material = SystemManager.Instance.hitMat;
            enemyManager.sprite.color = SystemManager.Instance.hitColor;

            enemyManager.hitCount -= Time.deltaTime * SystemManager.Instance.timeScale;
            return false;
        }

        //스폰 콜라이더에 닿아 반대편으로 보내질때 잠시대기
        if (enemyManager.oppositeCount > 0)
        {
            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화

            //점프시퀀스 초기화
            if (enemyManager.nowAction == EnemyManager.Action.Jump && jumpSeq.IsActive())
            {
                jumpSeq.Pause();

                //그림자 위치 초기화
                if (shadow)
                    shadow.localPosition = Vector2.zero;
            }

            enemyManager.oppositeCount -= Time.deltaTime * SystemManager.Instance.timeScale;
            return false;
        }

        enemyManager.state = EnemyManager.State.Idle;

        // enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
        enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation; // 위치 고정 해제
        enemyManager.sprite.material = enemyManager.originMat;
        enemyManager.sprite.color = enemyManager.originColor;
        transform.DOPlay();
        if (enemyManager.anim != null)
            enemyManager.anim.speed = 1f; //애니메이션 속도 초기화

        return true;
    }

    void ManageAction()
    {
        // 점프중이면 리턴
        if (enemyManager.nowAction == EnemyManager.Action.Jump)
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

            return;
        }
        // 공격중이면 리턴
        if (enemyManager.nowAction == EnemyManager.Action.Attack)
        {
            return;
        }

        //현재 행동 초기화
        enemyManager.nowAction = EnemyManager.Action.Idle;

        // 현재 상태 idle 일때
        if (enemyManager.nowAction == EnemyManager.Action.Idle)
        {
            // 다음 행동이 Attack 이면
            if (enemyManager.nextAction == EnemyManager.Action.Attack)
            {
                // 현재 행동 공격으로 전환
                enemyManager.nowAction = EnemyManager.Action.Attack;

                // 다음 행동 초기화
                enemyManager.nextAction = EnemyManager.Action.Idle;

                //공격 패턴 선택
                ChooseAttack();
            }
            // 다음 행동이 Idle 이면
            else
            {
                // 현재 행동 점프로 전환
                enemyManager.nowAction = EnemyManager.Action.Jump;

                // 점프 실행
                Jump();
                // StartCoroutine(NewJump());
            }
        }
    }

    IEnumerator NewJump()
    {
        //애니메이터 끄기
        if (enemyManager.anim != null && enemyManager.anim.enabled)
            enemyManager.anim.enabled = false;

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position + Vector3.up * 2f;

        //해당 방향으로 속도 입력
        enemyManager.rigid.velocity = dir.normalized * 20f;

        // 해당 방향으로 속도 입력
        DOTween.To(() => enemyManager.rigid.velocity, x => enemyManager.rigid.velocity = x, Vector2.zero, 2f)
        .SetEase(Ease.Unset);
        // enemyManager.rigid.velocity = dir.normalized * 10f;

        //몬스터 올라간만큼 그림자 내리기
        shadow.DOLocalMove(Vector2.down * 2f, 1f);

        yield return new WaitForSeconds(1f);

        //속도 멈춤
        enemyManager.rigid.velocity = Vector2.zero;

        yield return new WaitForSeconds(1f);

        //움직일 방향
        dir = Vector3.down * 10f;

        //내려보내기
        enemyManager.rigid.velocity = dir;

        // 그림자 올리기
        shadow.DOLocalMove(Vector2.zero, 0.5f);

        yield return new WaitForSeconds(0.5f);

        //속도 멈춤
        enemyManager.rigid.velocity = Vector2.zero;

        if (landEffect)
        {
            // 착지 이펙트 스폰
            GameObject effect = LeanPool.Spawn(landEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
            //파티클 시간 끝나면 디스폰
            LeanPool.Despawn(effect, effect.GetComponent<ParticleSystem>().main.duration);
        }

        yield return new WaitForSeconds(1f);

        // 랜덤 숫자가 atkRatio 보다 적으면 
        if (Random.value < atkRatio)
        {
            // 다음 행동 Attack 으로 예약
            enemyManager.nextAction = EnemyManager.Action.Attack;
        }

        //현재 행동 초기화
        if (enemyManager.nowAction != EnemyManager.Action.Attack)
            enemyManager.nowAction = EnemyManager.Action.Idle;
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

        // float upTime = speed; //올라갈때 시간
        float downTime = 0.2f; //내려갈때 시간

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
            // jumpCoolCount = jumpCoolTime;

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
            // 왕관 머리 따라 내려가기
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
            // 그림자 길쭉해지기
            shadow.DOScale(new Vector2(9 * 0.8f, 3 * 1.2f), 0.1f)
        )
        .Join(
            // 왕관 같이 올라가기
            crownObj.DOLocalMove(new Vector2(0, 1.2f), 0.2f)
        )
        .Join(
            //몬스터 올라간만큼 그림자 내리기
            shadow.DOLocalMove(Vector2.down * jumpHeight, jumpUptime)
            .SetEase(jumpEase)
        // .SetEase(Ease.Linear)
        )
        .AppendCallback(() =>
        {
            //콜라이더 충돌 켜기
            if (jumpCollisionOff)
                enemyManager.coll.enabled = true;

            //현재위치 기준 착지위치 다시 계산
            jumpLandPos = (Vector2)transform.position + Vector2.down * jumpHeight;

            //착지위치까지 내려가기
            transform.DOMove(jumpLandPos, downTime);
        })
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
            // 그림자 납작해지기
            shadow.DOScale(new Vector2(9f * 1.2f, 3f * 0.8f), 0.1f)
        )
        .Join(
            // 왕관 머리에 안착
            crownObj.DOLocalMove(new Vector2(0, -1), 0.6f)
            .SetEase(Ease.OutBack, 2)
        )
        .Append(
            enemyManager.spriteObj.DOScale(new Vector2(1f, 1f), 0.2f)
        )
        .Join(
            shadow.DOScale(new Vector2(9f, 3f), 0.2f)
        )
        .Join(
            crownObj.DOLocalMove(new Vector2(0, 0), 0.2f)
        )
        //1초 후딜 대기
        .AppendInterval(1f)
        .OnComplete(() =>
        {
            // 랜덤 숫자가 atkRatio 보다 적으면 
            if (Random.value < atkRatio)
            {
                // 다음 행동 Attack 으로 예약
                enemyManager.nextAction = EnemyManager.Action.Attack;
            }

            //현재 행동 초기화
            if (enemyManager.nowAction != EnemyManager.Action.Attack)
                enemyManager.nowAction = EnemyManager.Action.Idle;
        });
        jumpSeq.Restart();
    }

    void ChooseAttack()
    {
        // print("공격 패턴 선택");

        // 트리거 켜져있고, 쿨타임 가능할때
        if (babyTrigger.atkTrigger && babyAtkCoolCount <= 0f)
        {
            // //새끼 슬라임 소환 쿨타임
            // babyAtkCoolCount = 30f;

            // StartCoroutine(BabySlimeSummon());

            // return;
        }

        // 플레이어가 일정 거리 내에 들어왔을때, 독뿜기 쿨타임 됬을때
        if (Vector2.Distance(PlayerManager.Instance.transform.position, transform.position) <= poisonAtkRange
        && poisonAtkCoolCount <= 0)
        {
            // 독뿜기 공격 쿨타임 갱신
            poisonAtkCoolCount = 5f;

            //독 뿜기 시전
            StartCoroutine(PoisonShot());

            return;
        }

        //가능한 공격 없을때
        // anim 끄기
        enemyManager.anim.enabled = false;

        // 현재 행동 초기화
        enemyManager.nowAction = EnemyManager.Action.Idle;

        // // 다음 행동 초기화
        // enemyManager.nextAction = EnemyManager.Action.Idle;
    }

    IEnumerator BabySlimeSummon()
    {
        print("슬라임 소환 패턴");

        // 떨림 애니메이션 시작
        enemyManager.anim.enabled = true;
        enemyManager.anim.speed = 1f;
        enemyManager.anim.SetBool("isShaking", true);

        //떨림 애니메이션 시간 대기
        yield return new WaitForSeconds(1f);

        //떨림 애니메이션 끝
        enemyManager.anim.SetBool("isShaking", false);
        // 바운스 애니메이션 시작
        enemyManager.anim.SetBool("isBounce", true);

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

        // 바운스 애니메이션 끝
        enemyManager.anim.SetBool("isBounce", false);
        //애니메이션 끄기
        enemyManager.anim.enabled = false;

        // 천천히 추욱 쳐지기
        enemyManager.spriteObj.DOScale(new Vector2(1.2f, 0.7f), 1f)
        .OnStart(() =>
        {
            crownObj.DOLocalMove(new Vector2(0, -2), 1f);
        })
        .OnComplete(() =>
        {
            // 스케일 복구
            enemyManager.spriteObj.DOScale(Vector2.one, 0.5f)
            .OnStart(() =>
            {
                crownObj.DOLocalMove(new Vector2(0, 0), 0.5f)
                .SetEase(Ease.InOutBack);
            })
            .SetEase(Ease.InOutBack)
            .OnComplete(() =>
            {
                // 현재 행동 초기화
                enemyManager.nowAction = EnemyManager.Action.Idle;
            });
        });

        yield return null;
    }

    IEnumerator PoisonShot()
    {
        // 떨림 애니메이션 시작
        enemyManager.anim.enabled = true;
        enemyManager.anim.SetBool("isShaking", true);

        //TODO 머터리얼 컬러 보라색으로 색 차오름
        float fillAmount = 0;
        spriteFill.material.SetFloat("_FillRate", fillAmount); // 0으로 초기화
        // DOTween.To(
        //     () => fillAmount,
        //     x => fillAmount = x, 1f, 5f);

        //변화되는 fill 값 입력해주기
        while (fillAmount < 1f)
        {
            fillAmount += Time.deltaTime * 0.1f;

            spriteFill.material.SetFloat("_FillRate", fillAmount);

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 보라색으로 모두 채워질때까지 대기
        yield return new WaitUntil(() => fillAmount >= 1f);

        //떨림 애니메이션 종료
        enemyManager.anim.SetBool("isShaking", false);

        // 보스에서 플레이어 방향
        Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 플레이어 방향으로 파티클 회전
        float angle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;
        poisonAtkParticle.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 납작해지기
        enemyManager.spriteObj.DOScale(new Vector2(1.2f, 0.8f), 0.5f);

        // 납작해지기 완료까지 대기
        yield return new WaitUntil(() => enemyManager.spriteObj.transform.localScale == new Vector3(1.2f, 0.8f));

        // 독 뿜기 공격
        poisonAtkParticle.Play();

        // 스케일 복구
        enemyManager.spriteObj.DOScale(Vector2.one, 0.3f)
        .SetEase(Ease.InOutBack);

        //TODO 머터리얼 컬러 보라색 다시 내리기
        // DOTween.To(
        //     () => fillAmount,
        //     x => fillAmount = x, 0f, 2f);

        //변화되는 fill 값 입력해주기
        while (fillAmount > 0f)
        {
            fillAmount -= Time.deltaTime;

            spriteFill.material.SetFloat("_FillRate", fillAmount);

            yield return new WaitForSeconds(Time.deltaTime * 10f);
        }

        // 보라색 모두 내려갈때까지 대기
        yield return new WaitUntil(() => fillAmount <= 0f);

        // 현재 행동 초기화
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        //플레이어가 충돌하고, 플레이어 대쉬 아닐때, 보스 점프 중일때
        if (other.gameObject.CompareTag("Player") && !PlayerManager.Instance.isDash && enemyManager.nowAction == EnemyManager.Action.Jump)
        {
            print("흡수 시작");
            //현재 행동 공격으로 전환
            enemyManager.nowAction = EnemyManager.Action.Attack;

            // 바운스 애니메이션 시작
            enemyManager.anim.enabled = true;
            enemyManager.anim.speed = 1f;
            enemyManager.anim.SetBool("isBounce", true);

            // 슬라임 콜라이더 trigger로 전환
            enemyManager.coll.isTrigger = true;

            // 플레이어 이동 막기
            PlayerManager.Instance.speedDebuff = 0;

            // 플레이어를 슬라임 가운데로 이동 시키기
            PlayerManager.Instance.transform.DOMove(jumpLandPos, 0.5f)
            .OnComplete(() =>
            {
                //플레이어가 슬라임 안에 있으면 true
                isInside = true;
            });
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 슬라임 안에 플레이어 있을때 나가면
        if (other.gameObject.CompareTag("Player") && isInside)
        {
            print("흡수 끝");
            //플레이어 흡수 정지
            isInside = false;

            // 애니메이터 비활성화
            enemyManager.anim.enabled = false;

            // 스케일 복구
            transform.localScale = Vector2.one;

            // 콜라이더 trigger 비활성화
            enemyManager.coll.isTrigger = false;

            //현재 행동 초기화
            enemyManager.nowAction = EnemyManager.Action.Idle;
        }
    }
}
