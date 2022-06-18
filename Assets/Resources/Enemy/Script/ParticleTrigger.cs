using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleTrigger : MonoBehaviour
{
    ParticleSystem particle;
    List<ParticleSystem.Particle> insideList = new List<ParticleSystem.Particle>(); // 플레이어에 닿은 파티클 목록
    public int numInside; // 플레이어에 닿은 파티클 개수

    [Header("Attack")]
    public ParticleAttack attack; // 파티클에 닿았을때 실행할 공격 종류 선택
    public enum ParticleAttack { Damage, Poison };
    public EnemyManager enemyManager;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();

        // 트리거 오브젝트로 플레이어 그림자 넣기
        particle.trigger.SetCollider(0, PlayerManager.Instance.shadow);
    }

    private void Update()
    {
        if (attack == ParticleAttack.Poison)
            PoisonTrigger();

        if (attack == ParticleAttack.Damage)
            DamageTrigger();
    }

    private void OnParticleTrigger()
    {
        // 플레이어 콜라이더에 inside 한 파티클 총 개수 산출
        numInside = particle.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);
    }

    void PoisonTrigger()
    {
        // 플레이어와 충돌한 독 웅덩이가 있을때, 플레이어 대쉬중 아닐때, 독 쿨타임중 아닐때
        if (numInside > 0 && !PlayerManager.Instance.isDash && PlayerManager.Instance.poisonCoolCount <= 0)
        {
            print("poison attack!");

            // 플레이어 코루틴으로 도트 피해 입히기
            StartCoroutine(PlayerManager.Instance.PoisonDotHit(2f, 5f));
        }
    }

    void DamageTrigger()
    {
        // 플레이어와 충돌한 파티클이 있을때, 플레이어 대쉬중 아닐때, 히트 쿨타임중 아닐때
        if (numInside > 0 && !PlayerManager.Instance.isDash && PlayerManager.Instance.hitCoolCount <= 0)
        {
            print("particle damage");

            // 피격 딜레이 갱신
            StartCoroutine(PlayerManager.Instance.HitDelay());

            // 플레이어에게 몬스터 파워만큼 데미지 주기
            PlayerManager.Instance.Damage(enemyManager.enemy.power);
        }
    }
}
