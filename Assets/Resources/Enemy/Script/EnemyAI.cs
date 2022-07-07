using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class EnemyAI : MonoBehaviour
{
    [Header("State")]
    public Vector3 targetDir; //플레이어 방향

    [Header("Refer")]
    public EnemyManager enemyManager;

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
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
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
        if (enemyManager && !enemyManager.ManageState())
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

        // 타겟 null 체크
        if (enemyManager.TargetObj != null)
            // 타겟 방향 계산
            targetDir = enemyManager.TargetObj.transform.position - transform.position;
        else
            // 타겟이 null 이면 멈추기
            enemyManager.rigid.velocity = Vector2.zero;

        //걷는 타입일때
        if (enemyManager.moveType == EnemyManager.MoveType.Walk || enemyManager.moveType == EnemyManager.MoveType.Dash)
        {
            Walk();
        }

        //점프 타입일때
        if (enemyManager.moveType == EnemyManager.MoveType.Jump)
        {
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

        //움직일 방향
        // Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //해당 방향으로 가속
        enemyManager.rigid.velocity = targetDir.normalized * enemyManager.speed * SystemManager.Instance.globalTimeScale;

        //움직일 방향에따라 회전
        if (targetDir.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
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
        // 타겟 null 체크
        if (enemyManager.TargetObj != null)
            // 타겟 방향 계산
            targetDir = enemyManager.TargetObj.transform.position - transform.position;
        else
            // 타겟이 null 이면 멈추기
            enemyManager.rigid.velocity = Vector2.zero;

        //움직일 방향에따라 좌우반전
        if (targetDir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        //움직일 거리, 플레이어 위치까지 갈수 있으면 플레이어 위치, 못가면 적 스피드
        float distance = targetDir.magnitude > enemyManager.range ? enemyManager.range : targetDir.magnitude;

        //해당 방향으로 가속
        enemyManager.rigid.velocity = targetDir.normalized * distance * SystemManager.Instance.globalTimeScale;
    }

    public void JumpMoveStop()
    {
        //rigid 이동 멈추기
        enemyManager.rigid.velocity = Vector2.zero;
    }

    public void JumpEnd()
    {
        // IDLE 애니메이션 전환
        enemyManager.animList[0].SetBool("Jump", false);

        // 현재 행동 끝내기
        enemyManager.nowAction = EnemyManager.Action.Idle;

        // 착지 이펙트 생성
        if (landEffect != null)
            LeanPool.Spawn(landEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
    }
}
