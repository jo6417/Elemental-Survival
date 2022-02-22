using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class ItemManager : MonoBehaviour
{
    public ItemInfo item;
    public string itemName;
    GameObject player;
    Collider2D col;
    Rigidbody2D rigid;
    bool isGet = false; //플레이어가 획득했는지

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
            print("플레이어 아이템 획득");
            col.enabled = false; //이중 충돌 방지
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

        yield return new WaitForSeconds(0.2f);

        //속도 점점 빨라지게 추가 계수
        float addSpeed = 0.5f;

        // 플레이어 방향으로 날아가기, 아이템 사라질때까지
        while (!isGet)
        {
            //플레이어 방향
            dir = player.transform.position - transform.position;
            addSpeed += Time.deltaTime;
            addSpeed = Mathf.Clamp(addSpeed, 0, 1.2f);

            // 플레이어 이동 속도보다 빠르게 따라오기
            rigid.velocity = dir.normalized * PlayerManager.Instance.moveSpeed * addSpeed;

            yield return null;

            //거리가 0.5f 이하일때 획득
            if (dir.magnitude <= 0.5f)
            {
                isGet = true;

                PlayerManager.Instance.GetItem(item);
                //아이템 속도 초기화
                rigid.velocity = Vector2.zero;

                //아이템 비활성화
                LeanPool.Despawn(transform);
                break;
            }
        }
    }
}
