using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class ItemManager : MonoBehaviour
{
    public ItemInfo item;
    public string itemName;
    GameObject player;
    Collider2D col;
    Rigidbody2D rigid;
    bool isGet = false; //플레이어가 획득했는지

    private void OnEnable() {
        // 아이템 획득여부 초기화
        isGet = false;
        // 콜라이더 초기화
        if(col)
        col.enabled = true;
    }

    private void Start()
    {
        player = PlayerManager.Instance.gameObject;
        rigid = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        //프리팹 이름으로 아이템 정보 찾아 넣기
        item = ItemDB.Instance.GetItemByName(transform.name.Split('_')[0]);
        itemName = item.itemName;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌 했을때
        if (other.CompareTag("Player"))
        {
            // print("플레이어 아이템 획득");
            col.enabled = false; //이중 충돌 방지
            // 플레이어에게 날아가기
            StartCoroutine(GotoPlayer());
        }
    }

    IEnumerator GotoPlayer()
    {
        // 아이템 위치부터 플레이어 쪽으로 방향 벡터
        Vector2 dir = player.transform.position - transform.position;

        // 플레이어 반대 방향으로 날아가기
        rigid.DOMove((Vector2)player.transform.position - dir.normalized * 5f, 0.3f);

        yield return new WaitForSeconds(0.3f);

        float itemSpeed = 0.5f;

        // 플레이어 방향으로 날아가기, 아이템 사라질때까지
        while (!isGet)
        {
            itemSpeed -= Time.deltaTime;
            itemSpeed = Mathf.Clamp(itemSpeed, 0.1f, 1f);

            rigid.DOMove(player.transform.position, itemSpeed);

            //거리가 0.5f 이하일때 획득
            if (Vector2.Distance(player.transform.position, transform.position) <= 0.5f)
            {
                GetItem();
                break;
            }

            yield return null;
        }
    }

    void GetItem()
    {
        isGet = true;

        PlayerManager.Instance.GainItem(item);
        //아이템 속도 초기화
        rigid.velocity = Vector2.zero;

        //아이템 비활성화
        LeanPool.Despawn(transform);
    }
}
