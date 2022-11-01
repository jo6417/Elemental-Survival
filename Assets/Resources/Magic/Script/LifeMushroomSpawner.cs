using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class LifeMushroomSpawner : MonoBehaviour
{
    MagicHolder magicHolder;
    MagicInfo magic;
    public GameObject lifeMushroom; // 회복 버섯
    public GameObject poisonSmokeEffect; //독 연기 이펙트
    float duration;

    ParticleSystem particle;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>(); //충돌한 파티클의 이벤트 정보들

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
        particle = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 지속시간 계산
        duration = MagicDB.Instance.MagicDuration(magic);

        // 독 도트뎀 지속시간에 반영
        magicHolder.poisonTime = duration;

        // 적이 죽을때 함수를 호출하도록 델리게이트에 넣기
        SystemManager.Instance.globalEnemyDeadCallback += DropLifeSeed;

        //플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform);
        transform.localPosition = Vector3.zero;
    }

    private void OnDisable()
    {
        // 해당 마법 장착 해제되면 델리게이트에서 함수 빼기
        SystemManager.Instance.globalEnemyDeadCallback -= DropLifeSeed;
    }

    private void Update()
    {
        //todo 쿨타임마다 Burst로 파티클 atkNum만큼 방출
        //todo 방출할때 사운드 재생
    }

    // 파티클에 충돌한 몬스터 감지
    private void OnParticleCollision(GameObject other)
    {
        ParticlePhysicsExtensions.GetCollisionEvents(particle, other, collisionEvents);

        for (int i = 0; i < collisionEvents.Count; i++)
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

    // 버섯 드랍하기
    public void DropLifeSeed(Character character)
    {
        // print(MagicDB.Instance.MagicCritical(magic));

        // 독 데미지 받는중에만 진행
        if (character.DebuffList[(int)Character.Debuff.Poison] != null)
            return;

        // 크리티컬 확률 = 드랍 확률
        bool isDrop = MagicDB.Instance.MagicCritical(magic);

        //크리티컬 데미지 = 회복량
        int healAmount = Mathf.RoundToInt(MagicDB.Instance.MagicCriticalPower(magic));
        healAmount = (int)Mathf.Clamp(healAmount, 1f, healAmount); //최소 회복량 1f 보장

        // HealSeed 마법 크리티컬 확률에 따라 드랍
        if (isDrop)
        {
            GameObject mushroom = LeanPool.Spawn(lifeMushroom, character.transform.position, Quaternion.identity, SystemManager.Instance.itemPool);

            //todo 버섯 드랍시 사운드 재생
            SoundManager.Instance.SoundPlay("LifeMushroom_Spawn", transform);

            SpriteRenderer mushroomSprite = mushroom.GetComponent<SpriteRenderer>();

            // 아이템에 체력 회복량 넣기
            mushroom.GetComponent<ItemManager>().amount = healAmount;

            //아이템 리지드 찾기
            Rigidbody2D itemRigid = mushroom.GetComponent<Rigidbody2D>();

            // 랜덤 방향으로 아이템 날리기
            itemRigid.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * Random.Range(3f, 5f);

            // 아이템 랜덤 회전 시키기
            itemRigid.angularVelocity = Random.value < 0.5f ? 360f : -360f;

            // 점점 썩어서 사라지기
            mushroomSprite.DOColor(Color.clear, duration)
            .SetEase(Ease.InExpo)
            .OnComplete(() =>
            {
                // 버섯이 살아있을때
                if (mushroom.activeInHierarchy)
                {
                    //디스폰
                    LeanPool.Despawn(mushroom);

                    //이펙트 소환
                    LeanPool.Spawn(poisonSmokeEffect, mushroom.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
                }
            });
        }
    }
}
