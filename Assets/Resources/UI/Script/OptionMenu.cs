using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.SceneManagement;

public class OptionMenu : MonoBehaviour
{
    [Header("State")]
    [ReadOnly, SerializeField] GameObject nowOption; // 현재 켜져있는 옵션 패널
    [SerializeField] Slider materVolume; // 마스터 볼륨
    [SerializeField] Slider bgmVolume; // 배경음 볼륨
    [SerializeField] Slider sfxVolume; // 효과음 볼륨
    [SerializeField] Slider uiVolume; // UI 볼륨
    public enum VolumeType { Master, BGM, SFX, UI };

    [Header("Refer")]
    private NewInput Option_Input;
    [SerializeField] private CanvasGroup pauseGroup; // 일시정지 메뉴 캔버스그룹
    [SerializeField] private MainMenuBtn mainMenu; // 메인메뉴 컴포넌트

    [Header("Option Menu")]
    [SerializeField] private GameObject optionSelectPanel; // 옵션 버튼 패널
    [SerializeField] private GameObject audioOptionPanel; // 오디오 설정 패널
    [SerializeField] private GameObject graphicOptionPanel; // 그래픽 설정 패널
    [SerializeField] private GameObject keyBindOptionPanel; // 키설정 설정 패널

    [Header("Audio Option")]
    [SerializeField] private Selectable optionSelect_FirstSelect; // 옵션 첫번째 Selectable
    [SerializeField] private Selectable audio_FirstSelect; // 오디오 첫번째 Selectable
    [SerializeField] private Selectable graphic_FirstSelect; // 그래픽 첫번째 Selectable

    [Header("Graphic Option")]
    public TMP_Dropdown screenModeDropdown; // 화면 모드 드롭다운 메뉴
    public Slider brightnessSlider; // 밝기 슬라이더
    public Toggle showDamageToggle; // 데미지 표시 여부 토글 버튼

    private void Awake()
    {
        //입력 초기화
        StartCoroutine(InputInit());

        // 화면 모드 드롭다운에 함수 넣기
        screenModeDropdown.onValueChanged.AddListener(delegate
        {
            SetScreenMode(screenModeDropdown.value);
        });

        // 밝기 슬라이더에 함수 넣기
        brightnessSlider.onValueChanged.AddListener(delegate
        {
            // 밝기 설정값 변수 바꾸기
            SystemManager.Instance.OptionBrightness = brightnessSlider.value;

            // 인게임일때
            if (SceneManager.GetActiveScene().name == SceneName.InGameScene.ToString())
                // 글로벌 라이트 값에 적용
                SystemManager.Instance.globalLight.intensity = SystemManager.Instance.GlobalBrightness * SystemManager.Instance.OptionBrightness;
            // 메인메뉴일때
            else
            {
                // 글로벌 라이트 찾기
                Light2D globalLight = null;
                foreach (Light2D light in FindObjectsOfType<Light2D>())
                    if (light.lightType == Light2D.LightType.Global)
                        globalLight = light;

                // 해당 글로벌 라이트에 밝기 옵션값 적용
                globalLight.intensity = SystemManager.Instance.OptionBrightness;
            }
        });

        // 데미지 표시 여부 토글에 함수 넣기
        showDamageToggle.onValueChanged.AddListener(delegate
        {
            // 데미지 표시 여부 변수 바꾸기
            SystemManager.Instance.showDamage = showDamageToggle.isOn;
        });
    }

    IEnumerator InputInit()
    {
        yield return null;
        // null 이 아닐때까지 대기
        // yield return new WaitUntil(() => UIManager.Instance.UI_Input != null);

        Option_Input = new NewInput();

        // 취소 입력
        Option_Input.UI.Cancel.performed += val =>
        {
            // 옵션 선택 패널 켜져있을때
            if (optionSelectPanel.activeInHierarchy)
                BackToPause();
            // 옵션 선택 패널 꺼져있을때
            else
            {
                if (nowOption != null)
                    BackToOption();
            }
        };

        // 입력 활성화
        Option_Input.Enable();
    }

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // UI 매니저 있을때
        if (UIManager.Instance)
            // 현재 열려있는 팝업 갱신
            UIManager.Instance.nowOpenPopup = gameObject;

