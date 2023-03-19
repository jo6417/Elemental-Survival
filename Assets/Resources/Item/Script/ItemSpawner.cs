using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class TestItem
    {
        [SerializeField] public DBEnums.ItemDBEnum itemName;
        [SerializeField] public int maxAmount;
        [HideInInspector] public List<GameObject> spawnedItems = new List<GameObject>();
    }

    [SerializeField] float spawnDelay = 0.1f;
    [SerializeField] List<TestItem> itemPrefabList = new List<TestItem>();
    // private int currentAmount = 0;
    private float timer = 0f;

    private void OnEnable()
    {
        // foreach (TestItem item in itemPrefabList)
        // {
        //     for (int i = 0; i < item.maxAmount; i++)
        //     {
        //         SpawnItem(item);
        //     }
        // }
    }

    private void Update()
    {
        if (!ItemDB.Instance.initDone)
            return;

        for (int i = 0; i < itemPrefabList.Count; i++)
        {
            TestItem testItem = itemPrefabList[i];

            // 스폰된 아이템 비활성화 됬으면 리스트에서 제거
            for (int j = testItem.spawnedItems.Count - 1; j >= 0; j--)
            {
                if (!testItem.spawnedItems[j] || !testItem.spawnedItems[j].activeSelf)
                    testItem.spawnedItems.Remove(testItem.spawnedItems[j]);
            }

            // 현재 아이템이 Max보다 적다면
            if (testItem.spawnedItems.Count < testItem.maxAmount)
            {
                timer += Time.deltaTime;
                if (timer >= spawnDelay)
                {
                    SpawnItem(testItem);
                    timer = 0f;
                }
            }
        }
    }

    private void SpawnItem(TestItem item)
    {
        // 아이템 정보 찾기
        ItemInfo itemInfo = ItemDB.Instance.GetItemByName(item.itemName.ToString());
        // 개수 1개로 초기화
        itemInfo.amount = 1;

        GameObject testItem = ItemDrop(itemInfo, transform.position);

        item.spawnedItems.Add(testItem);
    }

    private GameObject ItemDrop(SlotInfo slotInfo, Vector2 dropPos, Vector2 dropDir = default)
    {
        // 인벤토리 빈칸 없으면 필드 드랍
        GameObject dropObj = null;

        MagicInfo magicInfo = slotInfo as MagicInfo;
        ItemInfo itemInfo = slotInfo as ItemInfo;

        // 마법일때
        if (magicInfo != null)
        {
            // 마법 슬롯 아이템 만들기
            dropObj = LeanPool.Spawn(ItemDB.Instance.magicItemPrefab, dropPos, Quaternion.identity, transform);

            // 아이템 프레임 색 넣기
            dropObj.transform.Find("Frame").GetComponent<SpriteRenderer>().color = MagicDB.Instance.GradeColor[slotInfo.grade];

            // 아이템 아이콘 넣기
            dropObj.transform.Find("Icon").GetComponent<SpriteRenderer>().sprite = MagicDB.Instance.GetIcon(slotInfo.id);
        }

        // 아이템일때
        if (itemInfo != null)
        {
            dropObj = LeanPool.Spawn(ItemDB.Instance.GetItemPrefab(itemInfo.id), dropPos, Quaternion.identity, transform);
        }

        // 아이템 정보 넣기
        if (dropObj.TryGetComponent(out ItemManager itemManager))
        {
            if (itemManager == null)
                return null;

            itemManager.itemInfo = slotInfo as ItemInfo;
            itemManager.magicInfo = slotInfo as MagicInfo;
        }

        // 아이템 정보 삭제
        slotInfo = null;

        // 아이템 콜라이더 찾기
        Collider2D itemColl = dropObj.GetComponent<Collider2D>();
        // 아이템 rigid 찾기
        Rigidbody2D itemRigid = dropObj.GetComponent<Rigidbody2D>();

        // 콜라이더 끄기
        itemColl.enabled = false;

        Vector3 itemDir;
        // 드롭 방향 지정 했을때
        if (dropDir != default)
            itemDir = dropDir;
        // 드롭 방향 지정 없을때 
        else
            // 플레이어 반대 방향
            itemDir = (dropPos - (Vector2)PlayerManager.Instance.transform.position).normalized;

        // 방향 벡터를 각도로 바꾸기
        float angle = Mathf.Atan2(itemDir.y, itemDir.x) * Mathf.Rad2Deg;
        // 각도 랜덤성 추가
        angle = angle + Random.Range(-45, 45);

        // 각도를 방향 벡터로 바꾸기
        itemDir = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle));
        // print(angle + ":" + itemDir);

        // 플레이어 반대 방향, 랜덤 파워로 아이템 날리기
        itemRigid.velocity = itemDir.normalized * Random.Range(10f, 30f);

        // 랜덤으로 방향 및 속도 결정
        float randomRotate = Random.Range(1f, 3f);
        // 아이템 랜덤 속도로 회전 시키기
        itemRigid.angularVelocity = randomRotate < 2f ? 90f * randomRotate : -90f * randomRotate;

        // 아이템 드롭 사운드 재생
        // SoundManager.Instance.PlaySound("ItemDrop");

        // 콜라이더 켜기
        if (itemColl)
            itemColl.enabled = true;

        return dropObj;
    }
}
