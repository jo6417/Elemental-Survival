using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class ItemManager : MonoBehaviour
{
    public bool isGot = false; //획득 됬는지 여부
    public ItemInfo item;
    [HideInInspector]
    public int amount = 1; //아이템 개수
    [HideInInspector]
    public bool isBundle; //합쳐진 아이템인지
    [HideInInspector]
    public int gemTypeIndex = -1;

    public string itemName;
    Collider2D coll;
    Rigidbody2D rigid;
    Vector3 velocity;
    bool isGet = false; //플레이어가 획득했는지
    private float radius = 5;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        velocity = rigid.velocity;
    }

    private void OnEnable()
    {
        //아이템 스폰할때마다 초기화
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        //아이템 정보 없을때 충돌 끄기
        coll.enabled = false;

        //아이템DB 로드 완료까지 대기
        yield return new WaitUntil(() => ItemDB.Instance.loadDone);

        // item 정보 불러올때까지 대기
        // yield return new WaitUntil(() => item != null);

        // item 정보 없으면 프리팹 이름으로 아이템 정보 찾아 넣기
        if (item == null)
        {
            item = ItemDB.Instance.GetItemByName(transform.name.Split('_')[0]);
        }
        itemName = item.itemName;
        // print(itemName + " : " + item.itemName);

        //지불 원소젬 이름을 인덱스로 반환
        gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.elementNames, x => x == item.priceType);

        // 아이템 획득여부 초기화
        isGet = false;
        // 사이즈 초기화
        transform.localScale = Vector2.one;
        //아이템 개수 초기화
        amount = 1;
        //아이템 번들 여부 초기화
        isBundle = false;

        // 콜라이더 초기화
        coll.enabled = true;

        // 획득 여부 초기화
        isGot = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 원소젬이고 리스폰 콜라이더 안에 들어왔을때
        if (item != null && item.itemType == "Gem" && other.CompareTag("Respawn") && !isBundle)
        {
            //해당 타입 원소젬 개수 -1
            if (ItemDB.Instance.outGemNum[gemTypeIndex] > 0)
                ItemDB.Instance.outGemNum[gemTypeIndex]--;

            //원소젬 리스트에서 빼기
            ItemDB.Instance.outGem.Remove(gameObject);
        }

        // 플레이어와 충돌 했을때
        if (other.CompareTag("Player"))
        {
            // print("플레이어 아이템 획득");

            //이중 충돌 방지
            coll.enabled = false;

            // 획득 여부 갱신, 중복 획득 방지
            isGot = true;

            // 플레이어에게 날아가기
            StartCoroutine(GetMove(PlayerManager.Instance.transform));
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 원소젬이고 리스폰 콜라이더 밖으로 나갔을때
        if (item != null && item.itemType == "Gem" && other.CompareTag("Respawn") && !isBundle)
        {
            // 같은 타입 원소젬 개수가 본인포함 10개 이상일때
            if (ItemDB.Instance.outGemNum[gemTypeIndex] >= 9)
            {
                print(item.itemName + " : " + ItemDB.Instance.outGemNum[gemTypeIndex]);

                // 해당 원소젬 사이즈 키우고 개수 늘리기
                transform.localScale = Vector2.one * 2;
                amount = 5;
                isBundle = true;

                //해당 타입 원소젬 개수 초기화
                ItemDB.Instance.outGemNum[gemTypeIndex] = 0;

                // 이름 같은 원소젬 찾아서 다 디스폰 시키기
                List<GameObject> despawnList = ItemDB.Instance.outGem.FindAll(x => x.name == transform.name);
                print(despawnList.Count);
                foreach (var gem in despawnList)
                {
                    ItemDB.Instance.outGem.Remove(gem);
                    LeanPool.Despawn(gem);
                }
            }
            else
            {
                //해당 타입 원소젬 개수 +1
                ItemDB.Instance.outGemNum[gemTypeIndex]++;
                //카메라 밖으로 나간 원소젬 리스트에 넣기
                ItemDB.Instance.outGem.Add(gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 원소젬 합체 범위 기즈모
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    IEnumerator GetMove(Transform Getter)
    {
        // 아이템 위치부터 플레이어 쪽으로 방향 벡터
        Vector2 dir = Getter.position - transform.position;

        // 플레이어 반대 방향으로 날아가기
        if (rigid)
            rigid.DOMove((Vector2)Getter.position - dir.normalized * 5f, 0.3f);

        yield return new WaitForSeconds(0.3f);

        float itemSpeed = 0.5f;

        // 플레이어 방향으로 날아가기, 아이템 사라질때까지 방향 갱신하며 반복
        while (!isGet || gameObject.activeSelf)
        {
            itemSpeed -= Time.deltaTime;
            itemSpeed = Mathf.Clamp(itemSpeed, 0.01f, 1f);

            if (gameObject.activeSelf && rigid)
                rigid.DOMove(Getter.position, itemSpeed);

            //거리가 0.5f 이하일때 획득
            if (Vector2.Distance(Getter.position, transform.position) <= 0.5f)
            {
                GainItem();
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    void GainItem()
    {
        isGet = true;

        // 아이템이 젬 타입일때
        if (item.itemType == "Gem")
        {
            //플레이어 소지 젬 갯수 올리기
            PlayerManager.Instance.AddGem(item, amount);
            // print(item.itemName + amount);
        }
        // 아이템이 힐 타입일때
        else if (item.itemType == "Heal")
        {
            PlayerManager.Instance.Damage(-amount);
        }
        else
        {
            //아이템 획득
            PlayerManager.Instance.GetItem(item);
        }

        //아이템 속도 초기화
        if (gameObject.activeSelf && rigid)
            rigid.DOKill();
        rigid.velocity = Vector2.zero;

        //아이템 비활성화
        LeanPool.Despawn(transform);
    }
}
