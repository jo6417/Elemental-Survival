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
    [SerializeField] Attack absorbAttack; // 흡수 공격 컴포넌트

    [Header("Jump")]
    [SerializeField] private GameObject landEffect;
    [SerializeField] Vector2 jumpLandPos; //점프 착지 위치
    [SerializeField] string[] jumpSounds = { "KingSlime_Jump1", "KingSlime_Jump2", "KingSlime_Jump3" };
    int jump_lastIndex = -1;

    [Header("PoisonShot")]
    [SerializeField] Color poisonColor;
    [SerializeField] float poisonFillTime = 5f;
    [SerializeField] SpriteRenderer spriteFill; // 독 뿜기 기모을때 차오르는 스프라이트
    [SerializeField] Transform poisonDrop; // 독방울 프리팹
    [SerializeField] Transform poisonPool; // 독장판 프리팹
    [SerializeField] GameObject atkMarker; // 공격 위치 표시
    [SerializeField] AnimationCurve dropCurve; // 독방울 날아가는 커브

    [Header("BabySlimeSummon")]
    [SerializeField] float summonFillTime = 7f;
    [SerializeField] Color summonColor;
    public GameObject slimePrefab; //새끼 슬라임 프리팹

    [Header("Debug")]
    [SerializeField] Transform debugCanvas;

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
        character.nowAction = Character.State.Rest;

        // 독 채우기 이미지 끄기
        spriteFill.gameObject.SetActive(false);

        // 콜라이더 충돌 초기화
        character.physicsColl.enabled = false;
        character.physicsColl.isTrigger = false;

        // 플레이어 체력 흡수 콜백 넣기
        absorbAttack.attackCallback += AbsorbPlayer;

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

        // 초기화 끝 행동 시작
        character.nowAction = Character.State.Idle;
    }

    private void Update()
    {
        if (character.enemy == null)
            return;

        // 흡수 쿨타임 카운트 차감
        if (absorbCoolCount > 0)
            absorbCoolCount -= Time.deltaTime;

        // 공격중 아닐때
        if (character.nowAction != Character.State.Attack)
        {
            // 쿨타임 카운트 차감
            if (coolCount > 0)
                coolCount -= Time.deltaTime;
        }

        //움직일 방향
        Vector3 playerDir = PlayerManager.Instance.transform.position - transform.position;

        //! 거리 및 쿨타임 디버깅
        stateText.text = $"Distance : {string.Format("{0:0.00}", playerDir.magnitude)} \n CoolCount : {string.Format("{0:0.00}", coolCount)}";

        // Idle 아니면 리턴
        if (character.nowAction != Character.State.Idle)
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

    void FlipBody()
    {
        //움직일 방향에 따라 몸체 좌우반전
        if (playerDir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        // 디버그용 캔버스 좌우반전
        debugCanvas.rotation = Quaternion.Euler(0, 0, 0);
    }

    public void JumpSound()
    {
        // 걷기 발소리 재생
        jump_lastIndex = SoundManager.Instance.PlaySoundPool(jumpSounds.ToList(), transform.position, jump_lastIndex);
    }

    void AbsorbPlayer()
    {
        // 흡수 쿨타임 됬을때, 플레이어 대쉬 아닐때
        if (absorbCoolCount <= 0f && !PlayerManager.Instance.isDash)
        {
            // 현재 페이즈에 따라 공격력 계산 (1페이즈 1배, 2페이즈 1.5배, 3페이즈 2배)
            float damage = 3f * (nowPhase - (nowPhase - 1) * 0.5f);

            // 플레이어 체력 깎기
            PlayerManager.Instance.hitBox.Damage(damage, false);

            // 플레이어가 입은 데미지만큼 보스 회복
            character.hitBoxList[0].Damage(-damage, false, transform.position);

            // 쿨타임 갱신
            absorbCoolCount = 1f;
        }
    }

    void ManageAction()
    {
        // 바라보는 방향으로 좌우반전
        FlipBody();

        // 쿨타임 제로일때, 플레이어가 공격범위 이내일때
        if (coolCount <= 0 && playerDir.magnitude <= attackDistance)
        {
            // 현재 행동 공격으로 전환
            character.nowAction = Character.State.Attack;

            //공격 패턴 선택
            ChooseAttack();

            return;
        }
        else
        {
            // 현재 행동 점프로 전환
            character.nowAction = Character.State.Jump;

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
    }

    void JumpStart()
    {
        // print("점프 시작");

        // 점프 애니메이션으로 전환
        character.animList[0].SetBool("Jump", true);

        // 스프라이트 전체 레이어 레벨 높이기
        sorting.sortingOrder = 1;
    }

    public void JumpMove()
    {
        // 플레이어 방향 계산
        playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 바라보는 방향으로 좌우반전
        FlipBody();

        //움직일 거리, 플레이어 위치까지 갈수 있으면 플레이어 위치, 못가면 적 스피드
        float distance = playerDir.magnitude > character.rangeNow ? character.rangeNow : playerDir.magnitude;

        //착지 위치 변수에 저장
        jumpLandPos = transform.position + playerDir.normalized * distance;

        //플레이어 위치까지 doMove
        transform.DOMove(jumpLandPos, 1f);

        // 콜라이더 끄기
        character.physicsColl.enabled = false;

        // 점프 사운드 재생
        JumpSound();
    }

    public void Landing()
    {
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
        // IDLE 애니메이션 전환
        character.animList[0].SetBool("Jump", false);

        // 현재 행동 끝내기
        character.nowAction = Character.State.Idle;
    }

    IEnumerator WaterFill(Color fillColor, float fillTime)
    {
        // 기본 패턴의 시간에 페이즈 반영, 페이즈 높을수록 빨라짐
        fillTime = fillTime - (nowPhase - 1) * 2;

        // 떨림 애니메이션 시작
        character.animList[0].speed = 1f;
        character.animList[0].SetBool("isShaking", true);

        // 물 차오르는 소리 재생
        SoundManager.Instance.PlaySound("KingSlime_Fill");
        // 물 차오르는 소리 끄기 예약
        SoundManager.Instance.StopSound("KingSlime_Fill", 1f, fillTime);

        // 채우기 서브 스프라이트 찾기
        SpriteRenderer fillSub = spriteFill.transform.GetChild(0).GetComponent<SpriteRenderer>();

        // 채우기 스프라이트 위치 초기화
        spriteFill.transform.localPosition = Vector2.down * 5f;
        // 채우기 보조 스프라이트 위치 초기화
        spriteFill.transform.GetChild(0).localPosition = new Vector2(1f, 1f);
        // 채우기 스프라이트 나타내기
        spriteFill.gameObject.SetActive(true);

        // 채우기 스프라이트 켜기
        fillColor.a = 100f / 255f;
        spriteFill.color = fillColor;
        fillSub.color = fillColor;

        // 채우기 이미지 켜기
        spriteFill.gameObject.SetActive(true);

        // 채우기 스프라이트 올라가기
        spriteFill.transform.DOLocalMove((Vector2)spriteFill.transform.localPosition + Vector2.up * 1f, 0.5f)
        .SetLoops(9, LoopType.Incremental)
        .SetEase(Ease.OutBack);

        // 내부 서브 스프라이트 애니메이션
        fillSub.transform.DOLocalMove((Vector2)spriteFill.transform.GetChild(0).localPosition + Vector2.down * 2f, 0.5f)
        .SetLoops(8, LoopType.Yoyo)
        .OnComplete(() =>
        {
            spriteFill.transform.GetChild(0).DOLocalMove(new Vector2(1f, 0), 0.5f);
        });

        yield return new WaitForSeconds(fillTime);

        //떨림 애니메이션 종료
        character.animList[0].SetBool("isShaking", false);

        // 납작해지기
        character.spriteObj.DOScale(new Vector2(1.2f, 0.8f), 0.5f);
        // 납작해지며 왕관도 내려가기
        crownObj.DOLocalMove(new Vector2(0, -1), 0.5f);

        // 납작해지기 완료까지 대기
        yield return new WaitUntil(() => character.spriteObj.localScale == new Vector3(1.2f, 0.8f));

        // 채우기 스프라이트 끄기
        fillColor.a = 0;
        spriteFill.DOColor(fillColor, 1f);
        // 채우기 서브 스프라이트 끄기
        fillSub.DOColor(fillColor, 1f);
    }

    IEnumerator BabySlimeSummon()
    {
        print("슬라임 소환 패턴");

        // 몸안에 초록색 물 채우기
        yield return StartCoroutine(WaterFill(summonColor, summonFillTime));

        // 바운스 애니메이션 시작
        character.animList[0].SetBool("isBounce", true);

        // 페이즈에 따라 소환 횟수 산출
        int summonNum = 5 * nowPhase;
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
            Vector2 summonPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * Random.Range(5f, 20f);

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

            // 슬라임 소환 사운드 재생
            SoundManager.Instance.PlaySound("KingSlime_Slime_Summon");

            //소환 딜레이
            yield return new WaitForSeconds(0.2f);
        }

        // 바운스 애니메이션 끝
        character.animList[0].SetBool("isBounce", false);

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
                character.nowAction = Character.State.Idle;
            });
        });

        yield return null;
    }

    IEnumerator PoisonShot()
    {
        // 몸안에 보라색 물 채우기
        yield return StartCoroutine(WaterFill(poisonColor, poisonFillTime));

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

            // 독방울 투척 소리 재생
            SoundManager.Instance.PlaySound("KingSlime_Poison_Drop");

            // 독장판 범위
            float poisonRange = 10f * nowPhase;

            // 독방울 전방 착지 위치 계산 = 바라보는 방향에서 범위 안에 드랍
            Vector2 dropPos = (Vector2)transform.position + playerDir + Random.insideUnitCircle * poisonRange / 2f;

            // 떨어질 위치에 마커 생성
            GameObject marker = LeanPool.Spawn(atkMarker, dropPos, Quaternion.identity, SystemManager.Instance.effectPool);

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
                // 마커 없에기
                LeanPool.Despawn(marker);

                // 떨어진 위치에 독장판 소환
                LeanPool.Spawn(poisonPool, poisonDropObj.position, Quaternion.identity, SystemManager.Instance.effectPool);

                // 독방울 디스폰
                LeanPool.Despawn(poisonDropObj);

                // 독장판 생성 소리 재생
                SoundManager.Instance.PlaySound("KingSlime_Poison_Pool");
            });

            // 독방울 사이 딜레이
            yield return new WaitForSeconds(0.2f);
        }

        // 독 채우기 이미지 끄기
        spriteFill.gameObject.SetActive(false);

        // 스케일 복구
        character.spriteObj.DOScale(Vector2.one, 0.3f)
        .SetEase(Ease.InOutBack);
        // 왕관 위치 복구
        crownObj.DOLocalMove(Vector2.zero, 0.3f)
        .SetEase(Ease.InOutBack);

        yield return new WaitForSeconds(0.5f);

        // 현재 행동 초기화
        character.nowAction = Character.State.Idle;
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
        // // 흡수 중일때 플레이어 나가면
        // if (other.gameObject.CompareTag(SystemManager.TagNameList.Player.ToString())
        // && nowAbsorb)
        // {
        //     // print("흡수 끝");

        //     // 플레이어 흡수 정지
        //     nowAbsorb = false;

        //     // 점프 끝날때 초기화 함수 실행
        //     JumpEnd();

        //     // 이동 속도 디버프 해제
        //     PlayerManager.Instance.speedDeBuff = 1f;
        //     // 이동 속도 반영
        //     PlayerManager.Instance.Move();

        //     // 바운스 애니메이션 끝
        //     character.animList[0].SetBool("isBounce", false);

        //     // 스케일 복구
        //     transform.localScale = Vector2.one;

        //     // 충돌 콜라이더 trigger 비활성화
        //     character.physicsColl.isTrigger = false;

        //     //현재 행동 초기화
        //     character.nowAction = Character.State.Idle;
        // }
    }
}