        // 옵션 버튼 패널 열기
        optionSelectPanel.SetActive(true);

        // 마지막 선택 UI 갱신
        UICursor.Instance.UpdateLastSelect(optionSelect_FirstSelect);

        // 나머지 옵션 패널 모두 닫기
        audioOptionPanel.SetActive(false);
        graphicOptionPanel.SetActive(false);
        // keyBindOptionPanel.SetActive(false);

        // UIManager 초기화 대기
        yield return new WaitUntil(() => UIManager.Instance != null);

        // 일시정지 메뉴에서 pauseGroup 찾기
        if (pauseGroup == null) pauseGroup = UIManager.Instance.pausePanel.GetComponent<CanvasGroup>();
    }

    private void OnDestroy()
    {
        if (Option_Input != null)
        {
            Option_Input.Disable();
            Option_Input.Dispose();
        }
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
            case (int)VolumeType.UI:
                SoundManager.Instance.Set_UIVolume(uiVolume.value);
                break;
        }
    }

    public void OpenPanel(GameObject openPanel)
    {
        // UI 커서 끄기
        UICursor.Instance.UICursorToggle(false);

        // 현재 켜진 패널 갱신
        nowOption = openPanel;

        // 해당 옵션 패널 열기
        openPanel.SetActive(true);

        // 옵션 버튼 패널 끄기
        optionSelectPanel.SetActive(false);

        // 오디오 옵션 열었을때
        if (openPanel == audioOptionPanel)
        {
            // 마지막 선택 UI 갱신
            UICursor.Instance.UpdateLastSelect(audio_FirstSelect);

            // 볼륨값 모두 불러와 표시
            materVolume.value = SoundManager.Instance.masterVolume;
            bgmVolume.value = SoundManager.Instance.musicVolume;
            sfxVolume.value = SoundManager.Instance.sfxVolume;
            uiVolume.value = SoundManager.Instance.uiVolume;

            //todo 배경음 재생
        }

        // 그래픽 옵션 열었을때
        if (openPanel == graphicOptionPanel)
        {
            // 마지막 선택 UI 갱신
            UICursor.Instance.UpdateLastSelect(graphic_FirstSelect);

            // 화면모드 갱신
            screenModeDropdown.value = GetScreenMode(SystemManager.Instance.screenMode);
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

        // 마지막 선택 UI 갱신
        UICursor.Instance.UpdateLastSelect(optionSelect_FirstSelect);

        // 옵션값 세이브
        StartCoroutine(SaveManager.Instance.Save());
    }

    public void BackToPause()
    {
        // 인게임일때
        if (SceneManager.GetActiveScene().name == SceneName.InGameScene.ToString())
        {
            // 현재 열려있는 팝업 갱신
            UIManager.Instance.nowOpenPopup = UIManager.Instance.pausePanel;

            // 일시정지 메뉴 켜기
            // UIManager.Instance.pausePanel.SetActive(true);
            pauseGroup.alpha = 1f;

            // 옵션 메뉴 끄기
            UIManager.Instance.optionPanel.SetActive(false);
        }
        // 메인메뉴일때
        else
        {
            // 메인 메뉴 켜기
            mainMenu.BackToMenu();

            // 옵션 메뉴 끄기
            gameObject.SetActive(false);
        }
    }

    public void SetScreenMode(int _fullscreenMode)
    {
        switch (_fullscreenMode)
        {
            case 0:
                // 화면모드 전환
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                // 화면모드 전환
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case 2:
                // 화면모드 전환
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
        }
    }

    public int GetScreenMode(FullScreenMode _screenMode)
    {
        switch (_screenMode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                return 0;
            case FullScreenMode.Windowed:
                return 1;
            case FullScreenMode.FullScreenWindow:
                return 2;
            default:
                return 1;
        }
    }
}
