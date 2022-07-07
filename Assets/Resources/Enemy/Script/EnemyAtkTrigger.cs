using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAtkTrigger : MonoBehaviour
{
    public GameObject explosionPrefab;
    public SpriteRenderer atkRangeSprite;
    public EnemyManager enemyManager;

    public bool atkTrigger; //범위내 플레이어 들어왔는지 여부

    private void Awake()
    {
        //공격 범위 인디케이터 스프라이트 찾기
        atkRangeSprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        //폭발 이펙트 있을때
        if (explosionPrefab)
        {
            //폭발 콜라이더 및 이펙트 사이즈 동기화
            explosionPrefab.transform.localScale = transform.localScale;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 자폭 트리거가 되는 태그
        string triggerObj = "Player";

        // 고스트 여부에 따라 자폭 트리거 태그 바꾸기
        if (enemyManager.isGhost)
            triggerObj = "Enemy";
        else
            triggerObj = "Player";

        // 목표 대상이 범위 내에 들어왔을때
        if (other.CompareTag(triggerObj))
        {
            atkTrigger = true;

            // 자폭형 몬스터일때
            if (enemyManager && enemyManager.selfExplosion && !enemyManager.isDead)
            {
                // 자폭하기
                StartCoroutine(enemyManager.Dead());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        //플레이어가 범위 밖으로 나갔을때
        if (other.CompareTag("Player"))
        {
            atkTrigger = false;
        }
    }
}
