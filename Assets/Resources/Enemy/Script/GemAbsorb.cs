using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class GemAbsorb : MonoBehaviour
{
    public EnemyManager enemyManager;
    Collider2D coll;
    public float absorbSpeed = 1f; //흡수 속도
    public float getRange; //아이템 획득 범위

    private void OnTriggerStay2D(Collider2D other)
    {
        //아이템과 충돌 했을때
        if (other.CompareTag("Item"))
        {
            Rigidbody2D rigid = other.GetComponent<Rigidbody2D>();

            // 너무 가까우면 흡수해서 소지 아이템에 포함
            if (Vector2.Distance(transform.position, other.transform.position) <= getRange)
            {
                ItemInfo item = other.GetComponent<ItemManager>().item;

                //몬스터가 아이템 획득
                enemyManager.hasItemId.Add(item.id);

                //아이템 속도 초기화
                rigid.velocity = Vector2.zero;
                LeanPool.Despawn(other);

                return;
            }

            //아이템 움직일 방향
            Vector2 dir = transform.position - other.transform.position;

            // 가까이 끌어들이기
            rigid.AddForce(dir * Time.deltaTime * absorbSpeed);
        }
    }
}
