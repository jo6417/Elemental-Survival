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
    [SerializeField] Patten patten = Patten.None;
    enum Patten { FallAttack, PunchAttack, TrafficAtk, GroundAttack, Skip, None };
    enum Face { Idle, CloseEye, Dizzy, Hit, Electro, Rage, Watch, Rest, Fall }
    [SerializeField] float attackDistance = 10f;
    float speed;

    [Header("Refer")]
    [SerializeField] Character character;
    [SerializeField] Collider2D standColl; // 서있을때 물리 콜라이더
    [SerializeField] HitBox hitbox; // 피격 히트박스
    [SerializeField] Sprite fallSprite;
    [SerializeField] SpriteRenderer monitorSprite; // 모니터 스프라이트
    [SerializeField] SpriteRenderer bootScreenSprite; // 부팅용 화면
    [SerializeField] SpriteRenderer screenSprite; // 모니터 스크린 배경 화면
    [SerializeField] Image angryGauge; //분노 게이지 이미지
    [SerializeField] TextMeshProUGUI faceText;
    [SerializeField] Transform canvasChildren;
    [SerializeField] Animator anim;
    [SerializeField] Rigidbody2D rigid;
    [SerializeField] SpriteRenderer shadow;
    [SerializeField] ParticleSystem bodyElectro; // 모니터 본체 전기 이펙트
    AudioSource electroStreamSound;

    [Header("Phase")]
    [SerializeField] Color[] screen_phaseColor;
    [SerializeField] Color[] text_phaseColor;
    Color[] effect_phaseColor = {
        new Color(1f, 1f, 1f, 0) * 10f,
        new Color(30f, 200f, 200f, 0) * 10f,
        new Color(255f, 255f, 20f, 0) * 10f,
        new Color(180f, 0f, 0f, 0) * 10f
        };
    [SerializeField, ReadOnly] int nowPhase = 0;
    [SerializeField, ReadOnly] int nextPhase = 1;
    [SerializeField, ReadOnly] float damageMultiple = 1; // 페이즈에 따른 데미지 배율
    [SerializeField, ReadOnly] float speedMultiple = 1; // 페이즈에 따른 이동 속도 배율
    [SerializeField, ReadOnly] float projectileMultiple = 1; // 페이즈에 따른 투사체 배율
    [SerializeField] Material effectMat; // 현재 모든 전기 이펙트 머터리얼
    [SerializeField] ParticleSystem circleElectro; // 원형 전기 장판 범위
    [SerializeField] Transform glassEffect;

    [Header("Cable")]
    [SerializeField] LineRenderer L_Cable;
    [SerializeField] LineRenderer R_Cable;
    [SerializeField] Transform L_CableStart;
    [SerializeField] Transform R_CableStart;
    [SerializeField] SpriteRenderer L_PlugHead;
    [SerializeField] SpriteRenderer R_PlugHead;
    [SerializeField] Transform L_PlugTip;
    [SerializeField] Transform R_PlugTip;
    [SerializeField] Animator L_Plug;
    [SerializeField] Animator R_Plug;
    [SerializeField] EnemyAttack L_PlugAtk;
    [SerializeField] EnemyAttack R_PlugAtk;
    [SerializeField] GameObject L_CableSpark; //케이블 타고 흐르는 스파크
    [SerializeField] GameObject R_CableSpark; //케이블 타고 흐르는 스파크

    [Header("FallAtk")]
    [SerializeField] Collider2D fallAtkColl; // 해당 컴포넌트를 켜서 fallAtk 공격
    [SerializeField] Collider2D fallAtkPush; // 해당 콜라이더로 깔린 적들 밀어내기
    [SerializeField] EnemyAtkTrigger fallRangeTrigger; //엎어지기 범위 내에 플레이어가 들어왔는지 보는 트리거
    [SerializeField] SpriteRenderer fallRangeBackground;
    [SerializeField] SpriteRenderer fallRangeIndicator;
    [SerializeField] ParticleSystem fallDustEffect; //엎어질때 발생할 먼지 이펙트
    [SerializeField] ParticleSystem groundElectroAtk;

    [Header("PunchAtk")]
    [SerializeField] ParticleSystem craterEffect; //땅 갈라지는 이펙트
    [SerializeField] GameObject pulseAtk; // 케이블 끝에서 터지는 링모양 펄스 공격
    [SerializeField] GameObject electroSpreadAtk; // 지면따라 랜덤하게 퍼지는 전기 공격
    [SerializeField] GameObject electroMisile; // 펀치 후 날아갈 전기 프리팹

    [Header("TrafficAtk")]
    [SerializeField] SpriteRenderer[] trafficLedList;
    [SerializeField] GameObject LaserPrefab; //발사할 레이저 마법 프리팹
    [SerializeField] GameObject stopPulseEffect; //laser stop 할때 펄스 이펙트
    MagicInfo laserMagic = null; //발사할 레이저 마법 데이터
    SpriteRenderer laserRange;
    [SerializeField] EnemyAtkTrigger LaserRangeTrigger; //레이저 범위 내에 플레이어가 들어왔는지 보는 트리거
    [SerializeField] TextMeshProUGUI stopText;

    [Header("GroundAtk")]
    [SerializeField] SpriteRenderer thunderReady; // 번개 공격 및 인디케이터
    [SerializeField] GameObject thunderbolt; // 번개 공격 이펙트
    [SerializeField, ReadOnly] bool nowGroundAtk; // 현재 그라운드 어택 진행중 여부

    [Header("Cooltime")]
    [SerializeField] float coolCount;
    [SerializeField] float fallCooltime = 1f; //
    [SerializeField] float laserCooltime = 3f; //무궁화꽃 쿨타임
    [SerializeField] float groundPunchCooltime = 5f; // 그라운드 펀치 쿨타임
    [SerializeField] float earthGroundCooltime = 8f; //접지 패턴 쿨타임

    [Header("Sound")]
    [SerializeField] string[] walkSounds = { "Ascii_Walk1", "Ascii_Walk2", "Ascii_Walk3" };
    int walk_lastIndex = -1;

    //! 테스트
    [Header("Debug")]
    [SerializeField] TextMeshProUGUI stateText;

    private void Awake()
    {
        character = character == null ? GetComponentInChildren<Character>() : character;
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();

        laserRange = LaserRangeTrigger.GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    private void OnDisable()
    {
        // 몬스터 스폰 재개
        SystemManager.Instance.spawnSwitch = true;
    }

    IEnumerator Init()
    {
        // 휴식 상태로 초기화
        character.nowState = CharacterState.Rest;

        // 무적 상태로 변경
        character.invinsible = true;

        rigid.velocity = Vector2.zero; //속도 초기화

        // 신호등 끄기
        trafficLedList[0].transform.parent.gameObject.SetActive(false);

        // 스크린 유리 이펙트 위치 초기화
        glassEffect.localPosition = new Vector3(10f, 15f);

        // 케이블 숨기기
        // 케이블 머리 부유 애니메이션 끄기
        L_Plug.enabled = false;
        R_Plug.enabled = false;
        // 케이블 라인 렌더러 끄기
        L_Cable.enabled = false;
        R_Cable.enabled = false;
        // 케이블 헤드 끄기
        L_PlugHead.enabled = false;
        R_PlugHead.enabled = false;

        // 양쪽 플러그를 시작부분으로 초기화
        L_Plug.transform.localPosition = new Vector2(5, 0);
        R_Plug.transform.localPosition = new Vector2(-5, 0);
        // 플러그 각도 초기화
        L_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);
        R_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);

        // 애니메이션 멈추기
        anim.speed = 0f;
        // 스프라이트 끄기
        monitorSprite.enabled = false;
        // 누워있는 스프라이트로 변경
        monitorSprite.sprite = fallSprite;

        // 히트박스 끄기
        hitbox.gameObject.SetActive(false);

        // fallAtk 공격 비활성화
        fallAtkColl.enabled = false;
        // 밀어내기 콜라이더 켜기
        fallAtkPush.gameObject.SetActive(true);
        // 스탠드 물리 콜라이더 끄기
        standColl.enabled = false;

        // 부팅 화면 사이즈 초기화
        bootScreenSprite.transform.localScale = Vector2.zero;
        // 부팅 화면 켜기
        bootScreenSprite.color = Color.white;
        bootScreenSprite.gameObject.SetActive(true);
        // 화면 숨기기
        screenSprite.transform.localScale = Vector2.zero;
        // 그림자 끄기
        shadow.gameObject.SetActive(false);
        // 얼굴 텍스트 비우기
        faceText.text = "";
        // 레이저 텍스트 비우기
        stopText.text = "";

        // 화면 배경색 변경
        screenSprite.color = screen_phaseColor[1];

        // EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => character.enemy != null);
        //스피드 초기화
        speed = character.enemy.speed;

        // fallAtk 공격 비활성화
        fallAtkColl.enabled = false;

        transform.DOKill();

        // 위치 고정 해제
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //공격범위 오브젝트 초기화
        fallRangeBackground.enabled = false;
        laserRange.enabled = false;

        // MagicDB 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 레이저 마법 데이터 찾기
        if (laserMagic == null)
        {
            //찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            laserMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("LaserBeam"));

            // 강력한 데미지로 고정
            laserMagic.power = 10f;
        }

        // 맞을때마다 Hit 함수 실행
        if (character.hitCallback == null)
            character.hitCallback += Hit;

        // 현재 및 다음 페이즈 초기화
        nowPhase = 1;
        nextPhase = 1;

        // 전기 이펙트 색깔 초기화
        effectMat.color = effect_phaseColor[nowPhase];

        // 아웃라인 머터리얼로 교체
        monitorSprite.material = SystemManager.Instance.outLineMat;
        // 스프라이트 켜기
        monitorSprite.enabled = true;

        // 전기 흐르는 사운드 재생
        electroStreamSound = SoundManager.Instance.PlaySound("Ascii_ElectroStream", 1f, 0, -1);

        // 몸체 전기 파티클 켜기
        bodyElectro.Play();
        // 파티클 개수 초기화
        ParticleSystem.EmissionModule bodyEmission = bodyElectro.emission;
        bodyEmission.rateOverTimeMultiplier = 0;
        // 파티클 개수 점점 증가 시키기
        DOTween.To(() => bodyEmission.rateOverTimeMultiplier, x => bodyEmission.rateOverTimeMultiplier = x, 30f, 2f)
        .OnComplete(() =>
        {
            // 몸체 전기 파티클 끄기
            bodyElectro.Stop();
        });

        //todo 원래 묻어있던 이끼 사라짐 (이끼 묻고 누워있는 모니터 스프라이트로 애니메이션)

        // 원형 전기 공격
        yield return StartCoroutine(CircleGroundElectro());

        yield return new WaitForSeconds(1f);

        // 애니메이터 작동시키기
        anim.speed = 1f;

        // 모니터 스프라이트 레이어 바꾸기
        GetupLayer();

        // 밀어내기 콜라이더 끄기
        fallAtkPush.gameObject.SetActive(false);
        // 스탠드 물리 콜라이더 켜기
        standColl.enabled = true;
        // 캐릭터에 물리 콜라이더 정보 넣기
        character.physicsColl = standColl;

        // 일어나는 시간 대기
        yield return new WaitForSeconds(1f);

        // 부팅 화면 컬러 나타내기
        bootScreenSprite.DOColor(screen_phaseColor[1], 1f);
        // 화면 티비처럼 켜지는 이펙트
        bootScreenSprite.transform.DOScale(new Vector2(1f, 0.02f), 0.3f);
        yield return new WaitForSeconds(0.3f);
        bootScreenSprite.transform.DOScale(Vector2.one, 0.2f);
        yield return new WaitForSeconds(0.2f);

        // 화면 나타내기
        screenSprite.transform.localScale = Vector2.one;
        // 부팅 화면 끄기
        bootScreenSprite.gameObject.SetActive(false);

        // 티비 켜지는 소리
        SoundManager.Instance.PlaySound("Ascii_Screen_On");

        // 얼굴 투명하게
        Color emptyColor = text_phaseColor[1];
        emptyColor.a = 0;
        faceText.color = emptyColor;
        // 눈 감은 얼굴
        faceText.text = FaceReturn(Face.CloseEye);
        // 천천히 얼굴 나타내기
        faceText.DOColor(text_phaseColor[1], 1f);

        // 화면 정전기 소리 bzzzz...사운드 재생
        AudioSource screenSound = SoundManager.Instance.PlaySound("Ascii_Screen_Bzz", 0.5f);
        // 화면 정전기 소리 끄기 예약
        SoundManager.Instance.StopSound(screenSound, 0.5f, 0.5f);
        yield return new WaitForSeconds(1f);

        // 눈 깜빡이기
        faceText.text = FaceReturn(Face.CloseEye);
        yield return new WaitForSeconds(0.3f);
        faceText.text = FaceReturn(Face.Idle);
        SoundManager.Instance.PlaySound("Ascii_BlinkEye"); // 눈 뜨는 소리 재생
        yield return new WaitForSeconds(0.2f);
        faceText.text = FaceReturn(Face.CloseEye);
        yield return new WaitForSeconds(0.1f);
        faceText.text = FaceReturn(Face.Idle);
        SoundManager.Instance.PlaySound("Ascii_BlinkEye"); // 눈 뜨는 소리 재생
        yield return new WaitForSeconds(0.1f);
        faceText.text = FaceReturn(Face.CloseEye);
        yield return new WaitForSeconds(0.1f);
        faceText.text = FaceReturn(Face.Idle);
        SoundManager.Instance.PlaySound("Ascii_BlinkEye"); // 눈 뜨는 소리 재생
        yield return new WaitForSeconds(0.2f);

        // Idle 애니메이션 진행하며 초기화
        anim.SetTrigger("Awake");

        // 무적 상태 해제
        character.invinsible = false;
        monitorSprite.material = SystemManager.Instance.spriteLitMat;

        // 쿨타임 끝나면 idle로 전환, 쿨타임 차감 시작
        character.nowState = CharacterState.Idle;
    }

    string FaceReturn(Face face)
    {
        switch (face)
        {
            default: return "";
            case Face.Idle: return "● ▽ ●";
            case Face.CloseEye: return "- ▽ -";
            case Face.Dizzy: return "@ ▽ @";
            case Face.Hit: return "> ︿ <";
            case Face.Electro: return "Ϟ( ◕.̫ ◕ )Ϟ";
            case Face.Rage: return "◣` ︿ ´◢";
            case Face.Watch: return "⚆` ︿ ´⚆";
            case Face.Rest: return "x  _  x";
            case Face.Fall: return "◉ Д ◉";
        }
    }

    IEnumerator CircleGroundElectro()
    {
        // 원형 전기 공격 소환
        ParticleSystem circleAtk = LeanPool.Spawn(circleElectro, circleElectro.transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

        // 전기 공격 끄기
        circleAtk.transform.GetChild(0).gameObject.SetActive(false);
        // 인디케이터 사이즈 초기화
        circleAtk.transform.localScale = Vector2.zero;
        // 인디케이터 켜기
        circleAtk.gameObject.SetActive(true);

        // 인디케이터 바닥 나타내기
        SpriteRenderer circleSprite = circleAtk.GetComponent<SpriteRenderer>();
        circleSprite.color = new Color(1, 0, 0, 80f / 255f);
        // 인디케이터 확장
        circleAtk.transform.DOScale(Vector2.one * 15f, 1f)
        .SetEase(Ease.Linear);

        // 인디케이터 시간 대기
        yield return new WaitForSeconds(3f);

        // 원형 인디케이터 사운드 중지
        SoundManager.Instance.StopSound(electroStreamSound, 1f);

        // 인디케이터 중지
        circleAtk.Stop();
        // 인디케이터 바닥 숨기기
        circleSprite.DOColor(new Color(1, 0, 0, 0), 1f);

        // 원형 전기 공격 켜기
        circleAtk.transform.GetChild(0).gameObject.SetActive(true);

        // 전기 방출 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_ElectroRising");

        // 전기 방출 시간 대기
        yield return new WaitForSeconds(1f);

        // 공격 끝나면 디스폰
        LeanPool.Despawn(circleAtk);
    }

    IEnumerator PhaseChange()
    {
        // 무적 상태로 전환
        character.invinsible = true;
        monitorSprite.material = SystemManager.Instance.outLineMat;

        // 스크린 마스크 찾기
        SpriteMask screenMask = screenSprite.GetComponent<SpriteMask>();

        // 스크린 글래스 이펙트 움직이기
        glassEffect.DOLocalMove(new Vector3(-10f, 0f), 0.5f)
        .OnUpdate(() =>
        {
            // 이펙트 재생하는 동안 스크린 스프라이트 복제
            screenMask.sprite = screenSprite.sprite;
        })
        .OnStart(() =>
        {
            glassEffect.localPosition = new Vector3(10f, 15f);
        })
        .OnComplete(() =>
        {
            glassEffect.localPosition = new Vector3(10f, 15f);
        });

        // Idle 상태가 될때까지 대기
        yield return new WaitUntil(() => character.nowState == CharacterState.Idle);

        // 무적 상태로 전환
        character.invinsible = true;
        monitorSprite.material = SystemManager.Instance.outLineMat;

        // 휴식 상태로 변경
        character.nowState = CharacterState.Rest;

        // 속도 초기화
        character.rigid.velocity = Vector3.zero;

        // 헤롱헤롱 어지러운 표정
        faceText.text = FaceReturn(Face.Dizzy);

        // 케이블 숨기기
        StartCoroutine(ToggleCable(false));

        // 케이블 숨길때까지 대기
        yield return new WaitUntil(() => !L_PlugHead.enabled);

        // 시스템 리부팅 텍스트 띄우기
        faceText.text = "System\nRebooting";

        // 현재 배경 컬러
        Color nowScreenColor = screen_phaseColor[nowPhase];
        nowScreenColor.a = 0;
        // 현재 텍스트 컬러
        Color nowTextColor = text_phaseColor[nowPhase];
        nowTextColor.a = 0;

        // 모니터 배경 및 텍스트 깜빡거리기
        screenSprite.DOColor(nowScreenColor, 0.2f)
        .SetLoops(3, LoopType.Yoyo);
        faceText.DOColor(nowTextColor, 0.2f)
        .SetLoops(3, LoopType.Yoyo);

        // 넘어지기
        yield return StartCoroutine(Falldown());

        // 4방향 테두리 전기 이펙트 켜기
        for (int i = 0; i < 4; i++)
            fallAtkColl.transform.GetChild(i).GetComponent<ParticleSystem>().Play();

        // 전기 흐르는 사운드 재생
        electroStreamSound = SoundManager.Instance.PlaySound("Ascii_ElectroStream", 1f, 0, -1);

        yield return new WaitForSeconds(1f);

        // 원형 전기 공격
        yield return StartCoroutine(CircleGroundElectro());

        // 4방향 테두리 전기 이펙트 끄기
        for (int i = 0; i < 4; i++)
            fallAtkColl.transform.GetChild(i).GetComponent<ParticleSystem>().Stop();

        // 화면색, 글자색 변경
        // 다음 페이즈의 스크린 색상 적용
        screenSprite.color = screen_phaseColor[nextPhase];
        // 다음 페이즈의 화면 글자 색상 적용
        faceText.color = text_phaseColor[nextPhase];

        // 전기 이펙트 색깔 변경
        effectMat.color = effect_phaseColor[nowPhase];

        // 페이즈별로 스탯 적용
        switch (nextPhase)
        {
            case 1:
                // 데미지 배율, 이속 배율, 투사체 개수 배율 적용
                damageMultiple = 1f;
                speedMultiple = 1f;
                projectileMultiple = 1f;

                break;
            case 2:
                // 데미지 배율, 이속 배율, 투사체 개수 배율 적용
                damageMultiple = 1.2f;
                speedMultiple = 1.2f;
                projectileMultiple = 1.2f;

                break;
            case 3:
                // 데미지 배율, 이속 배율, 투사체 개수 배율 적용
                damageMultiple = 1.5f;
                speedMultiple = 1.5f;
                projectileMultiple = 1.5f;

                break;
        }

        // 몬스터의 기본 정보값
        EnemyInfo originEnemy = EnemyDB.Instance.GetEnemyByID(character.enemy.id);
        // 데미지 갱신
        character.enemy.power = originEnemy.power * damageMultiple;

        print(nowPhase + " -> " + nextPhase);

        // 딜레이
        yield return new WaitForSeconds(0.5f);

        // 일어나기 애니메이션 재생
        GetUp();

        // 딜레이
        yield return new WaitForSeconds(0.5f);
        // 페이즈 업 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_PhaseUp");

        // 일어나는 시간 대기
        yield return new WaitForSeconds(0.5f);

        // 현재 페이즈 숫자 올리기        
        nowPhase = nextPhase;
    }

    void Hit()
    {
        // 무적이 아닐때
        if (!character.invinsible)
            // 맞을때 표정 변화
            StartCoroutine(hitFace());
        // 무적일때
        else
        {
            // 아웃라인 머터리얼로 교체
            monitorSprite.material = SystemManager.Instance.outLineMat;
        }

        // 현재 1페이즈일때, 체력이 2/3 이하일때
        if (nowPhase == 1 && character.characterStat.hpNow / character.characterStat.hpMax <= 2f / 3f)
        {
            // 페이즈업 함수 실행 안됬을때
            if (nowPhase == nextPhase)
            {
                // 다음 페이스 숫자 올리기
                nextPhase = 2;

                // 다음 페이즈 예약
                StartCoroutine(PhaseChange());
            }
        }

        // 현재 2페이즈, 체력이 1/3 이하일때, 3페이즈
        if (nowPhase == 2 && character.characterStat.hpNow / character.characterStat.hpMax <= 1f / 3f)
        {
            // 페이즈업 함수 실행 안됬을때
            if (nowPhase == nextPhase)
            {
                // 다음 페이스 숫자 올리기
                nextPhase = 3;

                // 다음 페이즈 예약
                StartCoroutine(PhaseChange());
            }
        }

        // 체력이 0 이하일때, 죽었을때
        if (character.characterStat.hpNow <= 0)
        {
            //todo 당황하는 표정

            // 죽음 상태로 전환
            character.nowState = CharacterState.Dead;

            //todo 진행 중이던 동작 모두 취소
            // 애니메이션 idle로 전환
            anim.SetTrigger("Die");

            // 전기 예고 사운드 재생중이었으면 중지
            SoundManager.Instance.StopSound(electroStreamSound, 0f);

            //todo 소환 했던 모든 공격 없에기

            // 케이블 집어넣기
            StartCoroutine(ToggleCable(false));

            //todo 보스 전용 죽음 트랜지션 시작
            //todo 죽을때 표정
            //todo 온몸에서 전기 파지직거리는 폭파
            //todo 화면 깨지는 효과
            //todo 폭발 좀 하다가 얼굴 및 화면 배경 꺼짐
            //todo 넘어지고 폭발 인디케이터 확장
            //todo 거대한 폭발남기며 디스폰
        }
    }

    IEnumerator hitFace()
    {
        // 맞을때 표정
        faceText.text = FaceReturn(Face.Hit);

        yield return new WaitForSeconds(0.1f);

        // 표정 그대로일때
        if (faceText.text == FaceReturn(Face.Hit))
            // idle 표정으로 다시 바꾸기
            faceText.text = FaceReturn(Face.Idle);

    }

    private void Update()
    {
        if (character.enemy == null || laserMagic == null)
            return;

        // Idle 아니면 리턴
        if (character.nowState != CharacterState.Idle)
            return;

        // 상태 이상 있으면 리턴
        if (!character.ManageState())
            return;

        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
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

    void ManageAction()
    {
        // 공격 쿨타임 차감
        if (coolCount > 0)
            coolCount -= Time.deltaTime;

        // 플레이어 방향
        Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;

        // //! 
        // fallAtkDone = false;

        //! 거리 및 쿨타임 디버깅
        stateText.text = $"Distance : {string.Format("{0:0.00}", playerDir.magnitude)} \n CoolCount : {string.Format("{0:0.00}", coolCount)}";

        // 공격 쿨타임 됬을때, 공격 범위내에 들어왔을때
        if (coolCount <= 0 && playerDir.magnitude <= attackDistance)
        {
            // 속도 초기화
            character.rigid.velocity = Vector3.zero;

            // // 현재 액션 변경
            // character.nowAction = Character.Action.Attack;

            //공격 패턴 결정하기
            ChooseAttack();

            return;
        }
        else
        {
            // 공격 범위 내 위치로 이동
            Move();
        }
    }

    void Move()
    {
        // 걷기 상태로 전환
        character.nowState = CharacterState.Walk;

        //걸을때 표정
        faceText.text = FaceReturn(Face.Idle);

        // 걷기 애니메이션 시작
        anim.SetBool("isWalk", true);

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //해당 방향으로 가속
        rigid.velocity = dir.normalized * speed * SystemManager.Instance.globalTimeScale;

        //움직일 방향에따라 회전
        if (dir.x > 0)
        {
            // 반전된 상태일때
            if (transform.rotation != Quaternion.Euler(0, -180, 0))
            {
                // 보스 좌우반전
                transform.rotation = Quaternion.Euler(0, 180, 0);

                // 내부 캔버스 오브젝트들 좌우반전
                canvasChildren.localRotation = Quaternion.Euler(0, 180, 0);
            }
        }
        else
        {
            // 반전 없는 상태
            if (transform.rotation != Quaternion.Euler(0, 0, 0))
            {
                //보스 좌우반전 초기화
                transform.rotation = Quaternion.Euler(0, 0, 0);

                // 내부 캔버스 오브젝트들 좌우반전 초기화
                canvasChildren.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }

        // idle 상태로 전환
        character.nowState = CharacterState.Idle;
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
        character.nowState = CharacterState.Attack;

        // 걷기 애니메이션 끝내기
        anim.SetBool("isWalk", false);

        // 가능한 공격 중에서 랜덤 뽑기
        int atkType = Random.Range(0, 4);

        //! 테스트를 위해 패턴 고정
        if (patten != Patten.None)
            atkType = (int)patten;

        // 결정된 공격 패턴 실행
        switch (atkType)
        {
            case 0:
                StartCoroutine(FalldownAttack());
                break;

            case 1:
                StartCoroutine(PunchAttack());
                break;

            case 2:
                StartCoroutine(TrafficAtk());
                break;

            case 3:
                StartCoroutine(GroundAttack());
                break;
        }

        // 랜덤 쿨타임 입력
        coolCount = Random.Range(1f, 5f);
    }

    IEnumerator ToggleCable(bool showCable)
    {
        // 플러그 각도 초기화
        L_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);
        R_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);

        // 케이블 집어넣기
        if (!showCable)
        {
            // 이미 케이블 꺼져있으면 리턴
            if (!L_PlugHead.enabled && !R_PlugHead.enabled)
                yield break;

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
                yield break;

            // 케이블 헤드 켜기
            L_PlugHead.enabled = true;
            R_PlugHead.enabled = true;
            // 케이블 라인 렌더러 켜기
            L_Cable.enabled = true;
            R_Cable.enabled = true;

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

        yield return new WaitForSeconds(0.5f);
    }

    #region FallAtk

    IEnumerator Falldown()
    {
        // 걷기 애니메이션 종료
        anim.SetBool("isWalk", false);

        // 엎어질 준비 애니메이션 시작
        anim.SetTrigger("FallReady");

        // 케이블 숨기기
        StartCoroutine(ToggleCable(false));

        // 엎어질 범위 활성화
        fallRangeBackground.enabled = true;
        fallRangeIndicator.enabled = true;

        // 인디케이터 사이즈 초기화
        fallRangeIndicator.transform.localScale = Vector3.zero;

        // 넘어지기 전 알림 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_Falldown_Warning");

        // 인디케이터 사이즈 늘리기
        fallRangeIndicator.transform.DOScale(Vector3.one, 1f)
        .SetEase(Ease.Linear);
        yield return new WaitForSeconds(1f);

        // 넘어질때 표정
        faceText.text = FaceReturn(Face.Fall);
        // 엎어지는 애니메이션
        anim.SetBool("isFallAtk", true);

        yield return new WaitForSeconds(1f);
    }

    IEnumerator FalldownAttack()
    {
        // 헤롱헤롱 어지러운 표정
        faceText.text = FaceReturn(Face.Dizzy);

        // 넘어지기
        yield return StartCoroutine(Falldown());

        // 몸체 전기 파티클 반복
        ParticleSystem.MainModule bodyMain = bodyElectro.main;
        bodyMain.loop = true;
        bodyElectro.Play();

        yield return new WaitForSeconds(1f);

        // 4방향 중 한 방향씩 예고 한다음 방향 개수 충족 되면 방출
        List<int> chooseAngle = new List<int>();
        int atkDirNum = 0; // 공격 방향 개수
        List<ParticleSystem> groundAtks = new List<ParticleSystem>(); // 공격 오브젝트들

        // 페이즈에 따라 전기 방출 개수 산출
        switch (nowPhase)
        {
            case 1:
                // 2가지 방향
                atkDirNum = 2;
                break;
            case 2:
                // 3가지 방향
                atkDirNum = 3;
                break;
            case 3:
                // 4가지 방향
                atkDirNum = 4;
                break;
        }

        // 같은 패턴 3회 공격
        for (int j = 0; j < 3; j++)
        {
            // 공격 인스턴스 리스트 초기화
            groundAtks.Clear();

            // 모니터 테두리 전기 모두 켜기
            for (int i = 0; i < 4; i++)
                fallAtkColl.transform.GetChild(i).GetComponent<ParticleSystem>().Play();

            // 전기 예고 사운드 재생
            electroStreamSound = SoundManager.Instance.PlaySound("Ascii_ElectroStream", 1f, 0, -1);

            // 페이즈에 따라 방향 산출
            chooseAngle = SystemManager.Instance.RandomIndexes(4, atkDirNum);

            // 정해진 방향 모두 인디케이터 켜기
            for (int i = 0; i < atkDirNum; i++)
            {
                // 공격 각도
                float angle = chooseAngle[i] * 45f + 90f;
                // 각도를 방향 벡터로 바꾸기
                Vector2 atkDir = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle));

                // 공격 방향
                Quaternion atkRotation = Quaternion.Euler(Vector3.forward * chooseAngle[i] * 45);

                // 공격 소환 위치, 플레이어가 가운데로 오게
                Vector2 atkPos = (Vector2)PlayerManager.Instance.transform.position - atkDir * 1.28f * 30f;

                // 공격 준비 오브젝트 소환
                ParticleSystem readyElectro = LeanPool.Spawn(groundElectroAtk, atkPos, atkRotation, ObjectPool.Instance.effectPool);

                // 공격 오브젝트 리스트업
                groundAtks.Add(readyElectro);

                // 공격 오브젝트 끄기
                readyElectro.transform.GetChild(0).gameObject.SetActive(false);
                // 테두리 전기 파티클 켜기
                readyElectro.gameObject.SetActive(true);

                // 인디케이터 바닥 나타내기
                SpriteRenderer sideSprite = readyElectro.GetComponent<SpriteRenderer>();
                sideSprite.color = new Color(1, 0, 0, 0);
                sideSprite.DOColor(new Color(1, 0, 0, 80f / 255f), 1f);

                yield return new WaitForSeconds(0.5f);
            }

            // 모니터 테두리 전기 모두 끄기
            for (int i = 0; i < 4; i++)
                fallAtkColl.transform.GetChild(i).GetComponent<ParticleSystem>().Stop();

            yield return new WaitForSeconds(1f);

            // 전기 예고 사운드 중지
            SoundManager.Instance.StopSound(electroStreamSound, 1f);

            // 전기 방출 사운드 재생
            SoundManager.Instance.PlaySound("Ascii_ElectroRising");

            // 인디케이터 끄면서 공격 실행
            for (int i = 0; i < groundAtks.Count; i++)
            {
                // 테두리 파티클 끄기
                ParticleSystem readyEffect = groundAtks[i];
                readyEffect.Stop();
                // 인디케이터 장판 투명하게
                readyEffect.GetComponent<SpriteRenderer>().DOColor(new Color(1, 0, 0, 0f), 0.5f);

                // 예고한 모든 방향 공격 실행
                groundAtks[i].transform.GetChild(0).gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(1f);

            // 모든 전기 공격 디스폰
            for (int i = 0; i < groundAtks.Count; i++)
            {
                LeanPool.Despawn(groundAtks[i].gameObject);
            }
        }

        // 몸체 전기 파티클 끄기
        bodyMain.loop = false;
        bodyElectro.Stop();

        yield return new WaitForSeconds(0.5f);

        // fallAtk 공격 비활성화 및 일어서기
        GetUp();
    }

    void FallAtkEnable()
    {
        // 히트박스 비활성화
        hitbox.gameObject.SetActive(false);

        // 엎어질 범위 비활성화
        fallRangeBackground.enabled = false;
        fallRangeIndicator.enabled = false;

        // fallAtk 공격 활성화
        fallAtkColl.enabled = true;

        // 콜라이더로 깔린 적들 밀어내기
        fallAtkPush.gameObject.SetActive(true);

        // 스탠드 물리 콜라이더 끄기
        standColl.enabled = false;

        // 모니터 스프라이트 레이어 바꾸기
        // FallLayer();

        // 먼지 파티클 활성화
        fallDustEffect.gameObject.SetActive(true);

        // 카메라 흔들기
        UIManager.Instance.CameraShake(0.5f, 0.3f, 50, 90f, false, false);

        // 넘어지기 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_Falldown");
    }

    void FallAtkDisable()
    {
        // fallAtk 공격 비활성화
        fallAtkColl.enabled = false;
    }

    void HitboxOff()
    {
        // 히트박스 비활성화
        hitbox.gameObject.SetActive(false);
    }

    void HitboxOn()
    {
        // 히트박스 활성화
        hitbox.gameObject.SetActive(true);
    }

    void GetUp()
    {
        // fallAtk 공격 비활성화
        fallAtkColl.enabled = false;

        // 밀어내기 콜라이더 끄기
        fallAtkPush.gameObject.SetActive(false);

        // 스탠드 물리 콜라이더 켜기
        standColl.enabled = true;

        //일어서기 및 휴식 애니메이션 재생
        GetupAnim();
    }

    void FallLayer()
    {
        // 모니터 스프라이트 레이어 바꾸기
        monitorSprite.sortingLayerID = SortingLayer.NameToID("Ground");
    }
    void GetupLayer()
    {
        // 모니터 스프라이트 레이어 바꾸기
        monitorSprite.sortingLayerID = SortingLayer.NameToID("Character&Object");
    }

    void GetupAnim()
    {
        // 일어서기
        anim.SetBool("isFallAtk", false);

        // 휴식 후 Idle 상태로 전환
        StartCoroutine(RestAnim());
    }
    #endregion

    IEnumerator RestAnim()
    {
        // 케이블 숨기기
        StartCoroutine(ToggleCable(false));

        //휴식 시작
        anim.SetBool("isRest", true);

        // 휴식 상태로 변경
        character.nowState = CharacterState.Rest;

        //휴식할때 표정
        faceText.text = FaceReturn(Face.Rest);

        // 휴식 시간만큼 대기
        yield return new WaitForSeconds(2f);

        //휴식 끝
        anim.SetBool("isRest", false);

        // 눈 깜빡이기
        faceText.text = FaceReturn(Face.CloseEye);
        yield return new WaitForSeconds(0.3f);
        faceText.text = FaceReturn(Face.Idle);
        SoundManager.Instance.PlaySound("Ascii_BlinkEye"); // 눈 뜨는 소리 재생
        yield return new WaitForSeconds(0.2f);
        faceText.text = FaceReturn(Face.CloseEye);
        yield return new WaitForSeconds(0.1f);
        faceText.text = FaceReturn(Face.Idle);
        SoundManager.Instance.PlaySound("Ascii_BlinkEye"); // 눈 뜨는 소리 재생
        yield return new WaitForSeconds(0.1f);
        faceText.text = FaceReturn(Face.CloseEye);
        yield return new WaitForSeconds(0.1f);
        faceText.text = FaceReturn(Face.Idle);
        SoundManager.Instance.PlaySound("Ascii_BlinkEye"); // 눈 뜨는 소리 재생
        yield return new WaitForSeconds(0.2f);

        // 보스 무적 해제
        character.invinsible = false;
        monitorSprite.material = SystemManager.Instance.spriteLitMat;

        // 쿨타임 끝나면 idle로 전환, 쿨타임 차감 시작
        character.nowState = CharacterState.Idle;
    }

    #region PunchAtk

    IEnumerator PunchAttack()
    {
        // 얼굴 바꾸기
        faceText.text = FaceReturn(Face.Electro);

        // 케이블 꺼내기
        StartCoroutine(ToggleCable(true));

        // 케이블 애니메이션 끄기
        L_Plug.enabled = false;
        R_Plug.enabled = false;

        // 전원 케이블을 옆으로 이동
        L_Plug.transform.DOLocalMove(new Vector3(-15f, 10f, 0), 0.5f);
        R_Plug.transform.DOLocalMove(new Vector3(15f, 10f, 0), 0.5f);

        yield return new WaitForSeconds(0.5f);

        // 케이블 sort layer를 모니터 앞으로 바꾸기
        L_Cable.sortingOrder = 1;
        R_Cable.sortingOrder = 1;

        // 조준 시간 계산
        float aimTime = Random.Range(1f, 2f);

        // 좌측 플러그 날리기
        StartCoroutine(ShotPunch(true, aimTime));
        // 우측 플러그 날리기
        yield return StartCoroutine(ShotPunch(false, aimTime + 0.5f));

        yield return new WaitForSeconds(0.5f);

        // 케이블 숨기기
        StartCoroutine(ToggleCable(false));

        //휴식 시작
        StartCoroutine(RestAnim());
    }

    IEnumerator ShotPunch(bool isLeft, float aimTime)
    {
        // 케이블 방향에 따라 변수 정하기
        Animator plugControler = isLeft ? L_Plug : R_Plug;
        EnemyAttack plugAtk = isLeft ? L_PlugAtk : R_PlugAtk;
        Transform cableStart = isLeft ? L_CableStart : R_CableStart;
        LineRenderer cableLine = isLeft ? L_Cable : R_Cable;
        GameObject cableSpark = isLeft ? L_CableSpark : R_CableSpark;
        Transform plugHead = isLeft ? L_PlugHead.transform : R_PlugHead.transform;
        Transform plugTip = isLeft ? L_PlugTip : R_PlugTip;

        // 전선 타고 스파크
        StartCoroutine(CableSpark(isLeft, aimTime));

        float aimCount = aimTime;
        while (aimCount > 0)
        {
            //시간 차감
            aimCount -= Time.deltaTime;

            // 플러그에서 플레이어 방향
            Quaternion rotation = Quaternion.Euler(0, 0, SystemManager.Instance.GetVector2Dir(PlayerManager.Instance.transform.position, plugControler.transform.position) - 90f);

            // 플러그가 플레이어 방향 조준
            plugHead.rotation = rotation;

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 플러그 애니메이션 끄기
        plugControler.enabled = false;

        // 케이블 끝 전기 파티클 켜기
        plugTip.gameObject.SetActive(true);

        // 찌릿 사운드 전역 재생
        SoundManager.Instance.PlaySound("Ascii_Sting");

        // 플러그 날리기 전에 떨기
        plugHead.DOShakePosition(0.3f, 0.3f, 50, 90, false, false);
        yield return new WaitForSeconds(0.3f);

        // 플레이어 방향
        Vector3 playerDir = PlayerManager.Instance.transform.position - plugHead.transform.position;
        // 플레이어 위치
        Vector2 playerPos = PlayerManager.Instance.transform.position - playerDir.normalized * 2.7f;
        // 날아갈 시간
        float shotTime = playerDir.magnitude / 20f;

        // 플레이어 위치로 플러그 헤드 이동
        plugHead.transform.DOMove(playerPos, shotTime)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
            // 이동 완료되면 플러그 멈추기
            plugHead.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        });

        // 플레이어 위치로 플러그 컨트롤러 이동
        plugControler.transform.DOMove(playerPos, shotTime)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
            // 땅에 꽂힐때 이펙트 소환 : 흙 파티클 튀기, 바닥 갈라지는 애니메이션
            LeanPool.Spawn(craterEffect, plugTip.position, Quaternion.identity, ObjectPool.Instance.effectPool);

            // 땅 충돌 사운드 재생
            SoundManager.Instance.PlaySound("Ascii_CableGround", plugTip.transform.position);
        });

        // 날아가는동안 대기
        yield return new WaitForSeconds(shotTime);

        // 도달 완료하면 공격 콜라이더 켜기
        plugAtk.gameObject.SetActive(true);

        // 플러그 흔들기
        plugControler.transform.DOShakePosition(0.2f, 0.3f, 50, 90, false, false);

        // 플러그에서 공격 펄스 방출
        LeanPool.Spawn(pulseAtk, plugTip.position, Quaternion.identity, ObjectPool.Instance.magicPool);

        // 공격 시간동안 대기
        yield return new WaitForSeconds(0.5f);

        // 공격 콜라이더 끄기
        plugAtk.gameObject.SetActive(false);

        // 2페이즈 이상일때 투사체날리기
        if (nowPhase >= 2)
        {
            // 케이블 스파크 시작
            yield return StartCoroutine(CableSpark(isLeft, aimTime));

            int shotNum = 3;
            for (int i = 0; i < shotNum; i++)
            {
                // 플러그에서 전기 방출
                GameObject groundSpark = LeanPool.Spawn(electroMisile, plugAtk.transform.position, Quaternion.identity, ObjectPool.Instance.magicPool);

                // 뒤쪽 랜덤 위치
                Vector2 backPos = (Vector2)plugAtk.transform.position + Random.insideUnitCircle * 10f;

                // 플레이어 - 랜덤 위치 거리
                float backDis = Vector2.Distance(PlayerManager.Instance.transform.position, backPos);
                // 플레이어 - 플러그 거리
                float plugDis = Vector2.Distance(PlayerManager.Instance.transform.position, plugAtk.transform.position);

                // 플레이어쪽에 더 가까우면 뒤쪽으로 가게 수정
                if (backDis < plugDis)
                    backPos = (Vector2)plugAtk.transform.position + ((Vector2)plugAtk.transform.position - backPos);

                // 도착 위치, 플레이어 근처
                Vector2 endPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle * 3f;

                // 베지어 곡선 위치 잡기
                Vector3[] bezierCurve = { plugAtk.transform.position, backPos, endPos };

                // 도착 지점까지 거리
                float endDis = Vector2.Distance(endPos, backPos);
                print(endDis);

                // 추적 시간
                float followTime = endDis / 15f;

                // 전기가 플레이어 추적
                groundSpark.transform.DOPath(bezierCurve, followTime, PathType.CatmullRom, PathMode.TopDown2D, 10, Color.red)
                .SetEase(Ease.InCirc)
                .OnComplete(() =>
                {
                    // 3페이즈일때
                    if (nowPhase >= 3)
                    {
                        // 투사체 사라질때 전기 공격 남김
                        LeanPool.Spawn(electroSpreadAtk, groundSpark.transform.position, Quaternion.identity, ObjectPool.Instance.magicPool);
                    }
                    else
                    {
                        // 투사체 사라질때 파동 공격 남김
                        LeanPool.Spawn(pulseAtk, groundSpark.transform.position, Quaternion.identity, ObjectPool.Instance.magicPool);
                    }

                    // 디스폰
                    groundSpark.GetComponent<ParticleManager>().SmoothDespawn();
                });

                yield return new WaitForSeconds(0.1f);
            }

            // 공격 시간동안 대기
            yield return new WaitForSeconds(1f);
        }

        // 공격 완료하면 플러그 이동 재개
        plugHead.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

        // 케이블 시작점은 모니터 뒤로 이동
        Vector2 startPos = isLeft ? new Vector2(5, 0) : new Vector2(-5, 0);

        // 플러그 원위치
        Vector2 plugPos = isLeft ? new Vector2(-12, 10) : new Vector2(12, 10);
        plugControler.transform.DOLocalMove(plugPos, shotTime / 2f)
        .OnComplete(() =>
        {
            // 플러그 애니메이션 켜기
            plugControler.enabled = true;
        });

        // 플러그 원위치 하는동안 대기
        yield return new WaitForSeconds(shotTime / 2f);
    }

    #endregion

    #region GroundAtk

    IEnumerator GroundAttack()
    {
        // 그라운드 어택 시작
        nowGroundAtk = true;

        // 얼굴 바꾸기
        faceText.text = FaceReturn(Face.Electro);

        // 케이블 꺼내기
        yield return StartCoroutine(ToggleCable(true));

        // 플러그 땅에 꼽기
        StartCoroutine(Grounding(true));
        yield return StartCoroutine(Grounding(false));

        yield return new WaitForSeconds(0.5f);

        // 케이블 끝 전기 방출 이펙트 켜기
        L_PlugHead.transform.GetChild(3).gameObject.SetActive(true);
        R_PlugHead.transform.GetChild(3).gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        // 페이즈 2 이상일때
        if (nowPhase >= 2)
        {
            StartCoroutine(ThunderAtk());
        }

        // 정해진 횟수만큼 전기 방출
        for (int i = 0; i < nowPhase * 2 + 1; i++)
        {
            // 방출 할때마다 딜레이
            yield return new WaitForSeconds(1f);

            // 페이즈에 따라 방출 개수
            int electroNum = 30 * nowPhase / 3;

            // 전기 방출
            StartCoroutine(ElectroRelease(true, electroNum));
            yield return StartCoroutine(ElectroRelease(false, electroNum));
        }

        // 케이블 끝 전기 방출 이펙트 끄기
        L_PlugHead.transform.GetChild(3).GetComponent<ParticleManager>().SmoothDisable();
        R_PlugHead.transform.GetChild(3).GetComponent<ParticleManager>().SmoothDisable();

        // 그라운드 어택 종료
        nowGroundAtk = false;

        yield return new WaitForSeconds(1f);

        // 플러그 이동 가능
        L_PlugHead.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        R_PlugHead.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

        // 플러그 각도 초기화
        L_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);
        R_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);

        // 케이블 sort layer를 모니터 뒤로 바꾸기
        L_Cable.sortingOrder = -1;
        R_Cable.sortingOrder = -1;

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

    IEnumerator ThunderAtk()
    {
        // 현재 그라운드 어택 중일때
        while (nowGroundAtk)
        {
            int atkNum = nowPhase * 2 + 1;
            // 페이즈에 따른 개수만큼 번개 내려치기 시작
            for (int i = 0; i < atkNum; i++)
            {
                // 마지막 번개 아닐때
                if (i < atkNum - 1)
                    // 번개 하나 소환
                    StartCoroutine(Thunder(i * 0.2f));
                // 마지막 번개일때
                else
                    // 해당 번개 끝날때까지 대기
                    yield return StartCoroutine(Thunder(i * 0.2f));
            }
        }
    }

    IEnumerator Thunder(float delay)
    {
        // 번개마다 딜레이후 시작
        yield return new WaitForSeconds(delay);

        // 플레이어 주변 공격 위치
        Vector2 atkPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * 5f;

        // 번개 공격 소환
        SpriteRenderer thunderSprite = LeanPool.Spawn(thunderReady, atkPos, Quaternion.identity, ObjectPool.Instance.magicPool);

        // 번개 인디케이터 파티클
        ParticleManager thunderReadyEffect = thunderSprite.GetComponent<ParticleManager>();
        // 공격 콜라이더
        Collider2D thunderColl = thunderSprite.GetComponent<Collider2D>();
        // 3페이즈시 번개 등속 이동해야함
        Rigidbody2D thunderRigid = thunderSprite.GetComponent<Rigidbody2D>();
        float aimTime = 1.5f / nowPhase;

        // 공격 콜라이더 끄기
        thunderColl.enabled = false;

        // 인디케이터 색깔 나타내기
        thunderSprite.color = new Color(1, 0, 0, 0);
        thunderSprite.DOColor(new Color(1, 0, 0, 80f / 255f), aimTime);

        // 페이즈 3일때
        if (nowPhase >= 3)
        {
            WaitForSeconds wait = new WaitForSeconds(Time.deltaTime);
            while (aimTime > 0)
            {
                // 플레이어 방향
                Vector2 playerDir = PlayerManager.Instance.transform.position - thunderSprite.transform.position;

                // 플레이어 방향으로 속도 고정
                thunderRigid.velocity = playerDir.normalized * 3f;

                aimTime -= Time.deltaTime;
                yield return wait;
            }

            // 끝나면 멈추기
            thunderRigid.velocity = Vector3.zero;
        }
        else
        {
            // 인디케이터 시간 대기
            yield return new WaitForSeconds(aimTime);
        }

        yield return new WaitForSeconds(0.5f);

        // 인디케이터 스프라이트 끄기
        thunderSprite.DOColor(new Color(1, 0, 0, 0), 0.5f);
        // 인디케이터 파티클 끄기
        thunderReadyEffect.particle.Stop();

        // 번개 이펙트 소환
        LeanPool.Spawn(thunderbolt, thunderSprite.transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

        yield return new WaitForSeconds(0.3f);

        // 번개 사운드 재생
        SoundManager.Instance.PlaySound("Thunder", thunderSprite.transform.position);

        // 3 페이즈일때
        if (nowPhase >= 3)
        {
            yield return new WaitForSeconds(0.1f);

            // 번개공격 디스폰시 번개 장판으로 스플래쉬 데미지
            LeanPool.Spawn(electroSpreadAtk, atkPos, Quaternion.identity, ObjectPool.Instance.magicPool);
        }
        // 1~2 페이즈일때
        else
        {
            // 공격 콜라이더 켜기
            thunderColl.enabled = true;
            yield return new WaitForSeconds(0.1f);
            // 공격 콜라이더 끄기
            thunderColl.enabled = false;
        }

        // 번개 공격 디스폰
        thunderReadyEffect.SmoothDespawn();
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

        yield return new WaitForSeconds(0.5f);

        // 플러그가 박힐 위치
        Vector3 downPos = isLeft ? new Vector2(-15f, 2.2f) : new Vector2(15f, 2.5f);
        Vector3 fixRotation = isLeft ? new Vector3(0, 0, 180f) : new Vector3(0, 0, -180f);

        // 플러그가 아래로 바라보기
        plugHead.transform.DORotate(fixRotation, 0.5f);

        // 플러그 컨트롤러 아래로 이동
        plugHead.transform.DOLocalMove(downPos, 1f)
        .SetEase(Ease.InBack, 5);
        // 플러그 헤드 아래로 이동
        plugControler.transform.DOLocalMove(downPos, 1f)
        .SetEase(Ease.InBack, 5);

        yield return new WaitForSeconds(1f);

        // 케이블 땅에 꽂는 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_CableGround", plugTip.transform.position);

        // 이동 완료되면 플러그 멈추기
        plugHead.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

        // 땅에 꽂힐때 바닥 균열 소환 : 흙 파티클 튀기, 바닥 갈라짐
        LeanPool.Spawn(craterEffect, plugTip.position, Quaternion.identity, ObjectPool.Instance.effectPool);
    }

    IEnumerator ElectroRelease(bool isLeft, int summonNum)
    {
        Animator plugControler = isLeft ? L_Plug : R_Plug;
        Transform plugHead = isLeft ? L_PlugHead.transform : R_PlugHead.transform;
        LineRenderer cableLine = isLeft ? L_Cable : R_Cable; // 케이블 라인 오브젝트
        GameObject cableSpark = isLeft ? L_CableSpark : R_CableSpark; // 케이블 타고 흐르는 스파크 이펙트
        Transform cableTip = isLeft ? L_PlugAtk.transform : R_PlugAtk.transform; // 케이블 타고 흐르는 스파크 이펙트
        SpriteRenderer cableTipRange = cableTip.GetComponent<SpriteRenderer>();

        // 전선 타고 스파크 흘리기
        yield return StartCoroutine(CableSpark(isLeft));

        // 각 투사체 위치 저장할 배열
        Vector3[] lastPos = new Vector3[summonNum];

        // 랜덤 각도 추가
        float anglePlus = Random.Range(0, 90f);

        // 원형으로 전기 방출
        // 투사체 개수만큼 반복
        for (int i = 0; i < summonNum; i++)
        {
            // 소환할 각도를 벡터로 바꾸기
            float angle = 360f * i / summonNum + anglePlus;
            Vector3 summonDir = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), Mathf.Cos(Mathf.Deg2Rad * angle), 0);
            // print(angle + " : " + summonDir);

            // 투사체 소환 위치, 케이블 끝에서 summonDir 각도로 범위만큼 곱하기 
            Vector3 targetPos = cableTip.transform.position + summonDir * 5f;

            // 각 투사체의 소환 위치 저장
            lastPos[i] = targetPos;

            // 전기 오브젝트 생성
            GameObject atkObj = LeanPool.Spawn(electroMisile, cableTip.transform.position, Quaternion.identity, ObjectPool.Instance.magicPool);

            // summonDir 방향으로 발사
            atkObj.GetComponent<Rigidbody2D>().velocity = summonDir * 20f;
            // summonDir 방향으로 회전
            atkObj.transform.rotation = Quaternion.Euler(0, 0, SystemManager.Instance.GetVector2Dir(summonDir) - 90f);

            // 공격 컴포넌트 찾기
            Attack attack = atkObj.GetComponent<Attack>();

            // 타겟을 플레이어로 전환
            attack.SetTarget(MagicHolder.TargetType.Player);

            // 페이즈에 따른 데미지 지정
            attack.power = 3 * nowPhase * 2f + 1f;
        }

        // 전기 방출 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_CableGround");

        // 플러그 떨림
        plugHead.transform.DOShakePosition(0.2f, 0.3f, 50, 90, false, false);

        // 케이블 끝에서 전기 방출 이펙트
        cableTip.GetComponent<ParticleSystem>().Play();

        // 방출 시간 대기
        yield return new WaitForSeconds(1f);
    }

    #endregion

    #region TrafficAtk

    IEnumerator TrafficAtk()
    {
        // print("laser atk");

        // 레이저 준비 애니메이션 시작
        anim.SetTrigger("LaserReady");

        // 모니터에 화를 참는 얼굴
        faceText.text = FaceReturn(Face.Rage);

        // 동시에 점점 빨간색 게이지가 차오름
        angryGauge.fillAmount = 0f;
        DOTween.To(() => angryGauge.fillAmount, x => angryGauge.fillAmount = x, 1f, 1f);

        // 케이블 꺼내기
        StartCoroutine(ToggleCable(true));

        // 케이블 sort layer를 모니터 앞으로 바꾸기
        L_Cable.sortingOrder = 1;
        R_Cable.sortingOrder = 1;

        //게이지 모두 차오르면 
        yield return new WaitUntil(() => angryGauge.fillAmount >= 1f);

        // 끝나면 빨간색 화면 비우기
        angryGauge.fillAmount = 0;

        // 레이저 준비 애니메이션 시작
        anim.SetTrigger("LaserSet");

        // 레이저 텍스트 켜기
        stopText.gameObject.SetActive(true);
        // 공격 준비 끝나면 stop 띄우기
        stopText.text = "STOP";

        // 신호등 켜기
        trafficLedList[0].transform.parent.gameObject.SetActive(true);

        // 랜덤 딜레이 계산
        float delay = Random.Range(1f, 2f);

        // 신호등 순서대로 켜기
        for (int i = 0; i < 3; i++)
        {
            // 모든 신호등 끄기
            for (int j = 0; j < 3; j++)
            {
                Color offColor = trafficLedList[j].color;
                offColor.a = 0.5f;
                trafficLedList[j].color = offColor;
            }

            // 순서에 해당하는 신호등만 켜기
            Color onColor = trafficLedList[i].color;
            onColor.a = 1f;
            trafficLedList[i].color = onColor;

            // 빨간색 아닐때
            if (i < 2)
            {
                // 신호등 바뀌는 사운드 재생
                SoundManager.Instance.PlaySound("Ascii_Traffic_Led");

                // 딜레이 만큼 대기
                yield return new WaitForSeconds(delay);
            }
            else
                // 정지 사운드 재생
                SoundManager.Instance.PlaySound("Ascii_Traffic_Stop");
        }

        // 펄스 이펙트 커지기
        stopPulseEffect.SetActive(true);

        // 무궁화꽃 주문 끝날때까지 보스 무적상태 만들기
        character.invinsible = true;
        monitorSprite.material = SystemManager.Instance.outLineMat;

        // 감시 직전 잠깐 딜레이
        yield return new WaitForSeconds(0.5f);

        // 감시 애니메이션 시작
        anim.SetBool("isLaserWatch", true);

        // stop 텍스트 지우기
        stopText.text = "";
        // 노려보는 얼굴
        faceText.text = FaceReturn(Face.Watch);

        //몬스터 스폰 멈추기
        SystemManager.Instance.spawnSwitch = false;
        // 모든 몬스터 멈추기
        List<Character> enemys = ObjectPool.Instance.enemyPool.GetComponentsInChildren<Character>().ToList();
        foreach (Character character in enemys)
        {
            // 보스 본인이 아닐때
            if (character != this.character)
                character.stopCount = 3f;
        }

        //감시 시간
        float watchTime = Time.time;
        //플레이어 현재 위치 기억하기
        Vector3 playerPos = PlayerManager.Instance.transform.position;

        // print("watch start : " + Time.time);

        // 플레이어 조준 코루틴 실행
        IEnumerator aimCable = AimCable();
        StartCoroutine(aimCable);

        // 감시 시간 타이머
        while (Time.time - watchTime < 3)
        {
            //플레이어 위치 변경됬으면 레이저 충전
            if (playerPos != PlayerManager.Instance.transform.position)
            {
                // 화난 얼굴로 변경
                faceText.text = FaceReturn(Face.Rage);
                // 얼굴 색 변경
                faceText.color = Color.red;

                // 충전 시간
                float chargeTime = 1f;

                // 레이저 여러번 발사
                for (int j = 0; j < 3; j++)
                {
                    // 양쪽 케이블 타고 전기 흘리기
                    StartCoroutine(CableSpark(true, chargeTime));
                    StartCoroutine(CableSpark(false, chargeTime));

                    // 6칸이므로 6번 반복
                    for (int i = 0; i < 6; i++)
                    {
                        string chargeText = "\n\n(" + new string('■', i) + new string('□', 6 - i) + ")";

                        stopText.text = chargeText;

                        // 6칸이므로 충전시간/6 시간만큼 대기
                        yield return new WaitForSeconds(chargeTime / 6f);
                    }

                    stopText.text = "\n\n(" + new string('■', 6) + ")";

                    //케이블 반짝이는 이벤트 켜기
                    L_PlugHead.transform.GetChild(1).gameObject.SetActive(true);
                    R_PlugHead.transform.GetChild(1).gameObject.SetActive(true);

                    // 풀충전 상태로 이펙트 시간동안 잠깐 대기
                    yield return new WaitForSeconds(0.5f);

                    // 플러그 끝 전기 이펙트 켜기
                    L_PlugTip.gameObject.SetActive(true);
                    R_PlugTip.gameObject.SetActive(true);

                    // 좌,우 레이저 발사 실행, 끝날때까지 대기
                    StartCoroutine(ShotLaser(true));
                    StartCoroutine(ShotLaser(false));

                    // 점사 사이 딜레이 대기
                    yield return new WaitForSeconds(1f);
                }

                // 플러그 끝 전기 이펙트 끄기
                L_PlugTip.gameObject.SetActive(false);
                R_PlugTip.gameObject.SetActive(false);

                // 케이블 sort layer를 모니터 뒤로 바꾸기
                L_Cable.sortingOrder = -1;
                R_Cable.sortingOrder = -1;

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
                SystemManager.Instance.spawnSwitch = true;
                // 모든 몬스터 움직임 재개
                SystemManager.Instance.globalTimeScale = 1f;

                // 플러그 각도 초기화
                L_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);
                R_PlugHead.transform.rotation = Quaternion.Euler(Vector3.zero);

                break;
            }

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 신호등 끄기
        trafficLedList[0].transform.parent.gameObject.SetActive(false);

        // 조준 코루틴 멈추기
        StopCoroutine(aimCable);

        // 케이블 집어넣기
        StartCoroutine(ToggleCable(false));

        // print("watch end : " + Time.time);

        // 감시 종료
        anim.SetBool("isLaserWatch", false);

        // 얼굴 색 변경
        faceText.color = text_phaseColor[nowPhase];

        // 레이저 텍스트 끄기
        stopText.gameObject.SetActive(false);

        // 딜레이
        // yield return new WaitForSeconds(0.5f);

        //몬스터 스폰 재개
        if (!SystemManager.Instance.spawnSwitch)
            SystemManager.Instance.spawnSwitch = true;
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
        GameObject chargeEffect = isLeft ? L_PlugHead.transform.GetChild(4).gameObject : R_PlugHead.transform.GetChild(4).gameObject; // 케이블 플러그

        // 케이블 끝 차지 이펙트 재생
        chargeEffect.SetActive(true);

        // 전선 타고 흐르는 스파크 사운드 재생
        SoundManager.Instance.PlaySound("Ascii_CableSpark");

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

        // 전선 사운드 끄기
        // SoundManager.Instance.StopSound(cableSound, 1f);

        // 케이블 끝 차지 이펙트 끄기
        chargeEffect.SetActive(false);

        // 스파크 끄기
        cableSpark.SetActive(false);

        yield return null;
    }

    IEnumerator ShotLaser(bool isLeft)
    {
        // print("레이저 발사");

        // 쏘는 케이블 변경
        Transform plugTip = isLeft ? L_PlugTip : R_PlugTip;

        //움직이면 레이저 발사 애니메이션 재생
        anim.SetBool("isLaserAtk", true);

        // 레이저 쏠때 얼굴
        faceText.text = FaceReturn(Face.Rage);

        // 3점사로 사격
        for (int i = 0; i < 3; i++)
        {
            // 발사할때 플러그 반동
            plugTip.parent.DOKill();
            plugTip.parent.DOPunchPosition((plugTip.parent.position - PlayerManager.Instance.transform.position).normalized, 0.2f, 10, 1);

            //레이저 생성
            GameObject magicObj = LeanPool.Spawn(LaserPrefab, plugTip.position, Quaternion.identity, ObjectPool.Instance.magicPool);

            LaserBeam laser = magicObj.GetComponent<LaserBeam>();
            // 레이저 발사할 오브젝트 넣기
            laser.startObj = plugTip;

            MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
            // magic 데이터 넣기
            magicHolder.magic = laserMagic;

            // 타겟을 플레이어로 전환
            magicHolder.SetTarget(MagicHolder.TargetType.Player);
            // 플레이어 주변 위치에 쏘기
            magicHolder.targetPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle * 3f;

            // 레이저 발사음 재생
            SoundManager.Instance.PlaySound("Ascii_LaserBeam", magicHolder.targetPos);

            // 3점사 딜레이 대기
            yield return new WaitForSeconds(0.2f);
        }

        //레이저 발사 후딜레이
        yield return new WaitForSeconds(1f);

        //레이저 발사 종료
        anim.SetBool("isLaserAtk", false);
    }

    #endregion
}
