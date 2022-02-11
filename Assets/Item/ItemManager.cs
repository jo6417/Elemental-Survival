using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class ItemManager : MonoBehaviour
{
    public Item item;
    GameObject player;
    Collider2D col;
    Rigidbody2D rigid;

    private void Start()
    {
        player = PlayerManager.Instance.gameObject;
        rigid = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 아이템과 충돌 했을때
        if (other.CompareTag("Player"))
        {
            // 플레이어에게 날아가기
            StartCoroutine(AbsorbItem());
        }
    }

    IEnumerator AbsorbItem()
    {
        // 아이템 위치부터 플레이어 쪽으로 방향 벡터
        Vector2 dir = player.transform.position - transform.position;

        // 플레이어 반대 방향으로 날아가기
        rigid.velocity = -dir.normalized * 10f;

        yield return new WaitForSeconds(0.3f);

        // 플레이어 방향으로 날아가기, 아이템 사라질때까지
        while (gameObject)
        {
            dir = player.transform.position - transform.position; //방향 다시 계산

            // 플레이어 이동 속도보다 빠르게 따라오기
            rigid.velocity = dir.normalized * PlayerManager.Instance.moveSpeed * 1.2f;

            yield return null;

            if (dir.magnitude <= 0.5f)
            {
                GetItem();
                break;
            }
        }
    }

    void GetItem()
    {
        //아이템이 젬 타입일때
        if (item.itemType == Item.ItemType.Gem)
        {
            //플레이어 소지 젬 갯수 올리기
            PlayerManager.Instance.AddGem(item);
        }

        //아이템 속도 초기화
        rigid.velocity = Vector2.zero;

        //아이템 비활성화
        LeanPool.Despawn(transform);
    }
}
