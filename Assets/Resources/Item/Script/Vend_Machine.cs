using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vend_Machine : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] Canvas uiCanvas; // 가격, 상호작용키 안내 UI 캔버스
    [SerializeField] Interacter interacter;
    [SerializeField] GameObject showKey; //상호작용 키 표시 UI
    [SerializeField] private Transform itemDropper; //상품 토출구 오브젝트


    List<SlotInfo> productList = new List<SlotInfo>(); // 판매 상품 리스트
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
        yield return new WaitUntil(() => MagicDB.Instance.loadDone && ItemDB.Instance.loadDone);

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
        randomWeight.Add(10); // 하트 가중치
        randomWeight.Add(0); //todo 아티팩트 가중치
        randomWeight.Add(40); // 마법샤드 가중치
        randomWeight.Add(40); // 마법 가중치

        for (int i = 0; i < 9; i++)
        {
            int randomPick = SystemManager.Instance.WeightRandom(randomWeight);

            switch (randomPick)
            {
                // 하트일때
                case 0:
                    productList.Add(ItemDB.Instance.GetItemByName("Heart"));
                    break;
                // 아티팩트일때
                case 1:
                    productList.Add(ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Artifact));
                    break;
                // 마법 샤드일때
                case 2:
                    productList.Add(ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Shard));
                    break;
                // 마법일때
                case 3:
                    productList.Add(MagicDB.Instance.GetRandomMagic());
                    break;
            }

            // 판매중 여부 초기화
            soldOutList[i] = false;
        }
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

        // 드롭퍼 오브젝트 넣어주기
        VendMachineUI.Instance.itemDropper = itemDropper;

        // 상품 목록 전달
        VendMachineUI.Instance.productList = productList;
        VendMachineUI.Instance.soldOutList = soldOutList;

        // 자판기 UI 끄기
        UIManager.Instance.PopupUI(UIManager.Instance.vendMachinePanel);
    }
}
