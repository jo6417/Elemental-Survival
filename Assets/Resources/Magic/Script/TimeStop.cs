using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class TimeStop : MonoBehaviour
{
    [Header("Refer")]
    public MagicHolder magicHolder;
    public Sprite initialSprite; //스프라이트 초기화
    public Animator anim;
    public GameObject effect;
    public SpriteRenderer effectSprite;
    public Collider2D effectColl;
    public SpriteRenderer sprite;

    [Header("Stat")]
    bool isSuccess;

    private void Awake()
    {
        // anim = GetComponent<Animation>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //이펙트 비활성화
        effectSprite.enabled = false;
        effectColl.enabled = false;

        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);

        // 정지 지속시간 넣기
        magicHolder.stopTime = magicHolder.duration;

        // 시간정지 영역 전개
        StartCoroutine(ExpandMagic());
    }

    IEnumerator ExpandMagic()
    {
        // 스프라이트 색깔 초기화
        sprite.color = Color.blue;

        // 스마트폰 위치로 이동
        transform.position = CastMagic.Instance.phone.position;

        //애니메이션 멈추기
        anim.speed = 0f;

        // 위로 올라가기
        transform.DOLocalMove((Vector2)transform.position + Vector2.up * 2f, 0.5f)
        .OnComplete(() =>
        {
            //애니메이션 처음부터 재생
            anim.speed = 1f;
            anim.Rebind();
            anim.Play("TimeStop");
        });

        // 애니메이션 1초간 재생
        yield return new WaitForSeconds(1f);

        //애니메이션 멈추기
        anim.speed = 0f;
        anim.Rebind();

        // 스프라이트 초기화
        sprite.sprite = initialSprite;

        //회전 초기화
        transform.rotation = Quaternion.Euler(Vector3.zero);

        //마법 성공시
        if (isSuccess)
        {
            //부모 초기화
            transform.parent = null;

            // 스프라이트 민트색으로
            sprite.color = Color.cyan;

            // 이펙트 활성화
            effectSprite.enabled = true;
            effectColl.enabled = true;

            //사이즈 줄이기
            effect.transform.localScale = Vector2.zero;
            //사이즈 키우기
            effect.transform.DOScale(Vector2.one * magicHolder.range, 0.5f)
            .SetEase(Ease.OutBack);
        }
        //마법 실패시
        else
        {
            // 스프라이트 빨간색으로
            sprite.color = Color.red;

            // 이펙트 비활성화
            effectSprite.enabled = false;
            effectColl.enabled = false;
        }

        // 영역 확장되는 동안 대기
        yield return new WaitForSeconds(0.5f);

        //사이즈 줄어들기
        effect.transform.DOScale(Vector3.zero, magicHolder.duration)
        .SetEase(Ease.InCirc)
        .OnComplete(() =>
        {
            //콜라이더 끄기
            effectColl.enabled = false;
        });

        // 지속시간 만큼 대기
        yield return new WaitForSeconds(magicHolder.duration);

        // 서서히 사라지기
        sprite.DOColor(Color.clear, 0.5f)
        .SetEase(Ease.InCirc);

        // 쿨타임 만큼 대기
        yield return new WaitForSeconds(magicHolder.coolTime);

        // 시간정지 영역 다시 전개
        StartCoroutine(ExpandMagic());
    }
}
