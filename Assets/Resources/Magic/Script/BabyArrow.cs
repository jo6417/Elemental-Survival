using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;
using System.Linq;
using UnityEngine.Experimental.Rendering.Universal;
using PathCreation;
using PathCreation.Examples;

public class BabyArrow : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private ArrowState currentState = ArrowState.Idle; //화살의 상태
    private enum ArrowState { Idle, Attack };
    [SerializeField, ReadOnly] private bool nowIsQuick; // 현재 진행중인 마법이 퀵슬롯 마법인지 여부
    [SerializeField, ReadOnly] private bool init = false;
    [SerializeField, ReadOnly] private float speed;
    [SerializeField] int currentTargetIndex;
    private MagicInfo globalMagic;
    private MagicInfo quickMagic;
    [SerializeField] private float returnSpeed = 3f;

    [Header("Refer")]
    [SerializeField] private MagicHolder magicHolder;
    [SerializeField] private List<Transform> markerList = new List<Transform>();
    [SerializeField] private List<Character> targetList = new List<Character>();
    [SerializeField] private GameObject markerPrefab; //공격 위치 표시 마커
    [SerializeField] private ParticleSystem rangeBorderPrefab; // 범위 이펙트 프리팹

    [Header("Orbit")]
    [SerializeField] private Transform orbitObj; // 공전 오브젝트
    [SerializeField] private float orbitAngle; // 현재 공전 각도
    [SerializeField] private float orbitSpeed = 10f;

    [Header("BezierPath")]
    [SerializeField] private PathCreator pathCreator; // 베지어 곡선 생성기 프리팹
    //  private PathCreator pathCreator; // 베지어 곡선 생성기
    [SerializeField] private float distanceTravelled; // 현재 이동중인 베지어 곡선의 위치
    [SerializeField] private EndOfPathInstruction endOfPathInstruction;

    private void Awake()
    {
        // 액티브 사용시 함수를 콜백에 넣기
        magicHolder.magicCastCallback = QuickShot;

        // 베지어 곡선 생성기 없으면 새로 생성
        if (ObjectPool.Instance.magicPool.Find("pathCreator") == null)
        {
            pathCreator = new GameObject("pathCreator").AddComponent<PathCreator>();
            pathCreator.transform.SetParent(ObjectPool.Instance.magicPool);
            pathCreator.transform.position = Vector2.zero;
        }

        // Path가 바뀔때 경로 수정 콜백 함수 추가
        if (pathCreator != null)
            pathCreator.pathUpdated += OnPathChanged;
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        init = false;

        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);
        speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, true);

        // 쿨타임 체크용 글로벌 마법 정보 찾기
        globalMagic = MagicDB.Instance.GetMagicByID(magicHolder.magic.id);
        quickMagic = MagicDB.Instance.GetQuickMagicByID(magicHolder.magic.id);

        // 플레이어 밑에 공전 오브젝트 없으면 새로 생성
        if (PlayerManager.Instance.transform.Find("OrbitObject") == null)
        {
            GameObject newOrbitObj = new GameObject("OrbitObject");
            newOrbitObj.transform.SetParent(PlayerManager.Instance.transform);
            orbitObj = newOrbitObj.transform;
        }

        init = true;
    }

    private void OnDisable()
    {
        // 타겟 초기화
        Reset();

        // 공전 오브젝트 없에기
        if (orbitObj)
            LeanPool.Despawn(orbitObj);
    }

    private void FixedUpdate()
    {
        if (!init)
            return;

        if (currentState == ArrowState.Idle)
        {
            // 콜라이더 끄기
            magicHolder.atkColl.enabled = false;

            // 공전
            transform.position = OrbitAround(PlayerManager.Instance.transform, 3f, speed);
            // 공전 각도 증가
            orbitAngle += speed * orbitSpeed * Time.deltaTime;

            // 자동 공격일때, 쿨타임 제로일때
            if (!magicHolder.isQuickCast && globalMagic.coolCount <= 0)
                // 자동 공격 실행
                AutoShot();
        }
    }

    void AutoShot()
    {
        // 플레이어 위치 넣기
        magicHolder.targetPos = PlayerManager.Instance.transform.position;

        // 공격
        StartCoroutine(Attack());

        // 퀵 여부 초기화
        nowIsQuick = false;
    }

    void QuickShot()
    {
        // 공격 중이면 리턴
        if (currentState == ArrowState.Attack)
            return;

        // 쿨타임 남아있으면 리턴
        if (quickMagic.coolCount > 0)
            return;

        // 마우스 위치 넣기
        magicHolder.targetPos = PlayerManager.Instance.GetMousePos();

        StartCoroutine(Attack());

        // 퀵 여부 초기화
        nowIsQuick = true;
    }

    // 공격 모드
    private IEnumerator Attack()
    {
        // 타겟 리스트 비우기
        targetList.Clear();
        // 마커 리스트 비우기
        markerList.Clear();
        // 현재 인덱스 초기화
        currentTargetIndex = 0;

        // 타겟 위치 근처의 적들 찾기
        targetList = CastMagic.Instance.MarkEnemies(magicHolder.magic, magicHolder.targetPos);

        // 타겟이 없을때
        if (targetList.Count == 0)
        {
            // 초기화
            Reset();

            // 쿨다운 시작
            if (CastMagic.Instance != null)
                CastMagic.Instance.Cooldown(magicHolder.magic, nowIsQuick);

            yield break;
        }

        // 범위만큼 테두리 이펙트 재생
        ParticleSystem rangeEffect = LeanPool.Spawn(rangeBorderPrefab, magicHolder.targetPos, Quaternion.identity, ObjectPool.Instance.magicPool);
        ParticleSystem.ShapeModule rangeShape = rangeEffect.shape;
        // 이펙트에 범위 반영
        rangeShape.radius = magicHolder.range;
        rangeEffect.Play();

        // 현재 화살 위치에 마커 심기
        GameObject marker = LeanPool.Spawn(markerPrefab, transform.position, Quaternion.identity, ObjectPool.Instance.magicPool);
        // 마커 스프라이트 끄기
        marker.GetComponent<SpriteRenderer>().enabled = false;
        // 마커 리스트에 마커 저장
        markerList.Add(marker.transform);

        // 타겟에 모두 마커 심기
        foreach (Character target in targetList)
        {
            // 타겟이 있으면
            if (target)
            {
                // 해당 몬스터 위치에 마커 심기
                marker = LeanPool.Spawn(markerPrefab, target.transform.position, Quaternion.identity, target.transform);
                // 마커 스프라이트 켜기
                marker.GetComponent<SpriteRenderer>().enabled = true;

                // 심은 마커 리스트에 저장
                markerList.Add(marker.transform);
            }
        }

        // 공전 오브젝트 넣기
        markerList.Add(orbitObj);

        // 공격 시작
        currentState = ArrowState.Attack;
        // 공격 콜라이더 켜기
        magicHolder.atkColl.enabled = true;

        // 베지어 곡선으로 타겟 전부 순회
        StartCoroutine(MoveToTargets(markerList));

        yield return null;
    }

    private IEnumerator MoveToTargets(List<Transform> targetList)
    {
        // 인덱스 초기화
        currentTargetIndex = 0;

        // 공격중이면 반복
        while (currentState == ArrowState.Attack)
        {
            // 베지어 루트 재설정
            if (targetList.Count > 0)
            {
                // 베지어 곡선 만들기
                BezierPath bezierPath = new BezierPath(targetList, false, PathSpace.xy);
                pathCreator.bezierPath = bezierPath;
            }

            // 루트 따라서 화살 이동
            if (pathCreator != null)
            {
                // path 상에서 증가할 위치값 계산
                float speedIncrease = speed * Time.deltaTime;
                // 복귀할때는 속도 추가 상승
                if (currentTargetIndex + 1 == markerList.Count)
                    speedIncrease *= 2f;

                // path 상에서 화살의 위치값 증가
                distanceTravelled += speedIncrease;

                // 화살 이동
                transform.position = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);

                // 화살 각도
                Quaternion rotation = pathCreator.path.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);
                transform.rotation = rotation * Quaternion.Euler(0, 90f, 90f);

                // (화살과 가장 가까운 경로의 비율 > 현재 index의 정점과 가장 가까운 경로의 비율) 일때 해당 정점 넘어간 것으로 판단
                if (currentTargetIndex < markerList.Count
                && pathCreator.path.GetClosestTimeOnPath(transform.position) > pathCreator.path.GetClosestTimeOnPath(markerList[currentTargetIndex].position))
                {
                    // 마커 스프라이트 끄기
                    if (markerList[currentTargetIndex] != orbitObj)
                        markerList[currentTargetIndex].GetComponent<SpriteRenderer>().enabled = false;

                    // 마커 인덱스 증가
                    currentTargetIndex++;
                }

                // 끝까지 도달하면 Idle 상태로 전환
                if (distanceTravelled >= pathCreator.path.length)
                {
                    currentState = ArrowState.Idle;

                    break;
                }
            }

            // 모든 마커 상태 검사
            foreach (Transform marker in markerList)
            {
                // 마커가 꺼졌을때
                if (marker.gameObject.activeInHierarchy)
                {
                    Character markedCharacter = marker.GetComponentInParent<Character>();
                    // 마커를 지닌 몬스터가 죽었을때
                    if (markedCharacter != null && markedCharacter.isDead)
                    {
                        // 마커의 부모 바꾸기, 월드 위치 그대로 유지
                        marker.SetParent(ObjectPool.Instance.magicPool, true);
                    }
                }
            }

            yield return new WaitForEndOfFrame();
        }

        // 마커, 타겟 모두 리셋
        Reset();

        // 쿨다운 시작
        if (CastMagic.Instance != null)
            CastMagic.Instance.Cooldown(magicHolder.magic, nowIsQuick);
    }

    void OnPathChanged()
    {
        distanceTravelled = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
        // print("OnPathChanged");
    }

    void Reset()
    {
        // 모든 마커 삭제
        for (int i = 0; i < markerList.Count; i++)
            // 공전 오브젝트 아닐때
            if (markerList[i] != orbitObj)
                LeanPool.Despawn(markerList[i]);

        // 마커 리스트 비우기
        markerList.Clear();
        // 타겟 리스트 비우기
        targetList.Clear();
        // 현재 인덱스 초기화
        currentTargetIndex = 0;
        // 경로 위치 초기화
        distanceTravelled = 0;
    }

    private Vector2 OrbitAround(Transform center, float radius, float orbitSpeed)
    {
        //오브젝트를 공전시킬 위치
        Vector3 orbitPosition = center.position + (Quaternion.Euler(0f, 0f, orbitAngle) * Vector3.right * radius);

        // 공전 방향으로 회전
        transform.rotation = Quaternion.Euler(0f, 0f, orbitAngle);

        // 공전 오브젝트 위치 이동
        orbitObj.position = orbitPosition;

        // 공전 위치를 리턴
        return orbitPosition;
    }
}
