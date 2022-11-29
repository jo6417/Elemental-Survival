using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;

public class KingSlime_AI : MonoBehaviour
{
    [Header("State")]
    [SerializeField] Patten patten = Patten.None;
    enum Patten { BabySlimeSummon, PoisonShot, Skip, None };
    public bool nowAbsorb; //슬라임 내부에 플레이어 있는지
    public float attackDistance; // 공격 가능 범위
    [SerializeField] TextMeshProUGUI stateText;
    Vector3 playerDir; // 실시간 플레이어 방향

    [Header("Phase")]
    [SerializeField, ReadOnly] int nowPhase = 1;
    [SerializeField, ReadOnly] int nextPhase = 1;

    [Header("Cooltime")]
    public float coolCount; //패턴 쿨타임 카운트
    public float absorbCoolCount; //체력 흡수 쿨타임 카운트
    public float babyAtkCoolTime; // 새끼 슬라임 소환 쿨타임
    public float poisonAtkCoolTime; // 독 뿜기 쿨타임

    [Header("Refer")]
    public Character character;
    public Transform crownObj;
    [SerializeField] SortingGroup sorting; // 보스 전체 레이어 관리

    [Header("Jump")]
    [SerializeField]
    private GameObject landEffect;
    public Vector2 jumpLandPos; //점프 착지 위치
    bool absorbAtkTrigger = false; //내리찍기 공격 트리거
    [SerializeField] string[] jumpSounds = { "KingSlime_Jump1", "KingSlime_Jump2", "KingSlime_Jump3" };
    int jump_lastIndex = -1;

    [Header("PoisonShot")]
    [SerializeField] SpriteRenderer spriteFill; // 독 뿜기 기모을때 차오르는 스프라이트
    [SerializeField] Transform poisonDrop; // 독방울 프리팹
    [SerializeField] Transform poisonPool; // 독장판 프리팹
    [SerializeField] AnimationCurve dropCurve; // 독방울 날아가는 커브

    [Header("BabySlimeSummon")]
    public GameObject slimePrefab; //새끼 슬라임 프리팹

    private void Awake()
    {
        character = character == null ? GetComponentInChildren<Character>() : character;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 휴식으로 초기화
        character.nowAction = Character.Action.Rest;

        // 콜라이더 충돌 초기화
        character.physicsColl.enabled = false;
        character.physicsColl.isTrigger = false;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => character.enemy != null);

        //애니메이션 스피드 초기화
        if (character.animList != null)
            character.animList[0].speed = 1f;

        //속도 초기화
        character.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        character.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //몸체 머터리얼 컬러 초기화
        Color bodyColor = Color.cyan;
        character.spriteList[0].material.SetColor("_TexColor", bodyColor);
        //아웃라인 컬러 초기화
        Color outLineColor = Color.cyan * 5f;
        character.spriteList[0].material.SetColor("_Color", outLineColor);
        //독 기모으기 스프라이트 초기화
        spriteFill.material.SetFloat("_FillRate", 0);

