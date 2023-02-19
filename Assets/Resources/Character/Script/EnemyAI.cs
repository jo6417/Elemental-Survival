using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    [Header("Rotate")]
    [SerializeField] bool flipAble = true; // 좌우반전 허용 여부
    [SerializeField] bool tiltFlip = false; // 반대로 기울이기 여부
    [SerializeField] float tiltAngle = 0; // 기울기 각도
    [SerializeField] AnimationCurve tiltCurve = new AnimationCurve();

    [Header("Refer")]
    public Character character;

    [Header("Walk")]
    // public Vector3 targetDir; //플레이어 방향
    public float targetRange = 1f; // 타겟 오차 범위
    public float searchCoolTime = 1f; // 타겟 위치 추적 시간
    public float searchCoolCount; // 타겟 위치 추적 시간 카운트

    [Header("Jump")]
    public float jumpCoolCount;
    [SerializeField]
    private float jumpCooltime = 0.5f;
    public GameObject landEffect;

    // [Header("Attack")]
    // public float attackRange;

    private void Awake()
    {
        character = character != null ? character : GetComponentInChildren<Character>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => character.enemy != null);

        // 죽은 상태일때
        if (character.isDead)
            //애니메이션 스피드 초기화
            if (character.animList != null)
            {
                foreach (Animator anim in character.animList)
                {
                    anim.speed = 1f;
                }
            }

        //속도 초기화
        character.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        character.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 콜라이더 충돌 초기화
        if (character.physicsColl != null)
            character.physicsColl.isTrigger = false;
    }

    private void Update()
    {
        // 타겟위치 카운트 다되면
        if (searchCoolCount <= 0)
        {
            // 추적 타이머 갱신
            searchCoolCount = searchCoolTime;

            // 타겟위치 갱신
            character.targetPos = FindTarget_Pos();
        }
        // 타겟위치 카운트 차감
        else
            searchCoolCount -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        // 몬스터 정보 없으면 리턴
        if (character.enemy == null)
        {
            character.FindEnemyInfo();
            return;
        }

        // 목표 위치를 추적 위치로 서서히 바꾸기
        character.movePos = Vector3.Lerp(character.movePos, character.targetPos, Time.deltaTime * character.speedNow);

        // 목표 방향 계산
        character.targetDir = character.movePos - transform.position;

        // // 타겟에서 일정거리 이상 벗어나면 쫓아가기
        // if (character.targetDir.magnitude > WorldSpawner.Instance.maxDistance)
        // {
        //     character.transform.position = WorldSpawner.Instance.BorderRandPos();
        // }

        // 상태 이상 있으면 리턴
        if (!character.ManageState())
            return;

        // 커스텀 AI 아닐때
        if (character.moveType != Character.MoveType.Custom)
            //행동 관리
            ManageAction();
    }

    // GameObject FindTarget_Obj()
    // {
    //     // 리턴할 오브젝트
    //     GameObject targetObj = null;

    //     // 현재 타겟이 범위 밖에 있으면
    //     if (character.targetDir.magnitude > character.targetFindRange)
    //     {
    //         // 추적할 타겟 레이어
    //         int targetLayer = -1;

    //         // 고스트일때
    //         if (character.IsGhost)
    //             targetLayer = SystemManager.Instance.layerList.EnemyHit_Layer;
    //         // 고스트 아닐때
    //         else
    //             targetLayer = SystemManager.Instance.layerList.PlayerHit_Layer;

    //         // 새로운 타겟 찾기
    //         List<Collider2D> targetCollList = Physics2D.OverlapCircleAll(transform.position, character.targetFindRange, 1 << targetLayer).ToList();

    //         // 찾은 타겟이 있으면
    //         if (targetCollList.Count > 0)
    //             // 타겟중에 랜덤으로 리턴
    //             targetObj = targetCollList[Random.Range(0, targetCollList.Count)].gameObject;
    //     }

    //     // 타겟 리턴
    //     return targetObj;
    // }

    Vector3 FindTarget_Pos()
    {
        // 리턴할 추적 위치, 기본으로 본인 위치
        Vector3 pos = transform.position;

        // 타겟이 있을때 타겟 위치
        if (character.TargetObj != null)
            pos = character.TargetObj.transform.position;

        // 추적 위치 계산, 랜덤 위치 더해서 부정확하게 만들기
        pos = pos + (Vector3)Random.insideUnitCircle * targetRange;

        // 추적 위치 리턴
        return pos;
    }

    void ManageAction()
    {
        // Idle 아니면 리턴
        if (character.nowState != CharacterState.Idle)
            return;

        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // 방향따라 기울이기
        if (tiltAngle != 0)
        {
            float angleZ = Mathf.Lerp(0, Mathf.Abs(tiltAngle), Mathf.Abs(character.targetDir.normalized.x));

            // 오른쪽이면 반대로 기울기
            if (character.targetDir.normalized.x > 0)
                angleZ = -angleZ;

            // 스프라이트 몸체 기울이기
            character.spriteObj.localRotation = Quaternion.Lerp(character.spriteObj.localRotation, Quaternion.Euler(0, 0, angleZ), 0.1f);
            // character.spriteObj.localRotation = Quaternion.Euler(0, 0, angleZ);
        }

        // 걷기, 대쉬 타입일때
        if (character.moveType == Character.MoveType.Walk || character.moveType == Character.MoveType.Dash)
        {
            Walk();
        }

        //점프 타입일때
        if (character.moveType == Character.MoveType.Jump)
        {
            // 점프 쿨타임 아닐때, 플레이어가 공격 범위보다 멀때
            if (jumpCoolCount <= 0 && character.targetDir.magnitude > character.attackRange)
                JumpStart();
            else
            {
                // 점프 쿨타임 차감
                jumpCoolCount -= Time.deltaTime;

                // 점프휴식 타임, 이동 멈추기
                character.rigid.velocity = Vector2.zero;
            }
        }
    }

    void Flip()
    {
        // 반전 비허용이면 리턴
        if (!flipAble)
            return;

        //움직일 방향에따라 회전
        float leftAngle = character.lookLeft ? 180f : 0f;
        float rightAngle = character.lookLeft ? 0f : 180f;
        if (character.targetDir.x > 0)
            character.transform.rotation = Quaternion.Euler(0, leftAngle, 0);
        else
            character.transform.rotation = Quaternion.Euler(0, rightAngle, 0);
    }

    void Walk()
    {
        character.nowState = CharacterState.Walk;

        // 애니메이터 켜기
        if (character.animList.Count > 0)
        {
            foreach (Animator anim in character.animList)
            {
                anim.enabled = true;
            }
        }

        // 목표위치 도착했으면 위치 다시 갱신
        if (character.targetDir.magnitude < 0.5f)
        {
            searchCoolCount = 0f;
        }
        else
        {
            //해당 방향으로 가속
            character.rigid.velocity = character.targetDir.normalized * character.speedNow * character.moveSpeedDebuff * SystemManager.Instance.globalTimeScale;

            // 방향따라 좌우반전
            Flip();
        }

        character.nowState = CharacterState.Idle;
    }

    private void OnDrawGizmosSelected()
    {
        if (character == null)
            character = GetComponent<Character>();

        // 보스부터 이동 위치까지 직선
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, character.movePos);

        // 이동 위치 기즈모
        Gizmos.DrawIcon(character.movePos, "Circle.png", true, new Color(1, 0, 0, 0.5f));

        // 추적 위치 기즈모
        Gizmos.DrawIcon(character.targetPos, "Circle.png", true, new Color(0, 0, 1, 0.5f));

        // 추적 위치부터 이동 위치까지 직선
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(character.targetPos, character.movePos);
    }

    void JumpStart()
    {
        // 현재 행동 점프로 전환
        character.nowState = CharacterState.Jump;

        // 점프 애니메이션으로 전환
        character.animList[0].SetBool("Jump", true);

        // 점프 쿨타임 갱신
        jumpCoolCount = jumpCooltime;
    }

    public void JumpMove()
    {
        // 방향따라 좌우반전
        Flip();

        //움직일 거리, 목표 위치까지 갈수 있으면 목표 위치, 스피드보다 낮으면 캐릭터 스피드
        float distance = character.targetDir.magnitude > character.speedNow ? character.speedNow : character.targetDir.magnitude;

        // print(character.targetDir.normalized * distance * moveSpeedDebuff * SystemManager.Instance.globalTimeScale);

        //해당 방향으로 가속
        character.rigid.velocity = character.targetDir.normalized * distance * character.moveSpeedDebuff * SystemManager.Instance.globalTimeScale;

        // print(character.rigid.velocity);
    }

    public void JumpMoveStop()
    {
        // rigid 이동 멈추기
        character.rigid.velocity = Vector2.zero;
    }

    public void JumpEnd()
    {
        // IDLE 애니메이션 전환
        character.animList[0].SetBool("Jump", false);

        // 착지 이펙트 생성
        if (landEffect != null)
            LeanPool.Spawn(landEffect, transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

        // 현재 행동 끝내기
        character.nowState = CharacterState.Idle;
    }

    Vector3 PlayerNearPos(float range = 3f)
    {
        return PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle.normalized * range;
    }
}
