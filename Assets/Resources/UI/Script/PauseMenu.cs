using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Refer")]
    public CanvasGroup pauseGroup;
    public Selectable firstBtn;

    private void OnEnable()
    {
        // 마지막 선택 UI 갱신
        UICursor.Instance.UpdateLastSelect(firstBtn);

        // 켤때 초기화
        PauseToggle(true);

        // 일시정지 메뉴 나타내기
        pauseGroup.alpha = 1f;
    }

    private void OnDisable()
    {
        // 켤때 초기화
        if (SoundManager.Instance)
            PauseToggle(false);
    }

    public void PauseToggle(bool pauseToggle)
    {
        // 브금 재개 상태 변경
        SoundManager.Instance.bgmPause = pauseToggle;

        if (pauseToggle)
            // 배경음 정지
            SoundManager.Instance.nowBGM.Pause();
        else
            // 배경음 재개
            SoundManager.Instance.nowBGM.Play();

        // 마우스 커서 전환
        UICursor.Instance.CursorChange(pauseToggle);
    }

    public void OpenOptionMenu()
    {
        // 옵션 메뉴 켜기
        UIManager.Instance.optionPanel.SetActive(true);

        // 일시정지 메뉴 숨기기
        pauseGroup.alpha = 0f;
    }

    public void QuitMainMenu()
    {
        SystemManager.Instance.QuitMainMenu();
    }
}
