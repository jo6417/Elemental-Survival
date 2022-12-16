using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeSlime_AI : MonoBehaviour
{
    [SerializeField] Character character;
    [SerializeField] EnemyAtkTrigger meleeTrigger;
    [SerializeField] Attack attack;
    [SerializeField] float atkDelay = 0.5f; // 공격 딜레이

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
            StartCoroutine(ThornAtk());
        }
    }

    IEnumerator ThornAtk()
    {
        // 공격 콜라이더 끄기
        attack.atkColl.enabled = false;

        // 공격 활성화
        attack.gameObject.SetActive(true);

        // 공격 딜레이 대기
        yield return new WaitForSeconds(atkDelay);

        // 공격 콜라이더 켜기
        attack.atkColl.enabled = true;
    }
}
