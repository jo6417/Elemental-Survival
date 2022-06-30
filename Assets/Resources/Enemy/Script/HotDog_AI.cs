using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class HotDog_AI : MonoBehaviour
{
    public float farDistance = 10f;

    [Header("Refer")]
    AnimState animState;
    enum AnimState { isWalk, isRun, isHawl, isBark, Bite };
    public EnemyManager enemyManager;
    EnemyInfo enemy;
    public ParticleSystem breathEffect; //숨쉴때 입에서 나오는 불꽃
    public ParticleSystem handDust;
    public ParticleSystem footDust;
    public EnemyAtkTrigger biteTrigger;

    [Header("Stealth Atk")]
    public GameObject eyeTrailPrefab; // 눈에서 나오는 붉은 트레일
    public ParticleSystem groundSmoke; // 스텔스 할때 바닥에서 피어오르는 연기 이펙트
    public ParticleSystem eyeFlash; //눈이 번쩍하는 이펙트
    public ParticleSystem smokeEffect; // 안개 생성시 입에서 나오는 연기
    public EnemyAttack dashAtk;
    public SpriteRenderer shadowSprite; //그림자 스프라이트

    [Header("Meteor")]
    public TextMeshProUGUI stateText; //! 테스트 현재 상태
    public float meteorCoolTime;
    public float meteorRange;
    public int meteorNum;
    MagicInfo meteorMagic;
    GameObject meteorPrefab;

    private void Awake()
    {
        enemyManager = enemyManager == null ? GetComponentInChildren<EnemyManager>() : enemyManager;
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        // 호흡 이펙트 켜기
        breathEffect.gameObject.SetActive(true);
        // 스모크 이펙트 끄기
        smokeEffect.gameObject.SetActive(false);
        // 바닥 연기 이펙트 끄기
        groundSmoke.gameObject.SetActive(false);
        // 눈 번쩍하는 이펙트 끄기
        eyeFlash.gameObject.SetActive(false);

        // 발 먼지 이펙트 끄기
        handDust.Stop();
        footDust.Stop();

        //대쉬 어택 콜라이더 끄기
        dashAtk.enabled = false;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        enemyManager.rigid.velocity = Vector2.zero; //속도 초기화

        //애니메이션 초기화
        enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);
        enemyManager.animList[0].SetBool(AnimState.isHawl.ToString(), false);
        enemyManager.animList[0].SetBool(AnimState.isBark.ToString(), false);

        // 상태값 Idle로 초기화
        enemyManager.nowAction = EnemyManager.Action.Idle;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 메테오 마법 데이터 찾기
        if (meteorMagic == null)
        {
            //찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            meteorMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("Meteor"));

            // 강력한 데미지로 고정
            meteorMagic.power = 20f;

            // 메테오 떨어지는 속도 초기화
            meteorMagic.speed = 1f;

            // 메테오 프리팹 찾기
            meteorPrefab = MagicDB.Instance.GetMagicPrefab(meteorMagic.id);
        }

        //죽을때 델리게이트에 함수 추가
        enemyManager.enemyDeadCallback += Dead;
    }

    void Update()
    {
        if (enemyManager.enemy == null)
            return;

        // 상태 이상 있으면 리턴
        if (!enemyManager.ManageState())
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
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        // 플레이어와의 거리
        float distance = dir.magnitude;

        // Idle 아니면 리턴
        if (enemyManager.nowAction != EnemyManager.Action.Idle)
            return;

        // 플레이어 방향 쳐다보기
        if (dir.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // 물기 콜라이더에 플레이어 닿으면 bite 패턴
        if (biteTrigger.atkTrigger)
        {
            // 현재 액션 변경
            enemyManager.nowAction = EnemyManager.Action.Attack;

            // 호흡 이펙트 끄기
            StartCoroutine(breathEffect.GetComponent<ParticleManager>().SmoothDisable());

            // 물기 패턴 실행
            StartCoroutine(Bite());
            return;
        }

        // 먼 거리일때
        if (distance > farDistance)
        {
            //! 거리 확인용
            stateText.text = "Far : " + distance;

            // 플레이어 따라가기
            Walk();
        }
        // 가까울때
        else
        {
            //! 거리 확인용
            stateText.text = "Close : " + distance;

            // 속도 초기화
            enemyManager.rigid.velocity = Vector3.zero;

            //공격 패턴 결정하기
            ChooseAttack();
        }
    }

    void Walk()
    {
        enemyManager.nowAction = EnemyManager.Action.Walk;

        //애니메이터 켜기
        enemyManager.animList[0].enabled = true;

        // 거리가 멀면 Run 애니메이션으로 전환
        enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), true);

        //TODO 거리가 가까우면 Walk 애니메이션으로 전환
        // enemyManager.animList[0].SetBool(AnimState.isWalk.ToString(), true);

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        // 움직일 방향 2D 각도
        float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        float runSpeed = 1f;

        //TODO 거리가 멀면 추가 속도 1.5배
        // runSpeed = 1.5f;

        //해당 방향으로 가속
        enemyManager.rigid.velocity = dir.normalized * enemyManager.speed * SystemManager.Instance.globalTimeScale * runSpeed;

        // 상태값 Idle로 초기화
        SetIdle(0f);
    }

    void ChooseAttack()
    {
        // 현재 액션 변경
        enemyManager.nowAction = EnemyManager.Action.Attack;

        // Idle 애니메이션 재생
        enemyManager.animList[0].SetBool(AnimState.isWalk.ToString(), false);
        enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);

        // 발 먼지 이펙트 끄기
        handDust.Stop();
        footDust.Stop();

        // 호흡 이펙트 끄기
        StartCoroutine(breathEffect.GetComponent<ParticleManager>().SmoothDisable());

        // 랜덤 패턴 결정
        int randomNum = Random.Range(0, 3);

        print("randomNum : " + randomNum);

        //! 테스트를 위해 패턴 고정
        // randomNum = 2;

        switch (randomNum)
        {
            case 0:
                //TODO 근거리 및 중거리 헬파이어 패턴
                StartCoroutine(Hellfire());
                break;
            case 1:
                // 원거리 메테오 쿨타임 아닐때 meteor 패턴 코루틴
                StartCoroutine(Meteor());
                break;
            case 2:
                // 원거리 스텔스 쿨타임 아닐때 stealthAtk 패턴 코루틴
                StartCoroutine(StealthAtk());
                break;
        }
    }
    IEnumerator Bite()
    {
        yield return null;

        // Run 애니메이션 끄기
        enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);

        // 물기 애니메이션 재생
        enemyManager.animList[0].SetTrigger(AnimState.Bite.ToString());
    }

    void BiteEnd()
    {
        // 행동 초기화
        SetIdle(1f);
    }

    IEnumerator SetIdle(float endDelay)
    {
        // 호흡 이펙트 켜기
        breathEffect.gameObject.SetActive(true);

        yield return new WaitForSeconds(endDelay);

        // 상태값 Idle로 초기화
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    IEnumerator Hellfire()
    {
        yield return null;
    }

    #region Meteor
    IEnumerator Meteor()
    {
        yield return null;

        // 하울링 애니메이션 재생
        enemyManager.animList[0].SetBool(AnimState.isHawl.ToString(), true);
    }

    // meteor 애니메이션 끝날때쯤 meteor 소환 함수
    public void CastMeteor()
    {
        StartCoroutine(SummonMeteor());
    }

    IEnumerator SummonMeteor()
    {
        //메테오 개수만큼 반복
        for (int i = 0; i < meteorNum; i++)
        {
            // 메테오 떨어질 위치
            Vector2 targetPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle * 20f;

            // 메테오 생성
            GameObject magicObj = LeanPool.Spawn(meteorPrefab, targetPos, Quaternion.identity, SystemManager.Instance.magicPool);

            // 메테오 스프라이트 빨갛게
            // magicObj.GetComponent<SpriteRenderer>().color = Color.red;

            MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
            // magic 데이터 넣기
            magicHolder.magic = meteorMagic;

            // 타겟을 플레이어로 전환
            magicHolder.SetTarget(MagicHolder.Target.Player);

            // 메테오 목표지점 targetPos 넣기
            magicHolder.targetPos = targetPos;

            yield return new WaitForSeconds(0.1f);
        }

        // Idle 애니메이션 재생
        enemyManager.animList[0].SetBool(AnimState.isHawl.ToString(), false);

        // 상태값 Idle로 초기화
        SetIdle(1f);
    }
    #endregion

    #region StealthAttack
    IEnumerator StealthAtk()
    {
        // 짖기 애니메이션 재생
        enemyManager.animList[0].SetBool(AnimState.isBark.ToString(), true);

        // 투명해질때까지 대기
        yield return new WaitUntil(() => enemyManager.spriteList[0].color == Color.clear);

        // 짖기 애니메이션 트리거 끄기
        enemyManager.animList[0].SetBool(AnimState.isBark.ToString(), false);

        // 돌진 횟수 계산 2~5회
        int atkNum = Random.Range(2, 6);

        //돌진 시작 방향 (좌 or 우)
        bool rightStart = true;
        // 짝수일때
        if (atkNum % 2 == 0)
            //오른쪽에서 시작
            rightStart = true;
        // 홀수일때
        else
            //왼쪽에서 시작
            rightStart = false;

        // 랜덤 횟수로 좌,우 번갈아 달려가며 공격
        for (int i = 0; i < atkNum; i++)
        {
            // 돌진 시작 위치 계산
            Vector3 dashStartPos = rightStart ? Camera.main.ViewportToWorldPoint(new Vector2(1, 0.5f)) : Camera.main.ViewportToWorldPoint(new Vector2(0, 0.5f));
            Vector3 dashEndPos = rightStart ? Camera.main.ViewportToWorldPoint(new Vector2(0, 0.5f)) : Camera.main.ViewportToWorldPoint(new Vector2(1, 0.5f));
            dashStartPos.z = 0f;
            dashEndPos.z = 0f;

            //돌진 시작 위치로 이동
            transform.position = dashStartPos;

            // 돌진 방향으로 회전
            if ((dashEndPos - dashStartPos).x > 0)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            // 눈이 빛나는 인디케이터 이펙트 보여주기
            eyeFlash.gameObject.SetActive(true);

            // 스프라이트 색 초기화
            foreach (SpriteRenderer sprite in enemyManager.spriteList)
            {
                sprite.DOColor(Color.white, 0.2f);
            }
            //그림자 색 초기화
            shadowSprite.DOColor(new Color(0, 0, 0, 0.5f), 0.2f);

            // 피격 콜라이더 켜기
            foreach (Collider2D coll in enemyManager.hitCollList)
            {
                coll.enabled = true;
            }

            // 대쉬 어택 콜라이더 켜기
            dashAtk.enabled = true;

            // 인디케이터 시간 대기
            yield return new WaitForSeconds(0.5f);

            // 눈 위치에 아키라 트레일 생성
            GameObject eyeTrail = LeanPool.Spawn(eyeTrailPrefab, eyeFlash.transform.position, Quaternion.identity, eyeFlash.transform.parent);

            // Run 애니메이션 재생
            enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), true);

            // 엔드 포지션으로 트윈
            transform.DOMove(dashEndPos, 1f)
            .OnComplete(() =>
            {
                // 아이 트레일 부모 바꾸기
                eyeTrail.transform.SetParent(SystemManager.Instance.effectPool);

                // 아이 아키라 트레일 1초후 디스폰
                LeanPool.Despawn(eyeTrail, 1f);
            });

            // 돌진시간 대기
            yield return new WaitForSeconds(0.8f);

            // 돌진 끝나면 스프라이트 다시 투명해지기
            foreach (SpriteRenderer sprite in enemyManager.spriteList)
            {
                sprite.DOColor(Color.clear, 0.2f);
            }
            //그림자 투명하게
            shadowSprite.DOColor(Color.clear, 0.2f);

            //출발 방향 반대로 돌리기
            rightStart = !rightStart;

            // 애니메이션 idle
            enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);

            // 돌진 후딜레이 대기
            yield return new WaitForSeconds(0.5f);
        }

        // 플레이어 주변 랜덤 위치로 이동
        transform.position = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * 20f;

        // 보스를 부모로 지정
        groundSmoke.transform.SetParent(transform);
        // 바닥 연기 이펙트 끄기
        groundSmoke.gameObject.SetActive(false);

        // 대쉬 어택 콜라이더 끄기
        dashAtk.enabled = false;
        // 충돌 콜라이더 켜기
        enemyManager.physicsColl.enabled = true;

        // 피격 콜라이더 켜기
        foreach (Collider2D coll in enemyManager.hitCollList)
        {
            coll.enabled = true;
        }

        // 애니메이션 idle
        enemyManager.animList[0].SetBool(AnimState.isRun.ToString(), false);

        // 글로벌 라이트 초기화
        DOTween.To(x => SystemManager.Instance.globalLight.intensity = x, SystemManager.Instance.globalLight.intensity, SystemManager.Instance.globalLightDefault, 0.5f);

        // 스프라이트 색 초기화
        foreach (SpriteRenderer sprite in enemyManager.spriteList)
        {
            sprite.DOColor(Color.white, 0.5f);
        }

        //그림자 색 초기화
        shadowSprite.DOColor(new Color(0, 0, 0, 0.5f), 0.5f);

        // 상태값 Idle로 초기화
        SetIdle(0.5f);
    }

    public void MakeFog()
    {
        // 배경 어두워지고 보스 투명해지기
        StartCoroutine(Cloaking());
    }

    IEnumerator Cloaking()
    {
        // 스모크 이펙트 켜기
        smokeEffect.gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);

        // 플레이어를 부모로 지정
        groundSmoke.transform.SetParent(PlayerManager.Instance.transform);
        groundSmoke.transform.localPosition = Vector3.zero;
        // 바닥 연기 이펙트 켜기
        groundSmoke.gameObject.SetActive(true);

        //충돌 콜라이더 끄기
        enemyManager.physicsColl.enabled = false;

        // 피격 콜라이더 끄기
        foreach (Collider2D coll in enemyManager.hitCollList)
        {
            coll.enabled = false;
        }

        // 글로벌 라이트 어둡게
        DOTween.To(x => SystemManager.Instance.globalLight.intensity = x, SystemManager.Instance.globalLight.intensity, 0.1f, 1f);

        // 스프라이트 투명해지며 사라지기
        foreach (SpriteRenderer sprite in enemyManager.spriteList)
        {
            sprite.DOColor(Color.clear, 1f);
        }

        // 그림자도 투명하게
        shadowSprite.DOColor(Color.clear, 1f);

        // 투명도 절반까지 대기
        yield return new WaitForSeconds(0.5f);

        // 스모크 이펙트 끄기
        StartCoroutine(smokeEffect.GetComponent<ParticleManager>().SmoothDisable());
    }
    #endregion

    #region FootDust
    void HandDustPlay()
    {
        if (enemyManager.nowAction == EnemyManager.Action.Walk)
            handDust.Play();
    }

    void HandDustStop()
    {
        handDust.Stop();
    }

    void FootDustPlay()
    {
        if (enemyManager.nowAction == EnemyManager.Action.Walk)
            footDust.Play();
    }

    void FootDustStop()
    {
        footDust.Stop();
    }
    #endregion

    void Dead()
    {
        // 글로벌 라이트 초기화
        SystemManager.Instance.globalLight.intensity = SystemManager.Instance.globalLight.intensity;

        // 그림자 색 초기화
        shadowSprite.color = new Color(0, 0, 0, 0.5f);

        // 보스를 부모로 지정
        groundSmoke.transform.SetParent(transform);
        // 바닥 연기 이펙트 끄기
        groundSmoke.gameObject.SetActive(false);

        // 호흡 이펙트 켜기
        breathEffect.gameObject.SetActive(true);
        // 스모크 이펙트 끄기
        smokeEffect.gameObject.SetActive(false);
        // 바닥 연기 이펙트 끄기
        groundSmoke.gameObject.SetActive(false);
        // 눈 번쩍하는 이펙트 끄기
        eyeFlash.gameObject.SetActive(false);

        // 발 먼지 이펙트 끄기
        handDust.Stop();
        footDust.Stop();
    }
}
