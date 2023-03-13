using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magic_Machine : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] Canvas uiCanvas; // 가격, 상호작용키 안내 UI 캔버스
    [SerializeField] Interacter interacter;
    [SerializeField] GameObject showKey; //상호작용 키 표시 UI
    [SerializeField] private Transform itemDropper; //상품 토출구 오브젝트

    List<SlotInfo> productList = new List<SlotInfo>(); // 판매 상품 리스트

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

        // 랜덤 마법,샤드 뽑기
        productList.Clear();
        // 랜덤 뽑기 가중치 리스트
        List<float> randomWeight = new List<float>();
        randomWeight.Add(2); // 마법샤드 가중치
        randomWeight.Add(1); // 마법 가중치

        // 상품 목록 뽑기
        for (int i = 0; i < 15; i++)
        {
            SlotInfo slotInfo = null;

            // 나올때까지 뽑기 (단일 등급에 한해 unlockMagic에 없는 경우 다시 뽑기)
            while (slotInfo == null)
            {
                // 등급 뽑기 (가중치 반영)
                int targetGrade = SystemManager.Instance.WeightRandom(SystemManager.Instance.gradeWeight) + 1;
                // 상품 종류 뽑기
                int randomPick = SystemManager.Instance.WeightRandom(randomWeight);

                switch (randomPick)
                {
                    // 마법 샤드일때
                    case 0:
                        ItemInfo itemInfo = new ItemInfo(ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Shard, targetGrade));
                        if (itemInfo != null)
                            slotInfo = new ItemInfo(itemInfo);
                        break;
                    // 마법일때
                    case 1:
                        MagicInfo magicInfo = MagicDB.Instance.GetRandomMagic(targetGrade);
                        if (magicInfo != null)
                            slotInfo = new MagicInfo(magicInfo);
                        break;
                }
            }

            // 개수는 1개로 초기화
            slotInfo.amount = 1;

            // 리스트에 정보 저장
            productList.Add(slotInfo);
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
        MagicMachineUI.Instance.itemDropper = itemDropper != null ? itemDropper : transform;

        // 상품 리스트 참조 전달
        MagicMachineUI.Instance.productList = productList;

        // 매직머신 UI 띄우기
        UIManager.Instance.PopupUI(UIManager.Instance.magicMachinePanel);
    }
}
