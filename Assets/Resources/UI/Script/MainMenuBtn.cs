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
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        // 기본값 선택 대상을 preSelectedObj 에 넣기
        if (EventSystem.current.firstSelectedGameObject.TryGetComponent(out Button btn))
            preSelectedObj = btn;

        yield return null;
    }

    private void Update()
    {
        //선택된 대상을 저장하기
        SelectSave();
    }

    void SelectSave()
    {
        // 아무 대상이나 선택 되었을때 preSelectedObj 에 저장
        if (Input.anyKey)
        {
            //선택된 대상이 있을때
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                // 선택된 대상을 preSelectedObj 에 넣기
                if (EventSystem.current.currentSelectedGameObject.TryGetComponent(out Button btn))
                    preSelectedObj = btn;
            }
            //선택된 대상이 null 일때
            else
            {
                //방향키 인풋 들어오면 preSelectedObj를 선택
                float horizonInput = Input.GetAxisRaw("Horizontal");
                float verticalInput = Input.GetAxisRaw("Vertical");
                if (horizonInput != 0 || verticalInput != 0)
                    preSelectedObj.Select();
            }
        }
    }

    public void CharacterSelect()
    {
        // 캐릭터 선택 UI 토글
        characterSelectUI.SetActive(!characterSelectUI.activeSelf);
    }

    public void Play()
    {
        // 인게임 씬 불러오기
        SceneManager.LoadScene("InGameScene", LoadSceneMode.Single);
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
