using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SlotMachine : MonoBehaviour
{
    [Header("Refer")]
    public GameObject slotsParent;
    List<GameObject> slots = new List<GameObject>();
    public GameObject ledsParent;
    List<Image> leds = new List<Image>();
    public GameObject effectsParent;
    List<ParticleSystem> effects = new List<ParticleSystem>();
    public Button spinBtn; //슬롯머신 시작 버튼

    // int[] feverIndexes = new int[2]; // 피버찬스 인덱스 2가지
    int[] secondRandIndex = { 1, 3 }; // 1,3번 슬롯 중 하나 랜덤 선택 배열

    public float spinSpeed = 0.1f; //아이템이 한칸 이동하는 시간

    List<ItemInfo> prizes = new List<ItemInfo>(); //받게 될 아이템

    void Start()
    {
        // 각 슬롯의 led 이미지 추가하기
        leds.AddRange(ledsParent.GetComponentsInChildren<Image>());

        // 각 슬롯의 파티클 추가
        effects.AddRange(effectsParent.GetComponentsInChildren<ParticleSystem>());
    }

    private void OnEnable()
    {
        if (ItemDB.Instance.loadDone)
            SetSlot();
    }

    void SetSlot()
    {
        spinBtn.interactable = true; //버튼 활성화

        // 슬롯 오브젝트마다 리스트에 추가하기
        RectMask2D[] tempSlots = slotsParent.GetComponentsInChildren<RectMask2D>();
        slots.Clear();
        foreach (var slot in tempSlots)
        {
            slots.Add(slot.gameObject);
        }

        // 피버찬스 인덱스 2가지 넣기
        // for (int i = 0; i < feverIndexes.Length; i++)
        // {
        //     feverIndexes[i] = Random.Range(0, 5);
        // }
        // print(string.Join(" ,", feverIndexes));

        // 1,3번 슬롯 중 하나 랜덤 선택 배열
        int randSlotIndex = Random.Range(0, 2);

        for (int j = 0; j < slots.Count; j++)
        {
            //슬롯 한칸의 모든 아이템 오브젝트 리스트에 추가
            List<GameObject> items = new List<GameObject>();
            InfoHolder[] tempItems = slots[j].GetComponentsInChildren<InfoHolder>();
            foreach (var item in tempItems)
            {
                items.Add(item.gameObject);
            }

            //아이템 타입이 아티팩트인 모든아이템 리스트
            List<ItemInfo> itemList = ItemDB.Instance.itemDB.FindAll(x => x.itemType == "Artifact");
            // 중복 없는 랜덤 아이템 5개 뽑기
            int[] itemIDs = ItemDB.Instance.RandomItemIndex(items.Count);

            // 해당 슬롯에서 피버 아이템 선정
            int feverIndex = Random.Range(0, 5);

            // 아이템 슬롯마다 iteminfo, 아이콘 넣기
            for (int i = 0; i < items.Count; i++)
            {
                //아이템 찾기
                ItemInfo item = ItemDB.Instance.GetItemByID(itemIDs[i]);

                //툴팁 정보 넣기
                ToolTipTrigger tipTrigger = items[i].GetComponent<ToolTipTrigger>();
                tipTrigger.toolTipType = ToolTipTrigger.ToolTipType.ProductTip;
                tipTrigger.item = item;

                //아이템 정보 넣기
                InfoHolder infoHolder = items[i].GetComponent<InfoHolder>();
                infoHolder.holderType = InfoHolder.HolderType.itemHolder;
                infoHolder.id = itemIDs[i];

                //아이템 아이콘 넣기
                Sprite itemIcon = ItemDB.Instance.itemIcon.Find(x => x.name == item.itemName.Replace(" ", "") + "_Icon");

                //아이콘 못찾으면 넣을 기본 이미지
                if (itemIcon == null)
                    itemIcon = ItemDB.Instance.itemIcon.Find(x => x.name == "_ManaGem");

                items[i].transform.Find("Icon").GetComponent<Image>().sprite = itemIcon;


                // 피버찬스 표시 이미지 찾기
                Image image = items[i].transform.Find("FeverChance").GetComponent<Image>();
                Color tempColor = image.color;

                // 피버 없는 나머지 슬롯
                if (j != 2 && j != secondRandIndex[randSlotIndex])
                {
                    // print(j + " 나머지");
                    //피버찬스 없음
                    tempColor.a = 0;
                    image.color = tempColor;
                }
                // 2번째, (1 or 3)번째 슬롯일때
                else
                {
                    // print(j + " 피버");

                    //피버 찬스 인덱스면 알파값 255 아니면 0
                    tempColor.a = i == feverIndex ? 1 : 0;
                    image.color = tempColor;
                }
            }
        }

        // 피버찬스 LED 초기화
        for (int i = 0; i < leds.Count; i++)
        {
            if (i == 2)
            {
                leds[i].color = MagicDB.Instance.HexToRGBA("FFFFFF");
            }
            else
            {
                leds[i].color = MagicDB.Instance.HexToRGBA("646464");
            }
        }
    }

    public void ClickSpin()
    {
        spinBtn.interactable = false; //버튼 비활성화
        StartCoroutine(StartSpin());
    }

    IEnumerator StartSpin()
    {
        // 피버찬스 LED 초기화
        for (int i = 0; i < leds.Count; i++)
        {
            if (i == 2)
            {
                leds[i].color = MagicDB.Instance.HexToRGBA("FFFFFF");
            }
            else
            {
                leds[i].color = MagicDB.Instance.HexToRGBA("646464");
            }
        }

        //상품 리스트 초기화
        prizes.Clear();

        // 가운데 슬롯 돌리기
        //랜덤 숫자만큼 움직이기
        // int spinNum = Random.Range(5, 10); //! 테스트용으로 짧게
        int spinNum = Random.Range(20, 30);
        print("spinNum: " + spinNum);
        yield return StartCoroutine(SpinSlot(GetSlotItems(2), spinNum));

        //피버 찬스면 1,3번 돌리기
        if (CheckFever(2))
        {
            //피버 LED 켜기
            OnFeverLED(1);
            OnFeverLED(3);

            //랜덤 숫자만큼 움직이기
            spinNum = Random.Range(20, 30);
            print("spinNum: " + spinNum);
            StartCoroutine(SpinSlot(GetSlotItems(1), spinNum));
            yield return StartCoroutine(SpinSlot(GetSlotItems(3), spinNum));

            //피버 찬스면 0.4번 돌리기
            if (CheckFever(1) || CheckFever(3))
            {
                //피버 LED 켜기
                OnFeverLED(0);
                OnFeverLED(4);

                //랜덤 숫자만큼 움직이기
                spinNum = Random.Range(20, 30);
                print("spinNum: " + spinNum);
                StartCoroutine(SpinSlot(GetSlotItems(0), spinNum));
                yield return StartCoroutine(SpinSlot(GetSlotItems(4), spinNum));
            }
        }

        //LED 켜져있는 인덱스 찾기
        List<int> prizeIndex = new List<int>();
        for (int i = 0; i < leds.Count; i++)
        {
            if (leds[i].color == MagicDB.Instance.HexToRGBA("FFFFFF"))
            {
                //led 켜져있는 인덱스 넣기
                prizeIndex.Add(i);
            }
        }

        //LED 깜빡이기
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < prizeIndex.Count; j++)
            {
                leds[prizeIndex[j]].color = MagicDB.Instance.HexToRGBA("646464");
            }

            yield return new WaitForSecondsRealtime(0.2f);

            for (int j = 0; j < prizeIndex.Count; j++)
            {
                leds[prizeIndex[j]].color = MagicDB.Instance.HexToRGBA("FFFFFF");
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }

        //상품 획득 파티클
        for (int j = 0; j < prizeIndex.Count; j++)
        {
            effects[prizeIndex[j]].Stop();
            effects[prizeIndex[j]].Play();
        }
        //파티클 시간 대기
        yield return new WaitForSecondsRealtime(1f);

        //상품 리스트 확인
        string p = "";
        for (int i = 0; i < prizes.Count; i++)
        {
            p = p + ", " + prizes[i].itemName;

            // 상품 획득하기
            PlayerManager.Instance.GetItem(prizes[i]);
        }
        print(prizes.Count + " : " + p);

        //! 테스트 : 5개 나올때까지 돌리기
        // if (prizes.Count != 5)
        // {
        //     StartCoroutine(StartSpin());
        // }

        //팝업창 끄기
        UIManager.Instance.PopupUI(UIManager.Instance.slotMachinePanel);
    }

    List<GameObject> GetSlotItems(int slotIndex)
    {
        //슬롯 한칸의 모든 아이템 오브젝트 리스트에 추가
        List<GameObject> items = new List<GameObject>();
        Transform slot = slots[slotIndex].transform.GetChild(0);
        // print(slot.childCount);

        for (int i = 0; i < slot.childCount; i++)
        {
            items.Add(slot.GetChild(i).gameObject);
        }

        return items;
    }

    bool CheckFever(int slotIndex)
    {
        Image feverImg = GetSlotItems(slotIndex)[2].transform.Find("FeverChance").GetComponent<Image>();
        Color feverColor = feverImg.color;

        // 해당 오브젝트의 알파값이 1이면 피버찬스 아이템
        if (feverColor.a == 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void OnFeverLED(int ledIndex)
    {
        //해당 슬롯 피버 LED 켜기
        leds[ledIndex].color = MagicDB.Instance.HexToRGBA("FFFFFF");
    }

    IEnumerator SpinSlot(List<GameObject> items, int spinNum)
    {
        //한칸 움직이는 시간
        float _spinSpeed = spinSpeed;
        //시작,끝에 느려지기 시작하는 인덱스
        int slowNum = 5;

        //첫번째 아이템 위치
        Vector2 firstItemPos = items[0].GetComponent<RectTransform>().anchoredPosition;
        //아이템 하나의 높이
        float itemHeight = items[0].GetComponent<RectTransform>().rect.height;

        // 랜덤 횟수만큼 스핀하기
        for (int j = 0; j < spinNum; j++)
        {
            //처음,끝일때 ease 바꾸기
            Ease _ease;
            if (j == 0)
            {
                _ease = Ease.InBack;
                _spinSpeed = spinSpeed * (slowNum - j);
            }
            else if (j == spinNum - 1)
            {
                _ease = Ease.OutBack;
                _spinSpeed = spinSpeed * (slowNum + 1 - spinNum + j);
            }
            else
            {
                _ease = Ease.Linear;
                _spinSpeed = spinSpeed;
            }

            // 해당 아이템 배열 일정시간마다 아래로 domove 하기
            for (int i = 0; i < items.Count; i++)
            {
                // 마지막 인덱스의 아이템은 맨 위 아이템 위치로 이동 및 인덱스 순서 0번으로 바꾸기
                if (i == items.Count - 1)
                {
                    items[i].GetComponent<RectTransform>().anchoredPosition = firstItemPos;

                    //하이어라키 상의 인덱스 변경
                    items[i].transform.SetSiblingIndex(0);

                    // items 리스트 상의 인덱스 변경
                    items.Insert(0, items[i]);
                    items.RemoveAt(items.Count - 1);
                }
                else
                {
                    // 아이템 크기만큼 아래로 이동
                    items[i].GetComponent<RectTransform>().DOAnchorPos(
                        (Vector2)items[i].GetComponent<RectTransform>().anchoredPosition - new Vector2(0, itemHeight), _spinSpeed)
                        .SetUpdate(true)
                        .SetEase(_ease);
                }
            }

            //한칸 이동하는 시간동안 대기
            yield return new WaitForSecondsRealtime(_spinSpeed);
        }

        // 멈춘 후 prizes 배열에 아이템 넣기
        GameObject prize = items[2];
        ItemInfo item = ItemDB.Instance.GetItemByID(prize.GetComponent<InfoHolder>().id);
        prizes.Add(item);
    }
}
