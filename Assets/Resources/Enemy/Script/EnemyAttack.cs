using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    Vector3 playerDir;
    public float activeAngleOffset; // 액티브 공격 오브젝트 방향 오프셋
    bool attackReady; //공격 준비중

    [Header("Refer")]
    public EnemyManager enemyManager;
    public string enemyName;
    public GameObject dashEffect;
    public GameObject activeObj; //공격시 활성화할 오브젝트

    [Header("Attack State")]
    public bool friendlyFire = false; // 충돌시 아군 피해 여부
    public bool flatDebuff = false; //납작해지는 디버프
    public bool knockBackDebuff = false; //넉백 디버프

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
        yield return new WaitUntil(() => enemyManager.enemy != null);

        // 대쉬 범위 초기화
        enemyManager.attackRange = enemyManager.enemy.range;

        // 적 정보 들어오면 이름 표시
        enemyName = enemyManager.enemy.enemyName;

        // 대쉬 이펙트 있으면 끄기
        if (dashEffect != null)
            dashEffect.SetActive(false);

        // 공격 오브젝트 있으면 끄기
        if (activeObj != null)
            activeObj.SetActive(false);
    }

    private void FixedUpdate()
    {
        // 이미 공격중이면 리턴
        if (enemyManager.nowAction == EnemyManager.Action.Attack)
        {
            //속도 멈추기
            enemyManager.rigid.velocity = Vector3.zero;
            return;
        }
    }

    private void Update()
    {
        // 몬스터 정보 없으면 리턴
        if (enemyManager.enemy == null)
            return;

        // 공격 준비중이면 리턴
        if (attackReady)
            return;

        //플레이어 방향 계산
        playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 공격 범위 안에 들어오면 공격 시작
        if (playerDir.magnitude <= enemyManager.attackRange && enemyManager.attackRange > 0)
            StartCoroutine(ChooseAttack());
    }

    IEnumerator ChooseAttack()
    {
        //움직일 방향에따라 회전
        if (playerDir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        // 이동 멈추기
        enemyManager.rigid.velocity = Vector3.zero;

        // 점프중이라면
        if (enemyManager.enemyAI && enemyManager.enemyAI.jumpCoolCount > 0)
        {
            //공격 준비로 전환
            attackReady = true;

            // Idle 상태 될때까지 대기
            yield return new WaitUntil(() => enemyManager.nowAction == EnemyManager.Action.Idle);

            //공격 준비 끝
            attackReady = false;
        }

        // 액티브 공격 오브젝트 있으면 해당 공격 함수 실행
        if (activeObj != null)
            StartCoroutine(ActiveAttack());

        // 돌진 이펙트 있으면 해당 공격 함수 실행
        if (dashEffect != null)
            StartCoroutine(DashAttack());
    }

    public IEnumerator DashAttack()
    {
        // print("Dash Attack");

        // 공격 액션으로 전환
        enemyManager.nowAction = EnemyManager.Action.Attack;

        // 밀리지 않게 kinematic으로 전환
        enemyManager.rigid.bodyType = RigidbodyType2D.Kinematic;

        //플레이어 방향 계산
        playerDir = PlayerManager.Instance.transform.position - transform.position;

        if (playerDir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        // 돌진 시작 인디케이터 켜기
        dashEffect.SetActive(true);

        // 뒤로 살짝 이동
        transform.DOMove(transform.position - playerDir.normalized, 1f);
        yield return new WaitForSeconds(1.5f);

        // 플레이어 방향으로 돌진
        transform.DOMove(transform.position + playerDir.normalized * 5f, 0.5f);
        yield return new WaitForSeconds(0.5f);

        // 쿨타임만큼 대기후 초기화
        yield return new WaitForSeconds(enemyManager.enemy.cooltime);
        // Idle로 전환
        enemyManager.nowAction = EnemyManager.Action.Idle;
        // rigid 타입 전환
        enemyManager.rigid.bodyType = RigidbodyType2D.Dynamic;
    }

    public IEnumerator ActiveAttack()
    {
        // print("Active Attack");

        // 공격 액션으로 전환
        enemyManager.nowAction = EnemyManager.Action.Attack;

        // 플레이어 방향 계산
        // playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 공격 오브젝트 각도 계산
        // float angle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;

        // 공격 오브젝트 생성
        // LeanPool.Spawn(activeObj, activeObj.transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

        // 공격 오브젝트 활성화
        activeObj.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        // 공격 오브젝트 비활성화
        activeObj.SetActive(false);

        // 쿨타임만큼 대기후 초기화
        yield return new WaitForSeconds(enemyManager.enemy.cooltime);
        // Idle로 전환
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }
}
