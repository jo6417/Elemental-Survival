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
    [SerializeField] RectTransform[] horizon_letterBoxes = new RectTransform[2];
    [SerializeField] RectTransform[] vertical_letterBoxes = new RectTransform[2];
    [SerializeField] TextMeshProUGUI resolutionText;

    private void Start()
    {
        rect = mainCamera.rect;
        resolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
    }

    private void Update()
    {
        // 화면 사이즈가 바뀌었을때
        if (screenSize.x != Screen.width || screenSize.y != Screen.height)
            // 해상도 변경 및 빈공간에 레터박스 넣기
            ChangeResolution();

        // 전체화면 여부 바뀌었을때
        if (SystemManager.Instance.isFullscreen != Screen.fullScreen)
            // 해상도 변경 및 빈공간에 레터박스 넣기
            ChangeResolution(true);
    }

    public void ChangeResolution(bool changeFullscreen = false)
    {
        // 전체화면 여부 갱신
        SystemManager.Instance.isFullscreen = Screen.fullScreen;
        // 해상도 갱신
        SystemManager.Instance.lastResolution = resolution;

        // 현재 스크린 사이즈 불러오기
        screenSize = new Vector2(Screen.width, Screen.height);

        // 창모드일때
        if (!SystemManager.Instance.isFullscreen)
        {
            if (changeFullscreen)
                // 마지막 창모드 해상도로 변경
                Screen.SetResolution((int)SystemManager.Instance.lastResolution.x, (int)SystemManager.Instance.lastResolution.y, SystemManager.Instance.isFullscreen);
            else
                // 해상도 저장
                SystemManager.Instance.lastResolution = screenSize;
        }
        else
        {
            if (changeFullscreen)
                // 전체화면 해상도로 변경
                Screen.SetResolution((int)resolution.x, (int)resolution.y, SystemManager.Instance.isFullscreen);
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

        if (resolutionText != null)
            resolutionText.text = SystemManager.Instance.lastResolution + "\n" + screenSize + "\n" + SystemManager.Instance.isFullscreen + "\n" + Time.unscaledTime;
    }
}
