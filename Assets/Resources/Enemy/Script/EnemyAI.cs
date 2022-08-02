using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class EnemyAI : MonoBehaviour
{
    [Header("State")]
    public Vector3 targetDir; //플레이어 방향
    public float moveSpeedDebuff = 1f; // 속도 디버프

    [Header("Refer")]
    public EnemyManager enemyManager;

    [Header("Walk")]
    public float moveResetTime = 3f;
    public float moveResetCount;

    [Header("Jump")]
    public float jumpCoolCount;
    [SerializeField]
    private float jumpCooltime = 0.5f;
    public GameObject landEffect;

    // [Header("Attack")]
    // public float attackRange;

    private void Awake()
    {
        enemyManager = enemyManager == null ? GetComponentInChildren<EnemyManager>() : enemyManager;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

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

        // 콜라이더 충돌 초기화
        enemyManager.physicsColl.isTrigger = false;
    }

    private void FixedUpdate()
    {
        // 몬스터 정보 없으면 리턴
        if (enemyManager.enemy == null)
            return;

        // 상태 이상 있으면 리턴
        if (!enemyManager.ManageState())
            return;

        //행동 관리
        ManageAction();
    }

    void ManageAction()
    {
        // Idle 아니면 리턴
        if (enemyManager.nowAction != EnemyManager.Action.Idle)
            return;

        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // 걷기, 대쉬 타입일때
        if (enemyManager.moveType == EnemyManager.MoveType.Walk || enemyManager.moveType == EnemyManager.MoveType.Dash)
        {
            Walk();
        }

        //점프 타입일때
        if (enemyManager.moveType == EnemyManager.MoveType.Jump)
        {
            // 타겟이 null일때
            if (enemyManager.TargetObj == null)
            {
                // 플레이어 주변 위치로 계산
                targetDir = PlayerNearPos() - transform.position;
            }
            else
                // 타겟 방향 계산
                targetDir = enemyManager.TargetObj.transform.position - transform.position;

            // 점프 쿨타임 아닐때, 플레이어가 공격 범위보다 멀때
            if (jumpCoolCount <= 0 && targetDir.magnitude > enemyManager.attackRange)
                JumpStart();
            else
            {
                // 점프 쿨타임 차감
                jumpCoolCount -= Time.deltaTime;

                // 점프휴식 타임, 이동 멈추기
                enemyManager.rigid.velocity = Vector2.zero;
            }
        }
    }

    void Walk()
    {
        enemyManager.nowAction = EnemyManager.Action.Walk;

        // 애니메이터 켜기
        if (enemyManager.animList.Count > 0)
        {
            foreach (Animator anim in enemyManager.animList)
            {
                anim.enabled = true;
            }
        }

        // 목표 위치 갱신 시간 됬을때
        if (moveResetCount <= 0)
        {
            moveResetCount = moveResetTime;

            // 타겟이 null일때
            if (enemyManager.TargetObj == null)
            {
                // 플레이어 주변 위치로 계산
                targetDir = PlayerNearPos() - transform.position;
            }
            // 타겟이 있을때
            else
                // 목표 방향 계산, 랜덤 위치 더해서 부정확하게 만들기
                targetDir = enemyManager.TargetObj.transform.position + (Vector3)Random.insideUnitCircle - transform.position;

            // print(moveToPos);
        }

        // 목표위치 도착했으면 위치 다시 갱신
        if (targetDir.magnitude < 0.1f)
        {
            moveResetCount = 0f;
        }
        else
        {
            //해당 방향으로 가속
            enemyManager.rigid.velocity = targetDir.normalized * enemyManager.speed * moveSpeedDebuff * SystemManager.Instance.globalTimeScale;

            //움직일 방향에따라 회전
            float leftAngle = enemyManager.lookLeft ? 180f : 0f;
            float rightAngle = enemyManager.lookLeft ? 0f : 180f;
            if (targetDir.x > 0)
                enemyManager.transform.rotation = Quaternion.Euler(0, leftAngle, 0);
            else
                enemyManager.transform.rotation = Quaternion.Euler(0, rightAngle, 0);
        }


        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    void JumpStart()
    {
        // 현재 행동 점프로 전환
        enemyManager.nowAction = EnemyManager.Action.Jump;

        // 점프 애니메이션으로 전환
        enemyManager.animList[0].SetBool("Jump", true);

        // 점프 쿨타임 갱신
        jumpCoolCount = jumpCooltime;
    }

    public void JumpMove()
    {
        // 타겟이 null일때
        if (enemyManager.TargetObj == null)
        {
            // 플레이어 주변 위치로 계산
            targetDir = PlayerNearPos() - transform.position;
        }
        // 타겟이 있을때
        else
            // 타겟 방향 계산
            targetDir = enemyManager.TargetObj.transform.position - transform.position;

        //움직일 방향에따라 회전
        float leftAngle = enemyManager.lookLeft ? 180f : 0f;
        float rightAngle = enemyManager.lookLeft ? 0f : 180f;
        if (targetDir.x > 0)
            enemyManager.transform.rotation = Quaternion.Euler(0, leftAngle, 0);
        else
            enemyManager.transform.rotation = Quaternion.Euler(0, rightAngle, 0);

        //움직일 거리, 플레이어 위치까지 갈수 있으면 플레이어 위치, 못가면 적 스피드
        float distance = targetDir.magnitude > enemyManager.speed ? enemyManager.speed : targetDir.magnitude;

        // print(targetDir.normalized * distance * moveSpeedDebuff * SystemManager.Instance.globalTimeScale);

        //해당 방향으로 가속
        enemyManager.rigid.velocity = targetDir.normalized * distance * moveSpeedDebuff * SystemManager.Instance.globalTimeScale;

        // print(enemyManager.rigid.velocity);
    }

    public void JumpMoveStop()
    {
        // rigid 이동 멈추기
        enemyManager.rigid.velocity = Vector2.zero;
    }

    public void JumpEnd()
    {
        // IDLE 애니메이션 전환
        enemyManager.animList[0].SetBool("Jump", false);

        // 착지 이펙트 생성
        if (landEffect != null)
            LeanPool.Spawn(landEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        // 현재 행동 끝내기
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    Vector3 PlayerNearPos(float range = 5f)
    {
        return PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle.normalized * range;
    }
}
