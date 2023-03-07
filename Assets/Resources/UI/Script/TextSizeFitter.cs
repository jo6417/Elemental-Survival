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
    [SerializeField] Vector2 padding;
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
            width = textMesh.preferredWidth;
        if (heightFitter)
            height = textMesh.preferredHeight;

        // RectTransform의 크기를 조정합니다.
        rectTransform.sizeDelta = new Vector2(width, height) + padding;
    }
}
