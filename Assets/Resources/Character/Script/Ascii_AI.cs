using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Lean.Pool;
using System.Linq;

public class Ascii_AI : MonoBehaviour
{
    [Header("State")]
    bool initDone = false;
    [SerializeField] Patten patten = Patten.None;
    enum Patten { PunchAttack, StopLaserAtk, GroundAttack, Skip, None };
    public float farDistance = 10f;
    public float closeDistance = 5f;
    float speed;
    List<int> atkList = new List<int>(); //공격 패턴 담을 변수

    [Header("Refer")]
    public Character character;
    EnemyInfo enemy;
    public Image angryGauge; //분노 게이지 이미지
    public TextMeshProUGUI faceText;
    public Transform canvasChildren;
    public Animator anim;
    public Rigidbody2D rigid;
    public SpriteRenderer shadow;

    [Header("Cable")]
    public LineRenderer L_Cable;
    public LineRenderer R_Cable;
    public Transform L_CableStart;
    public Transform R_CableStart;
    public SpriteRenderer L_PlugHead;
    public SpriteRenderer R_PlugHead;
    public Transform L_PlugTip;
    public Transform R_PlugTip;
    public Animator L_Plug;
    public Animator R_Plug;
    public EnemyAttack L_PlugAtk;
    public EnemyAttack R_PlugAtk;
    public GameObject L_CableSpark; //케이블 타고 흐르는 스파크
    public GameObject R_CableSpark; //케이블 타고 흐르는 스파크
    public ParticleSystem groundCrackEffect; //땅 갈라지는 이펙트
    public GameObject groundElectro; //바닥 전기 공격 프리팹

    [Header("FallAtk")]
    public Collider2D fallAtkColl; // 해당 컴포넌트를 켜야 fallAtk 타격 가능
    public EnemyAtkTrigger fallRangeTrigger; //엎어지기 범위 내에 플레이어가 들어왔는지 보는 트리거
    public SpriteRenderer fallRangeBackground;
    public SpriteRenderer fallRangeIndicator;
    public ParticleSystem fallDustEffect; //엎어질때 발생할 먼지 이펙트
    public bool fallAtkDone = false; //방금 폴어택 했을때 true, 다른공격 하면 취소

    [Header("LaserAtk")]
    public GameObject LaserPrefab; //발사할 레이저 마법 프리팹
    public GameObject pulseEffect; //laser stop 할때 펄스 이펙트
    MagicInfo laserMagic = null; //발사할 레이저 마법 데이터
    SpriteRenderer laserRange;
    public EnemyAtkTrigger LaserRangeTrigger; //레이저 범위 내에 플레이어가 들어왔는지 보는 트리거
    public TextMeshProUGUI laserText;

    [Header("Cooltime")]
    public float coolCount;
    public float fallCooltime = 1f; //
    public float laserCooltime = 3f; //무궁화꽃 쿨타임
    public float groundPunchCooltime = 5f; // 그라운드 펀치 쿨타임
    public float earthGroundCooltime = 8f; //접지 패턴 쿨타임

    [Header("Sound")]
    [SerializeField] string[] walkSounds = { "Ascii_Walk1", "Ascii_Walk2", "Ascii_Walk3" };
    int walk_lastIndex = -1;

    //! 테스트
    [Header("Debug")]
    public TextMeshProUGUI stateText;

    private void Awake()
    {
        character = character == null ? GetComponentInChildren<Character>() : character;
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();

        fallRangeBackground = fallRangeTrigger.GetComponent<SpriteRenderer>();
        laserRange = LaserRangeTrigger.GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 초기화 안됨
        initDone = false;

        //표정 초기화
        faceText.text = "...";

        rigid.velocity = Vector2.zero; //속도 초기화

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => character.enemy != null);

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (enemy == null)
            enemy = character.enemy;

        // fallAtk 공격 비활성화
        fallAtkColl.enabled = false;

        transform.DOKill();

        //애니메이션 스피드 초기화
        if (anim != null)
            anim.speed = 1f;

        // 위치 고정 해제
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //스피드 초기화
        speed = enemy.speed;

        //공격범위 오브젝트 초기화
        fallRangeBackground.enabled = false;
        laserRange.enabled = false;

        //그림자 초기화
        shadow.gameObject.SetActive(true);

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 레이저 마법 데이터 찾기
        if (laserMagic == null)
        {
            //찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            laserMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("LaserBeam"));

            // 강력한 데미지로 고정
            laserMagic.power = 20f;
        }

