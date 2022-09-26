using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBox : Character
{
    [Header("Refer")]
    [SerializeField] Sprite[] boxSpriteList = new Sprite[2];
    [SerializeField] SpriteRenderer boxSprite;
    [SerializeField] List<ItemInfo> dropList = new List<ItemInfo>();
    Collider2D coll;

    [Header("State")]
    [SerializeField] float boxHp = 5f; // 박스 체력 기본값
    int randomType; // 아이템 종류 (마법,샤드,아티팩트)
    public SlotInfo slotInfo;
    [SerializeField, ReadOnly] string productName;

    private void Awake()
    {
        // 콜라이더 찾기
        coll = coll != null ? coll : GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 닫힌 상자 스프라이트로 초기화
        boxSprite.sprite = boxSpriteList[0];

        // 마법,아이템 DB 모두 로딩 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone && ItemDB.Instance.loadDone);

        // null 이 아닌 상품이 뽑힐때까지 반복
        while (slotInfo == null)
        {
            // 아이템 종류 뽑기 (회복템, 자석, )
            randomType = Random.Range(0, 2);
            randomType = 0;

            // 해당 상품 내에서 랜덤 id
            switch (randomType)
            {
                // 회복 아이템일때
                case 0:
                    slotInfo = ItemDB.Instance.GetItemByName("Life Mushroom");
                    break;
                // 자석빔일때
                case 1:

                    break;
            }

            yield return null;
        }

        // 해당 상품 이름 확인
        productName = slotInfo.name;

        // 드랍 아이템 확정 될때까지 대기
        yield return new WaitUntil(() => slotInfo != null);

        // 박스 체력 초기화
        hpMax = boxHp;
        hpNow = hpMax;

        //todo 드랍 아이템 선정 (회복템, 자석... 중에서 랜덤)
        // 버섯 아이템으로 테스트
        ItemInfo dropItem = ItemDB.Instance.itemDB.Find(x => x.itemType == ItemDB.ItemType.Heal.ToString());
        // 드랍 개수 1개로 초기화
        dropItem.amount = 1;

        // 드랍 아이템 넣기
        nowHasItem.Clear();
        nowHasItem.Add(dropItem);

        // 캐릭터 초기화 완료
        initialStart = true;
        initialFinish = true;
    }
}
