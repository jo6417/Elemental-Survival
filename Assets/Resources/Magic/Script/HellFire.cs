using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class HellFire : MonoBehaviour
{
    MagicHolder magicHolder;
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
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 애니메이션 멈추기
        anim.speed = 0f;

        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder.initDone);

        // 레이어에 따라 해골 색 바꾸기
        if (magicHolder.gameObject.layer == SystemManager.Instance.layerList.EnemyAttack_Layer)
            // 몬스터 공격이면 빨간색
            skullSprite.color = new Color(1, 0, 0, 1);

        if (magicHolder.gameObject.layer == SystemManager.Instance.layerList.PlayerAttack_Layer)
            // 플레이어 공격이면 흰색
            skullSprite.color = new Color(1, 1, 1, 1);

        // 타겟 위치로 이동
        transform.position = magicHolder.targetPos;

        // 해골 화염 이펙트 켜기
        fireEffect.gameObject.SetActive(true);

        // 애니메이션 재생
        anim.speed = 1f;

        // 화염 기둥 솟을때 효과음 예약
        SoundManager.Instance.PlaySound("HellFire_Pop", transform.position, 1f, 1f);
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
