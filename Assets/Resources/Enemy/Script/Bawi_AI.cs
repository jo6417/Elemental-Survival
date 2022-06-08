using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Bawi_AI : MonoBehaviour
{
    [Header("Refer")]
    public EnemyManager enemyManager;
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
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
        // enemyManager.coll.isTrigger = false;
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

            foreach (SpriteRenderer sprite in enemyManager.spriteList)
            {
                sprite.material = enemyManager.originMat;
                sprite.color = SystemManager.Instance.stopColor; //시간 멈춤 색깔
            }
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
            foreach (SpriteRenderer sprite in enemyManager.spriteList)
            {
                sprite.material = SystemManager.Instance.hitMat;
                sprite.color = SystemManager.Instance.hitColor;
            }

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
        foreach (SpriteRenderer sprite in enemyManager.spriteList)
        {
            sprite.material = enemyManager.originMat;
            sprite.color = enemyManager.originColor;
        }
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

        // 플레이어와의 거리
        float distance = (PlayerManager.Instance.transform.position - transform.position).magnitude;

        //TODO 먼 거리 , 땅속에 드릴 꼽고 땅속에서 플레이어 따라간 후 솟아나기 패턴 (따라갈때 땅에 바위 가시 생김)
        //TODO 중간 거리, 있으면 걸어가기
        Walk();
        //TODO 일정거리 내에 있으면 공격 패턴 중 랜덤
        //TODO 드릴 돌진 패턴, 1~3단계 랜덤 차지, 차지 레벨 높을수록 빠름
        //TODO 큰 바위 던지기 패턴
        //TODO 작은 돌 샷건 패턴
    }

    void Walk()
    {
        enemyManager.nowAction = EnemyManager.Action.Walk;

        // 애니메이터 켜기
        if (enemyManager.animList.Count > 0)
        {
            foreach (Animator anim in enemyManager.animList)
            {
                //TODO Idle 애니메이션으로 전환
            }
        }

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        // 움직일 방향 2D 각도
        float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 가려는 방향으로 양쪽 손이 회전
        leftHand.rotation = Quaternion.Euler(0, 0, rotation);
        rightHand.rotation = Quaternion.Euler(0, 0, rotation);

        //해당 방향으로 가속
        enemyManager.rigid.velocity = dir.normalized * enemyManager.speed * SystemManager.Instance.globalTimeScale;

        // 움직일 방향에따라 머리만 Y축 회전
        if (dir.x > 0)
        {
            head.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            head.rotation = Quaternion.Euler(0, 180, 0);
        }

        enemyManager.nowAction = EnemyManager.Action.Idle;
    }
}