        // 초기화 끝 행동 시작
        character.nowAction = Character.Action.Idle;
    }

    private void Update()
    {
        if (character.enemy == null)
            return;

        // 공격중 아닐때
        if (character.nowAction != Character.Action.Attack)
            // 쿨타임 카운트 차감
            if (coolCount > 0)
                coolCount -= Time.deltaTime;

        //움직일 방향
        Vector3 playerDir = PlayerManager.Instance.transform.position - transform.position;

        //! 거리 및 쿨타임 디버깅
        stateText.text = $"Distance : {string.Format("{0:0.00}", playerDir.magnitude)} \n CoolCount : {string.Format("{0:0.00}", coolCount)}";

        // 플레이어 체력 흡수
        AbsorbPlayer();

        // Idle 아니면 리턴
        if (character.nowAction != Character.Action.Idle)
            return;

        // 상태 이상 있으면 리턴
        if (!character.ManageState())
            return;

        // 페이즈 올리는중이면 리턴
        if (nextPhase > nowPhase)
        {
            // 속도 초기화
            character.rigid.velocity = Vector3.zero;

            return;
        }

        // 행동 관리
        ManageAction();
    }

    public void JumpSound()
    {
        // 걷기 발소리 재생
        jump_lastIndex = SoundManager.Instance.PlaySoundPool(jumpSounds.ToList(), transform.position, jump_lastIndex);
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
                float damage = Random.Range(character.powerNow * 0.8f, character.powerNow * 1.2f);

                // 플레이어 체력 깎기
                PlayerManager.Instance.hitBox.Damage(damage, false);

                // 플레이어가 입은 데미지만큼 보스 회복
                character.hitBoxList[0].Damage(-damage, false, transform.position);

                // 쿨타임 갱신
                absorbCoolCount = 1f;
            }

            // 체력 흡수 패턴 쿨타임 차감
            absorbCoolCount -= Time.deltaTime;
        }
    }

    void ManageAction()
    {
        //움직일 방향에따라 좌우반전
        if (playerDir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        // 쿨타임 제로일때, 플레이어가 공격범위 이내일때
        if (coolCount <= 0 && playerDir.magnitude <= attackDistance)
        {
            // 현재 행동 공격으로 전환
            character.nowAction = Character.Action.Attack;

            //공격 패턴 선택
            ChooseAttack();

            return;
        }
        else
        {
            // 현재 행동 점프로 전환
            character.nowAction = Character.Action.Jump;

            // 점프 실행
            JumpStart();
        }
    }

    void ChooseAttack()
    {
        //! 패턴 스킵
        if (patten == Patten.Skip)
            return;

        // print("공격 패턴 선택");

        // 가능한 공격 중에서 랜덤 뽑기
        int atkType = Random.Range(0, 3);

        //! 테스트를 위해 패턴 고정
        if (patten != Patten.None)
            atkType = (int)patten;

        // 결정된 공격 패턴 실행
        switch (atkType)
        {
            case 0:
                // 쿨타임 초기화 
                coolCount = babyAtkCoolTime;
                // 공격 실행
                StartCoroutine(BabySlimeSummon());
                break;

            case 1:
                // 쿨타임 초기화 
                coolCount = poisonAtkCoolTime;
                // 공격 실행
                StartCoroutine(PoisonShot());
                break;

                // case 2:
                // // 쿨타임 초기화 
                // coolCount = ;
                // // 공격 실행
                // StartCoroutine();
                // break;
        }

        //가능한 공격 없을때
        // Idle 애니메이션으로 전환
        // character.animList[0].SetBool("isShaking", false);
        // character.animList[0].SetBool("isBounce", false);
        // character.animList[0].SetBool("Jump", false);
    }

    void JumpStart()
    {
        // print("점프 시작");

        // 점프 애니메이션으로 전환
        character.animList[0].SetBool("Jump", true);

        // 점프 쿨타임 갱신
        // coolCount = jumpCoolTime;

        // 스프라이트 전체 레이어 레벨 높이기
        sorting.sortingOrder = 1;
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
        float distance = dir.magnitude > character.rangeNow ? character.rangeNow : dir.magnitude;

        //착지 위치 변수에 저장
        jumpLandPos = transform.position + dir.normalized * distance;

        //플레이어 위치까지 doMove
        transform.DOMove(jumpLandPos, 1f);

        // 콜라이더 끄기
        character.physicsColl.enabled = false;

        // 점프 사운드 재생
        JumpSound();
    }

    public void Landing()
    {
        //플레이어 흡수 트리거 켜기
        absorbAtkTrigger = true;

        //콜라이더 켜기
        character.physicsColl.enabled = true;

        // 착지 이펙트 생성
        if (landEffect != null)
            LeanPool.Spawn(landEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        // 스프라이트 레이어 레벨 초기화
        sorting.sortingOrder = 0;

        // 착지 사운드 재생
        SoundManager.Instance.PlaySound("KingSlime_Landing", transform.position);
    }

    public void JumpEnd()
    {
        //플레이어 흡수 트리거 끄기
        absorbAtkTrigger = false;

        // IDLE 애니메이션 전환
        character.animList[0].SetBool("Jump", false);

        // 플레이어 흡수 못했으면
        if (!nowAbsorb)
        {
            // 현재 행동 끝내기
            character.nowAction = Character.Action.Idle;
        }
    }


    IEnumerator BabySlimeSummon()
    {
        print("슬라임 소환 패턴");

        // 떨림 애니메이션 시작
        character.animList[0].speed = 1f;
        character.animList[0].SetBool("isShaking", true);

        //todo 슬라임 바운스 소리 재생
        SoundManager.Instance.PlaySound("KingSlime_Boing");

        //떨림 애니메이션 시간 대기
        yield return new WaitForSeconds(1f);

        //떨림 애니메이션 끝
        character.animList[0].SetBool("isShaking", false);
        // 바운스 애니메이션 시작
        character.animList[0].SetBool("isBounce", true);

        // 애니메이터 활성화까지 대기
        // yield return new WaitUntil(() => chracter.animList[0].enabled);

        //todo 페이즈에 따라 소환 횟수 산출
        // 남은 체력에 비례해서 소환 횟수 산출, 5~15마리
        int summonNum = 5 + Mathf.RoundToInt(10 * (character.hpMax - character.hpNow) / character.hpMax);
        for (int i = 0; i < summonNum; i++)
        {
            //슬라임 소환
            GameObject babySlime = LeanPool.Spawn(slimePrefab, transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

            //컴포넌트 초기화
            // Collider2D babyColl = babySlime.GetComponent<Collider2D>();
            Character babyChracter = babySlime.GetComponent<Character>();

            // 소환수 히트박스 끄기
            for (int j = 0; j < babyChracter.hitBoxList.Count; j++)
            {
                babyChracter.hitBoxList[j].enabled = false;
            }

            //소환 위치
            Vector2 summonPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * 20f;

            // babyTrigger 범위내 랜덤 포지션으로 슬라임 아이콘 dojump 시키기
            babySlime.transform.DOJump(summonPos, 5f, 1, 1f)
            .OnComplete(() =>
            {
                // 소환수 히트박스 켜기
                for (int j = 0; j < babyChracter.hitBoxList.Count; j++)
                {
                    babyChracter.hitBoxList[j].enabled = true;
                }

                // 소환수 초기화
                babyChracter.initialStart = true;
            });

            //todo 슬라임 소환 사운드 재생
            SoundManager.Instance.PlaySound("KingSlime_Slime_Summon");

            //소환 딜레이
            yield return new WaitForSeconds(0.3f);
        }

        // 바운스 애니메이션 끝
        character.animList[0].SetBool("isBounce", false);
        //애니메이션 끄기
        // chracter.animList[0].enabled = false;

        // 천천히 추욱 쳐지기
        character.spriteObj.DOScale(new Vector2(1.2f, 0.7f), 1f)
        .OnStart(() =>
        {
            crownObj.DOLocalMove(new Vector2(0, -2), 1f);
        })
        .OnComplete(() =>
        {
            // 스케일 복구
            character.spriteObj.DOScale(Vector2.one, 0.5f)
            .OnStart(() =>
            {
                crownObj.DOLocalMove(new Vector2(0, 0), 0.5f)
                .SetEase(Ease.InOutBack);
            })
            .SetEase(Ease.InOutBack)
            .OnComplete(() =>
            {
                // 현재 행동 초기화
                character.nowAction = Character.Action.Idle;
            });
        });

        yield return null;
    }

    IEnumerator PoisonShot()
    {
        // 떨림 애니메이션 시작
        // chracter.animList[0].enabled = true;
        character.animList[0].SetBool("isShaking", true);

        // 머터리얼 컬러 0으로 초기화
        float fillAmount = 0;
        spriteFill.material.SetFloat("_FillRate", fillAmount);

        //todo 보라색 독 차오르는 소리 재생
        SoundManager.Instance.PlaySound("KingSlime_Poison_Fill");

        // // 머터리얼 컬러 보라색으로 색 차오름
        // while (fillAmount < 1f)
        // {
        //     fillAmount += Time.deltaTime;

        //     // fillAmount 만큼 보라색 채우기
        //     spriteFill.material.SetFloat("_FillRate", fillAmount);

        //     yield return new WaitForSeconds(Time.deltaTime);
        // }

        // 독 채우기 서브 스프라이트 찾기
        SpriteRenderer fillSub = spriteFill.transform.GetChild(0).GetComponent<SpriteRenderer>();

        //todo 독 스프라이트 켜기
        Color fillColor = spriteFill.color;
        fillColor.a = 150f / 255f;
        spriteFill.color = fillColor;
        //todo 독 서브 스프라이트 켜기
        Color fillSubColor = fillSub.color;
        fillSubColor.a = 150f / 255f;
        fillSub.color = fillSubColor;

        // 독 스프라이트 위치 초기화
        spriteFill.transform.localPosition = Vector2.down * 5f;
        // 독 보조 스프라이트 위치 초기화
        spriteFill.transform.GetChild(0).localPosition = new Vector2(1f, 1f);
        // 독 스프라이트 나타내기
        spriteFill.gameObject.SetActive(true);

        //todo 보라색 독 스프라이트 올라가기
        spriteFill.transform.DOLocalMove((Vector2)spriteFill.transform.localPosition + Vector2.up * 1f, 0.5f)
        .SetLoops(10, LoopType.Incremental)
        .SetEase(Ease.OutBack);

        //todo 내부 서브 스프라이트 애니메이션
        fillSub.transform.DOLocalMove((Vector2)spriteFill.transform.GetChild(0).localPosition + Vector2.down * 2f, 0.5f)
        .SetLoops(8, LoopType.Yoyo)
        .OnComplete(() =>
        {
            spriteFill.transform.GetChild(0).DOLocalMove(new Vector2(1f, 0), 1f);
        });

        yield return new WaitForSeconds(5f);

        //todo 독 스프라이트 끄기
        fillColor.a = 0;
        spriteFill.DOColor(fillColor, 0.5f);
        //todo 독 서브 스프라이트 끄기
        fillSubColor.a = 0;
        fillSub.DOColor(fillSubColor, 0.5f);

        //todo 보라색 독 차오르는 소리 끄기
        SoundManager.Instance.StopSound("KingSlime_Poison_Fill", 0.5f);

        // 보라색으로 모두 채워질때까지 대기
        // yield return new WaitUntil(() => fillAmount >= 1f);

        //떨림 애니메이션 종료
        character.animList[0].SetBool("isShaking", false);
        // chracter.animList[0].enabled = false;

        // 납작해지기
        character.spriteObj.DOScale(new Vector2(1.2f, 0.8f), 0.5f);
        // 납작해지며 왕관도 내려가기
        crownObj.DOLocalMove(new Vector2(0, -1), 0.5f);

        // 납작해지기 완료까지 대기
        yield return new WaitUntil(() => character.spriteObj.localScale == new Vector3(1.2f, 0.8f));

        // 독 뿜기 공격
        // poisonAtkParticle.Play();

        // 페이즈에 따른 독 방울 개수
        int poisonNum = 5 * nowPhase;

        // 독 방울 날리기
        for (int i = 0; i < poisonNum; i++)
        {
            // 보스에서 플레이어 방향
            Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;
            // 방향을 각도로 변환
            float startAngle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;
            // 방향에 따라 각도 추가
            if ((startAngle > 0 && startAngle < 45))
                startAngle += 45f;
            if ((startAngle > -225f && startAngle < -180f) || (startAngle > 135 && startAngle < 180))
                startAngle -= 45f;

            // 독방울 소환
            Transform poisonDropObj = LeanPool.Spawn(poisonDrop, transform.position, Quaternion.Euler(Vector3.forward * startAngle), SystemManager.Instance.effectPool);

            //todo 독방울 투척 소리 재생
            SoundManager.Instance.PlaySound("KingSlime_Poison_Drop");

            // 독장판 범위
            float poisonRange = 10f * nowPhase;

            // 독방울 전방 착지 위치 계산 = 바라보는 방향에서 범위 안에 드랍
            Vector2 dropPos = (Vector2)transform.position + playerDir + Random.insideUnitCircle * poisonRange / 2f;

            // 독방울 점프 높이
            float jumpPower = playerDir.magnitude / 2f;
            // 독방울 점프 시간
            float jumpTime = jumpPower * 0.1f;

            // 독방울 날아가는 동안 회전
            poisonDropObj.DORotate(startAngle < 90f ? Vector3.forward * -90 : Vector3.forward * 270, jumpTime)
            .SetEase(dropCurve);

            // 독방울 날리기
            poisonDropObj.DOJump(dropPos, jumpPower, 1, jumpTime)
            .SetEase(dropCurve)
            .OnComplete(() =>
            {
                // 떨어진 위치에 독장판 소환
                LeanPool.Spawn(poisonPool, poisonDropObj.position, Quaternion.identity, SystemManager.Instance.effectPool);

                // 독방울 디스폰
                LeanPool.Despawn(poisonDropObj);

                //todo 독장판 생성 소리 재생
                SoundManager.Instance.PlaySound("KingSlime_Poison_Pool");
            });

            // 독방울 사이 딜레이
            yield return new WaitForSeconds(0.2f);
        }

        // 스케일 복구
        character.spriteObj.DOScale(Vector2.one, 0.3f)
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
        character.nowAction = Character.Action.Idle;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // if (other.gameObject.CompareTag(SystemManager.TagNameList.Player.ToString()) // 플레이어가 충돌했을때
        // && !PlayerManager.Instance.isDash // 플레이어 대쉬 아닐때
        // && absorbAtkTrigger // 흡수 트리거 켜졌을때
        // && !nowAbsorb) // 현재 흡수중 아닐때
        // {
        //     // print("흡수 시작");
        //     //플레이어 체력 흡수중이면 true
        //     nowAbsorb = true;

        //     //현재 행동 공격으로 전환
        //     character.nowAction = Character.Action.Attack;

        //     // IDLE 애니메이션 전환
        //     character.animList[0].SetBool("Jump", false);
        //     // 바운스 애니메이션 시작
        //     character.animList[0].SetBool("isBounce", true);

        //     // 플레이어 위치 이동하는 동안 이동 금지
        //     PlayerManager.Instance.speedDeBuff = 0;
        //     // 이동 속도 반영
        //     PlayerManager.Instance.Move();
        //     // 플레이어 조작 차단
        //     PlayerManager.Instance.playerInput.Disable();

        //     // 플레이어를 슬라임 가운데로 이동 시키기
        //     PlayerManager.Instance.transform.DOMove(jumpLandPos, 0.5f)
        //     .OnComplete(() =>
        //     {
        //         // 이동 속도 디버프 걸기
        //         PlayerManager.Instance.speedDeBuff = 0.2f;
        //         // 이동 속도 반영
        //         PlayerManager.Instance.Move();
        //         // 플레이어 조작 활성화
        //         PlayerManager.Instance.playerInput.Enable();
        //     });
        // }
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
            character.animList[0].SetBool("isBounce", false);

            // 스케일 복구
            transform.localScale = Vector2.one;

            // 충돌 콜라이더 trigger 비활성화
            character.physicsColl.isTrigger = false;

            //현재 행동 초기화
            character.nowAction = Character.Action.Idle;
        }
    }
}
