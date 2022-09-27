using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class EnemyAttack : Attack
{
    [Header("State")]
    bool initDone = false;
    Vector3 targetDir;
    public float activeAngleOffset; // 액티브 공격 오브젝트 방향 오프셋
    bool attackReady; //공격 준비중
    public bool defaultCollOn = true; //항상 콜라이더 켜기 옵션
    public float cooltime; // 주기적 자동 공격일때 쿨타임
    [SerializeField, ReadOnly] private float coolCount; // 주기적 자동 공격일때 현재 쿨타임 카운트

    [Header("Refer")]
    public Character character;
    public string enemyName;
    public Collider2D atkColl; //공격 콜라이더
    public GameObject dashEffect;
    public GameObject rangeObj; //공격시 활성화할 오브젝트

    private void Awake()
    {
        character = character == null ? GetComponentInChildren<Character>() : character;
        atkColl = atkColl == null ? GetComponentInChildren<Collider2D>() : atkColl;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        initDone = false;

        //공격 콜라이더 끄기
        if (atkColl)
            atkColl.enabled = false;

        yield return new WaitUntil(() => character != null && character.enemy != null);

        // 대쉬 범위 초기화
        character.attackRange = character.enemy.range;

        // 적 정보 들어오면 이름 표시
        enemyName = character.enemy.enemyName;

        // 대쉬 이펙트 있으면 끄기
        if (dashEffect != null)
            dashEffect.SetActive(false);

        // 공격 오브젝트 있으면 끄기
        if (rangeObj != null)
            rangeObj.SetActive(false);

        //공격 준비 해제
        attackReady = false;

        // 콜라이더 항상 켜기일때
        if (defaultCollOn)
            atkColl.enabled = true;

        initDone = true;
    }

    private void Update()
    {
        //공격 오브젝트 아무것도 없으면 리턴
        if (rangeObj == null && dashEffect == null)
            return;

        // 초기화 안됬으면 리턴
        if (!initDone)
            return;

        // 몬스터 매니저 비활성화 되었으면 리턴
        if (!character)
            return;

        // 공격 범위 0이하면 자동 공격 안한다는 뜻이므로 리턴
        if (character.attackRange <= 0)
            return;

        // 상태 이상 있으면
        if (!character.ManageState())
        {
            // 이상 있으면 공격 콜라이더 끄기
            atkColl.enabled = false;
            return;
        }

        // 공격 준비중이면 리턴
        if (attackReady)
            return;

        // 타겟 없거나 비활성화면 리턴
        if (!character.TargetObj || !character.TargetObj.activeSelf)
            return;

        // 타겟 방향 계산
        targetDir = character.TargetObj.transform.position - transform.position;

        // 공격 범위 안에 들어오면 공격 시작
        if (targetDir.magnitude <= character.attackRange)
        {
            //공격 준비로 전환
            attackReady = true;

            StartCoroutine(ChooseAttack());
        }
    }

    IEnumerator ChooseAttack()
    {
        //움직일 방향에따라 회전
        float leftAngle = character.lookLeft ? 180f : 0f;
        float rightAngle = character.lookLeft ? 0f : 180f;
        if (targetDir.x > 0)
            character.transform.rotation = Quaternion.Euler(0, leftAngle, 0);
        else
            character.transform.rotation = Quaternion.Euler(0, rightAngle, 0);

        // 이동 멈추기
        character.rigid.velocity = Vector3.zero;

        // 점프중이라면
        if (character.enemyAI && character.enemyAI.jumpCoolCount > 0)
        {
            // Idle 상태 될때까지 대기
            yield return new WaitUntil(() => character.nowAction == Character.Action.Idle);
        }

        // 쿨타임 있으면 주기적으로 켜기
        if (cooltime > 0)
        {
            CooltimeAttack();
            yield break;
        }
        // 액티브 공격 오브젝트 있으면 해당 공격 함수 실행
        else if (rangeObj != null)
        {
            StartCoroutine(RangeAttack());
            yield break;
        }
        // 돌진 이펙트 있으면 해당 공격 함수 실행
        else if (dashEffect != null)
        {
            StartCoroutine(DashAttack());
            yield break;
        }
    }

    public void CooltimeAttack()
    {
        // 쿨타임 중일때
        if (coolCount > 0)
        {
            // 공격 오브젝트 끄기
            rangeObj.SetActive(false);

            // 힐 쿨타임 차감
            coolCount -= Time.deltaTime;
        }
        // 쿨타임 끝났을때
        else
        {
            // 공격 오브젝트 켜기
            rangeObj.SetActive(true);

            // 힐 쿨타임을 몬스터 쿨타임으로 갱신
            coolCount = cooltime;
        }
    }

    public IEnumerator DashAttack()
    {
        // print("Dash Attack");
        // 초기화 완료까지 대기
        yield return new WaitUntil(() => initDone);

        // 공격 액션으로 전환
        character.nowAction = Character.Action.Attack;

        // 밀리지 않게 kinematic으로 전환
        // enemyManager.rigid.bodyType = RigidbodyType2D.Kinematic;

        //플레이어 방향 다시 계산
        targetDir = character.TargetObj.transform.position - transform.position;

        //움직일 방향에따라 회전
        float leftAngle = character.lookLeft ? 180f : 0f;
        float rightAngle = character.lookLeft ? 0f : 180f;
        if (targetDir.x > 0)
            character.transform.rotation = Quaternion.Euler(0, leftAngle, 0);
        else
            character.transform.rotation = Quaternion.Euler(0, rightAngle, 0);

        // 돌진 시작 인디케이터 켜기
        dashEffect.SetActive(true);

        // 타겟 방향 반대로 살짝 이동
        character.rigid.velocity = -targetDir.normalized * 3f;
        // enemyManager.transform.DOMove(transform.position - targetDir.normalized, 1f);
        yield return new WaitForSeconds(1f);

        // rigid 타입 전환
        character.rigid.bodyType = RigidbodyType2D.Dynamic;

        //공격 콜라이더 켜기
        atkColl.enabled = true;

        // 타겟 방향으로 돌진
        character.rigid.velocity = targetDir.normalized * 20f;
        // enemyManager.transform.DOMove(transform.position + targetDir.normalized * 5f, 0.5f);
        yield return new WaitForSeconds(0.5f);

        // 속도 멈추기
        character.rigid.velocity = Vector3.zero;

        //공격 콜라이더 끄기
        atkColl.enabled = false;

        // 타겟 위치 추적 시간 초기화
        character.targetResetCount = 0f;

        // 쿨타임만큼 대기후 초기화
        yield return new WaitForSeconds(character.cooltimeNow / character.enemy.cooltime);
        // Idle로 전환
        character.nowAction = Character.Action.Idle;

        //공격 준비 해제
        attackReady = false;
    }

    public IEnumerator RangeAttack()
    {
        // print("Active Attack");
        // 초기화 완료까지 대기
        yield return new WaitUntil(() => initDone);

        // 공격 액션으로 전환
        character.nowAction = Character.Action.Attack;

        // 공격 오브젝트 활성화
        rangeObj.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        // 공격 오브젝트 비활성화
        rangeObj.SetActive(false);

        // 쿨타임만큼 대기후 초기화
        yield return new WaitForSeconds(character.cooltimeNow / character.enemy.cooltime);
        // Idle로 전환
        character.nowAction = Character.Action.Idle;

        //공격 준비 해제
        attackReady = false;
    }

    public IEnumerator AttackNDisable()
    {
        // 초기화 완료까지 대기
        yield return new WaitUntil(() => initDone);

        // 공격 콜라이더 활성화
        atkColl.enabled = true;

        // 1프레임동안 대기
        yield return new WaitForSeconds(Time.deltaTime);

        // 공격 콜라이더 비활성화
        atkColl.enabled = false;

        gameObject.SetActive(false);
    }
}
