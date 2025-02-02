using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeSlime_AI : MonoBehaviour
{
    [SerializeField] Character character;
    [SerializeField] EnemyAtkTrigger atkTrigger;
    [SerializeField] Attack attack;
    [SerializeField] float atkDelay = 0.5f; // 공격 딜레이
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Sprite[] spriteList = new Sprite[2];

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => character.initialFinish);

        // 공격 콜라이더에 공격력 반영
        attack.attack_power = character.powerNow;

        // 공격 콜라이더 끄기
        attack.atkColl.enabled = false;
        // 공격 비활성화
        attack.gameObject.SetActive(false);

        // 콜백에 공격 함수 넣기
        if (atkTrigger.attackAction == null)
            atkTrigger.attackAction += Attack;
    }

    private void Update()
    {
        // // 공격 트리거 활성화 및 idle 상태일때
        // if (atkTrigger.atkTrigger && character.nowState == Character.State.Idle)
        // {
        //     // 가시공격 실행
        //     StartCoroutine(ThornAttack());
        // }

        // // 공격중일때
        // if (character.nowState == Character.State.Attack)
        //     // 이동 멈추기
        //     character.rigid.velocity = Vector3.zero;
    }

    void Attack()
    {
        // 공격 액션으로 전환
        character.nowState = CharacterState.Attack;
        // 공격 쿨타임 갱신
        character.atkCoolCount = character.cooltimeNow;

        StartCoroutine(ThornAttack());
    }

    IEnumerator ThornAttack()
    {
        // 이동 멈추기
        character.rigid.velocity = Vector3.zero;

        // 찡그린 표정으로 변하기
        sprite.sprite = spriteList[1];

        // 공격 활성화
        attack.gameObject.SetActive(true);

        // 공격 준비 사운드 재생
        SoundManager.Instance.PlaySound("LifeSlime_AttackReady", transform.position);

        // 공격 준비시간 대기
        yield return new WaitForSeconds(atkDelay);

        // 공격 사운드 재생
        SoundManager.Instance.PlaySound("LifeSlime_Attack", transform.position);

        // 공격 콜라이더 켜기
        attack.atkColl.enabled = true;

        // 트리거 설정을 껐다켜서 콜라이더 활성화
        attack.atkColl.isTrigger = false;
        attack.atkColl.isTrigger = true;

        // 공격 중 대기
        yield return new WaitForSeconds(1f);

        // 공격 콜라이더 끄기
        attack.atkColl.enabled = false;

        // 표정 원복
        sprite.sprite = spriteList[0];

        // 공격 후딜레이 대기
        yield return new WaitForSeconds(2f);

        // idle 상태로 변경
        character.nowState = CharacterState.Idle;
    }
}
