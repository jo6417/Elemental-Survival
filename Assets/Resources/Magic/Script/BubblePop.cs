using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lean.Pool;
using UnityEngine;

public class BubblePop : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] ParticleSystem particle;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>(); //충돌한 파티클의 이벤트 정보들

    bool initDone = false; // 초기화 완료 여부

    private IDictionary<uint, ParticleSystem.Particle> _trackedParticles = new Dictionary<uint, ParticleSystem.Particle>();

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //초기화 완료 안됨
        initDone = false;

        yield return new WaitUntil(() => magicHolder.magic != null);
        MagicInfo magic = magicHolder.magic;

        // magicHolder 초기화 완료까지 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);

        // 슬로우 디버프 시간 갱신
        magicHolder.slowTime = MagicDB.Instance.MagicDuration(magic);

        ParticleSystem.EmissionModule particleEmmision = particle.emission;
        ParticleSystem.MainModule particleMain = particle.main;

        // 발사할 파티클 개수에 atkNum값 갱신
        particleEmmision.SetBurst(0, new ParticleSystem.Burst(0, 1, magicHolder.atkNum, 0.05f));

        // 파티클 속도에 speed값 갱신
        particleMain.startSpeed = MagicDB.Instance.MagicSpeed(magic, true);
        // 방울 사이즈에 scale값 갱신
        particleMain.startSize = new ParticleSystem.MinMaxCurve(1f * magicHolder.scale, 2f * magicHolder.scale);

        // 타겟에 따라 파티클 충돌 대상 레이어 바꾸기
        ParticleSystem.CollisionModule particleColl = particle.collision;

        // 플레이어가 쐈을때, 몬스터가 타겟
        if (magicHolder.GetTarget() == MagicHolder.TargetType.Enemy)
        {
            gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
            particleColl.collidesWith = SystemManager.Instance.layerList.EnemyHit_Mask;
        }

        // 몬스터가 쐈을때, 플레이어가 타겟
        if (magicHolder.GetTarget() == MagicHolder.TargetType.Player)
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

        //초기화 완료
        initDone = true;
    }

    private void OnParticleCollision(GameObject other)
    {
        // 초기화 완료 전이면 리턴
        if (!initDone)
            return;

        ParticlePhysicsExtensions.GetCollisionEvents(particle, other, collisionEvents);

        for (int i = 0; i < collisionEvents.Count; i++)
        {
            // 플레이어에 충돌하면 데미지 주기
            if (other.CompareTag(TagNameList.Player.ToString()) && PlayerManager.Instance.hitDelayCount <= 0 && !PlayerManager.Instance.isDash)
            {
                StartCoroutine(PlayerManager.Instance.hitBox.Hit(magicHolder));
            }

            // 몬스터에 충돌하면 데미지 주기
            if (other.CompareTag(TagNameList.Enemy.ToString()))
                if (other.TryGetComponent(out HitBox enemyHitBox))
                {
                    StartCoroutine(enemyHitBox.Hit(magicHolder));
                }
        }
    }

    void Update()
    {
        var liveParticles = new ParticleSystem.Particle[particle.particleCount];
        particle.GetParticles(liveParticles);

        // 생성 및 파괴된 파티클 위치 불러오기
        ParticleDelta particleDelta = GetParticleDelta(liveParticles);

        // 파티클 생성될때
        foreach (var particleAdded in particleDelta.Added)
        {
            // 해당 위치에 사운드 재생
            SoundManager.Instance.PlaySound("BubblePop_Birth", particleAdded.position);
        }

        // 파티클 사라질때
        foreach (var particleRemoved in particleDelta.Removed)
        {
            // 해당 위치에 사운드 재생
            SoundManager.Instance.PlaySound("BubblePop_Death", particleRemoved.position);
        }
    }

    private ParticleDelta GetParticleDelta(ParticleSystem.Particle[] liveParticles)
    {
        ParticleDelta deltaResult = new ParticleDelta();

        foreach (var activeParticle in liveParticles)
        {
            ParticleSystem.Particle foundParticle;
            if (_trackedParticles.TryGetValue(activeParticle.randomSeed, out foundParticle))
            {
                _trackedParticles[activeParticle.randomSeed] = activeParticle;
            }
            else
            {
                // 새로 생긴 파티클을 리스트에 추가
                deltaResult.Added.Add(activeParticle);
                _trackedParticles.Add(activeParticle.randomSeed, activeParticle);
            }
        }

        var updatedParticleAsDictionary = liveParticles.ToDictionary(x => x.randomSeed, x => x);
        var dictionaryKeysAsList = _trackedParticles.Keys.ToList();

        foreach (var dictionaryKey in dictionaryKeysAsList)
        {
            if (updatedParticleAsDictionary.ContainsKey(dictionaryKey) == false)
            {
                // 사라진 파티클을 리스트에 추가
                deltaResult.Removed.Add(_trackedParticles[dictionaryKey]);
                _trackedParticles.Remove(dictionaryKey);
            }
        }

        return deltaResult;
    }

    private class ParticleDelta
    {
        public IList<ParticleSystem.Particle> Added { get; set; } = new List<ParticleSystem.Particle>();
        public IList<ParticleSystem.Particle> Removed { get; set; } = new List<ParticleSystem.Particle>();
    }
}
