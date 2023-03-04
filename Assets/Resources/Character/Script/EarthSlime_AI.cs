using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class EarthSlime_AI : MonoBehaviour
{
    public Character character;
    public EnemyAI enemyAI;
    public Collider2D smashColl;
    [SerializeField] GameObject attackEffect;

    private void Awake()
    {
        // 점프 시작 콜백 넣기
        enemyAI.jumpStartAction += SmashColliderOn;
        // 점프 끝 콜백 넣기
        enemyAI.jumpEndAction += SmashColliderOff;
    }

    private void OnEnable()
    {
        // 스매쉬 콜라이더 비활성화
        smashColl.gameObject.SetActive(false);
    }

    void Attack()
    {
        // 공격 액션으로 전환
        character.nowState = CharacterState.Attack;
        // 공격 쿨타임 갱신
        character.atkCoolCount = character.cooltimeNow;

        StartCoroutine(StompAttack());
    }

    public IEnumerator StompAttack()
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

        yield return null;
    }

    public void SmashColliderOn()
    {
        // 스매쉬 콜라이더 활성화
        smashColl.gameObject.SetActive(true);
    }

    public void SmashColliderOff()
    {
        // 스매쉬 콜라이더 비활성화
        smashColl.gameObject.SetActive(false);

        // 착지 이펙트 소환
        LeanPool.Spawn(attackEffect, transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

        // Idle로 전환
        character.nowState = CharacterState.Idle;
    }
}
