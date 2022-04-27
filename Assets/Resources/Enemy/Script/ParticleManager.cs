using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class ParticleManager : MonoBehaviour
{
    ParticleSystem particle;

    private void Awake() {
        particle = GetComponent<ParticleSystem>();
    }

    private void OnEnable() {
        //초기화
        StartCoroutine(Initial());

        //시간 멈춤 확인
        StartCoroutine(StopCheck());
    }

    IEnumerator Initial()
    {
        if(particle == null)
        particle = GetComponent<ParticleSystem>();

        //파티클 끝날때까지 대기
        yield return new WaitUntil(() => particle.isStopped);

        //파티클 끝나면 디스폰
        LeanPool.Despawn(transform);
    }

    IEnumerator StopCheck()
    {
        while (gameObject.activeSelf)
        {
            if (VarManager.Instance.playerTimeScale == 0)
                particle.Pause();

            yield return null;
        }
    }
}
