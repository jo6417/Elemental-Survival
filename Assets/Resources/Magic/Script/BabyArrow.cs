using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;
using System.Linq;
using UnityEngine.Experimental.Rendering.Universal;

public class BabyArrow : MonoBehaviour
{
    public MagicHolder magicHolder;

    public enum State { Idle, Attack, Ready };
    public State state = State.Idle; //화살의 상태

    [Header("Refer")]
    public Light2D tailLight;
    public GameObject atkMark; //공격 위치 표시 마커
    public GameObject bloodParticle; // 타격시 혈흔 프리팹
    [SerializeField] List<Character> targetList = new List<Character>();
    Vector3 arrowLastPos;
    Rigidbody2D rigidArrow;
    Collider2D coll;
    SpriteRenderer sprite;
    Vector3 slowFollowPlayer;
    Vector3 spinOffset;
    GameObject spinObj = null;
    TrailRenderer tail; //화살 꼬리

    [Header("State")]
    [SerializeField] float coolCount; // 쿨타임 카운트
    [SerializeField] float speed;
    [SerializeField, ReadOnly] float aimTime = -1f;

    private void Awake()
    {
        tail = GetComponentInChildren<TrailRenderer>();
        rigidArrow = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        magicHolder = GetComponent<MagicHolder>();
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // magic 정보 들어올때까지 대기
        yield return new WaitUntil(() => magicHolder.initDone);

        speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, true);

        //플레이어 주변을 도는 마커
        spinObj = LeanPool.Spawn(atkMark, transform.position, Quaternion.identity);

        //플레이어와의 거리 보정
        spinObj.transform.position = slowFollowPlayer + Vector3.up * 3;
        spinOffset = spinObj.transform.position - slowFollowPlayer;

        // 밝기 초기화
        tailLight.intensity = 0;

