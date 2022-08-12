using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class Quad_AI : MonoBehaviour
{
    [Header("State")]
    [SerializeField]
    Patten patten = Patten.None;
    enum Patten { PushSide, PushCircle, BladeShot, FanSwing, None };
    private float atkCoolCount;
    public float pushSideCoolTime;
    public float pushCircleCoolTime;
    public float bladeShotCoolTime;
    public float fanSwingCoolTime;
    public float atkRange = 30f; // 공격 범위
    public float fanSensitive = 0.1f; //프로펠러 기울기 감도 조절

    [Header("Walk")]
    float targetSearchCount; // 타겟 위치 추적 시간 카운트
    [SerializeField]
    float targetSearchTime = 3f; // 타겟 위치 추적 시간
    Vector3[] fanDefaultPos = new Vector3[4]; // 프로펠러 원위치 리스트

    [Header("Refer")]
    public TextMeshProUGUI stateText; //! 테스트 현재 상태
    public EnemyManager enemyManager;
    public Transform body;
    public Transform fanParent;
    public Transform eye;
    public Color fanColorDefault = new Color(30f / 255f, 1f, 1f, 10f / 255f);
    public Color fanRageColor = new Color(1f, 30f / 255f, 30f / 255f, 1f);
    Transform[] fans = new Transform[4]; // 프로펠러 리스트
    SpriteRenderer[] wings = new SpriteRenderer[4]; // 프로펠러 날개 리스트
    ParticleManager[] flyEffects = new ParticleManager[4]; // 비행 파티클 리스트
    ParticleManager[] pushEffects = new ParticleManager[4]; // 밀어내기 이펙트 리스트
    public List<GameObject> restEffects = new List<GameObject>();
    SortingGroup sortingLayer; //전체 레이어

    [Header("PushSide")]
    public GameObject wallBox; // 플레이어 가두는 상자 프리팹
    public GameObject bladePrefab; // 프로펠러 톱날 프리팹

    [Header("PushCircle")]
    public GameObject windCutterPrefab; // 윈드커터 마법 프리팹
    MagicInfo windMagic;
    public ParticleSystem wallCircle; // 플레이어 가두는 원형 프리팹
    [SerializeField]
    float spinSpeed = 1f; // 프로펠러 공전 속도
    [SerializeField]
    float windShotNum = 50f; // 윈드커터 발사 횟수
    [SerializeField]
    float windShotDelay = 0.1f; // 윈드커터 발사 간격

    [Header("BladeShot")]
    public LineRenderer eyeLaser;
    public SpriteRenderer targetMarker;
    public float aimTime = 3f;

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
        }

        // 전체 레이어 그룹 찾기
        sortingLayer = GetComponent<SortingGroup>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        // 휴식 이펙트 전부 끄기
        for (int i = 0; i < restEffects.Count; i++)
        {
            restEffects[i].SetActive(false);
        }

        //애니메이션 스피드 초기화
        if (enemyManager.animList != null)
        {
            foreach (Animator anim in enemyManager.animList)
            {
                anim.speed = 1f;
            }
        }

        //속도 초기화
        enemyManager.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

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
        if (enemyManager.enemy == null)
            return;

        // 휴식 중에 리턴
        if (enemyManager.nowAction == EnemyManager.Action.Rest)
            return;

        // 타겟 추적 쿨타임 차감
        if (targetSearchCount > 0)
            targetSearchCount -= Time.deltaTime;
        // 쿨타임 됬을때
        else
        {
            // 플레이어 추정 위치 계산
            enemyManager.targetPos = PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle * 2f;

            // 추적 쿨타임 갱신
            targetSearchCount = targetSearchTime;
        }

        // 추적 위치 벡터를 서서히 이동
        enemyManager.movePos = Vector3.Lerp(enemyManager.movePos, enemyManager.targetPos, Time.deltaTime * 10f);

        // 플레이어 방향
        enemyManager.targetDir = enemyManager.movePos - body.position;

        // 플레이어 방향 각도
        float playerAngle = Mathf.Atan2(enemyManager.targetDir.y, enemyManager.targetDir.x) * Mathf.Rad2Deg;

        // 눈동자 플레이어 방향으로 이동
        eye.position = body.position + enemyManager.targetDir.normalized * 1f;

        // 눈동자 플레이어 방향으로 회전
        eye.rotation = Quaternion.Euler(0, 0, playerAngle + 135f);

        // Idle 상태 아니면 리턴
        if (enemyManager.nowAction != EnemyManager.Action.Idle)
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

        // Idle 아니면 리턴
        if (enemyManager.nowAction != EnemyManager.Action.Idle)
            return;

        // 공격 쿨타임 차감
        if (atkCoolCount > 0)
            atkCoolCount -= Time.deltaTime;

        // 플레이어 방향
        Vector2 dir = enemyManager.movePos - transform.position;

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
                enemyManager.rigid.velocity = Vector3.zero;

                //공격 패턴 결정하기
                ChooseAttack();

                return;
            }
        }
        else
            // 플레이어 따라가기
            Walk();
    }

    void ChooseAttack()
    {
        // 현재 액션 변경
        enemyManager.nowAction = EnemyManager.Action.Attack;

        // 애니메이터 끄기
        enemyManager.animList[0].enabled = false;

        // 랜덤 패턴 결정
        int randomNum = Random.Range(0, 5);

        // print("randomNum : " + randomNum);

        //! 테스트를 위해 패턴 고정
        if (patten != Patten.None)
            randomNum = (int)patten;

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
                StartCoroutine(BladeShot());
                //쿨타임 갱신
                atkCoolCount = bladeShotCoolTime;
                break;
        }
    }

    void Walk()
    {
        enemyManager.nowAction = EnemyManager.Action.Walk;

        //애니메이터 켜기
        enemyManager.animList[0].enabled = true;
        // Idle 애니메이션으로 전환
        // enemyManager.animList[0].SetBool("UseFist", false);

        //! 거리 확인
        stateText.text = "Distance : " + enemyManager.targetDir.magnitude;

        // 기울임 각도 계산
        float angleZ = -Mathf.Clamp(enemyManager.targetDir.x / 1.5f, -15f, 15f);
        Quaternion rotation = Quaternion.Lerp(fanParent.rotation, Quaternion.Euler(0, 0, angleZ), fanSensitive);

        // 프로펠러 기울이기
        fanParent.rotation = rotation;

        //해당 방향으로 가속
        enemyManager.rigid.velocity = enemyManager.targetDir.normalized * enemyManager.speed * SystemManager.Instance.globalTimeScale;

        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    private void OnDrawGizmosSelected()
    {
        // 보스부터 이동 위치까지 직선
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, enemyManager.movePos);

        // 이동 위치 기즈모
        Gizmos.DrawIcon(enemyManager.movePos, "Circle.png", true, new Color(1, 0, 0, 0.5f));

        // 추적 위치 기즈모
        Gizmos.DrawIcon(enemyManager.targetPos, "Circle.png", true, new Color(0, 0, 1, 0.5f));

        // 추적 위치부터 이동 위치까지 직선
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(enemyManager.targetPos, enemyManager.movePos);
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
        fanParent.DORotate(Vector3.forward, 2f);

        yield return new WaitForSeconds(2f);

        // 몸체 저공으로 내려오기
        body.DOLocalMove(new Vector3(0, 2.5f, 0), 1f)
        .SetEase(Ease.OutCubic)
        .OnComplete(() =>
        {
            // 몸체 및 프로펠러 레이어 0으로 내리기
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
            // 몸체 및 프로펠러 레이어 1로 올리기
            sortingLayer.sortingOrder = 1;
        });

        // 몸체 올라간 만큼 프로펠러들 낮추기
        fanParent.DOLocalMove(new Vector3(0, -3f, 0), 1f);

        yield return new WaitForSeconds(1f);

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
        wings[fanIndex].DOColor(fanRageColor, 1f);
        yield return new WaitForSeconds(1f);

        // 프로펠러 날개 오브젝트 끄기
        wings[fanIndex].gameObject.SetActive(false);
        // 프로펠러 색깔 초기화
        wings[fanIndex].color = fanColorDefault;

        // 발사 반동으로 떨림
        fans[fanIndex].transform.DOPunchPosition(-innerDir, 0.4f);

        // 블레이드 생성
        GameObject shotBlade = LeanPool.Spawn(bladePrefab, wings[fanIndex].transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

        // 블레이드에 몬스터 매니저 넣기
        shotBlade.GetComponent<EnemyAttack>().enemyManager = enemyManager;

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
        yield return new WaitForSeconds(1f);

        // 발사 방향 반대로 블레이드 회전
        shotBlade.transform.rotation = isLeftSide ? Quaternion.Euler(0, 180f, 0) : Quaternion.Euler(0, 0, 0);
        // 복귀 시간 대기
        yield return new WaitForSeconds(1f);

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
        fanParent.DORotate(Vector3.forward, 2f);

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
            // 몸체 및 프로펠러 레이어 0으로 내리기
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

            Vector2 fanPos = Vector2.zero;

            if (i == 0 || i == 2)
                fanPos.x = -3;
            if (i == 1 || i == 3)
                fanPos.x = 3;
            if (i == 0 || i == 1)
                fanPos.y = 3;
            if (i == 2 || i == 3)
                fanPos.y = -3;

            fans[i].DOLocalMove(fanPos, 0.2f);

            // 몸체보다 왼쪽에 있을때
            if (fans[i].position.x < body.position.x)
                fans[i].DORotate(Vector3.forward * 90f, 0.2f);
            // 몸체보다 오른쪽에 있을때
            else
                fans[i].DORotate(Vector3.forward * -90f, 0.2f);

            // 그룹 레이어 통일
            fans[i].GetComponent<SortingGroup>().sortingOrder = 0;
        }

        yield return new WaitForSeconds(0.2f);

        // 점점 빠르게 반 바퀴만 돌리기
        fanParent.DORotate(Vector3.forward * 180f, 0.5f / spinSpeed, RotateMode.WorldAxisAdd)
        .OnUpdate(() =>
        {
            // 팬들의 각도가 항상 바깥쪽 바라보기
            for (int i = 0; i < 4; i++)
            {
                // 몸체보다 왼쪽에 있을때
                if (fans[i].position.x < body.position.x)
                    fans[i].rotation = Quaternion.Euler(0, 0, 90);
                // 몸체보다 오른쪽에 있을때
                else
                    fans[i].rotation = Quaternion.Euler(0, 0, -90);
            }
        })
        .SetEase(Ease.InSine);

        // 반 바퀴 돌리는 동안 대기
        yield return new WaitForSeconds(0.5f / spinSpeed);

        // 팬을 빠르게 공전시키기
        fanParent.DORotate(Vector3.forward * 360f, 1f / spinSpeed, RotateMode.WorldAxisAdd)
        .OnUpdate(() =>
        {
            // 팬들의 각도가 항상 바깥쪽 바라보기
            for (int i = 0; i < 4; i++)
            {
                // 몸체보다 왼쪽에 있을때
                if (fans[i].position.x < body.position.x)
                    fans[i].rotation = Quaternion.Euler(0, 0, 90);
                // 몸체보다 오른쪽에 있을때
                else
                    fans[i].rotation = Quaternion.Euler(0, 0, -90);
            }
        })
        .SetEase(Ease.Linear)
        .SetLoops(-1);

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

                // 몸체에서 프로펠러까지 방향
                Vector2 fanDir = shotterFan.position - body.transform.position;
                // 프로펠러 전방 목표 지점
                Vector2 targetPos = (Vector2)shotterFan.position + fanDir.normalized;

                // 윈드커터 생성
                GameObject magicObj = LeanPool.Spawn(windCutterPrefab, shotterFan.transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

                // 매직홀더 찾기
                MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();

                // magic 데이터 넣기
                magicHolder.magic = windMagic;

                // 타겟을 플레이어로 전환
                magicHolder.SetTarget(MagicHolder.Target.Player);

                // 프로펠러 전방을 목표지점으로 설정
                magicHolder.targetPos = targetPos;

                // 타겟 오브젝트 넣기
                magicHolder.targetObj = PlayerManager.Instance.gameObject;
            }

            // 발사 지연시간
            yield return new WaitForSeconds(windShotDelay);
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

        yield return new WaitForSeconds(1f);

        // 원형 벽 비활성화
        wallCircle.gameObject.SetActive(false);

        // 팬 공전값 초기화
        fanParent.DORotate(Vector3.zero, 1f, RotateMode.FastBeyond360)
        .SetEase(Ease.OutCirc);

        // 몸체 원위치
        body.DOLocalMove(new Vector3(0, 5.5f, 0), 1f)
        .SetEase(Ease.OutCubic);

        // 과부하로 착지해서 휴식
        StartCoroutine(OverloadRest());
    }

    IEnumerator BladeShot()
    {
        // 발사할 날개 선정
        SpriteRenderer wing = wings[Random.Range(0, 4)];

        //todo 날개 빠르게 돌리며 빨갛게 달아오름
        wing.DOColor(fanRageColor, aimTime);

        //todo 날개에서 연기 피어오르는 이펙트 켜기

        // 조준 시간 초기화
        float aimCount = aimTime;

        // 조준 하는동안 플레이어 위쪽에서 계속 추적
        while (aimCount > 0)
        {
            // 플레이어 위쪽 목표 위치
            Vector3 targetPos = PlayerManager.Instance.transform.position + Vector3.up * 8f;

            // 목표 위치로 이동
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);

            // 플레이어 방향
            Vector2 playerDir = enemyManager.movePos - transform.position;
            // 플레이어 방향 각도
            float angle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg + 90f;
            // 기울임 각도 계산
            Quaternion rotation = Quaternion.Lerp(fanParent.rotation, Quaternion.Euler(0, 0, angle), fanSensitive);

            // 드론 아래쪽이 플레이어를 바라보게 기울이기
            fanParent.rotation = rotation;

            // 추적하는동안 눈에서 레이저로 목표지점 표시
            // 레이저 및 마커 활성화
            eyeLaser.enabled = true;
            targetMarker.enabled = true;

            // 레이저 포인트 설정
            eyeLaser.SetPosition(0, eyeLaser.transform.position);
            eyeLaser.SetPosition(1, enemyManager.movePos);
            // 타겟 위치에 마커 옮기기
            targetMarker.transform.position = enemyManager.movePos;

            // 조준 시간 차감
            aimCount -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 레이저 및 마커 비활성화
        eyeLaser.enabled = false;
        targetMarker.enabled = false;

        // 발사할 프로펠러 진동
        wing.transform.parent.DOShakePosition(0.2f, 0.05f, 10, 90f, false, false);

        //todo 날개에서 연기 피어오르는 이펙트 끄기
        //todo 프로펠러에서 불씨 파티클 튀기기

        //todo 플레이어 위치에 톱니 발사
        // 블레이드 생성
        GameObject shotBlade = LeanPool.Spawn(bladePrefab, wing.transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

        // 블레이드에 몬스터 매니저 넣기
        shotBlade.GetComponent<EnemyAttack>().enemyManager = enemyManager;

        // 블레이드 스프라이트 켜기
        SpriteRenderer bladeSprite = shotBlade.GetComponentInChildren<SpriteRenderer>();
        bladeSprite.enabled = true;

        // 발사 방향에 따라 발사체 회전
        shotBlade.transform.rotation
        = enemyManager.movePos.x > transform.position.x
        ? Quaternion.Euler(0, 0, 0)
        : Quaternion.Euler(0, 180f, 0);

        //사이즈 제로에서 키우기
        shotBlade.transform.localScale = Vector3.zero;
        shotBlade.transform.DOScale(Vector3.one, 0.2f);

        // 플레이어 쪽으로 블레이드 발사
        shotBlade.transform.DOMove(enemyManager.movePos, 0.3f);

        //todo 톱니에 잔상 효과
        //todo 날아간 톱니는 목표 위치 바닥에 박힘, 마스크로 아래 절반 가리기
        //todo 박힐때 흙 튀는 파티클
        //todo 톱니와 바닥 맞닿는 부분에 생성되어 고정된 흙 무더기 파티클, 뽑힐때 사라짐

        //todo 쏘는 반대 방향으로 보스 몸 전체 넉백 이동
        Vector3 shotDir = enemyManager.movePos - transform.position;
        transform.DOMove(transform.position - shotDir.normalized, 0.5f)
        .SetEase(Ease.OutCirc);

        //todo 쏘는 프로펠러에 따라 fanParent 기울어졌다 돌아오는 넉백

        //todo 몸 전체 진동
        //todo 땅에 박힌 프로펠러 진동

        //todo 블레이드 땅에서 뽑히며 프로펠러로 날아와서 디스폰
        //todo 뽑힐때 흙 튀는 파티클

        //todo 블레이드 장착되며 wing 스프라이트 켜기
        //todo 불꽃 파티클 튀기기
        //todo 보스 몸체 넉백으로 날아온 방향대로 넉백, 기울이기 넉백

        //todo 플레이어는 피격시 출혈 데미지 및 넉백

        //todo 과부하 휴식
        // StartCoroutine(OverloadRest());
    }

    IEnumerator OverloadRest()
    {
        // Rest 상태로 전환
        enemyManager.nowAction = EnemyManager.Action.Rest;

        for (int i = 0; i < 4; i++)
        {
            GameObject flyDust = flyEffects[i].gameObject;

            // 프로펠러 원위치
            fans[i].DOLocalMove(fanDefaultPos[i], 1f)
            .OnComplete(() =>
            {
                // 비행 먼지 이펙트 켜기
                flyDust.SetActive(true);
            });

            // 프로펠러 회전값 초기화
            fans[i].DORotate(Vector3.zero, 1f);

            // 앞,뒤 프로펠러 그룹 레이어 구분
            if (i == 0 || i == 3)
                fans[i].GetComponent<SortingGroup>().sortingOrder = 0;
            else
                fans[i].GetComponent<SortingGroup>().sortingOrder = 1;
        }

        // 원위치 시간 대기
        yield return new WaitForSeconds(1f);

        // 메인 애니메이터 켜기
        enemyManager.animList[0].enabled = true;

        // 보스 그 자리에서 과부하로 착지해서 휴식 애니메이션
        enemyManager.animList[0].SetBool("isRest", true);

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
            enemyManager.animList[i + 1].enabled = false;

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
        enemyManager.animList[0].SetBool("isRest", false);

        // 휴식 이펙트 전부 끄기
        for (int i = 0; i < restEffects.Count; i++)
        {
            restEffects[i].GetComponent<ParticleManager>().SmoothDisable();
        }

        // 프로펠러 애니메이터 전부 켜기
        for (int i = 1; i < 5; i++)
        {
            Animator fanAnim = enemyManager.animList[i];

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
        yield return new WaitForSeconds(1f);

        // 전체 레이어 1로 올리기
        sortingLayer.sortingOrder = 1;

        // 메인 애니메이터 끄기
        enemyManager.animList[0].enabled = false;

        // Idle 상태로 전환
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }
}
