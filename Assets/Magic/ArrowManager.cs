using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class ArrowManager : MonoBehaviour
{
    public enum State { Ready, Attack };
    public State state = State.Ready; //화살의 상태

    [Header("Refer")]
    public Transform player;
    public GameObject atkRange; //공격 범위 표시
    public GameObject atkMark; //공격 위치 표시 마커

    public int attackNum = 1; //공격 횟수
    public float atkDuration = 1; //공격 속도
    public float atkDelay = 1;
    Vector3[] atkPos = null;
    GameObject[] marks = null;
    List<GameObject> marker = null;
    Vector3 arrowLastPos;
    Rigidbody2D rigidArrow;
    Collider2D col;
    SpriteRenderer sprite;
    Magic magic;

    void Start()
    {
        marker = new List<GameObject>();
        rigidArrow = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        magic = GetComponent<MagicProjectile>().magic;

        StartCoroutine(shotArrow());
    }

    void Update()
    {
        // 날아가는 방향으로 화살 돌리기
        ArrowDirection();

        // 공격 범위 플레이어 따라다니기
        atkRange.transform.position = player.transform.position;

        // 공격 할때만 콜라이더 활성화
        if (state == State.Attack)
            col.enabled = true;

        if (state == State.Ready)
            col.enabled = false;
    }

    //화살 발사
    IEnumerator shotArrow()
    {
        markAttackPos();

        // 같은 속도로 날아가게 코루틴으로 변경
        if (atkPos != null)
        for (int i = 0; i < atkPos.Length; i++)
        {
            // 마크 포지션 방향으로 domove
            transform.DOMove(atkPos[i], atkDuration)
            .OnStart(() => {
                state = State.Attack; //공격 상태로 전환
            })
            .OnComplete(() =>
            {
                state = State.Ready; //준비 상태로 전환
                // 도착하면 마크 오브젝트 삭제
                LeanPool.Despawn(marks[i]);
            });

            //공격 시간동안 대기
            yield return new WaitForSeconds(atkDuration);
        }

        // if(atkPos != null)
        // transform.DOPath(atkPos, atkSpeed, PathType.Linear, PathMode.TopDown2D, 10, Color.red)
        // .OnStart(() =>
        // {
        //     state = State.Attack; //공격 상태로 전환
        // })
        // .OnComplete(() =>
        // {
        //     state = State.Ready; //준비 상태로 전환
        // });

        // 공격 딜레이 대기
        yield return new WaitForSeconds(atkDelay);

        // 코루틴 재시작
        StartCoroutine(shotArrow());
    }

    // 플레이어 주변 랜덤 적 위치에 마크하기
    void markAttackPos()
    {
        // 플레이어 중심 범위 안의 적 배열에 담기
        float range = magic.range * PlayerManager.Instance.range;
        Collider2D[] colls = null;
        colls = Physics2D.OverlapCircleAll(player.position, range, 1 << LayerMask.NameToLayer("Enemy"));

        // 범위 사이즈 반영해서 보여주기
        atkRange.transform.localScale = new Vector2(range / 2, range / 2);

        int Num = attackNum
        + PlayerManager.Instance.projectileNum; //플레이어 투사체 갯수 추가 계수

        //범위 안의 적이 공격횟수보다 많으면 공격횟수만큼 반복, 아니면 범위 안의 적 갯수만큼 반복
        int atkNum = colls.Length > Num ? Num : colls.Length;

        // 공격 위치 배열 초기화
        atkPos = colls.Length == 0 ? null : new Vector3[atkNum];
        marks = colls.Length == 0 ? null : new GameObject[atkNum];

        //TODO 중복된 적 마크 방지
        List<int> indexList = new List<int>();
        for (int i = 0; i < colls.Length; i++)
        {
            //리스트에 모든 인덱스 넣기
            indexList.Add(i);
        }

        for (int i = 0; i < atkNum; i++)
        {
            //인덱스 리스트에서 랜덤한 난수 생성
            int index = Random.Range(0, indexList.Count);

            // 공격 위치
            Vector3 pos = colls[indexList[index]].transform.position;

            //이미 선택된 인덱스 제거
            indexList.RemoveAt(index);

            // 공격 위치에 마커 생성
            GameObject mark = LeanPool.Spawn(atkMark, pos, Quaternion.identity);
            marks[i] = mark;
            // 공격 시간 후 마커 삭제
            // LeanPool.Despawn(mark, atkSpeed);

            // 공격 위치 배열에 추가
            atkPos[i] = pos;
        }
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
