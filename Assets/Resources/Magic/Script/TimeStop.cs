using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class TimeStop : MonoBehaviour
{
    MagicInfo magic;

    [Header("Refer")]
    public MagicHolder magicHolder;
    public Sprite initialSprite; //스프라이트 초기화
    public Animator anim;
    public GameObject effect;
    public SpriteRenderer effectSprite;
    public Collider2D effectColl;
    public SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // anim = GetComponent<Animation>();
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        // 스프라이트 색깔 초기화
        spriteRenderer.color = Color.blue;

        //이펙트 비활성화
        effectSprite.enabled = false;
        effectColl.enabled = false;

        //magic 정보 들어올때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;
        Vector2 targetPos = magicHolder.targetPos;

        //마법 쿨타임
        float coolTime = MagicDB.Instance.MagicCoolTime(magic);

        // 스프라이트 초기화 하기
        spriteRenderer.sprite = initialSprite;

        //애니메이션 멈추기
        anim.speed = 0f;

        //플레이어 위치로 이동
        transform.position = targetPos;

        //플레이어 위치에서 시작
        // transform.localPosition = Vector2.zero;

        //플레이어 머리 위까지 올라가기
        transform.DOLocalMove((Vector2)transform.position + Vector2.up * 2f, 0.5f)
        .OnComplete(() =>
        {
            //애니메이션 처음부터 재생
            anim.speed = 1f;
            anim.Rebind();
            anim.Play("TimeStop");
        });

        //모래시계 돌리는 시간
        magic.coolCount = coolTime * 0.5f; //쿨타임의 절반
        while (magic.coolCount > 0)
        {
            //카운트 차감, 플레이어 자체속도 반영
            magic.coolCount -= Time.deltaTime;

            yield return null;
        }

        //마법 범위
        float range = MagicDB.Instance.MagicRange(magic);

        //마법 지속시간
        float duration = MagicDB.Instance.MagicDuration(magic);

        //마법 성공여부 = 크리티컬 성공여부
        bool isSuccess = MagicDB.Instance.MagicCritical(magic);

        //애니메이션 멈추기
        anim.speed = 0f;
        anim.Rebind();

        // 스프라이트 초기화
        spriteRenderer.sprite = initialSprite;

        //회전 초기화
        transform.rotation = Quaternion.Euler(Vector3.zero);

        //마법 성공시
        if (isSuccess)
        {
            //부모 초기화
            transform.parent = null;

            // 스프라이트 민트색으로
            spriteRenderer.color = Color.cyan;

            // 이펙트 활성화
            effectSprite.enabled = true;
            effectColl.enabled = true;

            //사이즈 줄이기
            effect.transform.localScale = Vector2.zero;
            //사이즈 키우기
            effect.transform.DOScale(Vector2.one * range, 0.5f);
        }
        //마법 실패시
        else
        {
            // 스프라이트 빨간색으로
            spriteRenderer.color = Color.red;

            // 이펙트 비활성화
            effectSprite.enabled = false;
            effectColl.enabled = false;
        }

        //지속시간 만큼 대기
        yield return new WaitForSeconds(duration);

        //서서히 사라지기
        spriteRenderer.DOColor(Color.clear, 0.5f)
        .SetEase(Ease.InCirc);

        //사이즈 줄어들어 사라지기
        effect.transform.DOScale(Vector3.zero, duration)
        .SetEase(Ease.InCirc)
        .OnComplete(() =>
        {
            // 디스폰
            LeanPool.Despawn(transform);
        });
    }
}
