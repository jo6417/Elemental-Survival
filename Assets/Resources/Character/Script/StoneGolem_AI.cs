using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneGolem_AI : MonoBehaviour
{
    public Character character;
    public EnemyAtkTrigger smashTrigger;
    public Collider2D smashColl;

    private void OnEnable()
    {
        // 공격 트리거 비활성화
        smashTrigger.atkTrigger = false;

        // 스매쉬 콜라이더 비활성화
        smashColl.enabled = false;
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

        // 공격 트리거 콜라이더에 닿으면 공격
        if (smashTrigger.atkTrigger)
        {
            // 공격 액션으로 전환
            character.nowState = Character.State.Attack;

            StartCoroutine(SmashAttack());
        }
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

        // 쿨타임만큼 대기후 초기화
        yield return new WaitForSeconds(character.cooltimeNow / character.enemy.cooltime);
        // Idle로 전환
        character.nowState = Character.State.Idle;

        // 공격 트리거 끄기
        // smashTrigger.atkTrigger = false;
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
