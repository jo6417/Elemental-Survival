using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class PoisonPool : MonoBehaviour
{
    [Header("Refer")]
    MagicHolder magicHolder;
    MagicInfo magic;
    SpriteRenderer sprite;
    Collider2D coll;
    [SerializeField]
    ParticleSystem bubbleEffect;
    Color originColor;

    private void Awake()
    {
        sprite = sprite == null ? GetComponent<SpriteRenderer>() : sprite;
        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;
        coll = coll == null ? GetComponent<Collider2D>() : coll;

        // 원래 색 저장
        originColor = sprite.color;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 콜라이더 비활성화
        coll.enabled = false;

        // 스프라이트 원래 색으로 초기화
        sprite.color = originColor;

        yield return new WaitUntil(() => magicHolder.magic != null);

        // 거품 파티클 켜기
        bubbleEffect.Play();

        // 콜라이더 활성화
        coll.enabled = true;

        // 마법 지속시간 계산
        float duration = MagicDB.Instance.MagicDuration(magic);

        // 디스폰 예약
        StartCoroutine(DespawnMagic(duration));
    }

    IEnumerator DespawnMagic(float delay = 0)
    {
        //딜레이 만큼 대기
        yield return new WaitForSeconds(delay);

        // 콜라이더 끄기
        coll.enabled = false;

        // 거품 파티클 stop
        bubbleEffect.Stop();

        // 거품 파티클 사라지는 동안 스프라이트 서서히 사라지기
        Color clearColor = sprite.color;
        clearColor.a = 0f;
        sprite.DOColor(clearColor, 2f);

        // 거품 파티클 사라지는 시간 대기 후 디스폰
        LeanPool.Despawn(transform, 2f);
    }
}
