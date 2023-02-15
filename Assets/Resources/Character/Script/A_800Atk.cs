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
    public EnemyAtkTrigger atkTrigger; // 해당 트리거에 타겟 들어오면 공격
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
        atkTrigger.transform.localScale = Vector2.one * attackRange;

        // 적 정보 들어오면 이름 표시
        enemyName = character.enemy.name;

        //공격 콜라이더 끄기
        meleeColl.gameObject.SetActive(false);

        // 콜백에 공격 함수 넣기
        if (atkTrigger.attackAction == null)
            atkTrigger.attackAction += Attack;
    }

    private void Update()
    {
        // 몬스터 정보 없으면 리턴
        if (character.enemy == null)
            return;

        // 이미 공격중이면 리턴
        if (character.nowState == Character.State.Attack)
        {
            //속도 멈추기
            character.rigid.velocity = Vector3.zero;
            return;
        }

        // 타겟 방향 계산
        if (character.TargetObj != null)
            targetDir = character.TargetObj.transform.position - transform.position;

        // // 공격 트리거 켜지면 공격 시작
        // if (meleeAtkTrigger.atkTrigger)
        //     StartCoroutine(MeleeAttack());
    }

    void Attack()
    {
        StartCoroutine(MeleeAttack());
    }

    IEnumerator MeleeAttack()
    {
        // print("Melee Attack");

        // 공격 액션으로 전환
        character.nowState = Character.State.Attack;
        // 공격 쿨타임 갱신
        character.atkCoolCount = character.cooltimeNow;

        //움직일 방향에따라 회전
        if (targetDir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        // 이동 멈추기
        character.rigid.velocity = Vector3.zero;

        // 공격 애니메이션 실행
        character.animList[0].SetTrigger("isAttack");

        // 공격 켤때까지 대기
        yield return new WaitUntil(() => meleeColl.gameObject.activeSelf);
        // 후딜레이 대기
        yield return new WaitForSeconds(1f);

        // Idle 상태로 초기화
        character.nowState = Character.State.Idle;
    }

    public void OnMeleeEffect()
    {
        // 공격 이펙트 켜기
        meleeColl.gameObject.SetActive(true);
    }
}
