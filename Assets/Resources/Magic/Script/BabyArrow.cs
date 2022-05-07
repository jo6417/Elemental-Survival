using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;
using System.Linq;
using UnityEngine.Experimental.Rendering.Universal;

public class BabyArrow : MonoBehaviour
{
    public enum State { Ready, Attack };
    public State state = State.Ready; //화살의 상태

    [Header("Refer")]
    public Light2D headLight;
    public GameObject atkMark; //공격 위치 표시 마커
    List<GameObject> marker = null;
    Vector3 arrowLastPos;
    Rigidbody2D rigidArrow;
    Collider2D col;
    SpriteRenderer sprite;
    public MagicInfo magic;
    Vector3 slowFollowPlayer;
    Vector3 spinOffset;
    GameObject spinObj = null;
    TrailRenderer tail; //화살 꼬리

    private void Awake()
    {
        tail = GetComponentInChildren<TrailRenderer>();
    }

    private void OnEnable()
    {
        //비활성화 되고 다시 스폰 될때는 Enable에서 공격 실행
        if (magic != null)
            StartCoroutine(shotArrow());
    }

    void Start()
    {
        marker = new List<GameObject>();
        rigidArrow = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        magic = GetComponent<MagicHolder>().magic;

        //플레이어 주변을 도는 마커
        spinObj = Instantiate(atkMark, transform.position, Quaternion.identity);

        //플레이어와의 거리 보정
        spinObj.transform.position = slowFollowPlayer + Vector3.up * 3;
        spinOffset = spinObj.transform.position - slowFollowPlayer;

        //처음 스폰 될때는 Start에서 공격 실행
        if (magic != null)
            StartCoroutine(shotArrow());
    }

    void Update()
    {
        // if (VarManager.Instance.playerTimeScale == 0)
        // {
        //     tail.time = Mathf.Infinity;
        //     return;
        // }
        // else
        // {
        //     if (tail.time == Mathf.Infinity)
        //     {
        //         //서서히 낮추기
        //         tail.time = 1000f;
        //         DOTween.To(() => tail.time, x => tail.time = x, 1f, 0.5f);
        //     }
        // }

        // 공격 할때만 콜라이더 활성화
        if (state == State.Attack)
            col.enabled = true;

        if (state == State.Ready)
        {
            col.enabled = false;

            //플레이어 주위 공전
            SpinPosition();

            //플레이어 주변 회전하며 따라가기
            float followSpeed = MagicDB.Instance.MagicSpeed(magic, true);
            transform.position = Vector3.Lerp(transform.position, spinObj.transform.position, Time.deltaTime * followSpeed);
            // transform.position = spinObj.transform.position;

            //따라가는 방향으로 회전
            transform.rotation = Quaternion.Euler(spinObj.transform.rotation.eulerAngles - Vector3.forward * 90f);
        }
    }

    //화살 발사
    IEnumerator shotArrow()
    {
        // 마크한 적의 위치 리스트
        List<Vector2> enemyPos = MarkEnemyPos(magic);

        //트윈 모두 중단
        transform.DOPause();

        state = State.Attack; //공격 상태로 전환

        // light 밝기 올리기
        DOTween.To(() => headLight.intensity, x => headLight.intensity = x, 2f, 0.5f);

        Vector2 targetPos; //목표 위치

        float endRotation; //날아갈 각도

        GameObject mark = null;

        for (int i = 0; i < enemyPos.Count; i++)
        {
            //목표 위치
            targetPos = enemyPos[i];

            //적의 위치에 마커 생성
            mark = LeanPool.Spawn(atkMark, targetPos, Quaternion.identity);

            //공격하는데 걸리는 시간 = 거리 / 속력
            float atkDuration = Vector2.Distance(transform.position, targetPos) / MagicDB.Instance.MagicSpeed(magic, true);

            //날아갈 각도
            Vector2 dir = targetPos - (Vector2)transform.position;
            endRotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

            // 마크 포지션 방향으로 domove
            Sequence sequence = DOTween.Sequence();
            sequence
            .Prepend(
                //날아갈 방향으로 회전
                transform.DORotate(Vector3.forward * endRotation, 0.1f)
                .SetEase(Ease.OutCirc)
            )
            .Append(
                transform.DOMove(targetPos, atkDuration / enemyPos.Count)
            )
            .OnComplete(() =>
            {
                // 도착하면 마크 오브젝트 삭제
                LeanPool.Despawn(mark);
            });

            yield return new WaitUntil(() => !mark.activeSelf);
        }

        state = State.Ready; //준비 상태로 전환

        // light 밝기 낮추기
        DOTween.To(() => headLight.intensity, x => headLight.intensity = x, 0f, 0.5f);

        //쿨타임 만큼 대기
        float coolTime = MagicDB.Instance.MagicCoolTime(magic);
        // yield return new WaitForSeconds(coolTime);
        float coolCount = coolTime;
        while (coolCount > 0)
        {
            //카운트 차감, 플레이어 자체속도 반영
            coolCount -= Time.deltaTime;

            yield return null;
        }

        //다시 반복
        StartCoroutine(shotArrow());
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

    // 플레이어 주변 랜덤 적 위치에 마크하기
    List<Vector2> MarkEnemyPos(MagicInfo magic)
    {
        List<Vector2> enemyPos = new List<Vector2>();

        //캐릭터 주변의 적들
        List<Collider2D> enemyPosList = new List<Collider2D>();
        float range = MagicDB.Instance.MagicRange(magic);
        enemyPosList = Physics2D.OverlapCircleAll(PlayerManager.Instance.transform.position, range, 1 << LayerMask.NameToLayer("Enemy")).ToList();

        // 투사체 개수 (마법 및 플레이어 투사체 버프 합산)
        int magicProjectile = MagicDB.Instance.MagicProjectile(magic);

        // 적 위치 리스트에 넣기
        for (int i = 0; i < magicProjectile; i++)
        {
            // 플레이어 주변 범위내 랜덤 위치 벡터 생성
            Vector2 pos = (Vector2)PlayerManager.Instance.transform.position +
            new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * range;

            // 범위내 적의 위치
            if (enemyPosList.Count > 0)
            {
                Collider2D col = enemyPosList[Random.Range(0, enemyPosList.Count)];
                pos = col.transform.position;

                //임시 리스트에서 지우기
                enemyPosList.Remove(col);
            }

            // 범위내에 적이 있으면 적위치, 없으면 무작위 위치 넣기
            enemyPos.Add(pos);
        }

        //적의 위치 리스트 리턴
        return enemyPos;
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
