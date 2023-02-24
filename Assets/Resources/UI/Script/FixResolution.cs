using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// [ExecuteInEditMode]
public class FixResolution : MonoBehaviour
{
    [SerializeField] Vector2 resolution;
    [SerializeField] Camera mainCamera;
    [SerializeField] Rect rect;
    [SerializeField] Vector2 screenSize; // 현재 스크린 사이즈
    [SerializeField] Vector2 windowedScreenSize; // 창모드일때 스크린 사이즈
    [SerializeField] RectTransform[] horizon_letterBoxes = new RectTransform[2];
    [SerializeField] RectTransform[] vertical_letterBoxes = new RectTransform[2];

    private void Start()
    {
        rect = mainCamera.rect;
        resolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
    }

    private void Update()
    {
        // 해상도 및 화면모드 확인
        ResolutionCheck();
    }

    void ResolutionCheck()
    {
        if (SystemManager.Instance == null)
            return;

        // 창모드일때, 화면 사이즈가 바뀌었을때
        if (!Screen.fullScreen && (windowedScreenSize.x != Screen.width || windowedScreenSize.y != Screen.height))
        {
            // 창모드 해상도 갱신
            windowedScreenSize = new Vector2(Screen.width, Screen.height);

            // 해상도 변경 및 빈공간에 레터박스 넣기
            ChangeResolution(SystemManager.Instance.isFullscreen);

            return;
        }

        // 전체화면 여부 바뀌었을때
        if (SystemManager.Instance.isFullscreen != Screen.fullScreen)
        {
            // 화면모드 갱신
            SystemManager.Instance.isFullscreen = Screen.fullScreen;

            // 해상도 변경 및 빈공간에 레터박스 넣기
            ChangeResolution(SystemManager.Instance.isFullscreen, true);

            return;
        }
    }

    public void ChangeResolution(bool _isFullscreen, bool changeMode = false)
    {
        // 현재 스크린 사이즈 불러오기
        screenSize = _isFullscreen ? resolution : new Vector2(Screen.width, Screen.height);

        // 전체화면으로 전환
        if (_isFullscreen)
        {
            // 전체화면으로 강제 변경시
            if (changeMode)
                // 전체화면 해상도로 변경
                Screen.SetResolution((int)resolution.x, (int)resolution.y, _isFullscreen);
        }
        // 창모드로 전환
        else
        {
            // 창모드로 강제 변경시
            if (changeMode)
            {
                // 해상도 불러오기
                windowedScreenSize = SystemManager.Instance.lastResolution;
                // 마지막 창모드 해상도로 변경
                Screen.SetResolution((int)windowedScreenSize.x, (int)windowedScreenSize.y, _isFullscreen);
            }
            // 창모드에서 사이즈 바꿀때
            else
                // 해상도 저장
                SystemManager.Instance.lastResolution = windowedScreenSize;
        }

        float scaleheight = ((float)screenSize.x / screenSize.y) / ((float)resolution.x / resolution.y); // (가로 / 세로)
        float scalewidth = 1f / scaleheight;
        scaleheight = Mathf.Clamp01(scaleheight);
        scalewidth = Mathf.Clamp01(scalewidth);

        rect.height = scaleheight;
        rect.y = (1f - scaleheight) / 2f;
        rect.width = scalewidth;
        rect.x = (1f - scalewidth) / 2f;

        mainCamera.rect = rect;

        // 세로 레터박스 끄기
        for (int i = 0; i < vertical_letterBoxes.Length; i++)
            vertical_letterBoxes[i].gameObject.SetActive(false);
        // 가로 레터박스 끄기
        for (int i = 0; i < horizon_letterBoxes.Length; i++)
            horizon_letterBoxes[i].gameObject.SetActive(false);

        // 위아래 레터박스 사이즈 계산
        Vector2 letterScale;
        if (rect.x == 0)
        {
            letterScale = new Vector2(screenSize.x, (1f - scaleheight) * screenSize.y / 2f);

            // 가로 레터박스 사이즈로 이미지 스케일링
            for (int i = 0; i < horizon_letterBoxes.Length; i++)
            {
                horizon_letterBoxes[i].gameObject.SetActive(true);
                horizon_letterBoxes[i].sizeDelta = letterScale;
            }
        }
        else
        {
            letterScale = new Vector2((1f - scalewidth) * screenSize.x / 2f, screenSize.y);

            // 세로 레터박스 사이즈로 이미지 스케일링
            for (int i = 0; i < vertical_letterBoxes.Length; i++)
            {
                vertical_letterBoxes[i].gameObject.SetActive(true);
                vertical_letterBoxes[i].sizeDelta = letterScale;
            }
        }

        print(resolution + " : " + screenSize + " : " + _isFullscreen + " : " + changeMode + " : " + Time.unscaledTime);
    }
}
