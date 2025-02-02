using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;
using Lean.Pool;
using UnityEngine.Rendering;
using System.Text;

public class Bawi_AI : EnemyAI
{
    [Header("State")]
    [SerializeField, ReadOnly] bool startTransition = false;
    [SerializeField] Patten patten = Patten.None;
    enum Patten { FistDrop, BigStoneThrow, SmallStoneThrow, DrillDash, DrillChase, None, Skip };
    [SerializeField] float atkCoolCount; //쿨타임 카운트
    [SerializeField] float fistDropCoolTime;
    [SerializeField] float stoneThrowCoolTime;
    [SerializeField] float drillDashCoolTime;
    [SerializeField] float drillChaseCoolTime;
    [SerializeField] float atkRange = 20f; // 공격 범위
    [SerializeField] Vector2 fistParentLocalPos = new Vector2(5, 0); // fistParent 초기 위치
    [SerializeField] Vector2 drillParentLocalPos = new Vector2(-5, 0); // drillParent 초기 위치
    [SerializeField] Vector2 fistPartLocalPos = new Vector2(0, 5); // fistPart의 초기 위치
    [SerializeField] Vector2 drillPartLocalPos = new Vector2(0, -1); // drillPart 초기 위치
    [SerializeField, ReadOnly] Vector3 playerDir;
    [SerializeField, ReadOnly] bool isFloating = true; //부유 상태 여부
    [SerializeField] float aimCount = 1f; //드릴 조준 시간
    [SerializeField] float drillChaseSpeed = 15f; // 드릴 추적 속도

    [Header("Refer")]
    [SerializeField] TextMeshProUGUI stateText; //! 테스트 현재 상태
    [SerializeField] GameObject bigLandDust; //착지할때 헤드 먼지 파티클
    [SerializeField] GameObject smallLandDust; //착지할때 손 먼지 파티클
    [SerializeField] ParticleSystem chargePulse; // 차지 펄스 이펙트
    [SerializeField] ParticleSystem digDirtParticle; // 땅파기 파티클
    [SerializeField] ParticleSystem BurrowTrail; // 흙무더기 흔적 파티클
    [SerializeField] ParticleSystem DirtExplosion; // 흙 솟아나오며 터지는 파티클

    [Header("Head")]
    [SerializeField] SortingGroup headPart;
    [SerializeField] ParticleSystem headHoverEffect; // 머리 부유 이펙트
    [SerializeField] ParticleManager headDashDust; // 돌진시 땅에 남기는 먼지 이펙트

    [Header("Fist")]
    [SerializeField] Collider2D fistCrushColl; //주먹 으깨기 콜라이더
    [SerializeField] Transform fistParent; // 그림자까지 포함한 부모 오브젝트
    [SerializeField] Transform fistPart; // 주먹 스프라이트 부모 오브젝트
    [SerializeField] SpriteRenderer fistSprite; // 주먹 스프라이트
    [SerializeField] SpriteRenderer fistGhost; // 주먹 고스트 스프라이트
    [SerializeField] ParticleSystem fistHoverEffect; // 주먹 부유 파티클
    [SerializeField] GameObject fistGrabDust; //돌 부술때 먼지 파티클
    [SerializeField] GameObject fistLandDust; //주먹 내리찍을때 먼지 파티클
    [SerializeField] Sprite emptyFistSprite;
    [SerializeField] Sprite openFistSprite;
    [SerializeField] Sprite grabFistSprite;
    [SerializeField] GameObject stonePrefab; // 공격시 던질 돌 프리팹
    [SerializeField] ParticleManager fistDashDust; // 돌진시 땅에 남기는 먼지 이펙트
    [SerializeField] ParticleSystem fistChargeGathering; // 주먹 차지 기모으는 이펙트
    [SerializeField] ParticleSystem DirtExplosionCircle; // 원형 흙 튀기기

    [Header("Drill")]
    [SerializeField] Collider2D drillGhostColl; // 고스트 드릴 콜라이더
    [SerializeField] Rigidbody2D drillRigid; // 그림자까지 포함한 부모
    [SerializeField] Transform drillPart; // 드릴 스프라이트 부모 오브젝트
    [SerializeField] SpriteMask drillMask; // 드릴 땅속 들어가는 마스크
    [SerializeField] SpriteRenderer drillSprite; // 드릴 스프라이트
    [SerializeField] SpriteRenderer drillGhost; // 드릴 고스트 스프라이트
    [SerializeField] SpriteRenderer drillShadow; // 드릴 그림자
    [SerializeField] ParticleSystem drillHoverEffect; // 드릴 부유 파티클
    [SerializeField] Animator mainDrillAnim; // 메인 드릴 회전 애니메이터
    [SerializeField] Animator ghostDrillAnim; // 고스트 드릴 회전 애니메이터
    [SerializeField] ParticleSystem drillChargeGathering; // 드릴차지 기모으는 이펙트
    [SerializeField] ParticleManager drillDashDust; // 돌진시 땅에 남기는 먼지 이펙트
    AudioSource drillSpin; // 드릴 스핀 사운드

