using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class ParticleManager : MonoBehaviour
{
    ParticleSystem particle;
    Collider2D coll;
    public bool autoDespawn = true; //자동 디스폰 여부
    public float collOverTime = 0.2f;

    private void Awake()
    {
        particle = GetComponentInChildren<ParticleSystem>();
        coll = GetComponentInChildren<Collider2D>();
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        if (particle == null)
            particle = GetComponentInChildren<ParticleSystem>();

        //콜라이더 켜기
        if (coll)
        {
            coll.enabled = true;

            // 콜라이더 대기시간 0 이상일때
            if (collOverTime > 0)
            {
                // collOverTime 만큼 대기후 콜라이더 끄기
                yield return new WaitForSeconds(collOverTime);

                coll.enabled = false;
            }
        }

        //자동 디스폰일때
        if (autoDespawn)
        {
            //파티클 끝날때까지 대기
            yield return new WaitUntil(() => particle.isStopped);

            //파티클 끝나면 디스폰
            LeanPool.Despawn(transform);
        }
    }

    public void SmoothDespawn()
    {
        StartCoroutine(SmoothDespawnCoroutine());
    }

    IEnumerator SmoothDespawnCoroutine()
    {
        //파티클 재생 정지
        particle.Stop();

        // 남은 파티클 전부 사라질때까지 대기
        yield return new WaitForSeconds(particle.main.duration);

        // 디스폰
        LeanPool.Despawn(transform);
    }

    public void SmoothDisable()
    {
        StartCoroutine(SmoothDisableCoroutine());
    }

    IEnumerator SmoothDisableCoroutine()
    {
        //파티클 재생 정지
        particle.Stop();

        // 남은 파티클 전부 사라질때까지 대기
        yield return new WaitForSeconds(particle.main.duration);

        // 비활성화
        gameObject.SetActive(false);
    }
}
