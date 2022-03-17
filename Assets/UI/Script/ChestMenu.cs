using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ChestMenu : MonoBehaviour
{
    [Header("Refer")]
    public Image chestCover;
    public Sprite chestCover_Close;
    public Sprite chestCover_Open;
    
    public Image chestBody;
    public Sprite chestBody_Close;
    public Sprite chestBody_Open;
    public Button openBtn;

    // public GameObject prize; //상품 이미지

    public int[] test;

    [Header("Rate")]
    List<float> chestRate = new List<float>(); //랜덤 아이템을 모두 넣은 리스트
    public float slotMachineRate; //슬롯머신 확률 가중치
    public float vendMachineRate; //자판기 확률 가중치

    private void OnEnable() {
        //상자 스프라이트 초기화
        chestCover.sprite = chestCover_Close;
        chestBody.sprite = chestBody_Close;
        //버튼 상호작용 초기화
        openBtn.interactable = true;

        //상자 안에서 올라올 상품 이미지 초기화
        // prize.SetActive(false);
    }

    private void Start() {

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
    public void OpenChest(){
        //버튼 상호작용 비활성화
        openBtn.interactable = false;

        //상자 스프라이트 교체
        chestCover.sprite = chestCover_Open;
        chestBody.sprite = chestBody_Open;

        //TODO 확률에따라 자판기, 슬롯머신 상자안에서 올라오기
        //TODO 자판기, 슬롯머신 메뉴 띄우기
        
        StartCoroutine(RisePrize());
    }

    IEnumerator RisePrize(){
        GameObject popupUI = null;

        //상품 랜덤 선택
        int index = RandomPick();

        //! test 0으로 고정됨!
        index = 1;
        
        switch (index)
        {
            case 0:
            popupUI = UIManager.Instance.vendMachineUI;
            break;

            case 1:
            popupUI = UIManager.Instance.slotMachineUI;
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

    int RandomPick(){        

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
            else{
                randomNum -= chestRate[i];
            }
        }

        //랜덤 숫자가 1일때 마지막값 반환
        return chestRate.Count - 1;
    }
}
