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
        // 마지막 선택 UI 갱신
        UICursor.Instance.UpdateLastSelect(firstBtn);
    }

    public void OpenOptionMenu()
    {
        // 옵션 메뉴 켜기
        UIManager.Instance.optionPanel.SetActive(true);

        // 일시정지 메뉴 끄기
        gameObject.SetActive(false);
    }

    public void QuitMainMenu()
    {
        SystemManager.Instance.QuitMainMenu();
    }
}
