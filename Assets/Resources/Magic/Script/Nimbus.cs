using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Nimbus : MonoBehaviour
{
    [Header("State")]
    public State state = State.Ready;
    public enum State { Ready, Attack };
    Vector3 slowFollowPlayer;
    Vector3 spinOffset;

    [Header("Refer")]
    private MagicInfo magic;
    public MagicHolder magicHolder;
    public Animator anim;
    public Collider2D magicColl; //마법 데미지용 콜라이더
    public SpriteRenderer hitbox; //마법 타격 지점 인디케이터
    public GameObject atkMark; //공격 위치 표시 마커
    GameObject spinObj = null;

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        //플레이어 주변을 도는 마커
        spinObj = LeanPool.Spawn(atkMark, transform.position, Quaternion.identity);

        //플레이어와의 거리 보정
        float range = MagicDB.Instance.MagicRange(magic);
        transform.position = slowFollowPlayer + Vector3.up * range;

        spinOffset = transform.position - slowFollowPlayer;
    }

    private void Update()
    {
        // 마법정보 없으면 리턴
        if (magic == null)
            return;

        // 공격중이면 리턴
        if (state == State.Attack)
            return;

        // 쿨타임 아닐때
        if (magic.coolCount <= 0)
        {
            // 쿨타임 갱신
            magic.coolCount = MagicDB.Instance.MagicCoolTime(magic);

            // 공격 상태로 전환
            state = State.Attack;

            // 공격 시작
            StartCoroutine(StartAttack());

            return;
        }

        // 준비 상태일때 플레이어 주변 공전
        if (state == State.Ready)
        {
            // 쿨타임 차감
            magic.coolCount -= Time.deltaTime;

            //플레이어 주위 공전
            SpinPosition();

            //플레이어 주변 회전하며 따라가기
            float followSpeed = MagicDB.Instance.MagicSpeed(magic, true);
            transform.position = Vector3.Lerp(transform.position, spinObj.transform.position, Time.deltaTime * followSpeed);
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
    }

    IEnumerator StartAttack()
    {
        // 투사체 수만큼 주변의 적 마크
        List<Vector2> enemyPos = MarkEnemyPos(magic);

        //목표 위치
        Vector2 targetPos;
        GameObject mark = null;

        // 마크된 적 순서대로 위치 따라가기
        for (int i = 0; i < enemyPos.Count; i++)
        {
            //목표 위치
            targetPos = enemyPos[i];

            //적의 위치에 마커 생성
            mark = LeanPool.Spawn(atkMark, targetPos, Quaternion.identity);

            // 히트박스 빨간색으로 바꾸기
            hitbox.DOColor(new Color(1, 0, 0, 80f / 255f), 0.5f);

            //해당 몬스터 따라가기
            float aimCount = 0.5f;
            while (aimCount > 0)
            {
                // 위치 이동
                Vector2 movePos = Vector2.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
                transform.position = movePos;

                // 시간 차감 후 대기
                aimCount -= Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }

            // 애니메이션 재생해서 공격
            anim.SetBool("isAttack", true);

            //애니메이션 idle로 돌아올때까지 대기
            yield return new WaitUntil(() => !anim.GetBool("isAttack"));

            // 히트박스 노란색으로 초기화
            hitbox.DOColor(new Color(1, 1, 0, 80f / 255f), 0.5f);
        }

        // 모든 적 방문 후 쿨타임 시작
        state = State.Ready;
    }

    List<Vector2> MarkEnemyPos(MagicInfo magic)
    {
        // 마법 범위 계산
        float range = MagicDB.Instance.MagicRange(magic);
        // 투사체 개수 계산
        int atkNum = MagicDB.Instance.MagicProjectile(magic);

        List<Vector2> enemyPos = new List<Vector2>();

        // 적 위치 넣을 리스트 준비
        List<Collider2D> enemyColList = new List<Collider2D>();
        enemyColList.Clear();

        //범위 안의 모든 적 리스트에 담기
        enemyColList = Physics2D.OverlapCircleAll(PlayerManager.Instance.transform.position, range, 1 << LayerMask.NameToLayer("Enemy")).ToList();

        // 적 위치 리스트에 넣기
        for (int i = 0; i < atkNum; i++)
        {
            // 플레이어 주변 범위내 랜덤 위치 벡터 생성
            Vector2 pos = (Vector2)PlayerManager.Instance.transform.position
            + Random.insideUnitCircle.normalized * range;

            // 리스트에서 적 위치 꺼낸 후 삭제
            if (enemyColList.Count > 0)
            {
                Collider2D col = enemyColList[Random.Range(0, enemyColList.Count)];
                pos = col.transform.position;

                //임시 리스트에서 지우기
                enemyColList.Remove(col);

                // print(col.transform.name + col.transform.position);
            }

            // 범위내에 적이 있으면 적위치, 없으면 무작위 위치 넣기
            enemyPos.Add(pos);
        }

        //적의 위치 리스트 리턴
        return enemyPos;
    }

    public void ColliderOn()
    {
        //콜라이더 켜기
        magicColl.enabled = true;
    }

    public void ColliderOff()
    {
        //콜라이더 끄기
        magicColl.enabled = false;

        // Idle 애니메이션 켜기
        anim.SetBool("isAttack", false);
    }
}
