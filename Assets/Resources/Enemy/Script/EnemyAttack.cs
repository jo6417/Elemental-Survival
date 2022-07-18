using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    Vector3 targetDir;
    public float activeAngleOffset; // 액티브 공격 오브젝트 방향 오프셋
    bool attackReady; //공격 준비중

    [Header("Refer")]
    public EnemyManager enemyManager;
    public EnemyInfo enemy;
    public string enemyName;
    public Collider2D atkColl; //공격 콜라이더
    public GameObject dashEffect;
    public GameObject activeObj; //공격시 활성화할 오브젝트

    [Header("Attack State")]
    // public bool friendlyFire = false; // 충돌시 아군 피해 여부
    public bool flatDebuff = false; //납작해지는 디버프
    public bool knockBackDebuff = false; //넉백 디버프
    public bool poisonDebuff = false; // 독 디버프

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
        //공격 콜라이더 끄기
        if (atkColl)
            atkColl.enabled = false;

        yield return new WaitUntil(() => enemyManager != null && enemyManager.enemy != null);

        enemy = enemyManager.enemy;

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

        //공격 준비 해제
        attackReady = false;
    }

    private void FixedUpdate()
    {
        // 이미 공격중이면 리턴
        if (enemyManager.nowAction == EnemyManager.Action.Attack)
        {
            return;
        }
    }

    private void Update()
    {
        // 몬스터 매니저 비활성화 되었으면 리턴
        if (!enemyManager)
            return;

        // 상태 이상 있으면
        if (!enemyManager.ManageState())
        {
            // 이상 있으면 공격 콜라이더 끄기
            atkColl.enabled = false;
            return;
        }

        // 공격 준비중이면 리턴
        if (attackReady)
            return;

        // 타겟 없거나 비활성화면 리턴
        if (!enemyManager.targetObj || !enemyManager.targetObj.activeSelf)
            return;

        // 타겟 방향 계산
        targetDir = enemyManager.targetObj.transform.position - transform.position;

        // 공격 범위 안에 들어오면 공격 시작
        if (targetDir.magnitude <= enemyManager.attackRange && enemyManager.attackRange > 0)
        {
            //공격 준비로 전환
            attackReady = true;

            StartCoroutine(ChooseAttack());
        }
    }

    IEnumerator ChooseAttack()
    {
        //움직일 방향에따라 회전
        if (targetDir.x > 0)
            enemyManager.transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            enemyManager.transform.rotation = Quaternion.Euler(0, 180, 0);

        // 이동 멈추기
        enemyManager.rigid.velocity = Vector3.zero;

        // 점프중이라면
        if (enemyManager.enemyAI && enemyManager.enemyAI.jumpCoolCount > 0)
        {
            // Idle 상태 될때까지 대기
            yield return new WaitUntil(() => enemyManager.nowAction == EnemyManager.Action.Idle);
        }

        // 액티브 공격 오브젝트 있으면 해당 공격 함수 실행
        if (activeObj != null)
            StartCoroutine(RangeAttack());

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

        //플레이어 방향 다시 계산
        targetDir = enemyManager.targetObj.transform.position - transform.position;

        if (targetDir.x > 0)
            enemyManager.transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            enemyManager.transform.rotation = Quaternion.Euler(0, 180, 0);

        // 돌진 시작 인디케이터 켜기
        dashEffect.SetActive(true);

        // 타겟 방향 반대로 살짝 이동
        enemyManager.rigid.velocity = -targetDir.normalized * 3f;
        // enemyManager.transform.DOMove(transform.position - targetDir.normalized, 1f);
        yield return new WaitForSeconds(1f);

        // rigid 타입 전환
        enemyManager.rigid.bodyType = RigidbodyType2D.Dynamic;

        //공격 콜라이더 켜기
        atkColl.enabled = true;

        // 타겟 방향으로 돌진
        enemyManager.rigid.velocity = targetDir.normalized * 20f;
        // enemyManager.transform.DOMove(transform.position + targetDir.normalized * 5f, 0.5f);
        yield return new WaitForSeconds(0.5f);

        // 속도 멈추기
        enemyManager.rigid.velocity = Vector3.zero;

        //공격 콜라이더 끄기
        atkColl.enabled = false;

        // 타겟 위치 추적 시간 초기화
        enemyManager.targetResetCount = 0f;

        // 쿨타임만큼 대기후 초기화
        yield return new WaitForSeconds(enemyManager.enemy.cooltime);
        // Idle로 전환
        enemyManager.nowAction = EnemyManager.Action.Idle;

        //공격 준비 해제
        attackReady = false;
    }

    public IEnumerator RangeAttack()
    {
        // print("Active Attack");

        // 공격 액션으로 전환
        enemyManager.nowAction = EnemyManager.Action.Attack;

        // 공격 오브젝트 활성화
        activeObj.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        // 공격 오브젝트 비활성화
        activeObj.SetActive(false);

        // 쿨타임만큼 대기후 초기화
        yield return new WaitForSeconds(enemyManager.enemy.cooltime);
        // Idle로 전환
        enemyManager.nowAction = EnemyManager.Action.Idle;

        //공격 준비 해제
        attackReady = false;
    }
}