        // 쿨타임 갱신
        coolCount = magicHolder.coolTime;
    }

    void Update()
    {
        if (!magicHolder.initDone)
            return;

        // 준비 상태일때
        if (state == State.Idle)
        {
            // 콜라이더 끄기
            coll.enabled = false;

            //플레이어 주위 공전
            SpinPosition();

            // spinObj 를 따라가기
            transform.position = Vector3.Lerp(transform.position, spinObj.transform.position, Time.deltaTime * 5);

            // 따라가는 방향으로 회전
            Vector3 returnDir = spinObj.transform.position - transform.position;
            float rotation = Mathf.Atan2(returnDir.y, returnDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(Vector3.forward * (rotation - 90f));

            if (coolCount > 0)
                // 쿨타임 차감
                coolCount -= Time.deltaTime;
            else
            {
                // 쿨타임 되면 타겟 찾기
                SetTargets();
            }
        }

        // 공격하려고 날아가는 중일때
        if (state == State.Attack)
        {
            // 공격 콜라이더 켜기
            coll.enabled = true;

            // 타겟이 null이거나, 비활성화 됬거나, 고스트일때
            if (targetList[0] == null || !targetList[0].gameObject.activeSelf || targetList[0].IsGhost)
            {
                // 적을 놓쳤을때
                MissingTarget();
                return;
            }

            //다음 타겟 방향 계산
            Vector2 attackDir = targetList[0].transform.position - transform.position;
            float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg - 90f;

            // 타겟 방향 바라보기
            transform.rotation = Quaternion.Euler(0, 0, angle);

            float moveSpeed = Mathf.Clamp(attackDir.magnitude, 1f, attackDir.magnitude) * speed;

            // 타겟 방향 velocity로 이동
            rigidArrow.velocity = attackDir.normalized * moveSpeed;
        }

        // 다음 타겟 준비 상태일때
        if (state == State.Ready)
        {
            // 공격 콜라이더 끄기
            coll.enabled = false;

            if (coolCount > 0)
                // 쿨타임 차감
                coolCount -= Time.deltaTime;
            else
            {
                // 타겟이 남아있지 않을때
                if (targetList.Count == 0)
                {
                    // 쿨타임 갱신
                    coolCount = magicHolder.coolTime;

                    // light 밝기 낮추기
                    DOTween.To(() => tailLight.intensity, x => tailLight.intensity = x, 0f, aimTime);

                    // idle 상태로 바꾸고 리턴
                    state = State.Idle;

                    return;
                }

                // light 밝기 올리기
                DOTween.To(() => tailLight.intensity, x => tailLight.intensity = x, 5f, 0.2f);

                // 공격 시작
                state = State.Attack;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 공격중일때, 0번 타겟과 충돌시
        if (targetList.Count > 0 && other.TryGetComponent(out HitBox enemyHitBox))
        {
            if (enemyHitBox.character == targetList[0])
            {
                // 타겟 삭제 및 다음 타겟 공격 준비
                HitTarget();
            }
        }
    }

    //화살 발사
    void SetTargets()
    {
        // 범위내 모든 적 리스트 뽑기
        targetList = CastMagic.Instance.MarkEnemies(magicHolder.magic);

        // 리스트에 등록된 타겟이 있는데, null이거나, 비활성화 됬거나, 고스트일때
        while (targetList.Count > 0 && (targetList[0] == null || !targetList[0].gameObject.activeSelf || targetList[0].IsGhost))
            // 0번 타겟 삭제
            targetList.RemoveAt(0);

        // 남은 타겟이 있을때
        if (targetList.Count > 0)
            // 공격 준비 상태로 전환
            state = State.Ready;
    }

    void MissingTarget()
    {
        // 리스트에 등록된 타겟이 있는데, null이거나, 비활성화 됬거나, 고스트일때
        while (targetList.Count > 0 && (targetList[0] == null || !targetList[0].gameObject.activeSelf || targetList[0].IsGhost))
            // 0번 타겟 삭제
            targetList.RemoveAt(0);

        // 타겟이 없을때
        if (targetList.Count <= 0)
        {
            // Idle 상태로 변경
            state = State.Idle;
        }
        // 남은 타겟이 있을때
        else
            // Ready 상태로 변경
            state = State.Ready;
    }

    void HitTarget()
    {
        // 현재 타겟 삭제
        targetList.RemoveAt(0);

        // 리스트에 등록된 타겟이 있는데, null이거나, 비활성화 됬거나, 고스트일때
        while (targetList.Count > 0 && (targetList[0] == null || !targetList[0].gameObject.activeSelf || targetList[0].IsGhost))
            // 0번 타겟 삭제
            targetList.RemoveAt(0);

        // 조준 시간 시간 산출
        aimTime = Random.Range(0.5f, 1f);
        // 해당 시간으로 쿨타임 갱신
        coolCount = aimTime;

        // Ready 상태로 변경
        state = State.Ready;

        // light 밝기 낮추기
        DOTween.To(() => tailLight.intensity, x => tailLight.intensity = x, 0f, aimTime);

        // stopTime 시간동안 부드럽게 멈추기
        DOTween.To(() => rigidArrow.velocity, x => rigidArrow.velocity = x, Vector2.zero, aimTime)
        .SetDelay(0.2f);

        Vector2 nextDir;
        // 남은 타겟이 있을때
        if (targetList.Count > 0)
            //다음 타겟 방향 계산
            nextDir = targetList[0].transform.position - transform.position;
        // 타겟 없을때 플레이어 방향
        else
            nextDir = PlayerManager.Instance.transform.position - transform.position;

        // 각도 계산
        float nextAngle = Mathf.Atan2(nextDir.y, nextDir.x) * Mathf.Rad2Deg - 90f;
        // stopTime 시간동안 다음 타겟 방향 바라보기
        transform.DORotate(Vector3.forward * nextAngle, aimTime)
        .SetEase(Ease.OutBack, 3)
        .SetDelay(0.2f);
    }

    void SpinPosition()
    {
        float followSpeed = speed;

        // 중심점 벡터 slowFollowPlayer 가 플레이어 천천히 따라가기
        slowFollowPlayer = Vector3.Lerp(slowFollowPlayer, PlayerManager.Instance.transform.position, Time.deltaTime * followSpeed);

        // 중심점 기준으로 마법 오브젝트 위치 보정
        spinObj.transform.position = slowFollowPlayer + spinOffset;

        // 중심점 기준 공전위치로 이동
        float rotateSpeed = Time.deltaTime * 10f * speed;
        spinObj.transform.RotateAround(slowFollowPlayer, Vector3.back, rotateSpeed);

        // 중심점 벡터 기준으로 오프셋 재설정
        spinOffset = spinObj.transform.position - slowFollowPlayer;
    }

    //화살이 날아가는 방향
    void ArrowDirection()
    {
        // 날아가는 방향 바라보기
        if (transform.position != arrowLastPos)
        {
            Vector3 returnDir = (transform.position - arrowLastPos).normalized;
            float rotation = Mathf.Atan2(returnDir.y, returnDir.x) * Mathf.Rad2Deg;

            rigidArrow.rotation = rotation - 90;
            arrowLastPos = transform.position;
        }

    }
}
