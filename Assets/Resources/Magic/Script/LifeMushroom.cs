using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class LifeMushroom : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] Rigidbody2D rigid;
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] ParticleSystem mushroomAttack; // 버섯 공격 오브젝트
    [SerializeField] ParticleSystem atkParticle; // 공격시 시작 위치에 파티클 재생
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>(); //충돌한 파티클의 이벤트 정보들

    [Header("State")]
    float speed;
    int atkNum;
    float coolTime;
    float respawnRecord = 0;
    IEnumerator attackCoroutine = null;

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
        ParticleSystem.MainModule particleMain = mushroomAttack.main;
        ParticleSystem.EmissionModule particleEmission = mushroomAttack.emission;
        ParticleSystem.VelocityOverLifetimeModule particleVelocity = mushroomAttack.velocityOverLifetime;

        // 독 도트 데미지 지속시간 초기화
        magicHolder.poisonTime = duration;
        // 파티클 지속 시간 초기화
        particleMain.startLifetime = duration;
        // 파티클 속도 초기화
        particleVelocity.orbitalZ = speed;
        // 파티클 사이즈 초기화
        particleMain.startSize = range;
        // 파티클 개수 초기화
        ParticleSystem.Burst burst = mushroomAttack.emission.GetBurst(0);
        burst.count = atkNum;
        particleEmission.SetBurst(0, burst);

        // 파티클 공격 실행
        print("Play");
        mushroomAttack.Play();

        // 효과음 재생
        SoundManager.Instance.PlaySound("LifeMushroom_Spawn", transform.position, 0, 0.05f, atkNum, true);

        // 자동 시전일때
        if (!magicHolder.isManualCast)
        {
            // 플레이어 위치에 자식으로 들어가기
            transform.SetParent(PlayerManager.Instance.magicParent);
            transform.localPosition = Vector3.zero;
        }
        // 수동 시전일때
        else
        {
            // 타겟 위치로 날아가기
            rigid.velocity = (magicHolder.targetPos - transform.position).normalized * speed;
        }

        // if (magicHolder.magicCastCallback == null)
        //     magicHolder.magicCastCallback = SummonMushroom;

        // // 기존에 진행중인 자동 소환 중지
        // if (attackCoroutine != null)
        //     StopCoroutine(attackCoroutine);

        // // 자동 시전일때
        // if (!magicHolder.isManualCast)
        // {
        //     // 버섯 자동 소환
        //     attackCoroutine = AutoSpawn();
        //     StartCoroutine(attackCoroutine);
        // }
    }

    // IEnumerator AutoSpawn()
    // {
    //     // 자동 시전일때
    //     if (!magicHolder.isManualCast)
    //         // 버섯 파티클 소환
    //         SummonMushroom();

    //     // 쿨타임 대기
    //     // yield return new WaitForSeconds(coolTime);
    //     MagicInfo magic = MagicDB.Instance.GetMagicByID(magicHolder.magic.id);
    //     yield return new WaitUntil(() => magic.coolCount <= 0);

    //     // 자동 소환 다시 시전
    //     attackCoroutine = AutoSpawn();
    //     StartCoroutine(attackCoroutine);
    // }

    // void SummonMushroom()
    // {
    //     // 공격시 포자 이펙트 재생
    //     atkParticle.Play();

    //     // 부모 결정
    //     Transform parent = magicHolder.isManualCast ? ObjectPool.Instance.magicPool : PlayerManager.Instance.magicParent;
    //     // 버섯 소환
    //     ParticleSystem attack = LeanPool.Spawn(mushroomAttack, transform.position, Quaternion.identity, parent);

    //     // 마법 정보 전달
    //     MagicHolder attackMagicHolder = attack.GetComponent<MagicHolder>();
    //     attackMagicHolder.magic = magicHolder.magic;
    //     attackMagicHolder.poisonTime = magicHolder.poisonTime;

    //     // 플레이어가 쓴 마법일때
    //     if (magicHolder.GetTarget() == Attack.TargetType.Enemy)
    //         // 수동 시전일때
    //         if (magicHolder.isManualCast)
    //         {
    //             // 마우스 근처 위치
    //             Vector2 targetPos = PlayerManager.Instance.GetMousePos() + (Vector3)Random.insideUnitCircle * 2f;

    //             // 마우스 위치로 이동
    //             attack.transform.DOMove(targetPos, speed);
    //         }

    //     // 효과음 재생
    //     SoundManager.Instance.PlaySound("LifeMushroom_Spawn", transform.position, 0, 0.05f, atkNum, true);

    //     // 쿨다운 시작
    //     // CastMagic.Instance.Cooldown(magicHolder.magic, magicHolder.isManualCast, coolTime);
    // }

    private void OnParticleCollision(GameObject other)
    {
        // 충돌시 파티클 이벤트 수집
        int events = ParticlePhysicsExtensions.GetCollisionEvents(mushroomAttack, other, collisionEvents);

        for (int i = 0; i < events; i++)
        {
            // 몬스터에 충돌하면
            if (other.CompareTag(TagNameList.Enemy.ToString()))
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
