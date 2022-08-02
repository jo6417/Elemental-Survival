using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAtkTrigger : MonoBehaviour
{
    public GameObject explosionPrefab;
    public SpriteRenderer atkRangeBackground;
    public SpriteRenderer atkRangeFill;
    public EnemyManager enemyManager;

    public bool atkTrigger; //범위내 타겟 들어왔는지 여부

    private void Awake()
    {
        //공격 범위 인디케이터 스프라이트 찾기
        // atkRangeBackground = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (atkRangeBackground)
            atkRangeBackground.enabled = false;
        if (atkRangeFill)
            atkRangeFill.enabled = false;

        //폭발 이펙트 있을때
        if (explosionPrefab)
        {
            //폭발 콜라이더 및 이펙트 사이즈 동기화
            explosionPrefab.transform.localScale = transform.localScale;
        }

        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => enemyManager.enemy != null);

        // 고스트일때
        if (enemyManager.IsGhost)
        {
            // 플레이어가 공격하는 레이어
            gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
        }
        // 일반 몹일때
        else
        {
            // 몬스터가 공격하는 레이어
            gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 공격 트리거 켜진 상태면 리턴
        if (atkTrigger)
            return;

        // 플레이어가 충돌하면
        if (other.CompareTag(SystemManager.TagNameList.Player.ToString()))
        {
            atkTrigger = true;

            // 자폭형 몬스터일때
            if (enemyManager && enemyManager.selfExplosion && !enemyManager.isDead)
            {
                // 자폭하기
                StartCoroutine(enemyManager.hitBoxList[0].Dead());
            }
        }

        // 몬스터가 충돌하면
        if (other.CompareTag(SystemManager.TagNameList.Enemy.ToString()))
        {
            // 몬스터가 충돌했을때 히트박스 있을때
            if (other.TryGetComponent(out EnemyHitBox hitBox))
            {
                // 충돌 대상이 본인이면 리턴
                if (hitBox.enemyManager == enemyManager)
                    return;

                // 충돌 몬스터도 고스트일때 리턴
                if (hitBox.enemyManager.IsGhost)
                    return;
            }
            // 콜라이더가 히트박스를 갖고 있지 않을때 리턴
            else
                return;

            atkTrigger = true;

            // 자폭형 몬스터일때
            if (enemyManager && enemyManager.selfExplosion && !enemyManager.isDead)
            {
                // 자폭하기
                StartCoroutine(enemyManager.hitBoxList[0].Dead());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 공격 트리거 꺼진 상태면 리턴
        if (!atkTrigger)
            return;

        //  고스트 아닐때, 플레이어가 나가면
        if (!enemyManager.IsGhost && other.CompareTag(SystemManager.TagNameList.Player.ToString()))
            atkTrigger = false;

        // 고스트일때, 몬스터가 나가면
        if (enemyManager.IsGhost && other.CompareTag(SystemManager.TagNameList.Enemy.ToString()))
            atkTrigger = false;
    }
}
