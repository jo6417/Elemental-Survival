using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Refer")]
    public Selectable firstBtn;

    private void OnEnable()
    {
        // 버튼 선택 기본값
        firstBtn.Select();

        //마우스 고정해제
        Cursor.lockState = CursorLockMode.None;
    }

    public void OpenOptionMenu()
    {
        // 옵션 메뉴 켜기
        UIManager.Instance.optionPanel.SetActive(true);

        // 일시정지 메뉴 끄기
        gameObject.SetActive(false);
    }
}
