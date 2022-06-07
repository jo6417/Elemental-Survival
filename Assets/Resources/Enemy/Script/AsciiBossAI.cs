using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Lean.Pool;
using System.Linq;

public class AsciiBossAI : MonoBehaviour
{
    public NowState nowState;
    public enum NowState { Idle, Walk, Attack, Rest, Hit, Dead, TimeStop, SystemStop }

    [Header("Refer")]
    public Image angryGauge; //분노 게이지 이미지
    public EnemyAtkTrigger fallRangeTrigger; //엎어지기 범위 내에 들어왔는지 보는 트리거
    public EnemyAtkTrigger LaserRangeTrigger; //레이저 범위 내에 들어왔는지 보는 트리거
    public TextMeshProUGUI faceText;
    public TextMeshProUGUI laserText;
    public Transform canvasChildren;
    EnemyManager enemyManager;
    Collider2D coll;
    Animator anim;
    Rigidbody2D rigid;
    SpriteRenderer fallRange;
    // Collider2D fallColl;
    public GameObject LaserPrefab; //발사할 레이저 마법 프리팹
    public GameObject pulseEffect; //laser stop 할때 펄스 이펙트
    MagicInfo laserMagic = null; //발사할 레이저 마법 데이터
    SpriteRenderer laserRange;

    EnemyInfo enemy;
    float speed;
    float coolCount;
    List<int> atkList = new List<int>(); //공격 패턴 담을 변수

    //! 테스트
    [Header("Debug")]
    public bool fallAtkAble;
    public bool laserAtkAble;

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();

        fallRange = fallRangeTrigger.GetComponent<SpriteRenderer>();
        laserRange = LaserRangeTrigger.GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        //표정 초기화
        faceText.text = "...";

        rigid.velocity = Vector2.zero; //속도 초기화

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (enemy == null)
            enemy = enemyManager.enemy;

        //fall어택 콜라이더에 enemy 정보 넣어주기
        fallRangeTrigger.GetComponent<EnemyManager>().enemy = enemy;

        transform.DOKill();

        //애니메이션 스피드 초기화
        if (anim != null)
            anim.speed = 1f;

        // 위치 고정 해제
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //스피드 초기화
        speed = enemy.speed;

        // 콜라이더 충돌 초기화
        coll.isTrigger = false;

