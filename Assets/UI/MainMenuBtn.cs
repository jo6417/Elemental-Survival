using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBtn : MonoBehaviour
{
    public GameObject characterSelectUI;
    public GameObject shopUI;
    public GameObject collectionUI;
    public GameObject optionUI;

    public void CharacterSelect(){
        // 캐릭터 선택 UI 토글
        characterSelectUI.SetActive(!characterSelectUI.activeSelf);
    }

    public void Play(){
        // 인게임 씬 불러오기
        SceneManager.LoadScene("InGameScene", LoadSceneMode.Single);
    }

    public void Shop(){
        // 상점 UI 띄우기
        shopUI.SetActive(!shopUI.activeSelf);
    }

    public void Buy(){
        //TODO 상점 물건 구매
        print("를 구매");
    }

    public void Collection(){
        // 수집품 UI 띄우기
        collectionUI.SetActive(!collectionUI.activeSelf);
    }

    public void Option(){
        // 옵션 UI 토글
        optionUI.SetActive(!optionUI.activeSelf);
    }

    public void Quit(){
        // 게임 종료
        print("QuitGame");
        Application.Quit();
    }
}
