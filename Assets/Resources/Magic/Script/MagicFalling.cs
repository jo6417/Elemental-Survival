using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class MagicFalling : MonoBehaviour
{
    [Header("Refer")]
    SpriteRenderer sprite;
    private MagicInfo magic;
    public MagicHolder magicHolder;
    public string magicName;
    public Collider2D coll;
    Vector2 originScale; //원래 오브젝트 크기
    public Ease fallEase;
    public float angleOffset; //스프라이트 방향 보정
    public Vector2 startOffset; //시작할 위치

    public bool isExpand = false; //커지면서 등장 여부
    public bool isFade = false; //domove 끝나고 사라지기 여부

    [Header("Effect")]
    public GameObject despawnEffectPrefab;
    public GameObject particle; //파티클 오브젝트
    public GameObject despawnEffect;
    SpriteRenderer effectSprite;
    Animator effectAnim;

    private void Awake()
    {
        // 오브젝트 기본 사이즈 저장
        originScale = transform.localScale;

        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;

        //애니메이터 찾기
        sprite = sprite == null ? GetComponent<SpriteRenderer>() : sprite;
        coll = coll == null ? GetComponent<Collider2D>() : coll;

        //이펙트 관련 컴포넌트 찾기
        if (despawnEffect)
        {
            effectSprite = despawnEffect.GetComponent<SpriteRenderer>();
            effectAnim = despawnEffect.GetComponent<Animator>();
        }
    }

    private void OnEnable()
    {
        //초기화 하기
        StartCoroutine(Initial());

        StartCoroutine(FallingMagicObj());
    }

    IEnumerator Initial()
    {
        //스프라이트 초기화
        if (sprite != null)
        {
            sprite.color = Color.white;
            sprite.enabled = false;
        }

        //파티클 있으면 끄기
        if (particle)
            particle.SetActive(false);

        //이펙트 끄기
        if (despawnEffect)
        {
            if (effectSprite != null)
                effectSprite.enabled = false;
            if (effectAnim != null)
                effectAnim.enabled = false;
        }

        //시작할때 콜라이더 끄기
        ColliderTrigger(false);

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (magic == null)
        {
            magic = MagicDB.Instance.GetMagicByName(transform.name.Split('_')[0]);
            magicName = magic.magicName;
        }

        //magic 못찾으면 코루틴 종료
        if (magic == null)
            yield break;
    }

    IEnumerator FallingMagicObj()
    {
        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magic != null);

        // 마법 오브젝트 속도, 숫자가 작을수록 빠름
        float magicSpeed = MagicDB.Instance.MagicSpeed(magic, false);

        // 마법 오브젝트 점점 커지면서 나타내기
        if (isExpand)
        {
            transform.localScale = Vector2.zero;
            transform.DOScale(originScale, 0.5f)
            .SetEase(Ease.OutBack);
        }

        //시작 위치
        Vector2 startPos = startOffset + (Vector2)magicHolder.targetPos;

        //끝나는 위치
        Vector2 endPos = magicHolder.targetPos;

        //시작 위치로 올려보내기
        transform.position = startPos;

        //목표 위치 방향으로 회전
        Vector2 dir = endPos - startPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle + angleOffset, Vector3.forward);

        //목표 위치로 떨어뜨리기
        transform.DOMove(endPos, magicSpeed)
        .OnStart(() =>
        {
            //스프라이트 켜기
            if (sprite != null)
                sprite.enabled = true;

            //파티클 있으면 켜기
            if (particle)
                particle.SetActive(true);
        })
        .SetEase(fallEase)
        .OnComplete(() =>
        {
            if (isFade)
            {
                if (sprite != null)
                    // sprite.color = Color.clear;
                    sprite.DOColor(Color.clear, 0.5f);
            }

            //콜라이더 발동시키기
            ColliderTrigger(true);

            // 마법 오브젝트 속도
            // float duration = MagicDB.Instance.MagicDuration(magic);

            // 오브젝트 자동 디스폰하기
            if (gameObject.activeSelf)
                StartCoroutine(AutoDespawn(0.5f));
        });
    }

    public void ColliderTrigger(bool magicTrigger = true)
    {
        // 마법 트리거 발동 됬을때 (적 데미지 입히기, 마법 효과 발동)
        if (magicTrigger)
        {
            //콜라이더 켜기
            coll.enabled = true;

            // 이펙트 오브젝트 생성
            if (despawnEffectPrefab)
                LeanPool.Spawn(despawnEffectPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            //이펙트 켜기
            if (despawnEffect)
            {
                despawnEffect.SetActive(true);

                if (effectSprite != null)
                    effectSprite.enabled = true;
                if (effectAnim != null)
                    effectAnim.enabled = true;
            }
        }
        else
        {
            //콜라이더 끄기
            coll.enabled = false;

            //이펙트 애니메이터 끄기
            if (effectAnim)
                effectAnim.enabled = false;
        }
    }

    IEnumerator AutoDespawn(float delay = 0)
    {
        //range 속성만큼 지속시간 부여
        float delayCount = delay;
        while (delayCount > 0)
        {
            delayCount -= Time.deltaTime;
            yield return null;
        }

        //콜라이더 끄고 종료
        ColliderTrigger(false);

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }
}
