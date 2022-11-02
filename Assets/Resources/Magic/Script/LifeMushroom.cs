using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class LifeMushroom : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] ParticleSystem particle;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>(); //충돌한 파티클의 이벤트 정보들

    [Header("State")]
    float speed;
    int atkNum;
    float coolTime;
    float respawnRecord;
    IEnumerator cooldownCoroutine;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        MagicInfo magic = magicHolder.magic;

        // 스탯 초기화
        float power = MagicDB.Instance.MagicPower(magic);
        float duration = MagicDB.Instance.MagicDuration(magic);
        speed = MagicDB.Instance.MagicSpeed(magic, true);
        float range = MagicDB.Instance.MagicRange(magic);
        atkNum = MagicDB.Instance.MagicAtkNum(magic);
        coolTime = MagicDB.Instance.MagicCoolTime(magic);

        // 파티클 모듈 찾기
        ParticleSystem.MainModule particleMain = particle.main;
        ParticleSystem.EmissionModule particleEmission = particle.emission;
        ParticleSystem.VelocityOverLifetimeModule particleVelocity = particle.velocityOverLifetime;

        // 독 도트 데미지 지속시간 초기화
        magicHolder.poisonTime = duration;
        // 파티클 지속 시간 초기화
        particleMain.startLifetime = duration;
        // 파티클 속도 초기화
        particleVelocity.orbitalZ = speed;
        // 파티클 사이즈 초기화
        particleMain.startSize = range;
        // 파티클 개수 초기화
        ParticleSystem.Burst burst = particle.emission.GetBurst(0);
        burst.count = atkNum;
        particleEmission.SetBurst(0, burst);

        if (magicHolder.magicCastCallback == null)
            magicHolder.magicCastCallback = SummonMushroom;
    }

    void SummonMushroom()
    {
        // 파티클 재생해서 버섯 소환
        particle.Play();

        // 플레이어 위치에 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.shadowSprite.transform);
        transform.localPosition = Vector3.zero;

        // 수동 시전일때
        if (magicHolder.isManualCast)
        {
            // 타겟 위치로 이동
            transform.DOMove(magicHolder.targetPos, speed);
        }

        // 효과음 재생
        SoundManager.Instance.PlaySound("LifeMushroom_Spawn", transform.position, 0.05f, atkNum, true);

        // 쿨다운 코루틴 변수에 넣기
        cooldownCoroutine = CastMagic.Instance.Cooldown(MagicDB.Instance.GetMagicByID(magicHolder.magic.id), coolTime);
        // 글로벌 쿨다운 시작
        StartCoroutine(cooldownCoroutine);
    }

    private void Update()
    {
        // 시간마다 현재 개수 검사
        if (respawnRecord <= Time.time)
        {
            // 쿨타임 카운트 갱신
            respawnRecord = Time.time + coolTime;

            // 자동 시전일때
            if (!magicHolder.isManualCast)
            {
                // 버섯 소환
                SummonMushroom();

                // 쿨타임 카운트 갱신
                respawnRecord = Time.time + coolTime;
            }
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        // 충돌시 파티클 이벤트 수집
        int events = ParticlePhysicsExtensions.GetCollisionEvents(particle, other, collisionEvents);

        for (int i = 0; i < events; i++)
        {
            // 몬스터에 충돌하면
            if (other.CompareTag(SystemManager.TagNameList.Enemy.ToString()))
            {
                // print($"Enemy : {other.name} : {other.tag} : {other.layer}");

                if (other.TryGetComponent(out HitBox enemyHitBox))
                {
                    // 독 도트 데미지 주기
                    StartCoroutine(enemyHitBox.Hit(magicHolder));
                }
            }
        }
    }
}