        // 초기화 완료
        initDone = true;
    }

    private void Update()
    {
        if (enemy == null || laserMagic == null)
            return;

        // 상태 이상 있으면 리턴
        if (!character.ManageState())
            return;

        // AI 초기화 완료 안됬으면 리턴
        if (!initDone)
            return;

        // 행동 관리
        ManageAction();
    }

    void ManageAction()
    {
        // Idle 아니면 리턴
        if (character.nowAction != Character.Action.Idle)
            return;

        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // 공격 쿨타임 차감
        if (coolCount > 0)
            coolCount -= Time.deltaTime;

        // 플레이어 방향
        Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 플레이어와의 거리
        float playerDistance = playerDir.magnitude;

        //!
        fallAtkDone = false;

        // 폴어택 범위에 들어왔을때, 마지막 공격이 폴어택이 아닐때
        if (fallRangeTrigger.atkTrigger && !fallAtkDone)
        {
            //! 거리 확인용
            stateText.text = "Fall : " + playerDistance;

            // 속도 초기화
            character.rigid.velocity = Vector3.zero;

            // 현재 액션 변경
            character.nowAction = Character.Action.Attack;

            // 폴어택 공격
            FalldownAttack();

            // 연속 폴어택 방지
            fallAtkDone = true;

            // 쿨타임 갱신
            coolCount = fallCooltime;

            return;
        }

        // 공격 범위내에 있고 공격 쿨타임 됬을때
        if (playerDistance <= farDistance && playerDistance >= closeDistance && coolCount <= 0)
        {
            //! 거리 확인용
            stateText.text = "Attack : " + playerDistance;

            // 속도 초기화
            character.rigid.velocity = Vector3.zero;

            // // 현재 액션 변경
            // character.nowAction = Character.Action.Attack;

            //공격 패턴 결정하기
            ChooseAttack();
        }
        else
        {
            //! 거리 확인용
            stateText.text = "Move : " + playerDistance;

            // 공격 범위 내 위치로 이동
            Move();
        }
    }

    void Move()
    {
        // 걷기 상태로 전환
        character.nowAction = Character.Action.Walk;

        //걸을때 표정
        faceText.text = "● ▽ ●";

        // 걷기 애니메이션 시작
        anim.SetBool("isWalk", true);

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //해당 방향으로 가속
        rigid.velocity = dir.normalized * speed * SystemManager.Instance.globalTimeScale;

        //움직일 방향에따라 회전
        if (dir.x > 0)
        {
            if (transform.rotation != Quaternion.Euler(0, -180, 0))
            {
                //보스 좌우반전
                transform.rotation = Quaternion.Euler(0, 180, 0);

                //내부 텍스트 오브젝트들 좌우반전
                canvasChildren.rotation = Quaternion.Euler(0, 180, 0);
            }
        }
        else
        {
            if (transform.rotation != Quaternion.Euler(0, 0, 0))
            {
                //보스 좌우반전 초기화
                transform.rotation = Quaternion.Euler(0, 0, 0);

                //내부 텍스트 오브젝트들 좌우반전 초기화
                canvasChildren.rotation = Quaternion.Euler(0, 0, 0);
            }
        }

        // idle 상태로 전환
        character.nowAction = Character.Action.Idle;
    }

    public void WalkSound()
    {
        // 걷기 발소리 재생
        walk_lastIndex = SoundManager.Instance.PlaySoundPool(walkSounds.ToList(), transform.position, walk_lastIndex);
    }

    void ChooseAttack()
    {
        //! 패턴 스킵
        if (patten == Patten.Skip)
            return;

        // 공격 상태로 전환
        character.nowAction = Character.Action.Attack;

        // 걷기 애니메이션 끝내기
        anim.SetBool("isWalk", false);

        // 이제 폴어택 가능
        fallAtkDone = false;

        //공격 리스트 비우기
        atkList.Clear();

        // fall 콜라이더에 플레이어 있으면 리스트에 fall 공격패턴 담기
        // if (fallRangeTrigger.atkTrigger)
        //     atkList.Add(0);

        // Laser 콜라이더에 플레이어 있으면 리스트에 Laser 공격패턴 담기
        if (LaserRangeTrigger.atkTrigger)
            atkList.Add(1);

        // 가능한 공격 중에서 랜덤 뽑기
        int atkType = -1;
        if (atkList.Count > 0)
        {
            atkType = atkList[Random.Range(0, atkList.Count)];
        }

        //! 테스트를 위해 패턴 고정
        if (patten != Patten.None)
            atkType = (int)patten;

        // 결정된 공격 패턴 실행
        switch (atkType)
        {
            case 0:
                StartCoroutine(PunchAttack());
                break;

            case 1:
                StartCoroutine(StopLaserAtk());
                break;

            case 2:
                StartCoroutine(GroundAttack());
                break;
        }

        // 랜덤 쿨타임 입력
        coolCount = Random.Range(1f, 5f);

        //패턴 리스트 비우기
        atkList.Clear();
    }

    void ToggleCable(bool isPutIn)
    {
        // 케이블 시작점은 모니터 뒤로 이동
        L_CableStart.DOLocalMove(new Vector2(5, 0), 0.5f);
        R_CableStart.DOLocalMove(new Vector2(-5, 0), 0.5f);

        // 플러그 각도 초기화
        L_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);
        R_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);

        // 케이블 집어넣기
        if (isPutIn)
        {
            // 이미 케이블 꺼져있으면 리턴
            if (!L_PlugHead.enabled && !R_PlugHead.enabled)
                return;

            // 케이블 머리 부유 애니메이션 끄기
            L_Plug.enabled = false;
            R_Plug.enabled = false;

            // 양쪽 플러그를 시작부분으로 빠르게 domove
            L_Plug.transform.DOLocalMove(new Vector2(5, 0), 0.5f);
            R_Plug.transform.DOLocalMove(new Vector2(-5, 0), 0.5f)
            .OnComplete(() =>
            {
                // 케이블 라인 렌더러 끄기
                L_Cable.enabled = false;
                R_Cable.enabled = false;

                // 케이블 헤드 끄기
                L_PlugHead.enabled = false;
                R_PlugHead.enabled = false;
            });
        }
        // 케이블 꺼내기
        else
        {
            // 이미 케이블 켜져있으면 리턴
            if (L_PlugHead.enabled && R_PlugHead.enabled)
                return;

            // 케이블 헤드 켜기
            L_PlugHead.enabled = true;
            R_PlugHead.enabled = true;

            // 케이블 라인 렌더러 켜기
            L_Cable.enabled = true;
            R_Cable.enabled = true;

            // 케이블 시작점은 모니터 뒤로 이동
            L_CableStart.DOLocalMove(new Vector2(5, 0), 0.5f);
            R_CableStart.DOLocalMove(new Vector2(-5, 0), 0.5f);

            // 양쪽 플러그를 시작부분으로 빠르게 domove
            L_Plug.transform.DOLocalMove(new Vector2(-12, 10), 0.5f);
            R_Plug.transform.DOLocalMove(new Vector2(12, 10), 0.5f)
            .OnComplete(() =>
            {
                // 케이블 머리 부유 애니메이션 시작
                L_Plug.enabled = true;
                R_Plug.enabled = true;
            });
        }

    }

    #region FallAttack

    void FalldownAttack()
    {
        // 걷기 애니메이션 종료
        anim.SetBool("isWalk", false);

        // 앞뒤로 흔들려서 당황하는 표정
        faceText.text = "◉ Д ◉";

        // 엎어질 준비 애니메이션 시작
        anim.SetTrigger("FallReady");

        // 케이블 집어넣기
        ToggleCable(true);

        // 엎어질 범위 활성화
        fallRangeBackground.enabled = true;
        fallRangeIndicator.enabled = true;

        // 인디케이터 사이즈 초기화
        fallRangeIndicator.transform.localScale = Vector3.zero;

        // 넘어지기 전 알림 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_Falldown_Warning", transform.position);

        // 인디케이터 사이즈 늘리기
        fallRangeIndicator.transform.DOScale(Vector3.one, 1f)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            //넘어질때 표정
            faceText.text = "> ︿ <";

            //엎어지는 애니메이션
            anim.SetBool("isFallAtk", true);
        });
    }

    void FallAtkEnable()
    {
        // 엎어질 범위 비활성화
        fallRangeBackground.enabled = false;
        fallRangeIndicator.enabled = false;

        // fallAtk 공격 활성화
        fallAtkColl.enabled = true;

        // 먼지 파티클 활성화
        fallDustEffect.gameObject.SetActive(true);

        // 카메라 흔들기
        Camera.main.transform.DOShakePosition(0.5f, 0.3f, 50, 90f, false, false);

        // 넘어지기 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_Falldown", transform.position);
    }

    void FallAtkDisable()
    {
        // fallAtk 공격 비활성화
        fallAtkColl.enabled = false;

        //일어서기, 휴식 애니메이션 재생
        StartCoroutine(GetUpAnim());
    }

    IEnumerator GetUpAnim()
    {
        //일어날때 표정
        faceText.text = "x  _  x";

        // 엎어진채로 대기
        yield return new WaitForSeconds(0f);

        // 일어서기, 휴식 애니메이션 시작
        anim.SetBool("isFallAtk", false);

        StartCoroutine(RestAnim());
    }

    #endregion

    #region PunchAttack

    IEnumerator PunchAttack()
    {
        // 얼굴 바꾸기
        faceText.text = "Ϟ( ◕.̫ ◕ )Ϟ";

        // 케이블 애니메이션 끄기
        L_Plug.enabled = false;
        R_Plug.enabled = false;

        // 전원 케이블을 옆으로 이동
        L_Plug.transform.DOLocalMove(new Vector3(-15f, 10f, 0), 0.5f);
        R_Plug.transform.DOLocalMove(new Vector3(15f, 10f, 0), 0.5f);

        // 공격 케이블 시작점은 모니터 모서리로 이동
        L_CableStart.DOLocalMove(Vector3.zero, 0.5f);
        R_CableStart.DOLocalMove(Vector3.zero, 0.5f);

        yield return new WaitForSeconds(0.5f);

        // 케이블 sort layer를 모니터 앞으로 바꾸기
        L_Cable.sortingOrder = 1;
        R_Cable.sortingOrder = 1;

        // 조준 시간 계산
        float aimTime = Random.Range(1f, 2f);

        //좌측 플러그 떨면서 조준 사격
        L_Plug.transform.DOShakePosition(aimTime, 0.3f, 50, 90, false, false);
        StartCoroutine(ShotPunch(aimTime, true));

        aimTime += 0.5f;

        // 우측 플러그 떨면서 조준 사격
        R_Plug.transform.DOShakePosition(aimTime, 0.3f, 50, 90, false, false);
        yield return StartCoroutine(ShotPunch(aimTime, false));

        yield return new WaitForSeconds(0.5f);

        // 플러그 각도 초기화
        L_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);
        R_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);

        //휴식 시작
        StartCoroutine(RestAnim());
    }

    IEnumerator ShotPunch(float aimTime, bool isLeft)
    {
        // 케이블 방향에 따라 변수 정하기
        Animator plugControler = isLeft ? L_Plug : R_Plug;
        EnemyAttack plugAtk = isLeft ? L_PlugAtk : R_PlugAtk;
        Transform cableStart = isLeft ? L_CableStart : R_CableStart;
        LineRenderer cableLine = isLeft ? L_Cable : R_Cable;
        GameObject cableSpark = isLeft ? L_CableSpark : R_CableSpark;
        Transform plugHead = isLeft ? L_PlugHead.transform : R_PlugHead.transform;
        Transform plugTip = isLeft ? L_PlugTip : R_PlugTip;

        // 케이블 끝 전기 파티클 켜기
        plugTip.gameObject.SetActive(true);

        float aimCount = aimTime;
        while (aimCount > 0)
        {
            //시간 차감
            aimCount -= Time.deltaTime;

            // 플러그가 플레이어 방향 조준
            plugHead.rotation = Quaternion.Euler(0, 0, SystemManager.Instance.GetVector2Dir(PlayerManager.Instance.transform.position, plugControler.transform.position) - 90f);

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 케이블 끝 전기 파티클 끄기
        plugTip.gameObject.SetActive(false);

        // 케이블 끝이 반짝 빛나는 파티클 인디케이터
        plugHead.transform.GetChild(1).gameObject.SetActive(true);

        Vector2 playerPos = PlayerManager.Instance.transform.position;
        // 플레이어 위치로 플러그 헤드 이동
        plugHead.transform.DOMove(playerPos, 0.5f)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
            // 이동 완료되면 플러그 멈추기
            plugHead.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        });
        // 플레이어 위치로 플러그 컨트롤러 이동
        plugControler.transform.DOMove(playerPos, 0.5f)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
            // 땅에 꽂힐때 이펙트 소환 : 흙 파티클 튀기, 바닥 갈라지는 애니메이션
            LeanPool.Spawn(groundCrackEffect, plugTip.position, Quaternion.identity, SystemManager.Instance.effectPool);
        });

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(CableSpark(isLeft));

        // 도달 완료하면 공격 콜라이더 켜기
        plugAtk.gameObject.SetActive(true);
        StartCoroutine(plugAtk.AttackNDisable());

        // 플러그 흔들기
        plugControler.transform.DOShakePosition(0.5f, 0.3f, 50, 90, false, false);

        // 공격 시간동안 대기
        yield return new WaitForSeconds(0.5f);

        // 공격 완료하면 플러그 이동 재개
        plugHead.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

        // 케이블 sort layer를 모니터 뒤로 바꾸기
        cableLine.sortingOrder = -1;

        // 케이블 시작점은 모니터 뒤로 이동
        Vector2 startPos = isLeft ? new Vector2(5, 0) : new Vector2(-5, 0);
        cableStart.DOLocalMove(startPos, 0.5f);

        // 플러그 원위치
        Vector2 plugPos = isLeft ? new Vector2(-12, 10) : new Vector2(12, 10);
        plugControler.transform.DOLocalMove(plugPos, 0.5f)
        .OnComplete(() =>
        {
            // 케이블 머리 부유 애니메이션 시작
            plugControler.enabled = true;
        });

        // 원위치 하는동안 대기
        yield return new WaitForSeconds(0.5f);
    }

    #endregion

    #region GroundAttack

    IEnumerator GroundAttack()
    {
        // 얼굴 바꾸기
        faceText.text = "Ϟ( ◕.̫ ◕ )Ϟ";

        // 플러그 땅에 꼽기
        StartCoroutine(Grounding(true));
        yield return StartCoroutine(Grounding(false));

        // 전기 방출 횟수 계산
        int atkNum = Random.Range(3, 7);

        // 정해진 횟수만큼 전기 방출
        for (int i = 0; i < atkNum; i++)
        {
            // 방출 할때마다 딜레이
            yield return new WaitForSeconds(1f);

            // 전기 방출
            StartCoroutine(ElectroRelease(true, 50));
            yield return StartCoroutine(ElectroRelease(false, 50));
        }

        yield return new WaitForSeconds(0.5f);

        // 플러그 이동 가능
        L_PlugHead.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        R_PlugHead.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

        // 플러그 각도 초기화
        L_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);
        R_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);

        // 케이블 sort layer를 모니터 뒤로 바꾸기
        L_Cable.sortingOrder = -1;
        R_Cable.sortingOrder = -1;

        // 케이블 시작점은 모니터 뒤로 이동
        L_CableStart.DOLocalMove(new Vector2(5, 0), 0.5f);
        R_CableStart.DOLocalMove(new Vector2(-5, 0), 0.5f);

        // 플러그 원위치
        L_Plug.transform.DOLocalMove(new Vector2(-12, 10), 0.5f);
        R_Plug.transform.DOLocalMove(new Vector2(12, 10), 0.5f);

        yield return new WaitForSeconds(0.5f);

        // 케이블 애니메이션 시작
        L_Plug.enabled = true;
        R_Plug.enabled = true;

        //휴식 시작
        StartCoroutine(RestAnim());
    }

    IEnumerator Grounding(bool isLeft)
    {
        // 케이블 방향에 따라 변수 정하기
        Animator plugControler = isLeft ? L_Plug : R_Plug;
        Transform cableStart = isLeft ? L_CableStart : R_CableStart;
        LineRenderer cableLine = isLeft ? L_Cable : R_Cable;
        Transform plugHead = isLeft ? L_PlugHead.transform : R_PlugHead.transform;
        Transform plugTip = isLeft ? L_PlugTip : R_PlugTip;

        EnemyAttack plugAtk = isLeft ? L_PlugAtk : R_PlugAtk;
        GameObject cableSpark = isLeft ? L_CableSpark : R_CableSpark;

        // 케이블 애니메이션 끄기
        plugControler.enabled = false;

        // 전원 케이블을 옆으로 이동
        Vector2 plugPos = isLeft ? new Vector3(-15f, 10f, 0) : new Vector3(15f, 10f, 0);
        plugControler.transform.DOLocalMove(plugPos, 0.5f);

        // 공격 케이블 시작점은 모니터 모서리로 이동
        cableStart.DOLocalMove(Vector3.zero, 0.5f);

        yield return new WaitForSeconds(0.5f);

        // 케이블 sort layer를 모니터 앞으로 바꾸기
        cableLine.sortingOrder = 1;

        // 땅에 플러그 내리꼽기
        //플러그가 아래를 바라보기
        plugHead.transform.DORotate(new Vector3(0, 0, 180f), 0.5f);

        Vector3 downPos = isLeft ? transform.position + new Vector3(-15f, 2.2f, 0) : transform.position + new Vector3(15f, 2.5f, 0);
        // 플러그 헤드 아래로 이동
        plugControler.transform.DOMove(downPos, 1f)
        .SetEase(Ease.InBack);

        // 플러그 컨트롤러 아래로 이동
        plugHead.transform.DOMove(downPos, 1f)
        .SetEase(Ease.InBack);

        yield return new WaitForSeconds(1f);

        // 케이블 땅에 꽂는 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_CableGround", plugTip.transform.position);

        // 이동 완료되면 플러그 멈추기
        plugHead.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

        // 땅에 꽂힐때 이펙트 소환 : 흙 파티클 튀기, 바닥 갈라지는 애니메이션
        LeanPool.Spawn(groundCrackEffect, plugTip.position, Quaternion.identity, SystemManager.Instance.effectPool);
    }

    IEnumerator ElectroRelease(bool isLeft, int summonNum)
    {
        Animator plugControler = isLeft ? L_Plug : R_Plug;
        Transform plugHead = isLeft ? L_PlugHead.transform : R_PlugHead.transform;
        LineRenderer cableLine = isLeft ? L_Cable : R_Cable; // 케이블 라인 오브젝트
        GameObject cableSpark = isLeft ? L_CableSpark : R_CableSpark; // 케이블 타고 흐르는 스파크 이펙트
        Transform cableTip = isLeft ? L_PlugAtk.transform : R_PlugAtk.transform; // 케이블 타고 흐르는 스파크 이펙트
        SpriteRenderer cableTipRange = cableTip.GetComponent<SpriteRenderer>();

        // 케이블 공격범위 켜기
        cableTip.gameObject.SetActive(true);
        // 인디케이터 범위 색깔 초기화
        cableTipRange.color = new Color(1, 0, 0, 0.5f);

        // 스파크 켜기
        yield return StartCoroutine(CableSpark(isLeft));

        // 페이즈 배율 적용
        // summonNum = (int)(summonNum * projectileMultiple);

        // 각 투사체 위치 저장할 배열
        Vector3[] lastPos = new Vector3[summonNum];

        // 원형 각도 중에 비어있는 인덱스 뽑기, 해당 방향은 발사하지 않고 넘어감
        int emptyNum = 3; //비어있는 투사체 개수
        // 시작 인덱스 뽑기, +2했을때 최대치 넘지않게
        int emptyStart = Random.Range(0, summonNum - (emptyNum - 2)); //비어있는 투사체 시작 인덱스

        // 원형으로 전기 방출
        // 투사체 개수만큼 반복
        for (int i = 0; i < summonNum; i++)
        {
            // 비우기 인덱스면 넘기기
            if (i >= emptyStart && i <= emptyStart + emptyNum - 1)
                continue;

            // 소환할 각도를 벡터로 바꾸기
            float angle = 360f * i / summonNum;
            Vector3 summonDir = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), Mathf.Cos(Mathf.Deg2Rad * angle), 0);
            // print(angle + " : " + summonDir);

            // 투사체 소환 위치, 케이블 끝에서 summonDir 각도로 범위만큼 곱하기 
            Vector3 targetPos = cableTip.transform.position + summonDir * 5f;

            // 각 투사체의 소환 위치 저장
            lastPos[i] = targetPos;

            // 전기 오브젝트 생성
            GameObject magicObj = LeanPool.Spawn(groundElectro, cableTip.transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

            // summonDir 방향으로 발사
            magicObj.GetComponent<Rigidbody2D>().velocity = summonDir * 10f;
            // summonDir 방향으로 회전
            magicObj.transform.rotation = Quaternion.Euler(0, 0, SystemManager.Instance.GetVector2Dir(summonDir) - 90f);

            // 매직홀더 찾기
            MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
            // magic 데이터 넣기
            magicHolder.magic = laserMagic;

            // 타겟을 플레이어로 전환
            magicHolder.SetTarget(MagicHolder.TargetType.Player);

            // 투사체 목표지점 넣기
            magicHolder.targetPos = lastPos[i];

            // 타겟 오브젝트 넣기
            magicHolder.targetObj = PlayerManager.Instance.gameObject;
        }

        // 전기 방출 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_ElectroWave", cableTip.transform.position);

        // 플러그 떨림
        plugHead.transform.DOShakePosition(0.2f, 0.3f, 50, 90, false, false);

        // 케이블 끝에서 전기 방출 이펙트
        cableTip.GetComponent<ParticleSystem>().Play();
        // 빨간 인디케이터 투명하게
        cableTipRange.DOColor(Color.clear, 1f);

        // 방출 시간 대기
        yield return new WaitForSeconds(1f);

        // 케이블 공격범위 끄기
        cableTip.gameObject.SetActive(false);
    }

    #endregion

    #region LaserAttack

    IEnumerator StopLaserAtk()
    {
        // print("laser atk");

        // 레이저 준비 애니메이션 시작
        anim.SetTrigger("LaserReady");

        // 모니터에 화를 참는 얼굴
        faceText.text = "◣` ︿ ´◢";

        // 동시에 점점 빨간색 게이지가 차오름
        angryGauge.fillAmount = 0f;
        DOTween.To(() => angryGauge.fillAmount, x => angryGauge.fillAmount = x, 1f, 1f);

        //펄스 이펙트 활성화
        pulseEffect.SetActive(true);

        // 케이블 꺼내기
        ToggleCable(false);

        // 공격 케이블 시작점은 모니터 모서리로 이동
        L_CableStart.DOLocalMove(Vector3.zero, 0.5f);
        R_CableStart.DOLocalMove(Vector3.zero, 0.5f);

        // 케이블 sort layer를 모니터 앞으로 바꾸기
        L_Cable.sortingOrder = 1;
        R_Cable.sortingOrder = 1;

        //게이지 모두 차오르면 
        yield return new WaitUntil(() => angryGauge.fillAmount >= 1f);

        //todo 무궁화꽃 주문 끝날때까지 보스 무적상태 만들기

        // 레이저 충전 애니메이션 시작
        anim.SetTrigger("LaserSet");

        //채워질 글자
        string targetText = "무궁화꽃이\n피었습니다";

        // 텍스트 비우기
        laserText.text = "";

        // 공격 준비 글자 채우기
        float delay = 0.2f;
        while (laserText.text.Length < targetText.Length)
        {
            laserText.text = targetText.Substring(0, laserText.text.Length + 1);

            //글자마다 랜덤 딜레이 갱신 
            delay = Random.Range(0.2f, 0.5f);
            delay = 0.1f;

            yield return new WaitForSeconds(delay);
        }

        //펄스 이펙트 활성화
        pulseEffect.SetActive(true);

        // 공격 준비 끝나면 stop 띄우기
        laserText.text = "STOP";

        // Stop 표시될때까지 대기
        yield return new WaitUntil(() => laserText.text == "STOP");

        // 감시 애니메이션 시작
        anim.SetBool("isLaserWatch", true);

        // 노려보는 얼굴
        faceText.text = "⚆`  ︿  ´⚆";

        //몬스터 스폰 멈추기
        WorldSpawner.Instance.spawnSwitch = false;
        // 모든 몬스터 멈추기
        List<Character> enemys = SystemManager.Instance.enemyPool.GetComponentsInChildren<Character>().ToList();
        foreach (Character chracter in enemys)
        {
            // 보스 본인이 아닐때
            if (chracter != this.character)
                chracter.stopCount = 3f;
        }

        //감시 시간
        float watchTime = Time.time;
        //플레이어 현재 위치 기억하기
        Vector3 playerPos = PlayerManager.Instance.transform.position;

        print("watch start : " + Time.time);

        // 플레이어 조준 코루틴 실행
        IEnumerator aimCable = AimCable();
        StartCoroutine(aimCable);

        // 감시 시간 타이머
        while (Time.time - watchTime < 3)
        {
            //플레이어 위치 변경됬으면
            if (playerPos != PlayerManager.Instance.transform.position)
            {
                // 화난 얼굴로 변경
                faceText.text = "◣` ︿ ´◢";

                // 충전 시간
                float chargeTime = 1f;

                // 양쪽 케이블 타고 전기 흘리기
                StartCoroutine(CableSpark(true, chargeTime));
                StartCoroutine(CableSpark(false, chargeTime));

                // 6칸이므로 6번 반복
                for (int i = 0; i < 6; i++)
                {
                    string chargeText = "(" + new string('■', i) + new string('□', 6 - i) + ")";

                    laserText.text = chargeText;

                    // 6칸이므로 충전시간/6 시간만큼 대기
                    yield return new WaitForSeconds(chargeTime / 6f);
                }

                laserText.text = "(" + new string('■', 6) + ")";

                //케이블 반짝이는 이벤트 켜기
                L_PlugHead.transform.GetChild(1).gameObject.SetActive(true);
                R_PlugHead.transform.GetChild(1).gameObject.SetActive(true);

                // 풀충전 상태로 이펙트 시간동안 잠깐 대기
                yield return new WaitForSeconds(0.5f);

                // 플러그 끝 전기 이펙트 켜기
                L_PlugTip.gameObject.SetActive(true);
                R_PlugTip.gameObject.SetActive(true);

                // 레이저 발사 실행, 끝날때까지 대기
                yield return StartCoroutine(ShotLaser());

                // 플러그 끝 전기 이펙트 끄기
                L_PlugTip.gameObject.SetActive(false);
                R_PlugTip.gameObject.SetActive(false);

                // 케이블 sort layer를 모니터 뒤로 바꾸기
                L_Cable.sortingOrder = -1;
                R_Cable.sortingOrder = -1;

                // 케이블 시작점은 모니터 뒤로 이동
                L_CableStart.DOLocalMove(new Vector2(5, 0), 0.5f);
                R_CableStart.DOLocalMove(new Vector2(-5, 0), 0.5f);

                // 플러그 원위치
                L_Plug.transform.DOLocalMove(new Vector2(-12, 10), 0.5f);
                R_Plug.transform.DOLocalMove(new Vector2(12, 10), 0.5f);

                yield return new WaitForSeconds(0.5f);

                // 케이블 애니메이션 시작
                L_Plug.enabled = true;
                R_Plug.enabled = true;

                // 조준 코루틴 멈추기
                StopCoroutine(aimCable);

                //몬스터 스폰 재개
                WorldSpawner.Instance.spawnSwitch = true;
                // 모든 몬스터 움직임 재개
                SystemManager.Instance.globalTimeScale = 1f;

                // 플러그 각도 초기화
                L_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);
                R_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);

                break;
            }

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 조준 코루틴 멈추기
        StopCoroutine(aimCable);

        print("watch end : " + Time.time);

        // 감시 종료
        anim.SetBool("isLaserWatch", false);

        //몬스터 스폰 재개
        WorldSpawner.Instance.spawnSwitch = true;
        // 모든 몬스터 움직임 재개
        SystemManager.Instance.globalTimeScale = 1f;

        //휴식 시작
        StartCoroutine(RestAnim());
    }

    IEnumerator AimCable()
    {
        while (true)
        {
            // 양쪽 케이블이 플레이어 조준
            L_PlugHead.transform.rotation = Quaternion.Euler(0, 0, SystemManager.Instance.GetVector2Dir(PlayerManager.Instance.transform.position, L_PlugHead.transform.position) - 90f);
            R_PlugHead.transform.rotation = Quaternion.Euler(0, 0, SystemManager.Instance.GetVector2Dir(PlayerManager.Instance.transform.position, R_PlugHead.transform.position) - 90f);

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    IEnumerator CableSpark(bool isLeft, float duration = 1f)
    {
        LineRenderer cableLine = isLeft ? L_Cable : R_Cable; // 케이블 라인 오브젝트
        GameObject cableSpark = isLeft ? L_CableSpark : R_CableSpark; // 케이블 타고 흐르는 스파크 이펙트

        // 찌릿 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_Sting", cableSpark.transform.position);

        // 스파크 켜기
        cableSpark.SetActive(true);
        // 전기 파티클이 전선을 타고 플러그까지 빠르게 도달
        for (int i = 0; i < cableLine.positionCount; i++)
        {
            // 파티클 오브젝트가 라인 렌더러 포인트 순서대로 이동
            cableSpark.transform.localPosition = cableLine.GetPosition(i);

            // (1 / 링크개수) 만큼 대기
            yield return new WaitForSeconds(duration / cableLine.positionCount);
        }
        // 스파크 끄기
        cableSpark.SetActive(false);

        yield return null;
    }

    IEnumerator ShotLaser()
    {
        // print("레이저 발사");

        //움직이면 레이저 발사 애니메이션 재생
        anim.SetBool("isLaserAtk", true);

        // 레이저 쏠때 얼굴
        faceText.text = "◣` ︿ ´◢";

        // 양쪽 케이블에서 레이저 여러번 발사
        Transform plugTip = L_PlugTip;
        for (int i = 0; i < 10; i++)
        {
            // 쏘는 케이블 변경
            plugTip = plugTip == L_PlugTip ? R_PlugTip : L_PlugTip;

            // 발사할때 플러그 반동
            plugTip.parent.DOPunchPosition((plugTip.parent.position - PlayerManager.Instance.transform.position).normalized, 0.2f, 10, 1);

            //레이저 생성
            GameObject magicObj = LeanPool.Spawn(LaserPrefab, plugTip.position, Quaternion.identity, SystemManager.Instance.magicPool);

            LaserBeam laser = magicObj.GetComponent<LaserBeam>();
            // 레이저 발사할 오브젝트 넣기
            laser.startObj = plugTip;

            MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
            // magic 데이터 넣기
            magicHolder.magic = laserMagic;

            // 타겟을 플레이어로 전환
            magicHolder.SetTarget(MagicHolder.TargetType.Player);
            // 레이저 목표지점 targetPos 넣기
            magicHolder.targetPos = PlayerManager.Instance.transform.position;

            //레이저 쏘는 시간 대기
            yield return new WaitForSeconds(0.3f);
        }

        //레이저 발사 후딜레이
        yield return new WaitForSeconds(1f);

        //레이저 발사 종료
        anim.SetBool("isLaserAtk", false);
    }

    #endregion

    IEnumerator RestAnim()
    {
        //휴식 시작
        anim.SetBool("isRest", true);
        //휴식할때 표정
        faceText.text = "x  _  x";

        // 쿨타임 0 될때까지 대기
        yield return new WaitForSeconds(2f);
        // yield return new WaitUntil(() => coolCount <= 0);

        //휴식 끝
        anim.SetBool("isRest", false);

        // 쿨타임 끝나면 idle로 전환, 쿨타임 차감 시작
        character.nowAction = Character.Action.Idle;

        // // 케이블 꺼내기
        // ToggleCable(false);
    }

    // void SetIdle()
    // {
    //     //로딩중 텍스트 애니메이션
    //     StartCoroutine(LoadingText());
    // }

    // IEnumerator LoadingText()
    // {
    //     string targetText = "...";

    //     faceText.text = targetText;

    //     // 쿨타임 중일때
    //     while (coolCount > 0)
    //     {
    //         if (faceText.text.Length < targetText.Length)
    //             faceText.text = targetText.Substring(0, faceText.text.Length + 1);
    //         else
    //             faceText.text = targetText.Substring(0, 0);

    //         yield return new WaitForSeconds(0.2f);
    //     }

    //     // 쿨타임 끝나면 idle로 전환
    //     chracter.nowAction = Chracter.Action.Idle;
    // }
}
