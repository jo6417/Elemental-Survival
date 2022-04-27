using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject firstBtn;
    private Button lastSelected; //마지막 선택되었던 버튼

    private void OnEnable() {
        // 버튼 선택 기본값
        EventSystem.current.SetSelectedGameObject(firstBtn);
        lastSelected = firstBtn.GetComponent<Button>();
    }

    private void Update()
    {
        //선택된 대상을 저장하기
        // SelectSave();
    }

    // void SelectSave()
    // {
    //     // 아무 대상이나 선택 되었을때 preSelectedObj 에 저장
    //     if (Input.anyKey)
    //     {
    //         //선택된 대상이 있을때
    //         if (EventSystem.current.currentSelectedGameObject != null)
    //         {
    //             // 선택된 대상을 preSelectedObj 에 넣기
    //             if (EventSystem.current.currentSelectedGameObject.TryGetComponent(out Button btn))
    //                 lastSelected = btn;
    //         }
    //         //선택된 대상이 null 일때
    //         else
    //         {
    //             //방향키 인풋 들어오면 preSelectedObj를 선택
    //             float horizonInput = Input.GetAxisRaw("Horizontal");
    //             float verticalInput = Input.GetAxisRaw("Vertical");
    //             if (horizonInput != 0 || verticalInput != 0)
    //                 lastSelected.Select();
    //         }
    //     }
    // }
}
