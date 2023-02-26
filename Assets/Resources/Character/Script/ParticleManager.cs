using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using UnityEngine.Experimental.Rendering.Universal;

public class ParticleManager : MonoBehaviour
{
    [Header("State")]
    public ParticleSystem particle;
    Collider2D coll;

    [Header("State")]
    public bool autoDespawn = false; //자동 디스폰 여부
    [SerializeField] float despawnDelay; // 디스폰 딜레이
    public float collOverTime = 0f;
    public float modify_startLife;
    // public bool autoPlay = true;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
        coll = GetComponentInChildren<Collider2D>();
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        if (particle == null)
            particle = GetComponentInChildren<ParticleSystem>();

        ParticleSystem.MainModule main = particle.main;

        // // 자동시작 아니면 playOnAwake 옵션 끄기
        // if (!autoPlay)
        //     main.playOnAwake = false;

        //todo 파티클 지속시간 수정
        if (modify_startLife > 0)
        {
            main.startLifetime = modify_startLife;
        }

        // 콜라이더 있고, 콜라이더 제한시간 있을때
        if (coll != null && collOverTime > 0)
        {
            //콜라이더 켜기
            coll.enabled = true;

            // collOverTime 만큼 대기후 콜라이더 끄기
            yield return new WaitForSeconds(collOverTime);

            coll.enabled = false;
        }

        // // 자동 시작 아니면 초기화 끝나고 시작
        // if (!autoPlay)
        //     particle.Play();

        //자동 디스폰일때
        if (autoDespawn)
        {
            // 파티클 바로 시작되지 않았으면
            if (!particle.isPlaying)
                // 파티클 시작 할때까지 대기
                yield return new WaitUntil(() => particle.isPlaying);

            // 파티클 끝날때까지 대기
            yield return new WaitUntil(() => particle.isStopped);

            // 임의의 디스폰 딜레이 대기
            yield return new WaitForSeconds(despawnDelay);

            //파티클 끝나면 디스폰
            LeanPool.Despawn(transform);
        }
    }

    public void SmoothStop(float delay = 0)
    {
        if (gameObject.activeSelf)
            StartCoroutine(SmoothStopCoroutine(delay));
    }

    IEnumerator SmoothStopCoroutine(float delay = 0)
    {
        // 딜레이 동안 대기
        yield return new WaitForSeconds(delay);

        //파티클 재생 정지
        particle.Stop();
    }

    public void SmoothDespawn(float delay = 0)
    {
        if (gameObject.activeSelf)
            StartCoroutine(SmoothDespawnCoroutine(delay));
    }

    IEnumerator SmoothDespawnCoroutine(float delay = 0)
    {
        // 딜레이 동안 대기
        yield return new WaitForSeconds(delay);

        //파티클 재생 정지
        particle.Stop();

        // 남은 파티클 전부 사라질때까지 대기
        yield return new WaitForSeconds(particle.main.startLifetime.constantMax);

        // 디스폰
        LeanPool.Despawn(transform);
    }

    public void SmoothDisable()
    {
        if (gameObject.activeSelf)
            StartCoroutine(SmoothDisableCoroutine());
    }

    IEnumerator SmoothDisableCoroutine()
    {
        //파티클 재생 정지
        particle.Stop();

        // 남은 파티클 전부 사라질때까지 대기
        yield return new WaitForSeconds(particle.main.startLifetime.constantMax);

        // 비활성화
        gameObject.SetActive(false);
    }
}
