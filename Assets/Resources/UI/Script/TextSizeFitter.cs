using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class TextSizeFitter : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;
    [SerializeField] TextMeshProUGUI textMesh;
    [SerializeField] bool widthFitter = false;
    [SerializeField] bool heightFitter = false;
    [SerializeField] Vector2 padding; // 여백    
    [SerializeField] Vector2 maxSize; // 최대 사이즈
    [SerializeField] Vector2 minSize; // 최소 사이즈

    private void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (textMesh == null) textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void OnGUI()
    {
        InputText(null);
    }

    public void InputText(string text)
    {
        if (text != null)
            // 텍스트 갱신
            textMesh.text = text;

        // 기존의 사이즈로 초기화
        float width = rectTransform.sizeDelta.x;
        float height = rectTransform.sizeDelta.y;

        // 텍스트의 너비와 높이를 계산
        if (widthFitter)
            // 사이즈 최소,최대 범위 적용
            width = Mathf.Clamp(textMesh.preferredWidth, minSize.x, maxSize.x == 0 ? float.PositiveInfinity : maxSize.x);
        if (heightFitter)
            // 사이즈 최소,최대 범위 적용
            height = Mathf.Clamp(textMesh.preferredHeight, minSize.y, maxSize.y == 0 ? float.PositiveInfinity : maxSize.y);

        // RectTransform의 크기를 조정합니다.
        rectTransform.sizeDelta = new Vector2(width, height) + padding;
    }
}
