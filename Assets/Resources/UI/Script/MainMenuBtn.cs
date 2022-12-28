using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuBtn : MonoBehaviour
{
    public GameObject buttonParent;
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
        //todo 첫번째 버튼 기억
        UICursor.Instance.UpdateLastSelect(buttonParent.transform.GetChild(0).GetComponent<Button>());

        // 시간 속도 초기화
        Time.timeScale = 1f;

        yield return null;
    }

    public void CharacterSelect()
    {
        // 캐릭터 선택 UI 토글
        characterSelectUI.SetActive(!characterSelectUI.activeSelf);
    }

    public void Play()
    {
        //todo 메인메뉴 배경음 정지
        SoundManager.Instance.nowBGM.Stop();

        // 로딩하고 인게임 씬 띄우기
        StartCoroutine(SystemManager.Instance.LoadScene("InGameScene"));
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
        // 옵션 UI 토글
        optionUI.SetActive(!optionUI.activeSelf);
    }

    public void Quit()
    {
        // 게임 종료
        print("QuitGame");
        Application.Quit();
    }
}