        //공격범위 오브젝트 초기화
        fallRange.enabled = false;
        laserRange.enabled = false;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 레이저 마법 데이터 찾기
        if (laserMagic == null)
        {
            //찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            laserMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("Laser Beam"));

            // 강력한 데미지로 고정
            laserMagic.power = 20f;
        }
    }

    private void Update()
    {
        if (enemy == null || laserMagic == null)
            return;

        // fall 콜라이더에 플레이어 있으면 리스트에 fall 공격패턴 담기
        if (fallRangeTrigger.atkTrigger)
        {
            atkList.Add(0);
            fallAtkAble = true;
        }
        else
        {
            atkList.Remove(0);
            fallAtkAble = false;
        }

        // Laser 콜라이더에 플레이어 있으면 리스트에 Laser 공격패턴 담기
        if (LaserRangeTrigger.atkTrigger)
        {
            atkList.Add(1);
            laserAtkAble = true;
        }
        else
        {
            atkList.Remove(1);
            laserAtkAble = false;
        }

        // 공격 가능한 패턴 없을때 플레이어 따라가기
        if (atkList.Count == 0)
        {
            // 대기 상태면 걷기 시작
            if (nowState == NowState.Idle)
            {
                nowState = NowState.Walk;

                // 걷기 애니메이션 시작
                anim.SetBool("isWalk", true);
            }
        }
        else
        {
            //공격 가능하면 걷기 멈춤
            if (nowState == NowState.Walk)
            {
                // 걷기 애니메이션 끝내기
                anim.SetBool("isWalk", false);

                // Idle 애니메이션 시작
                SetIdle();
            }
        }

        if (nowState == NowState.Walk)
        {
            // 위치 고정 해제
            rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

            //플레이어 따라가기
            Walk();
        }
        else
        {
            //보스 자체 회전값 따라 캔버스 자식들 모두 역반전
            // canvasChildren.rotation = transform.rotation;

            //rigid 고정, 밀어도 안밀리게
            rigid.constraints = RigidbodyConstraints2D.FreezeAll;

            //이동 멈추기
            rigid.velocity = Vector2.zero;
        }

        // 공격 쿨타임 돌리기
        if (nowState == NowState.Idle)
        {
            if (coolCount <= 0)
            {
                nowState = NowState.Attack;

                ChooseAttack();
            }
            else
            {
                coolCount -= Time.deltaTime;
            }
        }
    }

    void Walk()
    {
        //걸을때 표정
        faceText.text = "● ▽ ●";

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //해당 방향으로 가속
        rigid.velocity = dir.normalized * speed * SystemManager.Instance.globalTimeScale;

        //움직일 방향에따라 회전
        if (dir.x > 0)
        {
            //내부 텍스트 오브젝트들 좌우반전
            if (transform.rotation == Quaternion.Euler(0, 0, 0))
            {
                canvasChildren.rotation = Quaternion.Euler(0, 180, 0);
            }

            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            //내부 텍스트 오브젝트들 좌우반전
            if (transform.rotation == Quaternion.Euler(0, 180, 0))
            {
                canvasChildren.rotation = Quaternion.Euler(0, 0, 0);
            }

            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    void ChooseAttack()
    {
        nowState = NowState.Attack;

        // 가능한 공격 중에서 랜덤 뽑기
        int randAtk = -1;
        if (atkList.Count > 0)
        {
            randAtk = atkList[Random.Range(0, atkList.Count)];
        }

        // 결정된 공격 패턴 실행
        switch (randAtk)
        {
            case 0:
                FalldownAttack();
                break;

            case 1:
                StartCoroutine(LaserAtk());
                break;
        }

        // 랜덤 쿨타임 입력
        coolCount = Random.Range(1f, 5f);

        //패턴 리스트 비우기
        atkList.Clear();
    }

    void FalldownAttack()
    {
        // 앞뒤로 흔들려서 당황하는 표정
        faceText.text = "◉ Д ◉";

        // 엎어질 준비 애니메이션 시작
        anim.SetTrigger("FallReady");

        //공격 범위 오브젝트 활성화
        // fallRangeObj.SetActive(true);

        // 엎어질 범위 활성화 및 반짝거리기
        fallRange.enabled = true;
        Color originColor = new Color(1, 0, 0, 0.2f);
        fallRange.color = originColor;

        fallRange.DOColor(new Color(1, 0, 0, 0f), 1f)
        .SetEase(Ease.InOutFlash, 5, 0)
        .OnComplete(() =>
        {
            fallRange.color = originColor;

            //넘어질때 표정
            faceText.text = "> ︿ <";

            //엎어지는 애니메이션
            anim.SetBool("isFallAtk", true);
        });
    }

    void FallAtkCollider()
    {
        // 콜라이더 내에 플레이어 아직 있으면 멈추기, 데미지 입히기
        if (fallRangeTrigger.atkTrigger)
        {
            //피격 딜레이 무적
            IEnumerator hitDelay = PlayerManager.Instance.HitDelay();
            StartCoroutine(hitDelay);

            bool isDead = PlayerManager.Instance.Damage(enemy.power);

            //죽었으면 리턴
            // if(isDead)
            // return;

            // 플레이어 멈추고 납작해지기
            StartCoroutine(PlayerManager.Instance.FlatDebuff());
        }

        //일어서기, 휴식 애니메이션 재생
        StartCoroutine(GetUpAnim());
    }

    IEnumerator GetUpAnim()
    {
        //일어날때 표정
        faceText.text = "x  _  x";

        // 엎어진채로 1초 대기
        yield return new WaitForSeconds(1f);

        // 일어서기, 휴식 애니메이션 시작
        anim.SetBool("isFallAtk", false);

        StartCoroutine(RestAnim(2f));
    }

    IEnumerator LaserAtk()
    {
        // print("laser atk");

        // 레이저 준비 애니메이션 시작
        anim.SetTrigger("LaserReady");

        // 모니터에 화를 참는 얼굴
        faceText.text = "◣` ︿ ´◢"; //TODO 얼굴 바꾸기

        // 동시에 점점 빨간색 게이지가 차오름
        angryGauge.fillAmount = 0f;
        DOTween.To(() => angryGauge.fillAmount, x => angryGauge.fillAmount = x, 1f, 2f);

        // 동시에 공격 범위 표시
        // 엎어질 범위 활성화 및 반짝거리기
        laserRange.enabled = true;
        Color originColor = new Color(1, 0, 0, 0.2f);
        laserRange.color = originColor;

        laserRange.DOColor(new Color(1, 0, 0, 0f), 1f)
        .SetEase(Ease.InOutFlash, 5, 0)
        .OnComplete(() =>
        {
            laserRange.color = originColor;
        });

        //게이지 모두 차오르면 
        yield return new WaitUntil(() => angryGauge.fillAmount >= 1f);

        // 무궁화 애니메이션 시작
        anim.SetTrigger("LaserSet");

        //채워질 글자
        string targetText = "무궁화꽃이\n피었습니다";

        //텍스트 비우기
        laserText.text = "";

        //공격 준비 글자 채우기
        StartCoroutine(LaserReadyText(targetText));

        // 글자 모두 표시되면 Stop 표시
        yield return new WaitUntil(() => laserText.text == "STOP");

        // 감시 애니메이션 시작
        anim.SetBool("isLaserWatch", true);

        // 노려보는 얼굴
        faceText.text = "⚆`  ︿  ´⚆";

        //몬스터 스폰 멈추기
        EnemySpawn.Instance.spawnSwitch = false;
        // 모든 몬스터 멈추기
        SystemManager.Instance.globalTimeScale = 0f;
        // List<EnemyManager> enemys = SystemManager.Instance.enemyPool.GetComponentsInChildren<EnemyManager>().ToList();
        // foreach (var enemy in enemys)
        // {
        //     enemy.stopCount = 3f;
        // }

        //감시 시간
        float watchTime = Time.time;
        //플레이어 현재 위치
        Vector3 playerPos = PlayerManager.Instance.transform.position;

        print("watch start : " + Time.time);

        // 플레이어 움직이는지 감시
        while (Time.time - watchTime < 3)
        {
            //플레이어 위치 변경됬으면 레이저 발사
            if (playerPos != PlayerManager.Instance.transform.position)
            {
                //움직이면 레이저 발사 애니메이션 재생
                anim.SetBool("isLaserAtk", true);

                // 레이저 쏠때 얼굴
                faceText.text = "◣` ︿ ´◢";

                // 플레이어에게 양쪽눈에서 레이저 여러번 발사
                int whitchEye = 0;
                for (int i = 0; i < 10; i++)
                {
                    ShotLaser(LaserRangeTrigger.transform.GetChild(whitchEye));

                    //쏘는 눈 변경
                    whitchEye = whitchEye == 0 ? 1 : 0;

                    //레이저 쏘는 시간 대기
                    yield return new WaitForSeconds(0.3f);
                }

                //레이저 쏘는 시간 대기
                yield return new WaitForSeconds(2f);

                //레이저 발사 종료
                anim.SetBool("isLaserAtk", false);

                // while문 탈출
                break;
            }

            yield return new WaitForSeconds(Time.deltaTime);
        }

        print("watch end : " + Time.time);

        // 감시 종료
        anim.SetBool("isLaserWatch", false);

        //몬스터 스폰 재개
        EnemySpawn.Instance.spawnSwitch = true;
        // 모든 몬스터 움직임 재개
        SystemManager.Instance.globalTimeScale = 1f;

        //휴식 시작
        StartCoroutine(RestAnim(3f));
    }

    void ShotLaser(Transform shotter)
    {
        print("레이저 발사");

        //레이저 생성
        GameObject magicObj = LeanPool.Spawn(LaserPrefab, shotter.position, Quaternion.identity, SystemManager.Instance.magicPool);

        LaserBeam laser = magicObj.GetComponent<LaserBeam>();
        // 레이저 발사할 오브젝트 넣기
        laser.startObj = shotter;

        MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
        // magic 데이터 넣기
        magicHolder.magic = laserMagic;

        // 타겟을 플레이어로 전환
        magicHolder.SetTarget(MagicHolder.Target.Player);
        // 레이저 목표지점 targetPos 넣기
        magicHolder.targetPos = PlayerManager.Instance.transform.position;
    }

    IEnumerator RestAnim(float restTime)
    {
        //휴식 시작
        anim.SetBool("isRest", true);
        //휴식할때 표정
        faceText.text = "x  _  x";

        nowState = NowState.Rest;

        yield return new WaitForSeconds(restTime);

        //휴식 끝
        anim.SetBool("isRest", false);

        // Idle 상태로 전환
        SetIdle();
    }

    void SetIdle()
    {
        nowState = NowState.Idle;

        //로딩중 텍스트 애니메이션
        StartCoroutine(LoadingText());
    }

    IEnumerator LoadingText()
    {
        string targetText = "...";

        faceText.text = targetText;

        while (nowState == NowState.Idle)
        {
            if (faceText.text.Length < targetText.Length)
                faceText.text = targetText.Substring(0, faceText.text.Length + 1);
            else
                faceText.text = targetText.Substring(0, 0);

            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator LaserReadyText(string targetText)
    {
        float delay = 0.2f;

        while (laserText.text.Length < targetText.Length)
        {
            laserText.text = targetText.Substring(0, laserText.text.Length + 1);

            //글자마다 랜덤 딜레이 갱신 
            delay = Random.Range(0.2f, 0.5f);
            delay = 0.1f;

            yield return new WaitForSeconds(delay);
        }

        laserText.text = "STOP";

        //펄스 이펙트 활성화
        pulseEffect.SetActive(true);
    }
}
