using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AfterImage : MonoBehaviour
{
    public SpriteRenderer targetSpriteRenderer;
    public Sprite targetSprite;
    public ParticleSystem particle;
    public float ghostTime = 1f; // 잔상 소멸 시간

    private void Awake()
    {
        targetSpriteRenderer = targetSpriteRenderer != null ? targetSpriteRenderer : GetComponent<SpriteRenderer>();
        particle = particle != null ? particle : GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        if (targetSpriteRenderer != null)
            // 파티클 스프라이트를 타겟 스프라이트로 업데이트
            particle.textureSheetAnimation.SetSprite(0, targetSpriteRenderer.sprite);
        else if (targetSprite != null)
        {
            // 파티클 스프라이트를 타겟 스프라이트로 업데이트
            particle.textureSheetAnimation.SetSprite(0, targetSprite);
        }

        // 잔상 소멸 시간 입력되면 
        if (ghostTime > 0)
        {
            // ghostTime 후에 사라짐
            ParticleSystem.MainModule main = particle.main;
            main.startLifetime = ghostTime;
        }
    }
}
