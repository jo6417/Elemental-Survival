using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class KingSlime_AI : MonoBehaviour
{
    [Header("Enemy")]
    public float coolCount; //패턴 쿨타임 카운트
    public float absorbCoolCount; //체력 흡수 쿨타임 카운트
    public float jumpCoolTime; // 점프 쿨타임
    public float babyAtkCoolTime; // 새끼 슬라임 소환 쿨타임
    public float poisonAtkCoolTime; // 독 뿜기 쿨타임
    public bool nowAbsorb; //슬라임 내부에 플레이어 있는지
    public float poisonAtkRange = 20f; //독 뿜기 공격 가능 범위
    public float atkRatio = 0.5f; //이동 멈추고 공격할 확률

    [Header("Refer")]
    public EnemyManager enemyManager;
    public Transform crownObj;
    public GameObject slimePrefab; //새끼 슬라임 프리팹
    public EnemyAtkTrigger babyTrigger; //새끼 슬라임 소환 범위
    public ParticleSystem poisonAtkParticle; //독 뿜기 파티클
    public ParticleSystem poisonPoolParticle; //독 웅덩이 파티클
    public SpriteRenderer spriteFill; // 독 뿜기 기모을때 차오르는 스프라이트

    [Header("Jump")]
    [SerializeField]
    private GameObject landEffect;
    public Vector2 jumpLandPos; //점프 착지 위치
    bool absorbAtkTrigger = false; //내리찍기 공격 트리거

    private void Awake()
    {
        enemyManager = enemyManager == null ? GetComponentInChildren<EnemyManager>() : enemyManager;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 콜라이더 충돌 초기화
        enemyManager.physicsColl.enabled = false;
        enemyManager.physicsColl.isTrigger = false;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        //애니메이션 스피드 초기화
        if (enemyManager.animList != null)
            enemyManager.animList[0].speed = 1f;

        //속도 초기화
        enemyManager.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //몸체 머터리얼 컬러 초기화
        Color bodyColor = Color.cyan;
        enemyManager.spriteList[0].material.SetColor("_TexColor", bodyColor);
        //아웃라인 컬러 초기화
        Color outLineColor = Color.cyan * 5f;
        enemyManager.spriteList[0].material.SetColor("_Color", outLineColor);
        //독 기모으기 스프라이트 초기화
        spriteFill.material.SetFloat("_FillRate", 0);
    }

    private void Update()
    {
        if (enemyManager.enemy == null)
            return;

        // 플레이어 체력 흡수
        AbsorbPlayer();

        // 상태 이상 있으면 리턴
        if (!enemyManager.ManageState())
            return;

        // 행동 관리
        ManageAction();
    }

    void AbsorbPlayer()
    {
        // 플레이어 흡수중일때
        if (nowAbsorb)
        {
            // 흡수 쿨타임 됬을때
            if (absorbCoolCount <= 0f)
            {
                // 고정 데미지에 확률 계산
                float damage = Random.Range(enemyManager.powerNow * 0.8f, enemyManager.powerNow * 1.2f);

                // 플레이어 체력 깎기
                PlayerManager.Instance.hitBox.Damage(damage, false);

                // 플레이어가 입은 데미지만큼 보스 회복
                enemyManager.hitBoxList[0].Damage(-damage, false, transform.position);

                // 쿨타임 갱신
                absorbCoolCount = 1f;
            }

            // 체력 흡수 패턴 쿨타임 차감
            absorbCoolCount -= Time.deltaTime;
        }
    }

    void ManageAction()
    {
        // Idle 아니면 리턴
        if (enemyManager.nowAction != Character.Action.Idle)
            return;

        // Idle일때 쿨타임 카운트 차감
        if (coolCount > 0)
        {
            coolCount -= Time.deltaTime;
            return;
        }

        // 랜덤 숫자가 atkRatio 보다 적으면 공격
        if (Random.value < atkRatio)
        {
            //공격 패턴 선택
            ChooseAttack();
        }
        // 랜덤 숫자가 atkRatio 보다 많으면 점프
        else
        {
            // 점프 실행
            JumpStart();
        }
    }

    void JumpStart()
    {
        // print("점프 시작");

        // 현재 행동 점프로 전환
        enemyManager.nowAction = Character.Action.Jump;

        // 점프 애니메이션으로 전환
        enemyManager.animList[0].SetBool("Jump", true);

        // 점프 쿨타임 갱신
        coolCount = jumpCoolTime;

        // 스프라이트 레이어 레벨 높이기
        foreach (SpriteRenderer sprite in enemyManager.spriteList)
        {
            sprite.sortingOrder = 1;
        }
    }

    public void JumpMove()
    {
        //움직일 방향
        Vector3 dir = PlayerManager.Instance.transform.position - transform.position;

        //움직일 방향에따라 좌우반전
        if (dir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        //움직일 거리, 플레이어 위치까지 갈수 있으면 플레이어 위치, 못가면 적 스피드
        float distance = dir.magnitude > enemyManager.rangeNow ? enemyManager.rangeNow : dir.magnitude;

        //착지 위치 변수에 저장
        jumpLandPos = transform.position + dir.normalized * distance;

        //플레이어 위치까지 doMove
        transform.DOMove(jumpLandPos, 1f);

        // 콜라이더 끄기
        enemyManager.physicsColl.enabled = false;
        // 콜라이더 trigger로 전환
        enemyManager.physicsColl.isTrigger = true;
    }

    public void Landing()
    {
        //플레이어 흡수 트리거 켜기
        absorbAtkTrigger = true;

        //콜라이더 켜기
        enemyManager.physicsColl.enabled = true;

        // 착지 이펙트 생성
        if (landEffect != null)
            LeanPool.Spawn(landEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        // 스프라이트 레이어 레벨 초기화
        foreach (SpriteRenderer sprite in enemyManager.spriteList)
        {
            sprite.sortingOrder = 0;
        }
    }

    public void JumpEnd()
    {
        //플레이어 흡수 트리거 끄기
        absorbAtkTrigger = false;

        // IDLE 애니메이션 전환
        enemyManager.animList[0].SetBool("Jump", false);

        // 플레이어 흡수 못했으면 콜라이더 충돌로 전환
        if (!nowAbsorb)
        {
            enemyManager.physicsColl.isTrigger = false;

            // 현재 행동 끝내기
            enemyManager.nowAction = Character.Action.Idle;
        }
    }
    void ChooseAttack()
    {
        // print("공격 패턴 선택");
        // 현재 행동 공격으로 전환
        enemyManager.nowAction = Character.Action.Attack;

        // 패턴 쿨타임 중일때 리턴
        if (coolCount > 0)
            return;

        //어떤 공격을 할지 랜덤 숫자 산출
        float atkNum = Random.value;

        // 범위 밖에 플레이어 있을때
        if (!babyTrigger.atkTrigger)
        {
            //새끼 슬라임 소환 쿨타임
            coolCount = babyAtkCoolTime;

            StartCoroutine(BabySlimeSummon());

            return;
        }

        // 플레이어가 일정 거리 내에 들어왔을때
        if (Vector2.Distance(PlayerManager.Instance.transform.position, transform.position) <= poisonAtkRange)
        {
            // 독뿜기 공격 쿨타임 갱신
            coolCount = poisonAtkCoolTime;

            //독 뿜기 시전
            StartCoroutine(PoisonShot());

            return;
        }

        //가능한 공격 없을때
        // Idle 애니메이션으로 전환
        enemyManager.animList[0].SetBool("isShaking", false);
        enemyManager.animList[0].SetBool("isBounce", false);
        enemyManager.animList[0].SetBool("Jump", false);

        // 현재 행동 초기화
        enemyManager.nowAction = Character.Action.Idle;
    }

    IEnumerator BabySlimeSummon()
    {
        print("슬라임 소환 패턴");

        // 떨림 애니메이션 시작
        enemyManager.animList[0].speed = 1f;
        enemyManager.animList[0].SetBool("isShaking", true);

        //떨림 애니메이션 시간 대기
        yield return new WaitForSeconds(1f);

        //떨림 애니메이션 끝
        enemyManager.animList[0].SetBool("isShaking", false);
        // 바운스 애니메이션 시작
        enemyManager.animList[0].SetBool("isBounce", true);

        // 애니메이터 활성화까지 대기
        // yield return new WaitUntil(() => enemyManager.animList[0].enabled);

        // 남은 체력에 비례해서 소환 횟수 산출, 5~15마리
        int summonNum = 5 + Mathf.RoundToInt(10 * (enemyManager.hpMax - enemyManager.hpNow) / enemyManager.hpMax);
        for (int i = 0; i < summonNum; i++)
        {
            //슬라임 소환
            GameObject babySlime = LeanPool.Spawn(slimePrefab, transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

            //컴포넌트 초기화
            // Collider2D babyColl = babySlime.GetComponent<Collider2D>();
            Character babyEnemyManager = babySlime.GetComponent<Character>();

            // 소환수 히트박스 끄기
            for (int j = 0; j < babyEnemyManager.hitBoxList.Count; j++)
            {
                babyEnemyManager.hitBoxList[i].enabled = false;
            }

            //소환 위치
            Vector2 summonPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * 20f;

            // babyTrigger 범위내 랜덤 포지션으로 슬라임 아이콘 dojump 시키기
            babySlime.transform.DOJump(summonPos, 5f, 1, 1f)
            .OnComplete(() =>
            {
                // 소환수 히트박스 켜기
                for (int j = 0; j < babyEnemyManager.hitBoxList.Count; j++)
                {
                    babyEnemyManager.hitBoxList[i].enabled = true;
                }

                // 소환수 초기화
                babyEnemyManager.initialStart = true;
            });

            //소환 딜레이
            yield return new WaitForSeconds(0.3f);
        }

        // 바운스 애니메이션 끝
        enemyManager.animList[0].SetBool("isBounce", false);
        //애니메이션 끄기
        // enemyManager.animList[0].enabled = false;

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
                enemyManager.nowAction = Character.Action.Idle;
            });
        });

        yield return null;
    }

    IEnumerator PoisonShot()
    {
        // 떨림 애니메이션 시작
        // enemyManager.animList[0].enabled = true;
        enemyManager.animList[0].SetBool("isShaking", true);

        // 머터리얼 컬러 0으로 초기화
        float fillAmount = 0;
        spriteFill.material.SetFloat("_FillRate", fillAmount);

        // 머터리얼 컬러 보라색으로 색 차오름
        while (fillAmount < 1f)
        {
            fillAmount += Time.deltaTime;

            spriteFill.material.SetFloat("_FillRate", fillAmount);

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 보라색으로 모두 채워질때까지 대기
        yield return new WaitUntil(() => fillAmount >= 1f);

        //떨림 애니메이션 종료
        enemyManager.animList[0].SetBool("isShaking", false);
        // enemyManager.animList[0].enabled = false;

        // 보스에서 플레이어 방향
        Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 플레이어 방향으로 파티클 회전
        float angle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;
        poisonAtkParticle.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 납작해지기
        enemyManager.spriteObj.DOScale(new Vector2(1.2f, 0.8f), 0.5f);
        // 납작해지며 왕관도 내려가기
        crownObj.DOLocalMove(new Vector2(0, -1), 0.5f);

        // 납작해지기 완료까지 대기
        yield return new WaitUntil(() => enemyManager.spriteObj.localScale == new Vector3(1.2f, 0.8f));

        // 독 뿜기 공격
        poisonAtkParticle.Play();

        // 스케일 복구
        enemyManager.spriteObj.DOScale(Vector2.one, 0.3f)
        .SetEase(Ease.InOutBack);
        // 왕관 위치 복구
        crownObj.DOLocalMove(Vector2.zero, 0.3f)
        .SetEase(Ease.InOutBack);

        // 머터리얼 컬러 보라색 다시 내리기
        while (fillAmount > 0f)
        {
            fillAmount -= Time.deltaTime;

            spriteFill.material.SetFloat("_FillRate", fillAmount);

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 보라색 모두 내려갈때까지 대기
        yield return new WaitUntil(() => fillAmount <= 0f);

        // 현재 행동 초기화
        enemyManager.nowAction = Character.Action.Idle;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag(SystemManager.TagNameList.Player.ToString()) // 플레이어가 충돌했을때
        && !PlayerManager.Instance.isDash // 플레이어 대쉬 아닐때
        && absorbAtkTrigger // 흡수 트리거 켜졌을때
        && !nowAbsorb) // 현재 흡수중 아닐때
        {
            // print("흡수 시작");
            //플레이어 체력 흡수중이면 true
            nowAbsorb = true;

            //현재 행동 공격으로 전환
            enemyManager.nowAction = Character.Action.Attack;

            // IDLE 애니메이션 전환
            enemyManager.animList[0].SetBool("Jump", false);
            // 바운스 애니메이션 시작
            enemyManager.animList[0].SetBool("isBounce", true);

            // 플레이어 위치 이동하는 동안 이동 금지
            PlayerManager.Instance.speedDeBuff = 0;
            // 이동 속도 반영
            PlayerManager.Instance.Move();
            // 플레이어 조작 차단
            PlayerManager.Instance.playerInput.Disable();

            // 플레이어를 슬라임 가운데로 이동 시키기
            PlayerManager.Instance.transform.DOMove(jumpLandPos, 0.5f)
            .OnComplete(() =>
            {
                // 이동 속도 디버프 걸기
                PlayerManager.Instance.speedDeBuff = 0.2f;
                // 이동 속도 반영
                PlayerManager.Instance.Move();
                // 플레이어 조작 활성화
                PlayerManager.Instance.playerInput.Enable();
            });
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 흡수 중일때 플레이어 나가면
        if (other.gameObject.CompareTag(SystemManager.TagNameList.Player.ToString())
        && nowAbsorb)
        {
            // print("흡수 끝");

            // 플레이어 흡수 정지
            nowAbsorb = false;

            // 점프 끝날때 초기화 함수 실행
            JumpEnd();

            // 이동 속도 디버프 해제
            PlayerManager.Instance.speedDeBuff = 1f;
            // 이동 속도 반영
            PlayerManager.Instance.Move();

            // 바운스 애니메이션 끝
            enemyManager.animList[0].SetBool("isBounce", false);

            // 스케일 복구
            transform.localScale = Vector2.one;

            // 충돌 콜라이더 trigger 비활성화
            enemyManager.physicsColl.isTrigger = false;

            //현재 행동 초기화
            enemyManager.nowAction = Character.Action.Idle;
        }
    }
}
