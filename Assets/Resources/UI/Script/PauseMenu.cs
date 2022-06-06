using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public Selectable firstBtn;

    private void OnEnable()
    {
        // 버튼 선택 기본값
        firstBtn.Select();

        //마우스 고정해제
        Cursor.lockState = CursorLockMode.None;
    }
}
