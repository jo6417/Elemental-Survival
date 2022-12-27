using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteInEditMode]
public class SimpleSlider : MonoBehaviour
{
    float nowFillAmount; // 값 저장
    [Range(0f, 1f)] public float fillAmount; // 채울 값 설정
    [SerializeField] SlicedFilledImage fill_Image; // 슬라이더 채우기 이미지
    [SerializeField] RectTransform handleArea;
    [SerializeField] RectTransform handle;
    enum FillType { Right, Left, Up, Down };
    [SerializeField] FillType fillType;
    public UnityEvent callbackEvent;

    private void OnGUI()
    {
        // 값 바뀌면 갱신
        if (fillAmount != nowFillAmount)
        {
            // 값 갱신
            nowFillAmount = fillAmount;

            // 슬라이더 값 반영
            if (fill_Image)
                fill_Image.fillAmount = fillAmount;

            float fillPosX = handleArea.rect.width * fillAmount;
            float fillPosY = handleArea.rect.height * fillAmount;

            // 핸들 앵커 설정
            switch (fillType)
            {
                case FillType.Right:
                    // 앵커 설정
                    handle.SetAnchor(AnchorPresets.MiddleLeft);

                    // 핸들 위치 시키기
                    handle.anchoredPosition = new Vector2(fillPosX, 0);

                    // 채우는 방향 설정
                    fill_Image.fillDirection = SlicedFilledImage.FillDirection.Right;

                    break;
                case FillType.Left:
                    // 앵커 설정
                    handle.SetAnchor(AnchorPresets.MiddleRight);

                    // 핸들 위치 시키기
                    handle.anchoredPosition = new Vector2(-fillPosX, 0);

                    // 채우는 방향 설정
                    fill_Image.fillDirection = SlicedFilledImage.FillDirection.Left;

                    break;
                case FillType.Down:
                    // 앵커 설정
                    handle.SetAnchor(AnchorPresets.TopCenter);

                    // 핸들 위치 시키기
                    handle.anchoredPosition = new Vector2(0, -fillPosY);

                    // 채우는 방향 설정
                    fill_Image.fillDirection = SlicedFilledImage.FillDirection.Down;

                    break;
                case FillType.Up:
                    // 앵커 설정
                    handle.SetAnchor(AnchorPresets.BottonCenter);

                    // 핸들 위치 시키기
                    handle.anchoredPosition = new Vector2(0, fillPosY);

                    // 채우는 방향 설정
                    fill_Image.fillDirection = SlicedFilledImage.FillDirection.Up;

                    break;
            }

            //todo 콜백 실행
            callbackEvent.Invoke();
        }
    }
}
