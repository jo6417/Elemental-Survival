using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionMenu : MonoBehaviour
{
    [Header("State")]
    [ReadOnly, SerializeField] GameObject nowOption; // 현재 켜져있는 옵션 패널
    [SerializeField] Slider materVolume; // 마스터 볼륨
    [SerializeField] Slider bgmVolume; // 배경음 볼륨
    [SerializeField] Slider sfxVolume; // 효과음 볼륨
    public enum VolumeType { Master, BGM, SFX };

    [Header("Refer")]
    [SerializeField] private GameObject optionSelectPanel; // 옵션 버튼 패널
    [SerializeField] private GameObject audioOptionPanel; // 오디오 설정 패널
    [SerializeField] private GameObject graphicOptionPanel; // 그래픽 설정 패널
    // [SerializeField] private GameObject keyBindOptionPanel; // 키설정 설정 패널

    private void Awake()
    {
        //입력 초기화
        StartCoroutine(InputInit());
    }

    IEnumerator InputInit()
    {
        // null 이 아닐때까지 대기
        yield return new WaitUntil(() => UIManager.Instance.UI_Input != null);

        // 취소 입력
        UIManager.Instance.UI_Input.UI.Cancel.performed += val =>
        {
            // 옵션 선택 패널 켜져있을때
            if (optionSelectPanel.activeSelf)
                BackToPause();
            // 옵션 선택 패널 꺼져있을때
            else
                BackToOption();
        };
    }

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //todo 옵션 버튼 패널 열기
        optionSelectPanel.SetActive(true);

        // 나머지 패널 모두 닫기
        audioOptionPanel.SetActive(false);
        graphicOptionPanel.SetActive(false);
        // keyBindOptionPanel.SetActive(false);

        yield return null;
    }

    public void VolumeChage(int type)
    {
        switch (type)
        {
            case (int)VolumeType.Master:
                SoundManager.Instance.Set_MasterVolume(materVolume.value);
                break;
            case (int)VolumeType.BGM:
                SoundManager.Instance.Set_BGMVolume(bgmVolume.value);
                break;
            case (int)VolumeType.SFX:
                SoundManager.Instance.Set_SFXVolume(sfxVolume.value);
                break;
        }
    }

    public void OpenPanel(GameObject openPanel)
    {
        // UI 커서 끄기
        UIManager.Instance.UICursorToggle(false);

        // 현재 켜진 패널 갱신
        nowOption = openPanel;

        // 해당 옵션 패널 열기
        openPanel.SetActive(true);

        // 옵션 버튼 패널 끄기
        optionSelectPanel.SetActive(false);

        // 오디오 옵션 열었을때
        if (openPanel == audioOptionPanel)
        {
            // 볼륨값 모두 불러와 표시
            materVolume.value = SoundManager.Instance.masterVolume;
            bgmVolume.value = SoundManager.Instance.bgmVolume;
            sfxVolume.value = SoundManager.Instance.sfxVolume;
        }
    }

    public void BackToOption()
    {
        // 기존 패널 끄기
        nowOption.SetActive(false);

        // 켜진 패널 삭제
        nowOption = null;

        // 옵션 메뉴 켜기
        optionSelectPanel.SetActive(true);

        // 옵션값 세이브
        StartCoroutine(SaveManager.Instance.Save());
    }

    public void BackToPause()
    {
        // 일시정지 메뉴 켜기
        UIManager.Instance.pausePanel.SetActive(true);

        // 옵션 메뉴 끄기
        UIManager.Instance.optionPanel.SetActive(false);
    }
}
