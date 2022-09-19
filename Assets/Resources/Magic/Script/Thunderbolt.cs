using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class Thunderbolt : MonoBehaviour
{
    MagicHolder magicHolder;
    [SerializeField] ParticleManager particleManager;
    [SerializeField] Collider2D atkColl; // 공격용 콜라이더

    float range;

    private void Awake()
    {
        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 공격용 콜라이더 끄기
        atkColl.enabled = false;

        yield return new WaitUntil(() => magicHolder.magic != null);
        // 스탯 불러오기
        range = MagicDB.Instance.MagicRange(magicHolder.magic);

        // 범위만큼 스케일 반영
        transform.localScale = Vector3.one * range / 10f;

        // 타겟 위치로 이동
        transform.position = magicHolder.targetPos;

        // 파티클 재생
        particleManager.particle.Play();

        // 공격용 콜라이더 켜기
        atkColl.enabled = true;

        yield return new WaitForSeconds(Time.deltaTime);

        // 공격용 콜라이더 끄기
        atkColl.enabled = false;

        // 파티클 끝날때까지 대기
        yield return new WaitUntil(() => particleManager.particle.isStopped);
        // 그을음 파티클 끝날때까지 대기
        yield return new WaitForSeconds(5f);
        // 디스폰
        StartCoroutine(AutoDespawn());
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

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }
}
