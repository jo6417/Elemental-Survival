using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneGolem_AI : MonoBehaviour
{
    public Character character;
    public EnemyAtkTrigger atkTrigger;
    public Collider2D smashColl;

    private void OnEnable()
    {
        // 공격 트리거 비활성화
        atkTrigger.atkTrigger = false;

        // 스매쉬 콜라이더 비활성화
        smashColl.enabled = false;

        // 콜백에 공격 함수 넣기
        if (atkTrigger.attackAction == null)
            atkTrigger.attackAction += Attack;
    }

    private void Update()
    {
        // 몬스터 매니저 비활성화 되었으면 리턴
        if (!character)
            return;

        // 상태 이상 있으면 리턴
        if (!character.ManageState())
            return;

        // 타겟 없거나 비활성화면 리턴
        if (!character.TargetObj || !character.TargetObj.activeSelf)
            return;

        // 이미 공격중이면 리턴
        if (character.nowState == Character.State.Attack)
        {
            // 이동 멈추기
            character.rigid.velocity = Vector3.zero;
            return;
        }

        // // 공격 쿨타임됬을때, 공격 트리거 켜졌을때, idle 상태일때
        // if (character.atkCoolCount <= 0 && atkTrigger.atkTrigger && character.nowState == Character.State.Idle)
        //     StartCoroutine(SmashAttack());
    }

    void Attack()
    {
        // 공격 액션으로 전환
        character.nowState = Character.State.Attack;
        // 공격 쿨타임 갱신
        character.atkCoolCount = character.cooltimeNow;

        StartCoroutine(SmashAttack());
    }

    public IEnumerator SmashAttack()
    {
        // print("SmashAttack");

        // 타겟 방향 계산
        Vector2 targetDir = character.TargetObj.transform.position - transform.position;

        // 타겟 방향에 따라 회전
        if (targetDir.x > 0)
            character.transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            character.transform.rotation = Quaternion.Euler(0, 180, 0);

        // 공격 애니메이션 재생
        character.animList[0].SetTrigger("Attack");

        // 스매쉬 콜라이더 비활성화까지 대기
        yield return new WaitUntil(() => !smashColl.enabled);

        // Idle로 전환
        character.nowState = Character.State.Idle;
    }

    public void SmashColliderOn()
    {
        // 스매쉬 콜라이더 활성화
        smashColl.enabled = true;
    }

    public void SmashColliderOff()
    {
        // 스매쉬 콜라이더 비활성화
        smashColl.enabled = false;
    }
}
