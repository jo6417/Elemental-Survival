using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class MagicAnim : MonoBehaviour
{
    MagicHolder magicHolder;
    public ParticleSystem atkStartEffect;
    public ParticleSystem despawnEffect;

    public Animator anim;

    private void Awake()
    {
        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;
        anim = anim == null ? GetComponent<Animator>() : anim;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //애니메이션 멈추기
        anim.speed = 0f;

        yield return new WaitUntil(() => magicHolder.magic != null);

        //애니메이션 재생
        anim.speed = 1f;
    }

    public void StartAtk()
    {
        // 콜라이더 켜기
        ColliderTrigger(true);

        // 이펙트 오브젝트 생성
        if (atkStartEffect)
            LeanPool.Spawn(atkStartEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
    }

    //애니메이션 끝날때 이벤트 함수
    public void EndMagic()
    {
        if (gameObject.activeSelf)
            StartCoroutine(AutoDespawn());
    }

    public void ColliderTrigger(bool magicTrigger = true)
    {
        // 마법 트리거 발동 됬을때 (적 데미지 입히기, 마법 효과 발동)
        if (magicTrigger)
        {
            //콜라이더 켜기
            magicHolder.coll.enabled = true;
        }
        else
        {
            //콜라이더 끄기
            magicHolder.coll.enabled = false;
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

        // 이펙트 오브젝트 생성
        if (despawnEffect)
            LeanPool.Spawn(despawnEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }
}
