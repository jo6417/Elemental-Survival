using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleTrigger : MonoBehaviour
{
    [Header("Refer")]
    public MagicHolder magicHolder;
    public MagicInfo magic;
    ParticleSystem particle;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>(); //충돌한 파티클의 이벤트 정보들
    List<ParticleSystem.Particle> insideList = new List<ParticleSystem.Particle>(); // 콜라이더에 닿은 파티클 목록
    public int numEnter; // 플레이어 콜라이더에 들어간 파티클 개수
    public int numInside; // 플레이어 콜라이더 안에 존재하는 파티클 개수

    [Header("Attack")]
    public ParticleAttack attack; // 파티클에 닿았을때 실행할 공격 종류 선택
    public enum ParticleAttack { Damage, Poison, Slow, Knockback, Burn };
    public EnemyManager enemyManager;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();

        // 트리거 오브젝트로 플레이어 그림자 넣기
        // particle.trigger.SetCollider(0, PlayerManager.Instance.shadow);
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 타겟에 따라 파티클 충돌 대상 레이어 바꾸기
        ParticleSystem.CollisionModule particleColl = particle.collision;

        if (magicHolder.GetTarget() == MagicHolder.Target.Enemy)
        {
            gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
            particleColl.collidesWith = SystemManager.Instance.layerList.EnemyHit_Mask;
        }

        if (magicHolder.GetTarget() == MagicHolder.Target.Player)
        {
            gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
            particleColl.collidesWith = SystemManager.Instance.layerList.PlayerHit_Mask;
        }

        // 타겟 방향을 쳐다보기
        Vector2 targetDir = magicHolder.targetPos - transform.position;
        float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(Vector3.forward * angle);

        // 초기화 완료 되면 파티클 시작
        particle.Play();
    }

    private void Update()
    {
        if (attack == ParticleAttack.Poison)
            PoisonTrigger();

        // if (attack == ParticleAttack.Damage)
        //     DamageTrigger();
    }

    private void OnParticleCollision(GameObject other)
    {
        ParticlePhysicsExtensions.GetCollisionEvents(particle, other, collisionEvents);

        for (int i = 0; i < collisionEvents.Count; i++)
        {
            // 플레이어에 충돌하면 데미지 주기
            if (other.CompareTag(SystemManager.TagNameList.Player.ToString()) && PlayerManager.Instance.hitBox.hitCoolCount <= 0 && !PlayerManager.Instance.isDash)
            {
                print($"Player : {other.name} : {other.tag} : {other.layer}");
                StartCoroutine(PlayerManager.Instance.hitBox.Hit(magicHolder.transform));
            }

            // 몬스터에 충돌하면 데미지 주기
            if (other.CompareTag(SystemManager.TagNameList.Enemy.ToString()))
            {
                print($"Enemy : {other.name} : {other.tag} : {other.layer}");

                if (other.TryGetComponent(out EnemyHitBox enemyHitBox))
                {
                    StartCoroutine(enemyHitBox.Hit(magicHolder.gameObject));
                }
            }
        }
    }

    private void OnParticleTrigger()
    {
        // 플레이어 콜라이더에 inside 한 파티클 총 개수 산출
        numInside = particle.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, insideList);

        for (int i = 0; i < numInside; i++)
        {
            ParticleSystem.Particle p = insideList[i];

            // print($"inside : {p.position}");
        }
    }

    void PoisonTrigger()
    {
        // 플레이어와 충돌한 독 웅덩이가 있을때, 플레이어 대쉬중 아닐때, 독 쿨타임중 아닐때
        if (numInside > 0 && !PlayerManager.Instance.isDash && PlayerManager.Instance.hitBox.poisonCoolCount <= 0)
        {
            print("poison attack!");

            // 플레이어 코루틴으로 도트 피해 입히기
            StartCoroutine(PlayerManager.Instance.hitBox.PoisonDotHit(2f, 5f));
        }
    }

    void DamageTrigger()
    {
        // 플레이어와 충돌한 파티클이 있을때, 플레이어 대쉬중 아닐때, 히트 쿨타임중 아닐때
        if (numInside > 0 && !PlayerManager.Instance.isDash && PlayerManager.Instance.hitBox.hitCoolCount <= 0)
        {
            print("particle damage");

            // 피격 딜레이 갱신
            StartCoroutine(PlayerManager.Instance.hitBox.HitDelay());

            // 플레이어에게 몬스터 파워만큼 데미지 주기
            PlayerManager.Instance.hitBox.Damage(enemyManager.enemy.power);
        }
    }
}
