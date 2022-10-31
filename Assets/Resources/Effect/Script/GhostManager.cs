using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class GhostManager : MonoBehaviour
{
    public SpriteRenderer sprite;
    public float fadeTime;
    public Color ghostColor;

    private void Awake()
    {
        ghostColor = sprite.color;
    }

    private void OnEnable()
    {
        // 스프라이트 활성화
        sprite.enabled = true;

        // 처음 색에서 알파값만 낮추기
        Color fadeColor = sprite.color;
        fadeColor.a = 0f;

        // 색깔 연해지다가 사라지기
        sprite.DOColor(fadeColor, fadeTime)
        .OnComplete(() =>
        {
            // 스프라이트 비활성화
            sprite.enabled = false;

            // 색깔 초기화
            sprite.color = ghostColor;

            LeanPool.Despawn(transform);
        });
    }
}
