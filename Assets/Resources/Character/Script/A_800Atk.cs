using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A_800Atk : MonoBehaviour
{
    public float attackRange;
    Vector3 targetDir;

    [Header("Refer")]
    public Character character;
    public string enemyName;
    public EnemyAtkTrigger meleeAtkTrigger; // 해당 트리거에 타겟 들어오면 공격
    public Collider2D meleeColl; // 공격 이펙트

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
        yield return new WaitUntil(() => character.enemy != null);

        // 공격 범위 초기화
        attackRange = character.enemy.range;

        // 근접 공격 범위에 반영
        meleeAtkTrigger.transform.localScale = Vector2.one * attackRange;

        // 적 정보 들어오면 이름 표시
        enemyName = character.enemy.name;

        //공격 콜라이더 끄기
        meleeColl.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 몬스터 정보 없으면 리턴
        if (character.enemy == null)
            return;

        // 이미 공격중이면 리턴
        if (character.nowAction == Character.Action.Attack)
        {
            //속도 멈추기
            character.rigid.velocity = Vector3.zero;
            return;
        }

        // 타겟 방향 계산
        if (character.TargetObj != null)
            targetDir = character.TargetObj.transform.position - transform.position;

        // 공격 트리거 켜지면 공격 시작
        if (meleeAtkTrigger.atkTrigger)
            StartCoroutine(MeleeAttack());
    }

    IEnumerator MeleeAttack()
    {
        // print("Melee Attack");

        // 공격 액션으로 전환
        character.nowAction = Character.Action.Attack;

        //움직일 방향에따라 회전
        if (targetDir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        // 이동 멈추기
        character.rigid.velocity = Vector3.zero;

        // 공격 애니메이션 실행
        character.animList[0].SetBool("isAttack", true);

        // 쿨타임만큼 대기
        yield return new WaitForSeconds(character.cooltimeNow / character.enemy.cooltime);

        // Idle 애니메이션으로 초기화
        character.animList[0].SetBool("isAttack", false);

        // 공격 트리거 끄기
        // meleeAtkTrigger.atkTrigger = false;

        // Idle 상태로 초기화
        character.nowAction = Character.Action.Idle;
    }

    public void OnMeleeEffect()
    {
        //공격 이펙트 켜기
        meleeColl.gameObject.SetActive(true);

        //플레이어 방향 계산
        targetDir = character.TargetObj.transform.position - transform.position;

        //플레이어 방향으로 회전
        float rotation = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
        meleeColl.transform.rotation = Quaternion.Euler(Vector3.forward * (rotation - 90f));
    }
}
