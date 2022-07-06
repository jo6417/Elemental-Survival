using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class HellFire : MonoBehaviour
{
    MagicHolder magicHolder;
    MagicInfo magic;
    public ParticleManager fireEffect;
    public ParticleSystem explosionEffect;
    public Animator anim;
    public SpriteRenderer skullSprite;

    private void Awake()
    {
        magicHolder = GetComponentInChildren<MagicHolder>();
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        // 애니메이션 멈추기
        anim.speed = 0f;

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 타겟에 따라 해골 색 바꾸기
        if (magicHolder.targetType == MagicHolder.Target.Player)
            // 플레이어가 타겟이면 빨간색
            skullSprite.color = new Color(1, 0, 0, 1);

        if (magicHolder.targetType == MagicHolder.Target.Enemy)
            // 몬스터가 타겟이면 흰색
            skullSprite.color = new Color(1, 1, 1, 1);

        // 타겟 위치로 이동
        transform.position = magicHolder.targetPos;

        // 해골 화염 이펙트 켜기
        fireEffect.gameObject.SetActive(true);

        // 애니메이션 재생
        anim.speed = 1f;
    }

    void SkullPopup()
    {
        // 타겟 오브젝트 변수 들어왔는지 검사
        if (magicHolder.targetObj == null)
            return;

        // 타겟이 X좌표 왼쪽에 있을때
        if (transform.position.x > magicHolder.targetObj.transform.position.x)
        {
            // X 반전
            skullSprite.flipX = true;
        }
        else
        {
            // X 반전 초기화
            skullSprite.flipX = false;
        }
    }

    void FireStop()
    {
        fireEffect.SmoothDisable();
    }

    IEnumerator ParticleWait()
    {
        // 불씨 및 흙 폭발 파티클 꺼질때까지 대기
        yield return new WaitUntil(() => !explosionEffect.gameObject.activeSelf);

        // 끝나면 디스폰
        LeanPool.Despawn(transform);
    }

    void Despawn()
    {
        // 파티클 끝나면 디스폰 시키기
        StartCoroutine(ParticleWait());
    }
}
