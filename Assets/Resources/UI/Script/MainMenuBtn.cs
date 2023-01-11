using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuBtn : MonoBehaviour
{
    public Selectable firstBtn;
    public GameObject buttonParent;
    public GameObject mainMenuPanel;
    public GameObject characterSelectUI;
    public GameObject shopUI;
    public GameObject collectionUI;
    public GameObject optionUI;

    private Button preSelectedObj;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 시간 속도 초기화
        SystemManager.Instance.TimeScaleChange(1f);

        // ui 커서 초기화까지 대기
        yield return new WaitUntil(() => UICursor.Instance != null);

        // 메인메뉴 패널 켜기        
        BackToMenu();

        yield return null;
    }

    public void CharacterSelect()
    {
        // 캐릭터 선택 UI 토글
        characterSelectUI.SetActive(!characterSelectUI.activeSelf);
    }

    public void Play()
    {
        // 버튼 Select 해제
        UICursor.Instance.UpdateLastSelect(null);

        // 메인메뉴 배경음 정지
        SoundManager.Instance.nowBGM.Stop();

        // 로딩하고 인게임 씬 띄우기
        SystemManager.Instance.StartGame();
    }

    public void Shop()
    {
        // 상점 UI 띄우기
        shopUI.SetActive(!shopUI.activeSelf);
    }

    public void Buy()
    {
        //TODO 상점 물건 구매
        print("를 구매");
    }

    public void Collection()
    {
        // 수집품 UI 띄우기
        collectionUI.SetActive(!collectionUI.activeSelf);
    }

    public void Option()
    {
        // 메인메뉴 끄기
        mainMenuPanel.SetActive(false);

        // 옵션 UI 켜기
        optionUI.SetActive(true);
    }

    public void Quit()
    {
        // 게임 종료
        print("QuitGame");
        Application.Quit();
    }

    public void BackToMenu()
    {
        // 메인메뉴 켜기
        mainMenuPanel.SetActive(true);

        // 첫번째 버튼 기억
        UICursor.Instance.UpdateLastSelect(firstBtn);

        // 다른 패널 끄기
        characterSelectUI.SetActive(false);
        shopUI.SetActive(false);
        collectionUI.SetActive(false);
        optionUI.SetActive(false);
    }
}
