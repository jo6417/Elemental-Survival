using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vend_Machine : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] Canvas uiCanvas; // 가격, 상호작용키 안내 UI 캔버스
    [SerializeField] Interacter interacter;
    [SerializeField] GameObject showKey; //상호작용 키 표시 UI
    public Transform itemDropper; //상품 토출구 오브젝트


    List<SlotInfo> productList = new List<SlotInfo>(); // 판매 상품 리스트
    float[] discountList = new float[9]; // 각 상품들의 할인율 배열
    bool[] soldOutList = new bool[9]; // 상품 품절 여부

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 캔버스 끄기
        uiCanvas.gameObject.SetActive(false);

        // 상호작용 키 UI 끄기
        showKey.SetActive(false);

        // 마법,아이템 DB 모두 로딩 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.initDone && ItemDB.Instance.initDone);

        // 상호작용 트리거 함수 콜백에 연결 시키기
        if (interacter.interactTriggerCallback == null)
            interacter.interactTriggerCallback += InteractTrigger;
        // 상호작용 함수 콜백에 연결 시키기
        if (interacter.interactSubmitCallback == null)
            interacter.interactSubmitCallback += InteractSubmit;

        // 캔버스 켜기
        uiCanvas.gameObject.SetActive(true);

        // 상품 목록 초기화
        productList.Clear();

        // 랜덤 뽑기 가중치 리스트
        List<float> randomWeight = new List<float>();
        randomWeight.Add(10); // 회복 아이템
        randomWeight.Add(5); // 가젯
        randomWeight.Add(40); // 마법샤드
        randomWeight.Add(40); // 마법

        for (int i = 0; i < 9; i++)
        {
            int randomPick = SystemManager.Instance.WeightRandom(randomWeight);

            SlotInfo slot = null;

            switch (randomPick)
            {
                // 회복 아이템
                case 0:
                    slot = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Heal);
                    break;
                // 가젯
                case 1:
                    slot = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Gadget);
                    break;
                // 마법 샤드
                case 2:
                    slot = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Shard);
                    break;
                // 마법
                case 3:
                    slot = MagicDB.Instance.GetRandomMagic();
                    break;
            }

            // 아이템, 마법 중에 null이 아닌 정보 리스트에 넣기
            ItemInfo item = slot as ItemInfo;
            MagicInfo magic = slot as MagicInfo;
            if (item != null)
                productList.Add(new ItemInfo(item));
            else if (magic != null)
                productList.Add(new MagicInfo(magic));

            //todo 가격 확정하기
            // 원래 가격
            float originPrice = slot.price;
            // 할인율
            float discountRate = 0;

            // 확률에 따라 할인 적용
            if (Random.value < 0.7f)
                // 최소 가격 이상일때만 할인 적용
                if (originPrice > 10)
                    // 0% ~ 50% 사이의 할인율을 랜덤하게 계산하고, 5% 단위로 반올림하여 할인율 계산
                    discountRate = Mathf.Round(Random.Range(0f, 0.5f) / 0.05f) * 0.05f;

            // 배열에 가격 저장
            discountList[i] = discountRate;

            // 판매중 여부 초기화
            soldOutList[i] = false;
        }
    }

    private void OnDisable()
    {
        // 아이템 드롭 오브젝트 초기화
        itemDropper = null;
    }

    public void InteractTrigger(bool isClose)
    {
        // 상호작용 불가능하면 리턴
        if (!uiCanvas.gameObject.activeSelf)
            return;

        //todo 플레이어 상호작용 키가 어떤 키인지 표시
        // pressKey.text = 

        // 상호작용 가능 거리 접근했을때
        if (isClose)
            // 상호작용 키 UI 나타내기
            showKey.SetActive(true);
        else
            // 상호작용 키 UI 숨기기
            showKey.SetActive(false);
    }

    public void InteractSubmit(bool isPress = false)
    {
        // 상호작용 불가능하면 리턴
        if (!uiCanvas.gameObject.activeSelf)
            return;

        // 인디케이터 꺼져있으면 리턴
        if (!showKey.activeSelf)
            return;

        // 상호작용 버튼 뗐을때
        if (!isPress)
            return;

        // 드롭퍼 오브젝트 넣어주기, 없으면 해당 자판기 넣기
        VendMachineUI.Instance.itemDropper = itemDropper != null ? itemDropper : transform;

        // 상품 목록 전달
        VendMachineUI.Instance.productList = productList;
        // 할인율 목록 전달
        VendMachineUI.Instance.discountList = discountList;
        // 품절 목록 전달
        VendMachineUI.Instance.soldOutList = soldOutList;

        // 자판기 UI 끄기
        UIManager.Instance.PopupUI(UIManager.Instance.vendMachinePanel);
    }
}
