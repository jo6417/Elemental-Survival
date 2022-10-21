using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class StickyBubble : MonoBehaviour
{
    [Header("Refer")]
    public MagicHolder magicHolder;
    public MagicInfo magic;
    public ParticleSystem bubbleParticle;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>(); //충돌한 파티클의 이벤트 정보들

    bool initDone = false; // 초기화 완료 여부

    private void Awake()
    {
        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;
        bubbleParticle = bubbleParticle == null ? GetComponent<ParticleSystem>() : bubbleParticle;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //초기화 완료 안됨
        initDone = false;

        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // magicHolder 초기화 완료까지 대기
        yield return new WaitUntil(() => magicHolder.initDone);

        // 슬로우 시간 갱신
        // magicHolder.slowTime = MagicDB.Instance.MagicDuration(magic);

        // 발사할 파티클 개수에 atkNum값 갱신
        ParticleSystem.EmissionModule particleEmmision = bubbleParticle.emission;
        particleEmmision.SetBurst(0, new ParticleSystem.Burst(0, 1, MagicDB.Instance.MagicPierce(magic), 0.05f));

        // 파티클 속도에 speed값 갱신
        ParticleSystem.MainModule particleMain = bubbleParticle.main;
        particleMain.startSpeed = MagicDB.Instance.MagicSpeed(magic, true);

        // 타겟에 따라 파티클 충돌 대상 레이어 바꾸기
        ParticleSystem.CollisionModule particleColl = bubbleParticle.collision;

        // 플레이어가 쐈을때, 몬스터가 타겟
        if (magicHolder.GetTarget() == MagicHolder.Target.Enemy)
        {
            gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
            particleColl.collidesWith = SystemManager.Instance.layerList.EnemyHit_Mask;
        }

        // 몬스터가 쐈을때, 플레이어가 타겟
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
        bubbleParticle.Play();

        //초기화 완료
        initDone = true;
    }

    private void OnParticleCollision(GameObject other)
    {
        // 초기화 완료 전이면 리턴
        if (!initDone)
            return;

        ParticlePhysicsExtensions.GetCollisionEvents(bubbleParticle, other, collisionEvents);

        for (int i = 0; i < collisionEvents.Count; i++)
        {
            // 플레이어에 충돌하면 데미지 주기
            if (other.CompareTag(SystemManager.TagNameList.Player.ToString()) && PlayerManager.Instance.hitBox.hitCoolCount <= 0 && !PlayerManager.Instance.isDash)
            {
                StartCoroutine(PlayerManager.Instance.hitBox.Hit(magicHolder));
            }

            // 몬스터에 충돌하면 데미지 주기
            if (other.CompareTag(SystemManager.TagNameList.Enemy.ToString()))
                if (other.TryGetComponent(out HitBox enemyHitBox))
                {
                    StartCoroutine(enemyHitBox.Hit(magicHolder));
                }
        }
    }
}
