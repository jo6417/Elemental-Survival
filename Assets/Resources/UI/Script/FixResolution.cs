using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixResolution : MonoBehaviour
{
    [SerializeField] Vector2 resolution = new Vector2(16f, 9f);
    [SerializeField] Camera mainCamera;
    [SerializeField] Rect rect;
    [SerializeField] Vector2 screenSize; // 현재 스크린 사이즈

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        rect = mainCamera.rect;
    }

    void Update()
    {
        // 화면 사이즈가 바뀌었을때
        if (screenSize.x != Screen.width || screenSize.y != Screen.height)
        {
            // 스크린 사이즈 갱신
            screenSize = new Vector2(Screen.width, Screen.height);

            float scaleheight = ((float)Screen.width / Screen.height) / ((float)resolution.x / resolution.y); // (가로 / 세로)
            float scalewidth = 1f / scaleheight;
            if (scaleheight < 1)
            {
                rect.height = scaleheight;
                rect.y = (1f - scaleheight) / 2f;
            }
            else
            {
                rect.width = scalewidth;
                rect.x = (1f - scalewidth) / 2f;
            }
            mainCamera.rect = rect;
        }
    }

    // 남는 공간 레터박스로 검은색 칠하기
    private void OnPreCull()
    {
        Debug.Log("OnPreCull");
        Rect newRect = new Rect(0, 0, 1, 1);
        mainCamera.rect = newRect;
        GL.Clear(true, true, Color.black);
        mainCamera.rect = rect;
    }
}
