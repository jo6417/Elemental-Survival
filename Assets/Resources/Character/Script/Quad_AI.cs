using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class Quad_AI : MonoBehaviour
{
    WaitForSeconds wait_1 = new WaitForSeconds(1f);

    [Header("State")]
    [SerializeField]
    Patten testPatten = Patten.None;
    enum Patten { PushSide, PushCircle, SingleShot, FanSmash, None };
    bool moveFanTilt = true; // 팬 기울이기 여부
    bool fanHorizon = true; // 팬 수평유지 여부
    private float atkCoolCount;
    public float pushSideCoolTime;
    public float pushCircleCoolTime;
    public float bladeShotCoolTime;
    public float fanSmashCoolTime;
    public float atkRange = 30f; // 공격 범위
    public float fanSensitive = 0.1f; //프로펠러 기울기 감도 조절

    [Header("Walk")]
    float targetSearchCount; // 타겟 위치 추적 시간 카운트
    [SerializeField]
    float targetSearchTime = 3f; // 타겟 위치 추적 시간
    Vector3[] fanDefaultPos = new Vector3[4]; // 프로펠러 원위치 리스트

    [Header("Refer")]
    public TextMeshProUGUI stateText; //! 테스트 현재 상태
    public Character character;
    public Transform body;
    public Transform head;
    public Transform fanParent;
    public Transform eye;
    public Color fanDefaultColor = new Color(0, 50f / 255f, 70f / 255f, 1f);
    public Color fanRageColor = new Color(1f, 30f / 255f, 0, 1f);
    public Color wingDefaultColor = new Color(30f / 255f, 1f, 1f, 10f / 255f);
    public Color wingRageColor = new Color(1f, 30f / 255f, 30f / 255f, 1f);
    Vector3 fanParentDefaultRotation = new Vector3(70, 0, 0);
    Vector3 fanDefaultRotation = new Vector3(-70, 0, 0);
    Transform[] fans = new Transform[4]; // 프로펠러 리스트
    SpriteRenderer[] wings = new SpriteRenderer[4]; // 프로펠러 날개 리스트
    ParticleManager[] flyEffects = new ParticleManager[4]; // 비행 파티클 리스트
    ParticleManager[] pushEffects = new ParticleManager[4]; // 밀어내기 이펙트 리스트
    ParticleManager[] sparkEffects = new ParticleManager[4]; // 프로펠러 스파크 이펙트 리스트
    ParticleManager[] chargeEffects = new ParticleManager[4]; // 프로펠러 차지 이펙트 리스트
    public List<GameObject> restEffects = new List<GameObject>();
    public SortingGroup sortingLayer; //전체 레이어

    [Header("PushSide")]
    public GameObject wallBox; // 플레이어 가두는 상자 프리팹
    public GameObject bladePrefab; // 프로펠러 톱날 프리팹

    [Header("PushCircle")]
    public GameObject windCutterPrefab; // 윈드커터 마법 프리팹
    MagicInfo windMagic;
    public ParticleSystem wallCircle; // 플레이어 가두는 원형 프리팹
    [SerializeField]
    float spinSpeed = 2f; // 프로펠러 공전 속도
    [SerializeField]
    float windShotNum = 50f; // 윈드커터 발사 횟수
    [SerializeField]
    float windShotDelay = 0.1f; // 윈드커터 발사 간격

    [Header("BladeShot")]
    public GameObject maskedBladePrefab; // 가로로 박히는 프로펠러 톱날 프리팹
    public GameObject dirtSlashEffect;
    public GameObject dirtLayEffect;
    // public ParticleSystem chargeEffect; // 기모으기 이펙트
    public ParticleSystem fanSparkEffect; // 프로펠러 스파크 이펙트
    public LineRenderer eyeLaser;
    public SpriteRenderer targetMarker;
    public float aimTime = 3f;

    [Header("FanSmash")]
    public float smashRange = 15f; // 프로펠러 휘두르는 범위
    public float smashReadyTime = 0.5f; // FanSmash 준비 시간
    public float smashTime = 2f; // 프로펠러 휘두르는 시간

    private void Awake()
    {
        // 프로펠러 관련 오브젝트 찾기
        for (int i = 0; i < 4; i++)
        {
            fans[i] = fanParent.GetChild(i);

            // 프로펠러 원래 로컬 위치 저장
            fanDefaultPos[i] = fans[i].localPosition;

            // 프로펠러 하위 오브젝트들 찾기
            wings[i] = fans[i].GetChild(0).GetComponentInChildren<SpriteRenderer>();
            flyEffects[i] = fans[i].GetChild(1).GetComponent<ParticleManager>();
            pushEffects[i] = fans[i].GetChild(2).GetComponentInChildren<ParticleManager>(true);
            sparkEffects[i] = fans[i].GetChild(3).GetComponentInChildren<ParticleManager>(true);
            chargeEffects[i] = fans[i].GetChild(4).GetComponentInChildren<ParticleManager>(true);

            // 각 파티클 초기화
            flyEffects[i].gameObject.SetActive(true);
            pushEffects[i].gameObject.SetActive(false);
            sparkEffects[i].gameObject.SetActive(false);
            chargeEffects[i].gameObject.SetActive(false);
        }

        // 레이어 1로 초기화
        sortingLayer.sortingOrder = 1;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => character.enemy != null);

        // 휴식 이펙트 전부 끄기
        for (int i = 0; i < restEffects.Count; i++)
        {
            restEffects[i].SetActive(false);
        }

        //애니메이션 스피드 초기화
        if (character.animList != null)
        {
            foreach (Animator anim in character.animList)
            {
                anim.speed = 1f;
            }
        }

        //속도 초기화
        character.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        character.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 원형 밀어내기 및 가두기 콜라이더 끄기
        wallCircle.gameObject.SetActive(false);
        wallCircle.GetComponentInChildren<CircleCollider2D>(true).enabled = false;
        wallCircle.GetComponentInChildren<EdgeCollider2D>(true).enabled = false;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 헬파이어 마법 데이터 찾기
        if (windMagic == null)
        {
            // 찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            windMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("WindCutter"));

            // 데미지 고정
            windMagic.power = 5f;
            // 스피드 고정
            windMagic.speed = 10f;
            // 지속시간 고정
            windMagic.duration = 2.5f;
        }
    }

    private void Update()
    {
        // 몬스터 정보 없으면 리턴
        if (character.enemy == null)
            return;

        // 타겟 추적 쿨타임 차감
        if (targetSearchCount > 0)
            targetSearchCount -= Time.deltaTime;
        // 쿨타임 됬을때
        else
        {
            // 플레이어 추정 위치 계산
            character.targetPos = PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle * 2f;

            // 추적 쿨타임 갱신
            targetSearchCount = targetSearchTime;
        }

        // 추적 위치 벡터를 서서히 이동
        character.movePos = Vector3.Lerp(character.movePos, character.targetPos, Time.deltaTime * 2f);

        // 플레이어 방향
        character.targetDir = character.movePos - head.position;

        // 플레이어 방향 각도
        float playerAngle = Mathf.Atan2(character.targetDir.y, character.targetDir.x) * Mathf.Rad2Deg;

        // 휴식 아닐때만
        if (character.nowState != Character.State.Rest)
        {
            // 눈동자 플레이어 방향으로 이동
            eye.position = head.position + character.targetDir.normalized * 1f;

            // 눈동자 플레이어 방향으로 회전
            eye.rotation = Quaternion.Euler(0, 0, playerAngle + 135f);
        }

        if (fanHorizon)
        {
            // 프로펠러들의 각도 유지
            for (int i = 0; i < 4; i++)
            {
                fans[i].rotation = Quaternion.Euler(0, 0, 0);
            }
        }

        // Idle 상태 아니면 리턴
        if (character.nowState != Character.State.Idle)
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

        // Idle 아니면 리턴
        if (character.nowState != Character.State.Idle)
            return;

        // 공격 쿨타임 차감
        if (atkCoolCount > 0)
            atkCoolCount -= Time.deltaTime;

        // 플레이어 방향
        Vector2 dir = character.movePos - transform.position;

        // 플레이어와의 거리
        float distance = dir.magnitude;

        //! 쿨타임 확인
        stateText.text = "CoolCount : " + atkCoolCount;

        // 범위 내에 있을때
        if (distance <= atkRange)
        {
            // 쿨타임 됬을때
            if (atkCoolCount <= 0)
            {
                // 속도 초기화
                character.rigid.velocity = Vector3.zero;

                //공격 패턴 결정하기
                ChooseAttack();

                return;
            }
        }

        // 플레이어 따라가기
        Walk();
    }

    void ChooseAttack()
    {
        // 현재 액션 변경
        character.nowState = Character.State.Attack;

        // 애니메이터 끄기
        character.animList[0].enabled = false;

        // 랜덤 패턴 결정
        int randomNum = Random.Range(0, 4);

        // print("randomNum : " + randomNum);

        //! 테스트를 위해 패턴 고정
        if (testPatten != Patten.None)
            randomNum = (int)testPatten;

        switch (randomNum)
        {
            case 0:
                // 한쪽으로 밀기 패턴
                StartCoroutine(SidePush());
                //쿨타임 갱신
                atkCoolCount = pushSideCoolTime;
                break;

            case 1:
                // 원형으로 밀기 패턴
                StartCoroutine(CirclePush());
                //쿨타임 갱신
                atkCoolCount = pushCircleCoolTime;
                break;

            case 2:
                // 단일 블레이드 발사 패턴
                StartCoroutine(SingleShot());
                //쿨타임 갱신
                atkCoolCount = bladeShotCoolTime;
                break;

            case 3:
                // 프로펠러 후려치기 패턴
                StartCoroutine(FanSmash());
                //쿨타임 갱신
                atkCoolCount = fanSmashCoolTime;
                break;
        }
    }

    void Walk()
    {
        character.nowState = Character.State.Walk;

        //애니메이터 켜기
        character.animList[0].enabled = true;
        // Idle 애니메이션으로 전환
        // chracter.animList[0].SetBool("UseFist", false);

        //! 거리 확인
        stateText.text = "Distance : " + character.targetDir.magnitude;

        // 기울임 각도 계산
        float angleZ = -Mathf.Clamp(character.targetDir.x / 1.5f, -15f, 15f);
        Quaternion rotation = Quaternion.Lerp(fanParent.localRotation, Quaternion.Euler(70, 0, angleZ), fanSensitive);

        // 프로펠러 기울이기
        if (moveFanTilt)
            fanParent.localRotation = rotation;

        // 플레이어까지 거리
        float distance = Vector3.Distance(character.movePos, transform.position);

        // 공격범위 이내 접근 못하게 하는 속도 계수
        float nearSpeed = distance < atkRange
        // 범위 안에 있을때
        ? character.targetDir.magnitude - atkRange
        // 범위 밖에 있을때
        : 1f;

        //해당 방향으로 가속
        character.rigid.velocity =
        character.targetDir.normalized
        * character.speedNow
        * SystemManager.Instance.globalTimeScale
        * nearSpeed;

        character.nowState = Character.State.Idle;
    }

    private void OnDrawGizmosSelected()
    {
        // 보스부터 이동 위치까지 직선
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, character.movePos);

        // 이동 위치 기즈모
        Gizmos.DrawIcon(character.movePos, "Circle.png", true, new Color(1, 0, 0, 0.5f));

        // 추적 위치 기즈모
        Gizmos.DrawIcon(character.targetPos, "Circle.png", true, new Color(0, 0, 1, 0.5f));

        // 추적 위치부터 이동 위치까지 직선
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(character.targetPos, character.movePos);
    }

    IEnumerator SidePush()
    {
        // 플레이어 위치에 벽 오브젝트 소환해서 플레이어 가두기
        GameObject wall = LeanPool.Spawn(wallBox, PlayerManager.Instance.transform.position, Quaternion.identity);
        // 스프라이트 찾기
        SpriteRenderer wallGround = wall.GetComponentInChildren<SpriteRenderer>();
        // 가두기 콜라이더 찾기
        Collider2D wallColl = wall.GetComponentInChildren<Collider2D>();
        // 모든 벽 파티클 찾기
        ParticleSystem[] wallParticles = wall.GetComponentsInChildren<ParticleSystem>(true);

        // 벽 콜라이더 끄기
        wallColl.enabled = false;
        // 바닥 색깔 초기화
        wallGround.DOColor(SystemManager.Instance.HexToRGBA("00FFFF", 30f / 255f), 0.5f);
        // 바닥 사이즈 키우기
        wallGround.transform.localScale = Vector3.zero;
        wallGround.transform.DOScale(new Vector2(50, 30), 0.5f);

        yield return new WaitForSeconds(0.5f);

        // 벽 콜라이더 켜기
        wallColl.enabled = true;
        // 벽 파티클 전부 켜기
        for (int i = 0; i < wallParticles.Length; i++)
        {
            wallParticles[i].Play();
        }

        // 좌,우 중 한쪽 선정
        bool isLeftSide = Random.value > 0.5f ? true : false;

        // 이동할 위치
        Vector3 wallSidePos = isLeftSide ? wall.transform.position + Vector3.left * 30f : wall.transform.position + Vector3.right * 30f;

        // 벽 가운데로 이동
        transform.DOMove(wallSidePos, 2f)
        .SetEase(Ease.InOutCubic);

        // 프로펠러 수평으로
        fanParent.DORotate(fanParentDefaultRotation, 2f);

        yield return new WaitForSeconds(2f);

        // 몸체 저공으로 내려오기
        body.DOLocalMove(new Vector3(0, 2.5f, 0), 1f)
        .SetEase(Ease.OutCubic)
        .OnComplete(() =>
        {
            // 머리 및 프로펠러 레이어 0으로 내리기
            sortingLayer.sortingOrder = 0;
        });

        // 해당 벽 파티클 끄기
        if (isLeftSide)
            wallParticles[1].Stop();
        else
            wallParticles[3].Stop();

        // 벽 모서리 위치 계산
        Vector2 leftUpEdge = new Vector2(
            wall.transform.position.x - wallGround.bounds.size.x / 2f,
            wall.transform.position.y + wallGround.bounds.size.y / 2f);
        Vector2 leftDownEdge = new Vector2(
            wall.transform.position.x - wallGround.bounds.size.x / 2f,
            wall.transform.position.y - wallGround.bounds.size.y / 2f);
        Vector2 rightUpEdge = new Vector2(
            wall.transform.position.x + wallGround.bounds.size.x / 2f,
            wall.transform.position.y + wallGround.bounds.size.y / 2f);
        Vector2 rightDownEdge = new Vector2(
            wall.transform.position.x + wallGround.bounds.size.x / 2f,
            wall.transform.position.y - wallGround.bounds.size.y / 2f);

        // 벽 가로 사이즈
        float horizonSize = wallGround.bounds.size.x;

        // 벽 가운데 위치
        Vector2 sideCenterPos = isLeftSide ? (leftUpEdge + leftDownEdge) / 2f : (rightUpEdge + rightDownEdge) / 2f;
        // 첫번째 프로펠러 위치
        Vector2 firstFanPos = sideCenterPos + Vector2.up * 12f;
        // 프로펠러 회전 각도
        Vector3 fanRotate = isLeftSide ? Vector3.forward * -90f : Vector3.forward * 90f;

        // 팬 수평유지 스위치 끄기
        fanHorizon = false;

        for (int i = 0; i < 4; i++)
        {
            Transform fan = fans[i];
            GameObject push = pushEffects[i].gameObject;

            // 비행 파티클 끄기
            flyEffects[i].SmoothDisable();

            // 벽 한쪽에 프로펠러를 일렬로 배치
            fan.DOMove(firstFanPos + Vector2.down * 8f * i, 1f);

            // 프로펠러 벽 안쪽으로 회전
            fan.DORotate(fanRotate, 1f)
            .OnComplete(() =>
            {
                // 밀어내기 바람 파티클 켜기
                push.SetActive(true);
            });

            yield return new WaitForSeconds(0.1f);
        }

        // 처음에는 일정시간 밀어내기만
        yield return new WaitForSeconds(3f);

        // 프로펠러 인덱스 리스트 만들기
        List<int> bladeIndexes = new List<int>();

        // 블레이드 발사 횟수 3~5번
        int shotNum = Random.Range(3, 6);

        //! 블레이드 발사 횟수 고정
        // shotNum = 0;

        // 블레이드 횟수만큼 발사
        for (int j = 0; j < shotNum; j++)
        {
            // 리스트 비우기
            bladeIndexes.Clear();
            // 리스트 인덱스로 모두 채우기
            for (int i = 0; i < 4; i++)
            {
                bladeIndexes.Add(i);
            }

            // 한번에 발사할 블레이드 개수 2~3개중 랜덤
            float multipleNum = Random.Range(2, 4);

            //! 블레이드 개수 고정
            // multipleNum = 2;

            for (int i = 0; i < multipleNum; i++)
            {
                // 남은 프로펠러 중 랜덤 인덱스 선정
                int fanIndex = Random.Range(0, bladeIndexes.Count);

                // bladeIndexes[fanIndex] 번째의 프로펠러 발사
                if (i == multipleNum - 1)
                    // 마지막 반복일때, 코루틴 시간 대기
                    yield return StartCoroutine(ShotBlade(bladeIndexes[fanIndex], isLeftSide, horizonSize));
                else
                    StartCoroutine(ShotBlade(bladeIndexes[fanIndex], isLeftSide, horizonSize));

                // 해당 인덱스 빼기
                bladeIndexes.RemoveAt(fanIndex);
            }
        }

        // 바람 밀어내기 이펙트 끄기
        for (int i = 0; i < 4; i++)
        {
            pushEffects[i].SmoothDisable();
        }

        // 벽 콜라이더 끄기
        wallColl.enabled = false;

        // 벽의 모든 파티클 끄기
        for (int i = 0; i < wallParticles.Length; i++)
        {
            wallParticles[i].Stop();
        }

        // 바닥 색깔 연해지기
        Color clearColor = wallGround.color;
        clearColor.a = 0;
        wallGround.DOColor(clearColor, 1f);

        // 몸체 올려서 원위치
        body.DOLocalMove(new Vector3(0, 5.5f, 0), 1f)
        .SetEase(Ease.OutCubic)
        .OnComplete(() =>
        {
            // 머리 및 프로펠러 레이어 1로 올리기
            sortingLayer.sortingOrder = 1;
        });

        // 머리 올라간 만큼 프로펠러들 낮추기
        fanParent.DOLocalMove(new Vector3(0, -3f, 0), 1f);

        yield return wait_1;

        // 프로펠러 부모 원위치
        fanParent.DOLocalMove(new Vector3(0, 0f, 0), 1f);

        // 벽 디스폰
        LeanPool.Despawn(wall);

        // 과부하 휴식 트랜지션 재생
        StartCoroutine(OverloadRest());
    }

    IEnumerator ShotBlade(int fanIndex, bool isLeftSide, float horizonSize)
    {
        // 안쪽 방향 벡터
        Vector3 innerDir = isLeftSide ? Vector3.right : Vector3.left;

        // 밀어내기 이펙트 끄기
        pushEffects[fanIndex].SmoothDisable();

        // 프로펠러 빨갛게 달아오르기
        wings[fanIndex].DOColor(wingRageColor, 1f);
        yield return wait_1;

        // 프로펠러 날개 오브젝트 끄기
        wings[fanIndex].gameObject.SetActive(false);
        // 프로펠러 색깔 초기화
        wings[fanIndex].color = wingDefaultColor;

        // 발사 반동으로 떨림
        fans[fanIndex].transform.DOPunchPosition(-innerDir, 0.4f);

        // 블레이드 생성
        GameObject shotBlade = LeanPool.Spawn(bladePrefab, wings[fanIndex].transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

        // 블레이드에 몬스터 매니저 넣기
        shotBlade.GetComponent<EnemyAttack>().character = character;

        // 블레이드 스프라이트 켜기
        SpriteRenderer bladeSprite = shotBlade.GetComponentInChildren<SpriteRenderer>();
        bladeSprite.enabled = true;

        // 발사 방향에 따라 발사체 회전
        shotBlade.transform.rotation = isLeftSide ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180f, 0);

        //사이즈 제로에서 키우기
        shotBlade.transform.localScale = Vector3.zero;
        shotBlade.transform.DOScale(Vector3.one, 0.2f);

        // 플레이어 쪽으로 프로펠러 블레이드 발사 후 복귀
        Vector3 blade_TargetPos = wings[fanIndex].transform.position + innerDir * horizonSize;
        shotBlade.transform.DOMove(blade_TargetPos, 1f)
        .SetLoops(2, LoopType.Yoyo);
        yield return wait_1;

        // 발사 방향 반대로 블레이드 회전
        shotBlade.transform.rotation = isLeftSide ? Quaternion.Euler(0, 180f, 0) : Quaternion.Euler(0, 0, 0);
        // 복귀 시간 대기
        yield return wait_1;

        // 블레이드 스프라이트 끄기
        bladeSprite.enabled = false;
        // 블레이드 디스폰
        LeanPool.Despawn(shotBlade);

        // 프로펠러 스프라이트 켜기
        wings[fanIndex].gameObject.SetActive(true);

        // 프로펠러 장착 반동으로 떨림
        fans[fanIndex].transform.DOPunchPosition(-innerDir, 0.4f);

        // 살짝 딜레이 후 밀어내기 바람 이펙트 켜기
        yield return new WaitForSeconds(0.5f);
        pushEffects[fanIndex].gameObject.SetActive(true);
    }

    IEnumerator CirclePush()
    {
        // 프로펠러 수평으로
        fanParent.DORotate(fanParentDefaultRotation, 2f);

        //플레이어 근처 위치로 이동
        transform.DOMove(PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle.normalized * 2, 0.5f);

        // 원형 바닥 스프라이트 찾기
        SpriteRenderer wallGround = wallCircle.GetComponentInChildren<SpriteRenderer>();
        // 가두기 콜라이더 찾기
        EdgeCollider2D wallColl = wallCircle.GetComponentInChildren<EdgeCollider2D>();
        // 밀어내기 콜라이더 찾기
        CircleCollider2D pushColl = wallCircle.GetComponentInChildren<CircleCollider2D>();
        // 모든 벽 파티클 찾기
        ParticleSystem[] wallParticles = wallCircle.GetComponentsInChildren<ParticleSystem>(true);

        yield return new WaitForSeconds(0.5f);

        // 몸체 저공으로 내려오기
        body.DOLocalMove(new Vector3(0, 2.5f, 0), 1f)
        .SetEase(Ease.OutCubic)
        .OnComplete(() =>
        {
            // 머리 및 프로펠러 레이어 0으로 내리기
            sortingLayer.sortingOrder = 0;
        });

        // 바닥 색깔 초기화
        wallGround.DOColor(SystemManager.Instance.HexToRGBA("00FFFF", 150f / 255f), 0.5f);

        // 원형 바닥 사이즈 초기화
        wallCircle.transform.localScale = Vector3.zero;
        // 원형 바닥 활성화
        wallCircle.gameObject.SetActive(true);
        // 원형 바닥 사이즈 키우기
        wallCircle.transform.DOScale(Vector3.one * 20, 0.5f);

        yield return new WaitForSeconds(0.5f);

        // 원형 테두리 및 밀어내기 파티클 켜기
        wallCircle.Play();

        // 가두기 콜라이더 켜기
        wallColl.enabled = true;
        // 밀어내기 콜라이더 켜기
        pushColl.enabled = true;

        // 모든 팬 위치 정렬
        for (int i = 0; i < 4; i++)
        {
            // 비행 이펙트 전부 끄기
            flyEffects[i].SmoothDisable();

            // 머리보다 왼쪽에 있을때
            if (fans[i].position.x < head.position.x)
                fans[i].DORotate(Vector3.forward * 90f, 0.2f);
            // 머리보다 오른쪽에 있을때
            else
                fans[i].DORotate(Vector3.forward * -90f, 0.2f);

            // 그룹 레이어 통일
            fans[i].GetComponent<SortingGroup>().sortingOrder = 0;
        }

        yield return new WaitForSeconds(0.2f);

        // 팬 수평유지 스위치 끄기
        fanHorizon = false;

        // 점점 빠르게 반 바퀴만 돌리기
        fanParent.DOLocalRotate(Vector3.forward * 180f, 0.5f / spinSpeed, RotateMode.LocalAxisAdd)
        .OnUpdate(() =>
        {
            // 팬들의 각도가 항상 바깥쪽 바라보기
            for (int i = 0; i < 4; i++)
            {
                // 머리보다 왼쪽에 있을때
                if (fans[i].position.x < head.position.x)
                    fans[i].rotation = Quaternion.Euler(0, 0, 90);
                // 머리보다 오른쪽에 있을때
                else
                    fans[i].rotation = Quaternion.Euler(0, 0, -90);
            }
        })
        .SetEase(Ease.InSine);

        // 반 바퀴 돌리는 동안 대기
        yield return new WaitForSeconds(0.5f / spinSpeed);

        // 팬을 빠르게 공전시키기
        fanParent.DOLocalRotate(Vector3.forward * 360f, 1f / spinSpeed, RotateMode.LocalAxisAdd)
        .OnUpdate(() =>
        {
            // 팬들의 각도가 항상 바깥쪽 바라보기
            for (int i = 0; i < 4; i++)
            {
                // 머리보다 왼쪽에 있을때
                if (fans[i].position.x < head.position.x)
                    fans[i].rotation = Quaternion.Euler(0, 0, 90);
                // 머리보다 오른쪽에 있을때
                else
                    fans[i].rotation = Quaternion.Euler(0, 0, -90);
            }
        })
        .SetEase(Ease.Linear)
        .SetLoops(-1);

        WaitForSeconds wait_windShotDelay = new WaitForSeconds(windShotDelay);

        // 윈드커터 마법을 모든 프로펠러에서 발사
        // 발사 횟수만큼 반복
        for (int j = 0; j < windShotNum; j++)
        {
            //! 발사 횟수 확인
            stateText.text = "Remain : " + (windShotNum - j - 1);

            // 프로펠러 개수만큼 반복
            for (int i = 0; i < 4; i++)
            {
                // 생성할 프로펠러
                Transform shotterFan = fans[i];

                // 머리에서 프로펠러까지 방향
                Vector2 fanDir = shotterFan.position - head.transform.position;
                // 프로펠러 전방 목표 지점
                Vector2 targetPos = (Vector2)shotterFan.position + fanDir.normalized * 20f;

                // 윈드커터 생성
                GameObject magicObj = LeanPool.Spawn(windCutterPrefab, shotterFan.transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

                // 매직홀더 찾기
                MagicHolder magicHolder = magicObj.GetComponentInChildren<MagicHolder>();

                // magic 데이터 넣기
                magicHolder.magic = windMagic;

                // 타겟을 플레이어로 전환
                magicHolder.SetTarget(MagicHolder.TargetType.Player);

                // 프로펠러 전방을 목표지점으로 설정
                magicHolder.targetPos = targetPos;

                // 타겟 오브젝트 넣기
                magicHolder.targetObj = PlayerManager.Instance.gameObject;
            }

            // 발사 지연시간
            yield return wait_windShotDelay;
        }

        // 가두기 콜라이더 끄기
        wallColl.enabled = false;
        // 밀어내기 콜라이더 끄기
        pushColl.enabled = false;

        // 벽의 모든 파티클 끄기
        for (int i = 0; i < wallParticles.Length; i++)
        {
            wallParticles[i].Stop();
        }

        // 바닥 색깔 연해지기
        Color clearColor = wallGround.color;
        clearColor.a = 0;
        wallGround.DOColor(clearColor, 1f);

        // 팬 공전 멈추기
        fanParent.DOPause();

        // 팬 수평유지 스위치 켜기
        fanHorizon = true;

        // 원형 벽 비활성화
        wallCircle.gameObject.SetActive(false);

        // 과부하로 착지해서 휴식
        StartCoroutine(OverloadRest());
    }

    IEnumerator SingleShot()
    {
        // 발사할 날개 인덱스 선정
        int wingIndex = Random.Range(0, 4);

        //! 발사할 날개 고정
        // wingNum = 1;

        SpriteRenderer shotWing = wings[wingIndex];

        // 날개 빠르게 돌리며 빨갛게 달아오름
        shotWing.DOColor(wingRageColor, aimTime);

        // 해당 날개의 차지 이펙트 활성화
        chargeEffects[wingIndex].gameObject.SetActive(true);

        // 조준 시간 초기화
        float aimCount = aimTime;

        // 레이저 및 마커 활성화
        eyeLaser.enabled = true;
        targetMarker.enabled = true;

        // 레이저 굵기
        float laserWidth = 0.3f;
        float targetWidth = 0;

        WaitForSeconds wait_deltaTime = new WaitForSeconds(Time.deltaTime);

        // 조준 하는동안 플레이어 위쪽에서 계속 추적
        while (aimCount > 0)
        {
            // 조준 시간 차감
            aimCount -= Time.deltaTime;

            // 플레이어 위쪽 목표 위치
            Vector3 targetPos = PlayerManager.Instance.transform.position + Vector3.up * 10f;

            // 목표 위치로 이동
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);

            // 플레이어 방향
            Vector2 playerDir = character.movePos - transform.position;
            // 플레이어 방향 각도
            float angle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg + 90f;
            // 기울임 각도 계산
            Quaternion rotation = Quaternion.Lerp(fanParent.rotation, Quaternion.Euler(70, 0, angle), fanSensitive);

            // 드론 아래쪽이 플레이어를 바라보게 기울이기
            fanParent.rotation = rotation;

            // 레이저 포인트 설정
            eyeLaser.SetPosition(0, eyeLaser.transform.position);
            eyeLaser.SetPosition(1, character.movePos);
            // 타겟 위치에 마커 옮기기
            targetMarker.transform.position = character.movePos;

            // 레이저 굵기 점점 얇아짐
            targetWidth = aimCount / aimTime * 0.3f;

            // 20% 남았을때
            if (aimCount / aimTime < 0.2f)
                // 레이저 점점 굵게
                targetWidth = (aimTime - aimCount) / aimTime * 0.5f;

            // 10% 남았을때
            if (aimCount / aimTime < 0.1f)
                // 0으로 줄어들기
                targetWidth = 0f;

            // 레이저 굵기 목표 굵기로 점점 변화
            laserWidth = Mathf.Lerp(laserWidth, targetWidth, 0.1f);

            // 레이저 굵기 적용
            eyeLaser.startWidth = laserWidth;
            eyeLaser.endWidth = laserWidth;

            yield return wait_deltaTime;
        }

        // 레이저 및 마커 비활성화
        eyeLaser.enabled = false;
        targetMarker.enabled = false;

        // 발사할 프로펠러 진동
        shotWing.transform.parent.DOShakePosition(0.2f, 0.05f, 10, 90f, false, false);

        // 프로펠러 차지 이펙트 끄기
        chargeEffects[wingIndex].SmoothDisable();

        // 프로펠러에서 불씨 파티클 생성
        LeanPool.Spawn(fanSparkEffect, shotWing.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        // 목표 위치에 블레이드 마스크 생성
        GameObject bladeMask = LeanPool.Spawn(maskedBladePrefab, character.movePos, Quaternion.identity, SystemManager.Instance.enemyPool);

        // 자식중에 블레이드 오브젝트 찾기
        Transform shotBlade = bladeMask.GetComponentInChildren<Animator>().transform;

        // 블레이드 위치를 프로펠러 위치로 초기화
        shotBlade.position = shotWing.transform.position;
        shotBlade.localScale = Vector3.zero;

        // 블레이드에 몬스터 매니저 넣기
        bladeMask.GetComponentInChildren<EnemyAttack>().character = character;

        // 블레이드 스프라이트 켜기
        SpriteRenderer bladeSprite = shotBlade.GetComponentInChildren<SpriteRenderer>();
        bladeSprite.enabled = true;

        // 발사 방향에 따라 발사체 회전
        shotBlade.rotation
        = character.movePos.x > transform.position.x
        ? Quaternion.Euler(0, 0, 0)
        : Quaternion.Euler(0, 180f, 0);

        //사이즈 제로에서 키우기
        shotBlade.DOScale(Vector3.one, 0.2f);

        // 플레이어 쪽으로 블레이드 발사
        shotBlade.DOMove(character.movePos, 0.4f)
        .OnComplete(() =>
        {
            // 블레이드 회전 애니메이터 멈추기
            shotBlade.GetComponentInChildren<Animator>().enabled = false;

            // 박힐때 흙 튀기기
            LeanPool.Spawn(dirtSlashEffect, bladeMask.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
            // 흙 깔아주기
            LeanPool.Spawn(dirtLayEffect, bladeMask.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
        });

        // wing 오브젝트 끄기
        shotWing.gameObject.SetActive(false);
        // wing 색깔 초기화
        shotWing.color = wingDefaultColor;
        // 발사된 날개 비행 파티클 끄기
        flyEffects[wingIndex].SmoothDisable();

        // 발사할때 위치 저장
        Vector2 shotPos = transform.position;
        // 발사할때 각도 저장
        Quaternion shotRotate = transform.rotation;

        // 쏘는 반대 방향으로 보스 몸 전체 넉백 이동
        Vector3 shotDir = character.movePos - transform.position;
        transform.DOMove(transform.position - shotDir.normalized * 3f, 0.5f)
        .SetEase(Ease.OutBack);

        // 프로펠러 방향에 따라 기울기 넉백 계산
        Vector3 knockbackRotate = wingIndex == 0 || wingIndex == 2
        ? Vector3.back * 20f
        : Vector3.forward * 20f;

        // 넉백으로 기울어졌다 돌아오기
        fanParent.DOPunchRotation(knockbackRotate, 0.5f, 5);

        yield return new WaitForSeconds(0.5f);

        // 발사할때 위치로 서서히 돌아오기
        transform.DOMove(shotPos, 1f)
        .SetEase(Ease.OutSine);

        yield return wait_1;

        // 몸 전체 약한 기울기 진동
        fanParent.DOShakeRotation(1f, Vector3.forward, 50, 90, false);
        // 땅에 박힌 프로펠러 강한 진동
        shotBlade.DOShakeRotation(1f, Vector3.forward * 5f, 100, 90, false);

        // 땅에 박힌 블레이드가 프로펠러 방향으로 살짝 이동
        shotBlade.DOMove(shotBlade.transform.position - shotDir.normalized * 0.1f, 0.1f)
        .SetEase(Ease.Linear);
        // 살짝 회전
        shotBlade.DORotate(Vector3.forward * 10f, 0.1f)
        .SetEase(Ease.Linear);

        yield return wait_1;

        // 블레이드 좌우 반전
        shotBlade.transform.rotation = Quaternion.Euler(shotBlade.transform.rotation.eulerAngles.x + 180f, 0, 0);

        // 블레이드 회전 애니메이터 켜기
        shotBlade.GetComponentInChildren<Animator>().enabled = true;

        // 뽑힐때 흙 튀기기
        LeanPool.Spawn(dirtSlashEffect, bladeMask.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        // 블레이드 프로펠러로 날아가기
        shotBlade.DOMove(shotWing.transform.parent.position, 0.2f)
        .SetEase(Ease.Linear);

        yield return new WaitForSeconds(0.2f);

        // 블레이드 디스폰
        LeanPool.Despawn(bladeMask);

        // 블레이드 장착되며 wing 오브젝트 켜기
        shotWing.gameObject.SetActive(true);
        // 발사된 날개 비행 파티클 켜기
        flyEffects[wingIndex].gameObject.SetActive(true);

        // 장착할때 불꽃 파티클
        LeanPool.Spawn(fanSparkEffect, shotWing.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        // 블레이드 날아온 방향으로 보스 몸 전체 넉백 이동
        transform.DOMove(transform.position - shotDir.normalized * 3f, 0.5f)
        .SetEase(Ease.OutBack);

        // 프로펠러 방향에 따라 기울기 넉백 계산
        knockbackRotate = wingIndex == 0 || wingIndex == 2
        ? Vector3.back * 20f
        : Vector3.forward * 20f;

        // 프로펠러 기울기 끄기
        moveFanTilt = false;

        // 기울기 넉백
        fanParent.DOPunchRotation(knockbackRotate, 0.5f, 5)
        .OnComplete(() =>
        {
            // 프로펠러 기울기 켜기
            moveFanTilt = true;
        });

        // Idle 상태로 전환
        character.nowState = Character.State.Idle;
    }

    IEnumerator FanSmash()
    {
        // 몸체 저공으로 내려오기
        body.DOLocalMove(new Vector3(0, 2.5f, 0), smashReadyTime)
        .SetEase(Ease.OutCubic)
        .OnComplete(() =>
        {
            // 머리 및 프로펠러 레이어 1으로
            sortingLayer.sortingOrder = 1;
        });

        // 프로펠러 부모 수평으로
        fanParent.DOLocalRotate(fanParentDefaultRotation, smashReadyTime)
        .SetEase(Ease.OutCubic);

        // 프로펠러들의 각도 유지
        for (int i = 0; i < 4; i++)
        {
            fans[i].DOLocalRotate(fanDefaultRotation, smashReadyTime)
            .SetEase(Ease.OutCubic);
        }

        // // 프로펠러 목표 각도
        // float targetAngle = 0;
        // // 가속도 초기화
        // float accel = 0;
        // // 가속도 방향 여부 +,-
        // bool isFaster = true;

        // 프로펠러 간격 빠르게 벌리기
        float fanPosX = 0;
        float fanPosY = 0;

        // 프로펠러 각각 이펙트 초기화
        for (int i = 0; i < 4; i++)
        {
            int fanIndex = i;

            // 비행 이펙트 전부 끄기
            flyEffects[fanIndex].SmoothDisable();

            // 그룹 레이어 통일
            fans[fanIndex].GetComponent<SortingGroup>().sortingOrder = 1;

            // 해당 날개의 차지 이펙트 활성화
            chargeEffects[fanIndex].gameObject.SetActive(true);

            // 프로펠러 가드 빨갛게 달궈짐
            SpriteRenderer fanSprite = fans[fanIndex].GetComponent<SpriteRenderer>();
            fanSprite.DOColor(fanRageColor, 1f)
            .SetDelay(0.5f);

            wings[fanIndex].DOColor(wingRageColor, 1f)
            .SetDelay(0.5f)
            .OnComplete(() =>
            {
                // 원형 불꽃 파티클 튀는 이펙트 켜기
                sparkEffects[fanIndex].gameObject.SetActive(true);

                // 해당 날개의 차지 이펙트 끄기
                chargeEffects[fanIndex].SmoothDisable();
            });
        }

        // 차지시간 대기
        yield return wait_1;

        for (int i = 0; i < 4; i++)
        {
            // 프로펠러 인덱스에 따라 위치 계산
            if (i == 1 || i == 3)
                fanPosX = smashRange;
            if (i == 0 || i == 2)
                fanPosX = -smashRange;
            if (i == 0 || i == 3)
                fanPosY = smashRange;
            if (i == 1 || i == 2)
                fanPosY = -smashRange;

            // 몸체와 프로펠러 간격 빠르게 벌렸다 좁히기
            fans[i].DOLocalMove(new Vector2(fanPosX, fanPosY), 0.5f)
            .SetDelay(0.5f)
            .SetLoops(2, LoopType.Yoyo);
        }

        // 프로펠러 전체 회전
        fanParent.DOLocalRotate(new Vector3(0, 0, 360f), smashTime, RotateMode.LocalAxisAdd)
        .SetEase(Ease.InOutBack)
        .OnUpdate(() =>
        {
            // 프로펠러들의 각도 유지
            for (int i = 0; i < 4; i++)
            {
                fans[i].rotation = Quaternion.Euler(0, 0, 0);
            }
        });

        yield return new WaitForSeconds(smashTime);

        for (int i = 0; i < 4; i++)
        {
            // 프로펠러 및 날개 색 초기화
            SpriteRenderer fanSprite = fans[i].GetComponent<SpriteRenderer>();
            fanSprite.DOColor(fanDefaultColor, 1f);
            wings[i].DOColor(wingDefaultColor, 1f);

            // 원형 불꽃 파티클 튀는 이펙트 끄기
            sparkEffects[i].SmoothDisable();
        }

        // WaitForSeconds wait_deltaTime = new WaitForSeconds(Time.deltaTime);

        // // 점점 빠르게 회전, 공격 끝나면 점점 느려짐
        // while (true)
        // {
        //     // 가속도 점점 증가 혹은 감소
        //     accel = isFaster ? accel + 0.5f : accel - 0.5f;

        //     // 속도 감소중에, 가속도 1 이하로 내려가면 반복문 중단
        //     if (!isFaster && accel <= 1f)
        //         break;

        //     // 가속도 제한
        //     accel = Mathf.Clamp(accel, 0, 10f);

        //     // 가속도 합산
        //     targetAngle += accel * Time.deltaTime * 80f;

        //     // 360도 넘지않게 보정
        //     if (targetAngle > 360f)
        //         targetAngle -= 360f;
        //     // 마이너스 되지않게 보정
        //     if (targetAngle < 0f)
        //         targetAngle += 360f;

        //     Quaternion fanRotate = Quaternion.Lerp(fanParent.localRotation, Quaternion.Euler(70, 0, targetAngle), Time.deltaTime * 10f);

        //     // 프로펠러들 회전
        //     fanParent.localRotation = fanRotate;

        //     // 프로펠러들의 각도 유지
        //     for (int i = 0; i < 4; i++)
        //     {
        //         fans[i].rotation = Quaternion.Euler(0, 0, 0);
        //     }

        //     yield return wait_deltaTime;
        // }

        // 과부하로 착지해서 휴식
        StartCoroutine(OverloadRest());
    }

    IEnumerator OverloadRest()
    {
        // Rest 상태로 전환
        character.nowState = Character.State.Rest;

        // 몸체 위로 다시 올라가기
        body.DOLocalMove(new Vector3(0, 5.5f, 0), 0.5f)
        .SetEase(Ease.OutCubic);

        // 프로펠러 0도가 되기위해 추가할 각도
        Vector3 addRotation = new Vector3(0, 0, 360 - fanParent.rotation.eulerAngles.z);
        // 프로펠러 회전값 초기화
        fanParent.DOLocalRotate(addRotation, 0.5f, RotateMode.LocalAxisAdd);

        for (int i = 0; i < 4; i++)
        {
            GameObject flyDust = flyEffects[i].gameObject;

            // 프로펠러 원위치
            fans[i].DOLocalMove(fanDefaultPos[i], 0.5f)
            .OnComplete(() =>
            {
                // 비행 먼지 이펙트 켜기
                flyDust.SetActive(true);
            });

            // 프로펠러 회전값 초기화
            fans[i].DORotate(Vector3.zero, 0.5f)
            .OnComplete(() =>
            {
                // 팬 수평유지 스위치 켜기
                fanHorizon = true;
            });

            // 앞,뒤 프로펠러 그룹 레이어 구분
            if (i == 0 || i == 3)
                fans[i].GetComponent<SortingGroup>().sortingOrder = 0;
            else
                fans[i].GetComponent<SortingGroup>().sortingOrder = 1;
        }

        // 원위치 시간 대기
        yield return new WaitForSeconds(0.5f);

        // 메인 애니메이터 켜기
        character.animList[0].enabled = true;

        // 보스 그 자리에서 과부하로 착지해서 휴식 애니메이션
        character.animList[0].SetBool("isRest", true);

        // 모든 레이어 0으로 내리기
        sortingLayer.sortingOrder = 0;

        // 휴식 이펙트 전부 켜기
        for (int i = 0; i < restEffects.Count; i++)
        {
            restEffects[i].SetActive(true);
        }

        for (int i = 0; i < 4; i++)
        {
            // 프로펠러 애니메이터 전부 끄기
            character.animList[i + 1].enabled = false;

            // 비행 파티클 전부 끄기
            flyEffects[i].SmoothDisable();
        }

        // 눈알 위치 오작동
        Sequence eyeSeq = DOTween.Sequence();
        eyeSeq
        .SetDelay(1f)
        .Append(
            eye.DOShakePosition(0.4f, 0.05f, 10, 90f, false, true)
        )
        .AppendInterval(Random.Range(0, 0.6f))
        .Append(
            eye.DOShakePosition(0.4f, 0.05f, 10, 90f, false, true)
        )
        .AppendInterval(Random.Range(0, 0.6f))
        .Append(
            eye.DOShakePosition(0.4f, 0.05f, 10, 90f, false, true)
        )
        .AppendInterval(Random.Range(0, 0.6f))
        .Append(
            eye.DOShakePosition(0.4f, 0.05f, 10, 90f, false, true)
        )
        .AppendInterval(Random.Range(0, 0.6f));

        // 1번 프로펠러 주기적으로 돌리기
        Sequence wingSeq = DOTween.Sequence();
        wingSeq.Append(
            wings[1].transform.DOLocalRotate(Vector3.forward * 90, 0.5f, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutCubic)
        )
        .SetDelay(1f)
        .SetLoops(3);

        // 3번 프로펠러 계속 돌리기
        wings[3].transform.DOLocalRotate(Vector3.forward * 90 * 5, 5f, RotateMode.LocalAxisAdd)
        .SetEase(Ease.InSine);

        // 휴식 시간 대기
        yield return new WaitForSeconds(5f);

        // 휴식 애니메이션 종료
        character.animList[0].SetBool("isRest", false);

        // 휴식 이펙트 전부 끄기
        for (int i = 0; i < restEffects.Count; i++)
        {
            restEffects[i].GetComponent<ParticleManager>().SmoothDisable();
        }

        // 프로펠러 애니메이터 전부 켜기
        for (int i = 1; i < 5; i++)
        {
            Animator fanAnim = character.animList[i];

            // 프로펠러 점점 빠르게 원래 속도까지 돌리기
            fanAnim.transform.DOLocalRotate(Vector3.forward * 360, 0.5f, RotateMode.LocalAxisAdd)
            .SetEase(Ease.InSine)
            .OnComplete(() =>
            {
                //끝나면 애니메이터 켜기
                fanAnim.enabled = true;
            });
        }

        // idle 복귀 시간 대기
        yield return wait_1;

        // 전체 레이어 1로 올리기
        sortingLayer.sortingOrder = 1;

        // 메인 애니메이터 끄기
        character.animList[0].enabled = false;

        // Idle 상태로 전환
        character.nowState = Character.State.Idle;
    }
}
