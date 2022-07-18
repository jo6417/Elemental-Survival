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

    bool init = false; // 초기화 완료 여부

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
        init = false;

        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // magicHolder 초기화 완료까지 대기
        yield return new WaitUntil(() => magicHolder.init);

        // 슬로우 시간 갱신
        magicHolder.slowTime = MagicDB.Instance.MagicDuration(magic);

        // 발사할 거품 개수에 projectile값 갱신
        ParticleSystem.EmissionModule particleEmmision = bubbleParticle.emission;
        particleEmmision.SetBurst(0, new ParticleSystem.Burst(0, 1, MagicDB.Instance.MagicPierce(magic), 0.05f));

        // 파티클 속도에 speed값 갱신
        ParticleSystem.MainModule particleMain = bubbleParticle.main;
        particleMain.startSpeed = MagicDB.Instance.MagicSpeed(magic, true);

        // 타겟에 따라 파티클 충돌 대상 레이어 바꾸기
        ParticleSystem.CollisionModule particleColl = bubbleParticle.collision;
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
        bubbleParticle.Play();

        //초기화 완료
        init = true;
    }

    private void OnParticleCollision(GameObject other)
    {
        // 초기화 완료 전이면 리턴
        if (!init)
            return;

        ParticlePhysicsExtensions.GetCollisionEvents(bubbleParticle, other, collisionEvents);

        for (int i = 0; i < collisionEvents.Count; i++)
        {
            //todo 충돌 지점에 거품 터진 스프라이트 남기기

            // 플레이어에 충돌하면 데미지 주기
            if (other.CompareTag(SystemManager.TagNameList.Player.ToString()) && PlayerManager.Instance.hitBox.hitCoolCount <= 0 && !PlayerManager.Instance.isDash)
            {
                StartCoroutine(PlayerManager.Instance.hitBox.Hit(transform));
            }

            // 몬스터에 충돌하면 데미지 주기
            if (other.CompareTag(SystemManager.TagNameList.Enemy.ToString()))
            {
                // print($"{other.name} : {other.tag} : {other.layer}");

                if (other.TryGetComponent(out EnemyHitBox enemyHitBox))
                {
                    StartCoroutine(enemyHitBox.Hit(gameObject));
                }
            }
        }
    }
}
