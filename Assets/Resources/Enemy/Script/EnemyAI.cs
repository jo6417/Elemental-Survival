using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class EnemyAI : MonoBehaviour
{
    [Header("Refer")]
    public EnemyManager enemyManager;
    public EnemyAtkTrigger attackTrigger_A;

    [Header("Jump")]
    public float jumpCoolCount;
    [SerializeField]
    private float jumpCooltime = 0.5f;
    public GameObject dustEffect;

    private void Awake()
    {
        enemyManager = enemyManager == null ? GetComponent<EnemyManager>() : enemyManager;
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
        enemyManager.coll.isTrigger = false;
    }

    void Update()
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

        //걷는 타입일때
        if (enemyManager.moveType == EnemyManager.MoveType.Walk)
        {
            Walk();
        }

        //점프 타입일때
        if (enemyManager.moveType == EnemyManager.MoveType.Jump)
        {
            // 점프중 아니고 일정 거리 내 들어오면 점프
            if (jumpCoolCount <= 0)
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
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //해당 방향으로 가속
        enemyManager.rigid.velocity = dir.normalized * enemyManager.speed * SystemManager.Instance.globalTimeScale;

        //움직일 방향에따라 회전
        if (dir.x > 0)
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
        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //움직일 방향에따라 좌우반전
        if (dir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        //움직일 거리, 플레이어 위치까지 갈수 있으면 플레이어 위치, 못가면 적 스피드
        float distance = dir.magnitude > enemyManager.range ? enemyManager.range : dir.magnitude;

        //해당 방향으로 가속
        enemyManager.rigid.velocity = dir.normalized * distance * SystemManager.Instance.globalTimeScale;
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
        if (dustEffect != null)
            LeanPool.Spawn(dustEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
    }
}
