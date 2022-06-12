using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleTrigger : MonoBehaviour
{
    [SerializeField]
    KingSlime_AI kingSlimeAI;
    ParticleSystem ps;
    List<ParticleSystem.Particle> insideList = new List<ParticleSystem.Particle>(); // 플레이어 콜라이더에 inside한 파티클 목록
    public int numInside; // 플레이어 콜라이더에 inside 한 파티클 총 개수

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        // 플레이어와 충돌한 독 웅덩이가 있을때, 플레이어 대쉬중 아닐때, 쿨타임중 아닐때
        if (numInside > 0 && !PlayerManager.Instance.isDash && PlayerManager.Instance.poisonDuration <= 0)
        {
            // print("poison attack!");

            // 플레이어 코루틴으로 도트 피해 입히기
            StartCoroutine(PlayerManager.Instance.PoisonDotHit(2f, 5f));
        }
    }

    private void OnParticleTrigger()
    {
        // 플레이어 콜라이더에 inside 한 파티클 총 개수 산출
        numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
    }
}
