using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindCutter : MonoBehaviour
{
    [Header("Refer")]
    public ParticleSystem particle;
    public MagicInfo magic;
    MagicHolder magicHolder;
    float duration;

    private void Awake()
    {
        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;
        particle = particle == null ? GetComponent<ParticleSystem>() : particle;
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 지속시간 초기화 
        duration = MagicDB.Instance.MagicDuration(magic);

        // 파티클 지속시간에 반영
        ParticleSystem.MainModule main = particle.main;
        main.startLifetime = duration;
    }
}
