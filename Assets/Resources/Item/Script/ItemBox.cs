using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBox : Character
{
    [Header("Refer")]
    HitBox hitBox;
    [SerializeField] Sprite[] boxSpriteList = new Sprite[2];
    [SerializeField] SpriteRenderer boxSprite;
    [SerializeField] List<ItemInfo> dropList = new List<ItemInfo>();
    [SerializeField] Collider2D coll;

    [Header("State")]
    [SerializeField] float boxHp = 5f; // 박스 체력 기본값
    int randomType; // 아이템 종류 (마법,샤드,아티팩트)
    public ItemInfo slotInfo;
    [SerializeField, ReadOnly] string productName;
    [SerializeField] List<float> randomRate = new List<float>(); // 개별 확률 적용 아이템

    protected override void Awake()
    {
        base.Awake();

        // 콜라이더 찾기
        coll = coll != null ? coll : GetComponent<Collider2D>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 콜라이더 끄기
        coll.enabled = false;

        // 닫힌 상자 스프라이트로 초기화
        boxSprite.sprite = boxSpriteList[0];

        // 외곽선 색 초기화
        boxSprite.material.SetColor("_OutLineColor", Color.white);

        // 마법,아이템 DB 모두 로딩 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone && ItemDB.Instance.loadDone);

        // 각각 아이템 개별 확률 적용
        randomRate.Add(40); // 원소젬 확률 가중치
        randomRate.Add(20); // 회복 아이템 확률 가중치
        randomRate.Add(20); // 자석 빔 확률 가중치
        randomRate.Add(10); // 슬롯머신 확률 가중치
        randomRate.Add(5); // 트럭 버튼 확률 가중치

        // null 이 아닌 상품이 뽑힐때까지 반복
        while (slotInfo == null)
        {
            // 아이템 종류 뽑기
            randomType = SystemManager.Instance.WeightRandom(randomRate);

            // 해당 상품 내에서 랜덤 id
            switch (randomType)
            {
                // 원소젬일때
                case 0:
                    // 랜덤 원소젬 뽑기
                    slotInfo = new ItemInfo(ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Gem));
                    break;
                // 회복 아이템일때
                case 1:
                    slotInfo = new ItemInfo(ItemDB.Instance.GetItemByName("Heart"));
                    break;
                // 자석빔일때
                case 2:
                    slotInfo = new ItemInfo(ItemDB.Instance.GetItemByName("Magnet"));
                    break;
                // 슬롯머신일때
                case 3:
                    slotInfo = new ItemInfo(ItemDB.Instance.GetItemByName("SlotMachine"));
                    break;
                // 트럭 버튼일때
                case 4:
                    slotInfo = new ItemInfo(ItemDB.Instance.GetItemByName("TruckButton"));
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

        // 드랍 아이템 넣기
        nowHasItem.Clear();
        // 드랍 개수 1개로 초기화
        slotInfo.amount = 1;
        nowHasItem.Add(slotInfo);

        // 생성된 박스를 리스트에 포함
        WorldSpawner.Instance.itemBoxList.Add(gameObject);

        // 죽을때 리스트에서 삭제 콜백 넣기
        hitCallback += RemoveList;

        // 캐릭터 초기화 완료
        initialStart = true;
        initialFinish = true;

        // 콜라이더 켜기
        // coll.enabled = true;
    }

    // private void OnTriggerExit2D(Collider2D other)
    // {
    //     // 스폰 콜라이더 밖으로 나갔을때
    //     if (other.CompareTag("Respawn"))
    //     {
    //         // 스폰 테두리 랜덤 위치로 이동
    //         transform.position = WorldSpawner.Instance.BorderRandPos();
    //     }
    // }

    void RemoveList()
    {
        // 죽을때
        if (hpNow <= 0)
        {
            // 스폰 리스트에서 해당 아이템 삭제
            WorldSpawner.Instance.itemBoxList.Remove(gameObject);

            // 상자 오픈 사운드 재생
            SoundManager.Instance.PlaySound("BoxOpen", transform.position, 0, 0.8f);
        }
    }
}
