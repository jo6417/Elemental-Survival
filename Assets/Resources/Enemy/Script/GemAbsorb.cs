using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

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
            ItemManager itemManager = other.GetComponent<ItemManager>();

            //해당 아이템 획득 여부 갱신, 중복 획득 방지
            itemManager.isCollision = true;

            // 자동 디스폰 중지, 색깔 초기화
            itemManager.sprite.DOKill();

            // 너무 가까우면 흡수해서 소지 아이템에 포함
            if (Vector2.Distance(transform.position, other.transform.position) <= getRange)
            {
                ItemInfo item = itemManager.item;

                if (item == null)
                {
                    print(other.name + " : " + other.transform.position);
                }

                //보유 아이템중 해당 아이템 있는지 찾기
                ItemInfo findItem = enemyManager.nowHasItem.Find(x => x.id == item.id);
                // 해당 아이템 보유하지 않았을때
                if (findItem == null)
                {
                    //개수 1개로 초기화
                    item.amount = 1;

                    //해당 아이템 획득
                    enemyManager.nowHasItem.Add(item);
                }
                // 해당 아이템 이미 보유했을때
                else
                {
                    // 보유한 아이템에 개수 증가
                    findItem.amount++;
                }

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
