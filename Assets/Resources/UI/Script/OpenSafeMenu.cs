using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class OpenSafeMenu : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] Sprite[] safeSprites = new Sprite[2];
    [SerializeField] Sprite[] doorSprites = new Sprite[2];
    [SerializeField] Image safe;
    [SerializeField] Image door;
    public Button openBtn;

    // public GameObject prize; //상품 이미지

    public int[] test;

    [Header("Rate")]
    List<float> chestRate = new List<float>(); //랜덤 아이템을 모두 넣은 리스트
    public float slotMachineRate; //슬롯머신 확률 가중치
    public float vendMachineRate; //자판기 확률 가중치

    private void OnEnable()
    {
        //상자 스프라이트 초기화
        safe.sprite = safeSprites[0];
        door.sprite = doorSprites[0];
        //버튼 상호작용 초기화
        openBtn.interactable = true;

        //버튼 선택
        openBtn.Select();

        //상자 안에서 올라올 상품 이미지 초기화
        // prize.SetActive(false);
    }

    private void Start()
    {
        // 가중치 넣기
        chestRate.Add(slotMachineRate);
        chestRate.Add(vendMachineRate);

        // 확률 테스트
        // test = new int[chestRate.Count];
        // for (int i = 0; i < 10000; i++)
        // {
        //     test[RandomPick()]++;
        // }
        // foreach (var t in test)
        // {
        //     print(t);
        // }
    }

    //상자 열기
    public void OpenChest()
    {
        // 버튼 상호작용 비활성화
        openBtn.interactable = false;

        // 상자 오픈 스프라이트 교체
        safe.sprite = safeSprites[1];
        door.sprite = doorSprites[1];

        // 아이템 뽑기
        StartCoroutine(GetPrize());
    }

    IEnumerator GetPrize()
    {
        //todo 아이템뽑기

        GameObject popupUI = null;

        //상품 랜덤 선택
        int index = RandomPick();

        //! test 0으로 고정됨!
        index = 0;

        switch (index)
        {
            case 0:
                popupUI = UIManager.Instance.vendMachinePanel;
                break;

            case 1:
                popupUI = UIManager.Instance.magicMachinePanel;
                break;
        }

        //상품 오브젝트 활성화 및 애니메이션 재생
        // prize.SetActive(true);

        // yield return new WaitForSecondsRealtime(0.5f); //상품 애니메이션 대기

        // prize.SetActive(false);

        //오브젝트 따라 팝업 띄우기
        UIManager.Instance.PopupUI(popupUI, true);

        yield return new WaitForSecondsRealtime(1f); //상품 애니메이션 대기

        //상자 팝업 닫기
        gameObject.SetActive(false);

        yield return null;
    }

    int RandomPick()
    {
        //아이템들의 확률 총합
        float totalRate = 0;
        foreach (var rate in chestRate)
        {
            totalRate += rate;
        }

        //랜덤 인덱스
        float randomNum = Random.value * totalRate;

        for (int i = 0; i < chestRate.Count; i++)
        {
            if (randomNum <= chestRate[i])
            {
                //랜덤 숫자가 가중치보다 작으면 해당 인덱스 반환
                return i;
            }
            else
            {
                randomNum -= chestRate[i];
            }
        }

        //랜덤 숫자가 1일때 마지막값 반환
        return chestRate.Count - 1;
    }
}