    private void Awake()
    {
        character = character == null ? GetComponent<Character>() : character;
    }

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());

        // 등장 트랜지션 재생
        // StartCoroutine(Appear());
    }

    IEnumerator Init()
    {
        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => character.enemy != null);

        //속도 초기화
        character.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        character.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //부유 상태로 전환
        isFloating = true;

        // // 고스트 드릴 오브젝트 비활성화
        // drillGhost.gameObject.SetActive(true);

        //드릴 스케일 및 위치 초기화
        drillGhost.transform.localScale = Vector2.one;
        drillGhost.transform.localPosition = Vector2.zero;

        // 각각 드릴 색 초기화
        drillSprite.color = Color.white;
        drillGhost.color = Color.clear;

        // 모든 파티클 초기화
        digDirtParticle.gameObject.SetActive(false);
        BurrowTrail.gameObject.SetActive(false);
        headDashDust.gameObject.SetActive(false);
        fistDashDust.gameObject.SetActive(false);
        fistChargeGathering.gameObject.SetActive(false);

        // 콜라이더 초기화
        character.physicsColl.enabled = false;
        drillGhostColl.enabled = false;
        fistCrushColl.enabled = false;
    }

    IEnumerator Appear()
    {
        // 등장 트랜지션 시작
        startTransition = false;

        //todo 머리, 주먹 숨기기
        headPart.gameObject.SetActive(false);
        fistPart.gameObject.SetActive(false);

        //todo 시간 멈추기
        SystemManager.Instance.TimeScaleChange(0f);
        // 플레이어 컨트롤 끄기
        PlayerManager.Instance.player_Input.Disable();
        // UI 컨트롤 끄기
        UIManager.Instance.UI_Input.Disable();

        float tweenTime = 2f;

        //todo 보스 위치로 카메라 이동
        UIManager.Instance.CameraMove(transform.position, 0.5f, true);
        //todo 천천히 카메라 줌인
        UIManager.Instance.CameraZoom(tweenTime, 3f);

        //todo 딜레이
        yield return new WaitForSecondsRealtime(1f);

        //todo 카메라 흔들기
        SystemManager.Instance.MainCamera.transform.DOShakePosition(2f, 1, 10, 90, false, false)
        .SetEase(Ease.InSine)
        .OnComplete(() =>
        {
            // 메인 카메라 위치 초기화
            SystemManager.Instance.MainCamera.transform.localPosition = Vector3.zero;
        });

        //todo 땅의 균열이 점점 커짐

        //todo 고스트 드릴 최대 크기로 켜기
        //todo 고스트 드릴 unscaledTime 회전
        //todo 땅속에서 거대 고스트 드릴이 튀어나와 화면 밖까지 올라감
        // 흙 파티클 튀기기
        // 카메라 짧고 크게 진동
        //todo 고스트 주먹이 착지
        // 흙 파티클 튀기기
        // 빠르게 줌아웃
        // 카메라 짧고 크게 진동
        //todo 주먹 고스트 줄어들며 해제
        //todo 주먹 뒤의 몸체 및 드릴 나와서 정위치

        //todo 양손 바닥에 2회 내리치기
        //todo 내리칠때마다 양손의 고스트 번쩍거리기
        //todo 입을 벌리고 사자후 외치기
        // 외치는동안 중간 강도로 길게 진동
        // 입에서 사자후 원형 도넛 이펙트 퍼지기
        // 외치는 동안 카메라 조금 줌인 했다가 끝날때쯤 복귀

        //todo 보스 체력 표시 및 시간 정지 해제

        // 등장 트랜지션 종료
        startTransition = true;
    }

    protected override void Update()
    {
        base.Update();

        // // 이동 리셋 카운트 차감
        // if (searchCoolCount > 0)
        //     searchCoolCount -= Time.deltaTime;

        // 몬스터 정보 없으면 리턴
        if (character.enemy == null)
            return;

        // 공격 중일때 리턴
        if (character.nowState == CharacterState.Attack)
            return;

        // 상태 이상 있으면 리턴
        if (!character.ManageState())
            return;

        //행동 관리
        ManageAction();
    }

    void ManageAction()
    {
        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // 플레이어 방향
        character.targetDir = character.movePos - transform.position;

        // 플레이어와의 거리
        float distance = character.targetDir.magnitude;

        // 플레이어 방향 쳐다보기
        if (character.targetDir.x > 0)
        {
            headPart.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            headPart.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        // Idle 아니면 리턴
        if (character.nowState != CharacterState.Idle)
            return;

        // 쿨타임 차감
        atkCoolCount -= Time.deltaTime;

        stateText.text = "CoolCount : " + atkCoolCount;

        // 쿨타임 됬을때, 범위 내에 있을때
        if (atkCoolCount <= 0 && distance <= atkRange)
        {
            //! 거리 확인용
            // stateText.text = "Close : " + distance;

            // 양쪽 손 회전값 초기화
            fistPart.transform.DORotate(new Vector3(0, 0, 90f), 0.5f);
            drillPart.transform.DORotate(new Vector3(0, 0, 90f), 0.5f);

            // 속도 초기화
            character.rigid.velocity = Vector3.zero;

            //공격 패턴 결정하기
            ChooseAttack();

            // if (moveSound != null)
            //     //todo 이동 사운드 정지
            //     SoundManager.Instance.StopSound(moveSound, 0.5f);

            return;
        }

        // 플레이어 따라가기
        Move();
    }

    void Move()
    {
        character.nowState = CharacterState.Move;

        // if (moveSound == null)
        //     //todo 이동 사운드 반복 재생
        //     moveSound = SoundManager.Instance.PlaySound("Bawi_Moving", transform, 0, 1f, 30, true);

        //애니메이터 켜기
        character.animList[0].enabled = true;
        // Idle 애니메이션으로 전환
        character.animList[0].SetBool("UseFist", false);
        character.animList[0].SetBool("UseDrill", false);

        // // 플레이어 근처 위치 계산
        // Vector3 playerPos = PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle * 0f;
        // //움직일 방향
        // Vector2 dir = playerPos - transform.position;

        // 움직일 방향 2D 각도
        float rotation = Mathf.Atan2(character.targetDir.y, character.targetDir.x) * Mathf.Rad2Deg;

        // 가려는 방향으로 양쪽 손이 회전
        fistPart.transform.rotation = Quaternion.Euler(0, 0, rotation);
        drillPart.transform.rotation = Quaternion.Euler(0, 0, rotation);

        // 거리가 1 이상일때
        if (character.targetDir.magnitude > 1f)
            //해당 방향으로 가속
            character.rigid.velocity = character.targetDir.normalized * character.speedNow * SystemManager.Instance.globalTimeScale;

        character.nowState = CharacterState.Idle;
    }

    public void Floating()
    {
        // 착지 상태일때 
        if (!isFloating)
        {
            // 부유 상태로 전환
            isFloating = true;

            // 물리 콜라이더 끄기
            character.physicsColl.enabled = false;

            // 부유 파티클 모두 켜기
            headHoverEffect.Play();
            drillHoverEffect.Play();
            fistHoverEffect.Play();

            // 드릴, 주먹 레이어 올리기
            drillSprite.GetComponent<SortingGroup>().sortingOrder = 1;
            fistSprite.GetComponent<SortingGroup>().sortingOrder = 1;
            // 머리 레이어 올리기
            headPart.sortingOrder = 1;

            //todo 호버링 사운드 재생
            // SoundManager.Instance.PlaySound("Bawi_Hover", transform.position);
        }
    }

    public void Landing()
    {
        // 부유 상태일때
        if (isFloating)
        {
            // 착지 상태로 전환
            isFloating = false;

            // 물리 콜라이더 켜기
            character.physicsColl.enabled = true;

            // 머리 먼지 파티클 생성
            LeanPool.Spawn(bigLandDust, transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

            if (character.animList[0].GetBool("UseDrill"))
            {
                // 드릴 먼지 파티클 생성
                LeanPool.Spawn(smallLandDust, fistParent.transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

                // 드릴 레이어 올리기
                drillSprite.GetComponent<SortingGroup>().sortingOrder = 1;
                // 주먹 레이어 내리기
                fistSprite.GetComponent<SortingGroup>().sortingOrder = 0;
            }

            if (character.animList[0].GetBool("UseFist"))
            {
                // 주먹 먼지 파티클 생성
                LeanPool.Spawn(smallLandDust, drillRigid.transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

                // 주먹 레이어 올리기
                fistSprite.GetComponent<SortingGroup>().sortingOrder = 1;
                // 드릴 레이어 내리기
                drillSprite.GetComponent<SortingGroup>().sortingOrder = 0;
            }

            // 머리 레이어 내리기
            headPart.sortingOrder = 0;

            // 착지 사운드 재생
            SoundManager.Instance.PlaySound("Bawi_Land");
        }

        //애니메이터 끄기
        character.animList[0].enabled = false;
    }

    void ChooseAttack()
    {
#if UNITY_EDITOR
        // 스킵이면 공격 취소
        if (patten == Patten.Skip)
            return;
#endif

        // 현재 액션 변경
        character.nowState = CharacterState.Attack;

        // 랜덤 패턴 결정
        int atkType = Random.Range(0, 5);

        // print("randomNum : " + randomNum);

#if UNITY_EDITOR
        // 테스트를 위해 패턴 고정
        if (patten != Patten.None)
            atkType = (int)patten;
#endif

        switch ((Patten)atkType)
        {
            case Patten.FistDrop:
                // 주먹 내려찍기 패턴
                StartCoroutine(FistDrop());
                //쿨타임 갱신
                atkCoolCount = fistDropCoolTime;
                break;
            case Patten.BigStoneThrow:
                // 큰 바위 던지기 패턴
                StartCoroutine(StoneThrow(false));
                //쿨타임 갱신
                atkCoolCount = stoneThrowCoolTime;
                break;
            case Patten.SmallStoneThrow:
                // 작은 돌 샷건 패턴
                StartCoroutine(StoneThrow(true));
                //쿨타임 갱신
                atkCoolCount = stoneThrowCoolTime;
                break;
            case Patten.DrillDash:
                // 드릴 돌진 패턴
                StartCoroutine(DrillDash());
                //쿨타임 갱신
                atkCoolCount = drillDashCoolTime;
                break;
            case Patten.DrillChase:
                // 드릴 추격 패턴
                StartCoroutine(DrillChase());
                //쿨타임 갱신
                atkCoolCount = drillChaseCoolTime;
                break;
        }
    }

    IEnumerator StoneThrow(bool isSmallStone = false)
    {
        // 머리 및 드릴 부유 이펙트 끄기
        headHoverEffect.Stop();
        drillHoverEffect.Stop();

        //주먹 공격 애니메이션으로 전환
        character.animList[0].SetBool("UseFist", true);

        // 보스 주변 랜덤 위치로 바위 위치 지정
        Vector2 grabPos = Random.insideUnitCircle.normalized * 10f;
        // 돌 잡는 시간
        float grabTime = 1f;

        // 바위 방향
        Vector2 dir = grabPos - (Vector2)transform.position;
        // 바위 방향 바라볼 2D 각도
        float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 플레이어 방향
        float playerAngle = 0f;

        // 바위 집기 시퀀스
        Sequence grabSeq = DOTween.Sequence();
        grabSeq
        .AppendCallback(() =>
        {
            // 손바닥으로 변경
            fistSprite.sprite = openFistSprite;
        })
        .Append(
            // 바위 위치로 손이 이동
            fistPart.transform.parent.DOLocalMove(grabPos, grabTime)
        )
        .Join(
            // 아래로 손 내려가기
            fistPart.transform.DOLocalMove(Vector3.up * 2f, grabTime)
        )
        .Join(
            // 잡는 위치 방향으로 회전
            fistPart.transform.DORotate(new Vector3(0, 0, rotation), grabTime)
        )
        .AppendCallback(() =>
        {
            // 바위 집은 손으로 변경
            fistSprite.sprite = grabFistSprite;

            // 바위 집는 사운드 재생
            SoundManager.Instance.PlaySound("Bawi_GrabStone");
        })
        // 잠시 대기
        .AppendInterval(0.5f)
        .AppendCallback(() =>
        {
            // 플레이어 방향
            playerDir = PlayerManager.Instance.transform.position - fistPart.transform.position;

            // 플레이어 방향 2D 각도
            playerAngle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;

            // 플레이어 방향으로 손이 회전
            fistPart.transform.DORotate(Vector3.forward * playerAngle, 1f);
        })
        .Join(
            // 던지기 로컬 위치로 이동
            fistPart.transform.parent.DOLocalMove(new Vector2(10f, 5f), 1f)
        )
        .Join(
            // 위로 손 올라가기
            fistPart.transform.DOLocalMove(Vector3.up * 5f, 1f)
        );

        //시퀀스 끝날때까지 대기
        yield return new WaitUntil(() => !grabSeq.active);

        // 타겟팅 지연시간
        float aimRate = 0.1f;

        // 일정시간 손이 플레이어 바라보기
        float aimCount = 1f;
        while (aimCount > 0)
        {
            // 플레이어 위치
            Vector3 playerPos = PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle * 3f;

            // 플레이어 방향
            playerDir = playerPos - fistPart.transform.position;

            // 플레이어 방향 2D 각도
            playerAngle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;

            // 플레이어 방향으로 손이 회전
            fistPart.transform.DORotate(Vector3.forward * playerAngle, aimRate);

            //시간 차감
            stateText.text = "Targeting : " + aimCount;
            aimCount -= aimRate;
            yield return new WaitForSeconds(aimRate);
        }

        // 작은 돌이면 돌 부수는 트랜지션
        if (isSmallStone)
        {
            // 바위 부수는 사운드 재생
            SoundManager.Instance.PlaySound("Bawi_CrushStone");

            // 주먹 위치 떨림
            fistPart.transform.DOPunchPosition(Vector3.up, 0.5f, 50, 1);

            // 주먹에서 먼지 파티클 발생
            LeanPool.Spawn(fistGrabDust, fistPart.transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

            yield return new WaitForSeconds(0.5f);
        }

        // 돌 발사 위치
        Vector2 throwPos = fistPart.transform.position + Quaternion.AngleAxis(playerAngle, Vector3.forward) * Vector3.right * 7f;

        //던지기 시작 각도
        float startAngle = playerAngle + 89f;
        //던지기 종료 각도
        float endAngle = playerAngle - 89f;

        // 던지기 시작 위치
        Vector2 startPos = fistPart.transform.position + Quaternion.AngleAxis(startAngle, Vector3.forward) * Vector3.right * 7f;
        // 던지기 종료 위치
        Vector2 endPos = fistPart.transform.position + Quaternion.AngleAxis(endAngle, Vector3.forward) * Vector3.right * 7f;

        // endPos가 더 높이 있을땐 startPos와 자리 바꾸기
        if (startPos.y < endPos.y)
        {
            Vector2 tempPos = startPos;

            startPos = endPos;
            endPos = tempPos;

            //던지기 시작 각도
            startAngle = playerAngle - 89f;
            //던지기 종료 각도
            endAngle = playerAngle + 89f;
        }

        //포물선 벡터
        Vector3[] parabola = { startPos, throwPos, endPos };

        float swingTime = 0.5f;

        // 던지기 시퀀스 재생
        Sequence throwSeq = DOTween.Sequence();
        throwSeq
        .Append(
            //던지기 시작할 위치로 이동
            fistPart.transform.parent.DOMove(startPos, 0.5f)
        )
        .Join(
            //던지기 전 각도로 회전
            fistPart.transform.DORotate(new Vector3(0, 0, startAngle), 0.5f)
        )
        .AppendCallback(() =>
        {
            //돌 날리기
            StartCoroutine(ShotStone(isSmallStone));

            // 던지기 사운드 재생
            SoundManager.Instance.PlaySound("Bawi_ThrowStone");
        })
        .Join(
            //포물선 그리며 손 휘두르기
            fistPart.transform.parent.DOPath(parabola, swingTime, PathType.CatmullRom, PathMode.TopDown2D, 10, Color.red)
        // .SetEase(Ease.OutBack)
        )
        .Join(
            // 던진 후 각도로 회전
            fistPart.transform.DORotate(new Vector3(0, 0, endAngle), swingTime)
        );

        // 던지기 시퀀스 끝날때까지 대기
        yield return new WaitUntil(() => !throwSeq.active);

        // 손 위치 기본 위치로 이동
        fistParent.transform.DOLocalMove(fistParentLocalPos, 0.5f);
        fistPart.transform.DOLocalMove(fistPartLocalPos, 0.5f)
        .OnComplete(() =>
        {
            // Idle 액션으로 전환
            character.nowState = CharacterState.Idle;

            // 일반 주먹으로 변경
            fistSprite.sprite = emptyFistSprite;
        });
    }

    IEnumerator ShotStone(bool isSmallStone)
    {
        // 손을 절반 휘두르는 만큼 대기
        yield return new WaitForSeconds(0.1f);

        //플레이어 위치
        Vector3 playerPos = PlayerManager.Instance.transform.position;

        //던질 돌 개수
        int stoneNum = 1;
        // 돌 사이즈
        Vector3 stoneScale = Vector3.one;

        // 작은돌 트리거면
        if (isSmallStone)
        {
            // 작은 돌 5개 던지기
            stoneNum = 5;
            // 돌 사이즈 작게
            stoneScale = Vector3.one * 0.5f;
        }
        else
        {
            // 큰돌 1개 던지기
            stoneNum = 1;
            // 돌 사이즈 크게
            stoneScale = Vector3.one;
        }

        for (int i = 0; i < stoneNum; i++)
        {
            // 중간에 돌 소환
            GameObject stoneObj = LeanPool.Spawn(stonePrefab, fistPart.transform.position, Quaternion.identity, ObjectPool.Instance.magicPool);

            // 아웃라인 색을 바꿔주기
            SpriteRenderer[] stoneSprites = stoneObj.GetComponentsInChildren<SpriteRenderer>(true);
            for (int j = 0; j < stoneSprites.Length; j++)
                stoneSprites[i].material.SetColor("_OutLineColor", new Color(1f, 0f, 0f, 1f));

            // 돌 스케일 설정
            stoneObj.transform.localScale = stoneScale;

            // 타겟 및 타겟위치 설정하기
            MagicHolder stoneMagicHolder = stoneObj.GetComponent<MagicHolder>();
            stoneMagicHolder.SetTarget(MagicHolder.TargetType.Player);

            // 마법 정보 인스턴스 만들어 넣기
            stoneMagicHolder.magic = new MagicInfo(MagicDB.Instance.GetMagicByName("SlingShot"));

            // 마법 랜덤 유지시간 추가
            stoneMagicHolder.AddDuration = Random.Range(0.5f, 1f) - 1f;

            // 마법 속도 배율 넣기
            stoneMagicHolder.MultipleSpeed = isSmallStone ? Random.Range(3f, 4f) : 2f;

            // 작은 돌일때
            if (isSmallStone)
                //플레이어 주변에 던지기
                stoneMagicHolder.targetPos = playerPos + (Vector3)Random.insideUnitCircle * 10f;
            // 큰 돌일때
            else
                //플레이어 정확하게 조준하고 던지기
                stoneMagicHolder.targetPos = playerPos;

            //마법 날리기
            MagicProjectile stoneProjec = stoneObj.GetComponent<MagicProjectile>();
            StartCoroutine(stoneProjec.ShotMagic());

            // 작은 돌일때
            if (isSmallStone)
            {
                //랜덤 대기시간
                float delay = Random.Range(0.01f, 0.05f);

                //일정 시간 대기
                yield return new WaitForSeconds(delay);
            }
        }

        // 손바닥으로 변경
        fistSprite.sprite = openFistSprite;
    }

    IEnumerator DrillDash()
    {
        // 머리 및 주먹 부유 이펙트 끄기
        headHoverEffect.Stop();
        fistHoverEffect.Stop();

        //드릴 사용 애니메이션으로 전환
        character.animList[0].SetBool("UseDrill", true);

        // 드릴 스핀 애니메이션 재생
        mainDrillAnim.SetBool("Spin", true);
        ghostDrillAnim.SetBool("Spin", true);

        //착지 할때까지 대기
        yield return new WaitUntil(() => !isFloating);

        // 차지 이펙트 켜기
        drillChargeGathering.gameObject.SetActive(true);
        drillChargeGathering.Play();

        // 드릴 머리보다 높게 들기
        Tween upTween = drillPart.transform.DOLocalMove(drillPartLocalPos - Vector2.up, 2f)
        .SetEase(Ease.InOutQuart);

        // 트윈 끝날때까지 대기
        yield return new WaitUntil(() => !upTween.active);

        // 랜덤 차지 횟수 정하기
        int chargeNum = Random.Range(1, 4);
        // 차지 횟수만큼 차지 반복
        yield return StartCoroutine(WeaponCharge(drillPart, drillPartLocalPos, chargeNum));

        // 차지 이펙트 끄기
        // StartCoroutine(drillChargeGathering.GetComponent<ParticleManager>().SmoothDisable());

        // 드릴 콜라이더 켜기
        drillGhostColl.enabled = true;

        //조준 시간 갱신
        aimCount = 2f;

        // 보스 위치 떨림
        transform.DOShakePosition(aimCount, 0.3f, 50, 90f, false, false);

        // 드릴 높이 낮추기
        drillPart.transform.DOLocalMove(drillPartLocalPos, 0.5f);

        // 드릴 타겟팅 지연시간
        float aimRate = 0.1f;

        // 바위 떨리는 사운드 재생
        AudioSource shakeSound = SoundManager.Instance.PlaySound("Bawi_Shake");

        // 조준 시간동안 플레이어 조준하기
        while (aimCount > 0)
        {
            // 플레이어 근처 위치
            Vector3 playerPos = PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle * 0.5f;

            // 보스에서 플레이어까지 방향
            playerDir = playerPos - transform.position;

            //드릴 위치
            Vector3 drillPos = transform.position + playerDir.normalized * 7f;

            // 플레이어 방향으로 드릴 위치 이동
            drillRigid.transform.DOMove(drillPos, aimRate);

            // 플레이어 방향의 각도
            float rotation = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;

            // 드릴이 플레이어 바라보기
            // drillPart.rotation = Quaternion.Euler(Vector3.forward * rotation);
            drillPart.transform.DORotate(Vector3.forward * rotation, aimRate);

            //시간 차감
            stateText.text = "AimCount : " + aimCount;
            aimCount -= aimRate;
            yield return new WaitForSeconds(aimRate);
        }

        // 떨리는 사운드 정지
        if (shakeSound != null)
            SoundManager.Instance.StopSound(shakeSound, 0.2f);

        // 대쉬 위치
        Vector2 dashPos = transform.position + (PlayerManager.Instance.transform.position - transform.position).normalized * 30f;

        // 대쉬 선딜 대기
        yield return new WaitForSeconds(0.5f);

        // 대쉬 먼지 파티클 켜기
        headDashDust.gameObject.SetActive(true);
        fistDashDust.gameObject.SetActive(true);
        // drillDashDust.SetActive(true);

        // 대쉬 사운드 재생
        SoundManager.Instance.PlaySound("Bawi_DrillDash");

        // 플레이어 방향으로 돌진
        transform.DOMove(dashPos, 1f)
        .SetEase(Ease.OutExpo);

        // 드릴 스핀 사운드 종료
        SoundManager.Instance.StopSound(drillSpin, 1f);

        //돌진하는 동안 대기
        yield return new WaitForSeconds(1f);

        // 드릴 콜라이더 끄기
        drillGhostColl.enabled = false;

        // 대쉬 먼지 파티클 끄기
        headDashDust.SmoothDisable();
        fistDashDust.SmoothDisable();

        //초기화 시간
        float resetTime = 1f;

        // 메인 드릴 색깔 복구
        drillSprite.DOColor(Color.white, resetTime);
        // 드릴 고스트 투명해지다 사라지고 스케일 초기화
        drillGhost.DOColor(new Color(1, 1, 1, 0f), resetTime)
        .OnComplete(() =>
        {
            // 고스트 드릴 스케일 초기화
            drillGhost.transform.localScale = Vector3.one;
        });

        // 드릴 사라지는 동안 대기
        yield return new WaitForSeconds(resetTime);

        // 드릴 위치, 각도, 스케일 초기화
        drillRigid.transform.DOLocalMove(drillParentLocalPos, resetTime);
        drillPart.transform.DOLocalMove(drillPartLocalPos, resetTime);
        drillPart.transform.DORotate(new Vector3(0, 0, 90f), resetTime);
        drillGhost.transform.DOScale(Vector3.one, resetTime);
        //그림자 스케일 초기화
        drillShadow.transform.DOScale(new Vector2(3f, 1f), resetTime);
        // 그림자 높이 초기화
        drillShadow.transform.DOLocalMove(Vector3.zero, resetTime);

        // 초기화 시간 대기
        yield return new WaitForSeconds(resetTime);

        // 드릴 스핀 애니메이션 종료
        mainDrillAnim.SetBool("Spin", false);
        ghostDrillAnim.SetBool("Spin", false);

        // Idle 애니메이션 재생
        character.animList[0].SetBool("UseDrill", false);
        // Idle 상태로 전환
        character.nowState = CharacterState.Idle;

        // 파츠 호버링 사운드 재생
        SoundManager.Instance.PlaySound("Bawi_Hover");
    }

    IEnumerator WeaponCharge(Transform partObj, Vector2 atkPos, int chargeNum, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        // 무기 및 고스트 찾기
        Transform mainObj = partObj.GetChild(0);
        Transform ghostObj = mainObj.GetChild(0);

        // 무기의 위치 찾기
        Vector3 mainObjPos = mainObj.localPosition;

        // 무기 및 고스트의 스프라이트 찾기
        SpriteRenderer weaponSprite = mainObj.GetComponent<SpriteRenderer>();
        SpriteRenderer ghostSprite = ghostObj.GetComponent<SpriteRenderer>();

        // 무기의 그림자 찾기
        Transform shadowObj = partObj.parent.Find("Shadow");

        for (int i = 0; i < chargeNum; i++)
        {
            // 현재 무기 스케일 구하기
            Vector3 nowGhostScale = ghostObj.localScale;
            //다음 무기 스케일 지정
            Vector3 upGhostScale = new Vector3(nowGhostScale.x + 1, nowGhostScale.y + 1, 1);

            //무기 차지 시간
            float reboundUpTime = 0.8f;

            // 드릴일때
            if (partObj != fistPart)
            {
                // 드릴 사운드 이름 계산
                StringBuilder spinSound = new StringBuilder();
                spinSound.Append("Bawi_DrillSpin");
                spinSound.Append(i);

                print(spinSound.ToString());

                // 드릴 스핀 사운드 반복 재생
                drillSpin = SoundManager.Instance.PlaySound(spinSound.ToString(), 1f, 0, -1, true);
            }

            // 무기 차지 시퀀스 (1 ~ 3번 랜덤 차지)
            Sequence chargeSeq = DOTween.Sequence();
            chargeSeq
            .AppendCallback(() =>
            {
                // 고스트 무기 오브젝트 활성화
                ghostObj.gameObject.SetActive(true);
            })
            .Append(
                // 반동에 의해 살짝 내려감
                mainObj.DOLocalMove(mainObjPos - Vector3.right * 0.5f, 0.2f)
                .SetEase(Ease.OutBack)
            )
            .AppendCallback(() =>
            {
                if (i == chargeNum - 1)
                {
                    //차지 이펙트 끄기
                    drillChargeGathering.Stop();
                }

                // 차지 펄스 이펙트 발생
                LeanPool.Spawn(chargePulse, partObj.position, Quaternion.identity, partObj);

                // 파워업 사운드 재생
                SoundManager.Instance.PlaySound("Bawi_PowerUp");
            })
            .Append(
                //원래 높이로 복구
                mainObj.DOLocalMove(mainObjPos, reboundUpTime)
            )
            .Join(
                // 메인 무기 반투명하게
                weaponSprite.DOColor(new Color(1, 1, 1, 0.5f), reboundUpTime)
            )
            .Join(
                // 고스트 무기 매우 밝게 나타내기
                ghostSprite.DOColor(new Color(1, 1, 1, 1f), reboundUpTime)
            )
            .Join(
                // 고스트 무기 스케일 키우기
                ghostObj.DOScale(upGhostScale, reboundUpTime)
            )
            .Join(
                // 그림자 크기 키우기
                shadowObj.DOScale(new Vector3(3f, 1f, 0) * (i + 2), reboundUpTime)
            )
            .Join(
                // 고스트 무기 색 안정화
                ghostSprite.DOColor(new Color(1, 1, 1, 0.5f), reboundUpTime)
            );

            // 시퀀스 끝날때까지 대기
            yield return new WaitUntil(() => !chargeSeq.active);

            // 마지막 차지가 아닐때만
            if (i < chargeNum - 1)
                // 드릴 스핀 사운드 종료
                SoundManager.Instance.StopSound(drillSpin, 1f);
        }

        // 차지 이펙트 끄기
        if (partObj == fistPart)
            fistChargeGathering.GetComponent<ParticleManager>().SmoothDisable();
        else
            drillChargeGathering.GetComponent<ParticleManager>().SmoothDisable();
    }

    IEnumerator FistDrop()
    {
        // 머리 및 드릴 부유 이펙트 끄기
        headHoverEffect.Stop();
        drillHoverEffect.Stop();

        // 주먹 공격 애니메이션 켜기
        character.animList[0].SetBool("UseFist", true);

        //착지 할때까지 대기
        yield return new WaitUntil(() => !isFloating);

        // 차지 이펙트 켜기
        fistChargeGathering.gameObject.SetActive(true);
        fistChargeGathering.Play();

        // 랜덤 차지 횟수 정하기
        int chargeNum = Random.Range(1, 4);
        //!
        // chargeNum = 3;

        // 차지 횟수에 따라 주먹 높이 올리기, 플레이어에게 차지 횟수 힌트가 됨
        fistPart.transform.DOLocalMove(new Vector3(0, 10f + 3f * chargeNum, 0), 1f);

        //주먹이 아래를 보게 회전
        fistPart.transform.DORotate(new Vector3(0, 0, -90f), 1f);

        // 랜덤 레벨로 주먹 커지기
        StartCoroutine(WeaponCharge(fistPart, fistPartLocalPos, chargeNum, 0.5f));

        // 조준시간 입력
        aimCount = 0.5f * (chargeNum + 1) + 0.5f;

        //콜라이더 끄기
        fistCrushColl.enabled = false;

        // 플레이어 위치 부드럽게 따라가기
        while (aimCount > 0)
        {
            // 플레이어 위치 계산, 오차 추가
            Vector3 playerPos = PlayerManager.Instance.transform.position + Random.insideUnitSphere * 3f;
            // 이동할 위치 계산
            Vector2 movePos = Vector2.Lerp(fistParent.position, playerPos, Time.deltaTime * 5f);

            // 주먹 위치 이동
            fistParent.position = movePos;

            // 시간 차감 후 대기
            stateText.text = "Targeting : " + aimCount;
            aimCount -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 주먹 멈춰서 진동
        fistPart.transform.DOShakePosition(0.2f, 0.3f, 50, 90f, false, false);
        yield return new WaitForSeconds(0.2f);

        // 주먹 떨어뜨리기
        fistPart.transform.DOLocalMove(new Vector3(0, 1.5f + 3f * chargeNum, 0), 0.5f)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
            // 흙 튀는 파티클 개수 수정
            ParticleSystem.EmissionModule emission = DirtExplosionCircle.emission;
            ParticleSystem.Burst burst = emission.GetBurst(0);
            burst.count = 30f + chargeNum * 10f;
            DirtExplosionCircle.emission.SetBurst(0, burst);
            // 흙 튀는 파티클 재생
            DirtExplosionCircle.Play();
            // LeanPool.Spawn(DirtExplosion, fistParent.position, Quaternion.identity, ObjectPool.Instance.effectPool);

            // 착지 먼지 이펙트 생성
            GameObject landDust = LeanPool.Spawn(smallLandDust, fistParent.position, Quaternion.identity, ObjectPool.Instance.effectPool);
            // 먼지 이펙트 사이즈 설정
            landDust.transform.localScale = Vector3.one * (chargeNum + 1);

            // 카메라 흔들기
            UIManager.Instance.CameraShake(0.2f, 0.3f, 50, 90f, false, false);

            // 콜라이더 켜기
            fistCrushColl.enabled = true;

            // 주먹 내려찍기 사운드 재생
            SoundManager.Instance.PlaySound("Bawi_FistImpact");
        });

        // 주먹 떨어지는데 1초, 0.5초 대기
        yield return new WaitForSeconds(1.2f);

        // 주먹 콜라이더 끄기
        fistCrushColl.enabled = false;

        yield return new WaitForSeconds(0.3f);

        //초기화 시간
        float resetTime = 1f;

        // 주먹 위치 초기화
        fistParent.transform.DOLocalMove(fistParentLocalPos, 1f);
        // 주먹 높이 초기화
        fistPart.transform.DOLocalMove(fistPartLocalPos, 1f);
        // 주먹 각도 초기화
        fistPart.transform.DORotate(new Vector3(0, 0, 90f), resetTime);
        // 주먹 고스트 스케일 초기화
        fistGhost.transform.DOScale(Vector3.one, resetTime);
        // 그림자 스케일 초기화
        fistPart.parent.Find("Shadow").DOScale(new Vector3(3f, 1f), resetTime);
        // 주먹 색 초기화
        fistSprite.DOColor(Color.white, resetTime);
        // 주먹 고스트 색 초기화
        fistGhost.DOColor(Color.clear, resetTime);

        yield return new WaitForSeconds(1f);

        // Idle 애니메이션 재생
        character.animList[0].SetBool("UseFist", false);
        // Idle 상태로 전환
        character.nowState = CharacterState.Idle;

        // 파츠 호버링 사운드 재생
        SoundManager.Instance.PlaySound("Bawi_Hover");
    }

    IEnumerator DrillChase()
    {
        // 머리 및 주먹 부유 이펙트 끄기
        headHoverEffect.Stop();
        fistHoverEffect.Stop();

        // 드릴 사용 애니메이션 전환
        character.animList[0].SetBool("UseDrill", true);

        // 드릴 스핀 애니메이션 재생
        mainDrillAnim.SetBool("Spin", true);

        // 드릴 콜라이더 켜기
        drillGhostColl.enabled = true;

        //착지 할때까지 대기
        yield return new WaitUntil(() => !isFloating);

        //부유 이펙트 끄기
        drillHoverEffect.Stop();

        // 드릴이 아래를 보게 회전
        drillPart.DOLocalRotate(new Vector3(0, 0, 90f), 1f);

        // 드릴 머리보다 높게 들기
        drillPart.DOLocalMove(Vector2.down * 2f, 1f)
        .SetEase(Ease.InOutQuart);

        // 드릴 스핀 사운드 재생
        SoundManager.Instance.PlaySound("Bawi_DrillSpin");

        // 드릴 다 들때까지 대기
        yield return new WaitForSeconds(1f);

        //드릴 마스크 켜기
        drillMask.enabled = true;

        //부유 이펙트 켜기
        drillHoverEffect.Play();
        // 부유 이펙트 빠르게
        ParticleSystem.EmissionModule hoverEmission = drillHoverEffect.emission;
        hoverEmission.rateOverTime = 30f;

        AudioSource drillSound = null;

        // 드릴 내려서 땅에 박기
        drillPart.DOLocalMove(Vector2.down * 0.3f, 0.5f)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
            //부유 이펙트 끄기
            drillHoverEffect.Stop();
            // 부유 이펙트 속도 초기화
            hoverEmission.rateOverTime = 5f;

            //땅파기 파티클 켜기
            digDirtParticle.gameObject.SetActive(true);
            digDirtParticle.Play();

            //흙무더기 파티클 켜기
            BurrowTrail.gameObject.SetActive(true);
            BurrowTrail.Play();

            // 드릴 땅에 꽂히는 사운드 재생
            SoundManager.Instance.PlaySound("Bawi_Drill_Impact");
            // 드릴 땅파기 사운드 재생
            drillSound = SoundManager.Instance.PlaySound("Bawi_DrillDig");
        });

        // 동시에 그림자 사이즈 줄여 없에기
        drillShadow.transform.DOScale(Vector3.zero, 0.5f)
        .SetEase(Ease.InBack);

        yield return new WaitForSeconds(0.5f);

        // 드릴 콜라이더 끄기
        drillGhostColl.enabled = false;

        // 드릴 내려서 땅속으로 천천히 내리기
        drillPart.DOLocalMove(Vector2.up * 0.5f, 1f)
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            // 땅파기 파티클 끄기
            digDirtParticle.GetComponent<ParticleManager>().SmoothDisable();

            // 드릴 땅파기 사운드 끄기
            SoundManager.Instance.StopSound(drillSound, 0.5f);
        });

        yield return new WaitForSeconds(2f);

        // 드릴 회전 초기화
        drillPart.localRotation = Quaternion.Euler(Vector3.forward * -90f);

        // 조준시간 입력
        aimCount = 10f;

        // 드릴 추적 사운드 반복 재생
        drillSound = SoundManager.Instance.PlaySound("Bawi_DrillChasing", drillSprite.transform, 1f, 0, -1, true);

        // 플레이어 위치 부드럽게 따라가기
        while (aimCount > 0)
        {
            // 플레이어 가까워지면 반복문 끝내기
            if (Vector2.Distance(drillRigid.position, PlayerManager.Instance.transform.position) <= 1f)
                break;

            // 플레이어 근처 위치 추적
            Vector3 playerPos = PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle * 3f;

            // 드릴에서 플레이어까지 방향
            playerDir = playerPos - drillRigid.transform.position;

            // 플레이어 방향으로 드릴 이동
            drillRigid.velocity = Vector2.Lerp(drillRigid.velocity.normalized, playerDir.normalized, Time.deltaTime * 10f) * drillChaseSpeed;

            // 시간 차감 후 대기
            stateText.text = "Targeting : " + aimCount;
            aimCount -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        if (drillSound != null)
            // 드릴 추적 사운드 정지
            SoundManager.Instance.StopSound(drillSound, 0.5f);

        // 드릴 속도 멈추기
        drillRigid.velocity = Vector3.zero;

        // 드릴 콜라이더 켜기
        drillGhostColl.enabled = true;

        // 흙무더기 파티클 끄기
        BurrowTrail.GetComponent<ParticleManager>().SmoothDisable();

        // 땅에서 나올때 튀는 흙 파티클
        LeanPool.Spawn(DirtExplosion, drillRigid.position, Quaternion.identity, ObjectPool.Instance.effectPool);

        // 드릴 솟아 나오기
        drillPart.DOLocalMove(Vector2.down * 2f, 1f)
        .SetEase(Ease.OutBack);

        // 동시에 그림자 사이즈 초기화
        drillShadow.transform.DOScale(new Vector3(3f, 1f, 1f), 0.5f)
        .SetEase(Ease.OutBack);

        // 드릴 튀어나올때 사운드 재생
        SoundManager.Instance.PlaySound("Bawi_Drill_Impact");

        yield return new WaitForSeconds(2f);

        // 드릴 콜라이더 끄기
        drillGhostColl.enabled = false;

        //드릴 마스크 끄기
        drillMask.enabled = false;

        // 드릴 위치 초기화
        drillRigid.transform.DOLocalMove(drillParentLocalPos, 1f);

        // 드릴 스핀 애니메이션 종료
        mainDrillAnim.SetBool("Spin", false);

        // Idle 애니메이션으로 전환
        character.animList[0].SetBool("UseDrill", false);
        // Idle 상태로 전환
        character.nowState = CharacterState.Idle;

        // 파츠 호버링 사운드 재생
        SoundManager.Instance.PlaySound("Bawi_Hover");
    }
}