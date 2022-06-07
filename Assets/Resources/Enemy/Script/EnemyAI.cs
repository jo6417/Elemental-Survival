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
        if (enemyManager.enemy == null)
            return;

        //상태 관리
        ManageState();

        //행동 관리
        ManageAction();
    }

    void ManageState()
    {
        //죽음 애니메이션 중일때
        if (enemyManager.isDead)
        {
            enemyManager.state = EnemyManager.State.Dead;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
            enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;

            if (enemyManager.animList.Count > 0)
            {
                foreach (Animator anim in enemyManager.animList)
                {
                    anim.speed = 0f;
                }
            }

            transform.DOPause();

            return;
        }

        //전역 타임스케일이 0 일때
        if (SystemManager.Instance.globalTimeScale == 0)
        {
            enemyManager.state = EnemyManager.State.MagicStop;

            // 애니메이션 멈추기
            if (enemyManager.animList.Count > 0)
            {
                foreach (Animator anim in enemyManager.animList)
                {
                    anim.speed = 0f;
                }
            }

            // 이동 멈추기
            enemyManager.rigid.velocity = Vector2.zero;

            transform.DOPause();
            return;
        }

        // 멈춤 디버프일때
        if (enemyManager.stopCount > 0)
        {
            enemyManager.state = EnemyManager.State.TimeStop;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
            enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            // 애니메이션 멈추기
            if (enemyManager.animList.Count > 0)
            {
                foreach (Animator anim in enemyManager.animList)
                {
                    anim.speed = 0f;
                }
            }

            enemyManager.sprite.material = enemyManager.originMat;
            enemyManager.sprite.color = SystemManager.Instance.stopColor; //시간 멈춤 색깔
            transform.DOPause();

            enemyManager.stopCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;
            return;
        }

        //맞고 경직일때
        if (enemyManager.hitCount > 0)
        {
            enemyManager.state = EnemyManager.State.Hit;

            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화

            // 머터리얼 및 색 변경
            enemyManager.sprite.material = SystemManager.Instance.hitMat;
            enemyManager.sprite.color = SystemManager.Instance.hitColor;

            enemyManager.hitCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;
            return;
        }

        //스폰 콜라이더에 닿아 반대편으로 보내질때 잠시대기
        if (enemyManager.oppositeCount > 0)
        {
            enemyManager.rigid.velocity = Vector2.zero; //이동 초기화

            enemyManager.oppositeCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;
            return;
        }

        //모든 문제 없으면 idle 상태로 전환
        enemyManager.state = EnemyManager.State.Idle;

        // rigid, sprite, 트윈, 애니메이션 상태 초기화
        // enemyManager.rigid.velocity = Vector2.zero; //이동 초기화
        // enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation; // 위치 고정 해제
        enemyManager.sprite.material = enemyManager.originMat;
        enemyManager.sprite.color = enemyManager.originColor;
        transform.DOPlay();
        // 애니메이션 속도 초기화
        if (enemyManager.animList.Count > 0)
        {
            foreach (Animator anim in enemyManager.animList)
            {
                anim.speed = 1f;
            }
        }
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
