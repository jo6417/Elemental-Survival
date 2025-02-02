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

    [Header("State")]
    [SerializeField] float boxHp = 5f; // 박스 체력 기본값
    int randomType; // 아이템 종류 (마법,샤드,아티팩트)
    public ItemInfo slotInfo;
    [SerializeField, ReadOnly] string productName;
    [SerializeField] List<float> randomRate = new List<float>(); // 개별 확률 적용 아이템

    protected override void Awake()
    {
        // Character 의 Awake 코드 실행
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 히트 콜라이더 모두 끄기
        foreach (Collider2D hitColl in hitBoxList[0].hitColls)
            hitColl.enabled = false;

        // 닫힌 상자 스프라이트로 초기화
        boxSprite.sprite = boxSpriteList[0];
        // 외곽선 색 초기화
        boxSprite.material.SetColor("_OutLineColor", Color.white);

        // 마법,아이템 DB 모두 로딩 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.initDone && ItemDB.Instance.initDone);
        // 캐릭터 초기화 대기
        yield return new WaitUntil(() => initialFinish);

        // 각각 아이템 개별 확률 적용
        randomRate.Add(40); // 원소젬 확률 가중치
        randomRate.Add(20); // 회복 아이템 확률 가중치
        randomRate.Add(20); // 자석 빔 확률 가중치
        randomRate.Add(10); // 슬롯머신 확률 가중치
        randomRate.Add(5); // 트럭 버튼 확률 가중치

        // 슬롯 정보 초기화
        slotInfo = null;

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

        // 박스 체력 초기화
        characterStat.hpMax = boxHp;
        characterStat.hpNow = characterStat.hpMax;

        // 드랍 아이템 초기화
        nowHasItem.Clear();
        // 드랍 개수 1개로 초기화
        slotInfo.amount = 1;
        // 드랍 아이템 넣기
        nowHasItem.Add(slotInfo);

        // 생성된 박스를 리스트에 포함
        WorldSpawner.Instance.itemBoxList.Add(gameObject);

        // 죽을때 리스트에서 삭제 콜백 넣기
        deadCallback += RemoveList;

        // 캐릭터 초기화 완료
        initialStart = true;
        initialFinish = true;

        // 히트 콜라이더 모두 켜기
        foreach (Collider2D hitColl in hitBoxList[0].hitColls)
            hitColl.enabled = true;
    }

    private void Update()
    {
        //히트 카운트 감소
        if (hitDelayCount > 0)
            hitDelayCount -= Time.deltaTime;
    }

    void RemoveList(Character character)
    {
        // 스폰 리스트에서 해당 박스 삭제
        WorldSpawner.Instance.itemBoxList.Remove(gameObject);

        // 상자 오픈 사운드 재생
        SoundManager.Instance.PlaySound("BoxOpen", transform.position, 0);
    }
}
