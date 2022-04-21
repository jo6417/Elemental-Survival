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
    MagicHolder magicHolder;
    public Animator anim;
    public string magicName;
    public Collider2D col;
    Vector2 originScale; //원래 오브젝트 크기
    public Ease fallEase;
    public float addAngle; //스프라이트 방향 보정
    public Vector2 startPos; //시작할 위치
    public bool isExpand = false; //커지면서 등장 여부
    public bool isFade = false; //domove 끝나고 사라지기 여부

    [Header("Effect")]
    public GameObject particle; //파티클 오브젝트
    public GameObject magicEffect;
    SpriteRenderer effectSprite;
    Animator effectAnim;

    private void Awake()
    {
        // 오브젝트 기본 사이즈 저장
        originScale = transform.localScale;

        //애니메이터 찾기
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();

        //이펙트 관련 컴포넌트 찾기
        if (magicEffect)
        {
            effectSprite = magicEffect.GetComponent<SpriteRenderer>();
            effectAnim = magicEffect.GetComponent<Animator>();
        }

        //초기화 하기
        StartCoroutine(Initial());
    }

    private void OnEnable()
    {
        StartCoroutine(FallingMagicObj());
    }

    IEnumerator Initial()
    {
        //스프라이트 초기화
        sprite.color = Color.white;
        sprite.enabled = false;

        //파티클 있으면 끄기
        if (particle)
            particle.SetActive(false);

        //이펙트 끄기
        if (magicEffect)
        {
            effectSprite.enabled = false;
            effectAnim.enabled = false;
        }

        //시작할때 콜라이더 끄기
        ColliderTrigger(false);

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => GetComponentInChildren<MagicHolder>() != null);
        magicHolder = GetComponentInChildren<MagicHolder>();
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
        //초기화
        StartCoroutine(Initial());

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magic != null);

        // 마법 오브젝트 속도, 숫자가 작을수록 빠름
        float magicSpeed = MagicDB.Instance.MagicSpeed(magic, false);

        // 마법 오브젝트 점점 커지면서 나타내기
        if (isExpand)
            transform.localScale = Vector2.zero;
        transform.DOScale(originScale, 0.5f)
        .SetEase(Ease.OutBack);

        //시작 위치
        Vector2 moveStartPos = startPos + (Vector2)magicHolder.targetPos;

        //끝나는 위치
        Vector2 endPos = magicHolder.targetPos;

        //시작 위치로 올려보내기
        transform.position = moveStartPos;

        //목표 위치로 떨어뜨리기
        transform.DOMove(endPos, magicSpeed)
        .OnStart(() =>
        {
            //스프라이트 켜기
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
                // sprite.color = Color.clear;
                sprite.DOColor(Color.clear, 0.5f);
            }

            //콜라이더 발동시키기
            ColliderTrigger(true);

            // 마법 오브젝트 속도
            // float duration = MagicDB.Instance.MagicDuration(magic);

            // 오브젝트 자동 디스폰하기
            StartCoroutine(AutoDespawn(0.5f));
        });
    }

    public void ColliderTrigger(bool magicTrigger = true)
    {
        // 마법 트리거 발동 됬을때 (적 데미지 입히기, 마법 효과 발동)
        if (magicTrigger)
        {
            //콜라이더 켜기
            col.enabled = true;

            // 이펙트 오브젝트 생성 (이펙트 있으면)
            // if (magicEffect)
            //     LeanPool.Spawn(magicEffect, transform.position + effectPos, Quaternion.identity);

            //이펙트 켜기
            if (magicEffect)
            {
                magicEffect.SetActive(true);
                effectSprite.enabled = true;
                effectAnim.enabled = true;
            }
        }
        else
        {
            //콜라이더 끄기
            col.enabled = false;

            //이펙트 애니메이터 끄기
            if (magicEffect)
                effectAnim.enabled = false;
        }
    }

    void OnCollider()
    {
        col.enabled = true;
    }

    //애니메이션 끝날때 이벤트 함수
    public void AnimEndDespawn()
    {
        StartCoroutine(AutoDespawn());
    }

    IEnumerator AutoDespawn(float duration = 0)
    {
        //range 속성만큼 지속시간 부여
        yield return new WaitForSeconds(duration);

        //콜라이더 끄고 종료
        ColliderTrigger(false);

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }
}
