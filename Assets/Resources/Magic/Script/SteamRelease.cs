using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamRelease : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] ParticleManager particleManager;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>(); //충돌한 파티클의 이벤트 정보들

    bool initDone = false; // 초기화 완료 여부

    [Header("Stat")]
    float power;
    float duration;
    float speed;
    float range;
    float scale;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //초기화 완료 안됨
        initDone = false;

        yield return new WaitUntil(() => magicHolder.magic != null);

        // magicHolder 초기화 완료까지 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);

        power = MagicDB.Instance.MagicPower(magicHolder.magic);
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);
        speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, true);
        range = MagicDB.Instance.MagicRange(magicHolder.magic) / 10f;
        scale = MagicDB.Instance.MagicScale(magicHolder.magic);

        // 데미지 갱신
        magicHolder.power = power;

        // 젖음 시간 갱신 (젖은 동안 전기 데미지 증가)
        // magicHolder.wetTime = MagicDB.Instance.MagicDuration(magic);
        // 넉백 넣기
        magicHolder.knockbackForce = 1f;

        ParticleSystem.MainModule particleMain = particleManager.particle.main;

        // range 만큼 파티클 거리 갱신
        // particleMain.startLifetime = new ParticleSystem.MinMaxCurve(range, range + 1f);

        // 파티클을 duration 만큼 반복
        // ParticleSystem.EmissionModule particleEmmision = particleManager.particle.emission;
        // particleEmmision.SetBurst(0, new ParticleSystem.Burst(0, 10, (int)(10f * duration), 0.1f));

        // 파티클 size에 scale값 반영
        particleMain.startSize = new ParticleSystem.MinMaxCurve(scale, scale * 3f);
        // 파티클 속도에 speed값 갱신
        particleMain.startSpeed = new ParticleSystem.MinMaxCurve(speed * 10f, speed * 20f);

        // 타겟에 따라 파티클 충돌 대상 레이어 바꾸기
        ParticleSystem.CollisionModule particleColl = particleManager.particle.collision;

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
        particleManager.particle.Play();

        //초기화 완료
        initDone = true;
    }

    private void OnParticleCollision(GameObject other)
    {
        // 초기화 완료 전이면 리턴
        if (!initDone)
            return;

        ParticlePhysicsExtensions.GetCollisionEvents(particleManager.particle, other, collisionEvents);

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
}
