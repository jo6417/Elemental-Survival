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
    Vector3 slowFollowPlayer;
    Vector3 spinOffset;
    GameObject spinObj = null;
    TrailRenderer tail; //화살 꼬리

    private void Awake()
    {
        tail = GetComponentInChildren<TrailRenderer>();
        marker = new List<GameObject>();
        rigidArrow = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        magicHolder = GetComponent<MagicHolder>();
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        // magic 정보 들어올때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        //플레이어 주변을 도는 마커
        spinObj = LeanPool.Spawn(atkMark, transform.position, Quaternion.identity);

        //플레이어와의 거리 보정
        spinObj.transform.position = slowFollowPlayer + Vector3.up * 3;
        spinOffset = spinObj.transform.position - slowFollowPlayer;

        //화살 발사
        StartCoroutine(shotArrow());
    }

    void Update()
    {
        if (magic == null)
            return;

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

            //따라가는 방향으로 회전
            Vector3 returnDir = spinObj.transform.position - transform.position;
            float rotation = Mathf.Atan2(returnDir.y, returnDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(Vector3.forward * (rotation - 90f));
        }
    }

    //화살 발사
    IEnumerator shotArrow()
    {
        // 마크한 적의 오브젝트 리스트
        List<GameObject> enemyObj = CastMagic.Instance.MarkEnemyObj(magic);

        //트윈 모두 중단
        transform.DOPause();

        state = State.Attack; //공격 상태로 전환

        // light 밝기 올리기
        DOTween.To(() => headLight.intensity, x => headLight.intensity = x, 2f, 0.5f);

        float endRotation; //날아갈 각도

        GameObject mark = null;

        for (int i = 0; i < enemyObj.Count; i++)
        {
            // 목표 오브젝트
            GameObject targetObj = enemyObj[i];

            //적의 위치에 마커 생성
            // mark = LeanPool.Spawn(atkMark, targetObj.transform.position, Quaternion.identity);

            //공격하는데 걸리는 시간 = 거리 / 속력
            float atkDuration = Vector2.Distance(transform.position, targetObj.transform.position) / MagicDB.Instance.MagicSpeed(magic, true);

            //날아갈 각도
            Vector3 dir = targetObj.transform.position - transform.position;
            endRotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

            // 마크 포지션 방향으로 domove
            Sequence sequence = DOTween.Sequence();
            sequence
            .Prepend(
                //날아갈 방향으로 회전
                transform.DORotate(Vector3.forward * endRotation, 0.01f)
                .SetEase(Ease.OutCirc)
            )
            .Append(
                transform.DOMove(targetObj.transform.position, atkDuration / enemyObj.Count)
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
        // float coolCount = coolTime;
        magic.coolCount = coolTime;
        while (magic.coolCount > 0)
        {
            //카운트 차감, 플레이어 자체속도 반영
            magic.coolCount -= Time.deltaTime;

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
