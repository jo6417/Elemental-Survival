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
    public MagicInfo magic;

    public enum State { Idle, Attack, Ready };
    public State state = State.Idle; //화살의 상태

    [Header("Refer")]
    public Light2D tailLight;
    public GameObject atkMark; //공격 위치 표시 마커
    public GameObject bloodParticle; // 타격시 혈흔 프리팹
    [SerializeField] List<GameObject> targetList = new List<GameObject>();
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
    [SerializeField] float coolTime;
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
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        speed = MagicDB.Instance.MagicSpeed(magic, true);
        coolTime = MagicDB.Instance.MagicCoolTime(magic);

        //플레이어 주변을 도는 마커
        spinObj = LeanPool.Spawn(atkMark, transform.position, Quaternion.identity);

        //플레이어와의 거리 보정
        spinObj.transform.position = slowFollowPlayer + Vector3.up * 3;
        spinOffset = spinObj.transform.position - slowFollowPlayer;

        // 밝기 초기화
        tailLight.intensity = 0;

        // 쿨타임 갱신
        coolCount = coolTime;
    }

    void Update()
    {
        if (magic == null)
            return;

        // 준비 상태일때
        if (state == State.Idle)
        {
            // 콜라이더 끄기
            coll.enabled = false;

            //플레이어 주위 공전
            SpinPosition();

            //플레이어 주변 회전하며 따라가기
            transform.position = Vector3.Lerp(transform.position, spinObj.transform.position, Time.deltaTime * 5);

            //따라가는 방향으로 회전
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

            // 타겟이 null이거나 비활성화 되었을때 (죽었을때)
            if (targetList[0] == null || !targetList[0].activeInHierarchy)
            {
                // 다음 타겟 준비
                StartCoroutine(AimTarget());
                // AimTarget();
                return;
            }

            //다음 타겟 방향 계산
            Vector2 attackDir = targetList[0].transform.position - transform.position;
            float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg - 90f;
            // Quaternion nextRotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * 10f);

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
                    coolCount = coolTime;

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
        if (state == State.Attack && targetList.Count > 0 && other.gameObject == targetList[0])
        {
            // 혈흔 이펙트 각도
            Quaternion rotation = Quaternion.Euler(transform.position - other.transform.position);

            // 혈흔 이펙트 생성
            LeanPool.Spawn(bloodParticle, other.transform.position, rotation, SystemManager.Instance.effectPool);

            // 타겟 삭제 및 다음 타겟 공격 준비
            StartCoroutine(AimTarget());
            // AimTarget();
        }
    }

    //화살 발사
    void SetTargets()
    {
        // 범위내 모든 적 리스트 뽑기
        targetList = CastMagic.Instance.MarkEnemyObj(magic);

        // 공격 준비 상태로 전환
        state = State.Ready;

        // if (targetList.Count > 0)
        //     StartCoroutine(AimTarget());

        #region OldCode
        // float endRotation; //날아갈 각도

        // GameObject mark = null;

        // for (int i = 0; i < targetList.Count; i++)
        // {
        //     // 목표 오브젝트
        //     GameObject targetObj = targetList[i];

        //     //적의 위치에 마커 생성
        //     // mark = LeanPool.Spawn(atkMark, targetObj.transform.position, Quaternion.identity);

        //     //공격하는데 걸리는 시간 = 거리 / 속력
        //     float atkDuration = Vector2.Distance(transform.position, targetObj.transform.position) / MagicDB.Instance.MagicSpeed(magic, true);

        //     //날아갈 각도
        //     Vector3 dir = targetObj.transform.position - transform.position;
        //     endRotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        //     // 마크 포지션 방향으로 domove
        //     Sequence sequence = DOTween.Sequence();
        //     sequence
        //     .Prepend(
        //         //날아갈 방향으로 회전
        //         transform.DORotate(Vector3.forward * endRotation, 0.01f)
        //         .SetEase(Ease.OutCirc)
        //     )
        //     .Append(
        //         transform.DOMove(targetObj.transform.position, atkDuration / targetList.Count)
        //     )
        //     .OnComplete(() =>
        //     {
        //         // 도착하면 마크 오브젝트 삭제
        //         LeanPool.Despawn(mark);
        //     });

        //     yield return new WaitUntil(() => !mark.activeSelf);
        // }

        // state = State.Ready; //준비 상태로 전환

        // // light 밝기 낮추기
        // DOTween.To(() => headLight.intensity, x => headLight.intensity = x, 0f, 0.5f);

        // //쿨타임 만큼 대기
        // float coolTime = MagicDB.Instance.MagicCoolTime(magic);
        // // yield return new WaitForSeconds(coolTime);
        // // float coolCount = coolTime;
        // magic.coolCount = coolTime;
        // while (magic.coolCount > 0)
        // {
        //     //카운트 차감, 플레이어 자체속도 반영
        //     magic.coolCount -= Time.deltaTime;

        //     yield return null;
        // }
        #endregion
    }

    IEnumerator AimTarget()
    {
        // ready 상태로 대기
        coolCount = 10f;

        // Ready 상태로 변경
        state = State.Ready;

        // 딜레이 대기
        yield return new WaitForSeconds(0.1f);

        // 조준 시간 시간 산출
        aimTime = Random.Range(0.1f, 0.2f);
        // 해당 시간으로 쿨타임 갱신
        coolCount = aimTime;

        // light 밝기 낮추기
        DOTween.To(() => tailLight.intensity, x => tailLight.intensity = x, 0f, aimTime);

        // stopTime 시간동안 부드럽게 멈추기
        DOTween.To(() => rigidArrow.velocity, x => rigidArrow.velocity = x, Vector2.zero, aimTime);

        // 현재 타겟이 있을때
        if (targetList.Count > 0 && targetList[0] != null && targetList[0].activeSelf)
            // 0번 타겟 리스트에서 삭제
            targetList.RemoveAt(0);

        // 다음 타겟이 있을때
        if (targetList.Count > 0 && targetList[0] != null && targetList[0].activeSelf)
        {
            //다음 타겟 방향 계산
            Vector2 nextDir = targetList[0].transform.position - transform.position;
            float nextAngle = Mathf.Atan2(nextDir.y, nextDir.x) * Mathf.Rad2Deg - 90f;

            // stopTime 시간동안 다음 타겟 방향 바라보기
            transform.DORotate(Vector3.forward * nextAngle, aimTime);
        }
    }

    void SpinPosition()
    {
        float followSpeed = MagicDB.Instance.MagicSpeed(magic, true);

        // 중심점 벡터 slowFollowPlayer 가 플레이어 천천히 따라가기
        slowFollowPlayer = Vector3.Lerp(slowFollowPlayer, PlayerManager.Instance.transform.position, Time.deltaTime * followSpeed);

        // 중심점 기준으로 마법 오브젝트 위치 보정
        spinObj.transform.position = slowFollowPlayer + spinOffset;

        // 중심점 기준 공전위치로 이동
        float speed = Time.deltaTime * 10f * MagicDB.Instance.MagicSpeed(magic, true);
        spinObj.transform.RotateAround(slowFollowPlayer, Vector3.back, speed);

        // 중심점 벡터 기준으로 오프셋 재설정
        spinOffset = spinObj.transform.position - slowFollowPlayer;

        // spinObj.transform.rotation = Quaternion.Euler(Vector3.zero);
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
