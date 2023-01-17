using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LightningSlime_AI : MonoBehaviour
{
    [SerializeField] Character character;
    [SerializeField] EnemyAtkTrigger meleeTrigger;
    [SerializeField] Attack attack;
    [SerializeField] Animator bodyAnim; // 몸체 애니메이터
    [SerializeField] Animator eyeBlink; // 깜빡이는 눈
    [SerializeField] GameObject eyeFrown; // 찡그린 눈

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => character.initialFinish);

        // 공격 콜라이더에 공격력 반영
        attack.fixedPower = character.powerNow;
    }

    private void Update()
    {
        // 공격 트리거 활성화 및 공격 꺼져있을때
        if (meleeTrigger.atkTrigger && !attack.gameObject.activeInHierarchy)
        {
            // 가시공격 실행
            StartCoroutine(ElectroAtk());
        }
    }

    IEnumerator ElectroAtk()
    {
        // 공격 상태로 변경
        character.nowState = Character.State.Attack;

        // 몸체 애니메이터 끄기
        bodyAnim.enabled = false;
        // 몸체 크기 초기화
        transform.localScale = Vector2.one;

        // 기본 눈 끄기
        eyeBlink.gameObject.SetActive(false);

        // 찡그린 눈 켜기
        eyeFrown.SetActive(true);

        // 양쪽으로 진동
        transform.DOPunchPosition(Vector2.right * 0.1f, 1f);

        //todo 공격 준비 사운드 재생

        // 공격 딜레이 대기
        yield return new WaitForSeconds(1f);

        // 기본 눈 켜기
        eyeBlink.gameObject.SetActive(true);

        // 공격 콜라이더 끄기
        attack.atkColl.enabled = false;

        // 공격 활성화
        attack.gameObject.SetActive(true);

        // 공격시 눈 애니메이션 켜기
        eyeBlink.SetBool("Attack", true);

        // 공격 딜레이 대기
        yield return new WaitForSeconds(0.2f);

        //todo 공격 사운드 재생

        // 공격 콜라이더 켜기
        attack.atkColl.enabled = true;

        // 몸 진동하기
        transform.DOPunchPosition(Vector2.one * 0.1f, 1f);

        // 공격 중 대기
        yield return new WaitForSeconds(1f);

        // 공격 비활성화
        attack.gameObject.SetActive(false);

        // 찡그린 눈 끄기
        eyeFrown.SetActive(false);

        // 일반 눈 애니메이션으로 복귀        
        eyeBlink.SetBool("Attack", false);

        // 몸체 애니메이터 끄기
        bodyAnim.enabled = true;

        // 공격 후딜레이 대기
        yield return new WaitForSeconds(1f);

        // idle 상태로 변경
        character.nowState = Character.State.Idle;
    }
}
