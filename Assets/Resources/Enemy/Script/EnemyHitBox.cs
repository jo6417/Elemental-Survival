using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitBox : MonoBehaviour
{
    public EnemyManager enemyManager;

    private void OnParticleCollision(GameObject other)
    {
        // 마법 파티클 충돌했을때
        if (other.transform.CompareTag("Magic") && !enemyManager.isDead && enemyManager.particleHitCount <= 0)
        {
            enemyManager.HitMagic(other.gameObject);

            //파티클 피격 딜레이 시작
            enemyManager.particleHitCount = 0.2f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 마법 트리거 충돌 했을때
        if (other.CompareTag("Magic") && enemyManager.hitCount <= 0)
        {
            enemyManager.HitMagic(other.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 계속 마법 트리거 콜라이더 안에 있을때
        if (other.transform.CompareTag("Magic") && enemyManager.hitCount <= 0)
        {
            // 마법 정보 찾기
            MagicHolder magicHolder = other.GetComponent<MagicHolder>();
            MagicInfo magic = magicHolder.magic;

            // 다단히트 마법일때만
            if (magic.multiHit)
                enemyManager.HitMagic(other.gameObject);
        }

        //적에게 맞았을때
        if (other.transform.CompareTag("EnemyAttack") && enemyManager.hitCount <= 0)
        {
            StartCoroutine(Hit(other.gameObject));
        }
    }

    IEnumerator Hit(GameObject other)
    {
        // 활성화 되어있는 EnemyAtk 컴포넌트 찾기
        if (other.gameObject.TryGetComponent<EnemyAttack>(out EnemyAttack enemyAtk) && enemyAtk.enabled)
        {
            // 공격한 몹 매니저
            EnemyManager atkEnemyManager = enemyAtk.enemyManager;

            // 공격한 몹의 정보가 들어올때까지 대기
            yield return new WaitUntil(() => enemyAtk.enemy != null);
            EnemyInfo atkEnemy = enemyAtk.enemy;

            // other가 본인일때 리턴
            if (atkEnemyManager == enemyManager)
            {
                // print("본인 타격");
                yield break;
            }

            // 타격한 적이 비활성화 되었으면 리턴
            // if (!hitEnemyManager.enabled)
            //     return;

            //피격 대상이 고스트일때
            if (enemyManager.IsGhost)
            {
                //고스트 아닌 적이 때렸을때만 데미지
                if (!atkEnemyManager.IsGhost)
                    enemyManager.Damage(atkEnemy.power, false);
            }
            //피격 대상이 고스트 아닐때
            else
            {
                //고스트가 때렸으면 데미지
                if (atkEnemyManager.IsGhost)
                    enemyManager.Damage(atkEnemy.power, false);

                // 아군 피해 옵션 켜져있을때
                if (enemyAtk.friendlyFire)
                    enemyManager.Damage(atkEnemy.power, false);
            }

            // 넉백 디버프 있을때
            if (enemyAtk.knockBackDebuff)
            {
                // print("enemy knock");

                // 넉백
                StartCoroutine(enemyManager.Knockback(other.gameObject, atkEnemyManager.enemy.power));
            }

            // flat 디버프 있을때, stop 카운트 중 아닐때
            if (enemyAtk.flatDebuff && enemyManager.stopCount <= 0)
            {
                // print("enemy flat");

                // 납작해지고 행동불능
                StartCoroutine(enemyManager.FlatDebuff());
            }
        }

        //마법 정보 찾기
        if (other.TryGetComponent(out MagicHolder magicHolder))
        {
            //적의 마법에 충돌
            if (magicHolder != null && magicHolder.enabled)
            {
                MagicInfo magic = magicHolder.magic;

                //데미지 계산
                float damage = MagicDB.Instance.MagicPower(magic);
                // 고정 데미지에 확률 계산
                damage = Random.Range(damage * 0.8f, damage * 1.2f);

                //크리티컬 성공 여부
                bool isCritical = MagicDB.Instance.MagicCritical(magic);
                //크리티컬 데미지 계산
                float criticalPower = MagicDB.Instance.MagicCriticalPower(magic);

                //크리티컬 곱해도 데미지가 그대로면 크리티컬 아님
                if (Mathf.RoundToInt(damage) >= Mathf.RoundToInt(damage * criticalPower))
                {
                    isCritical = false;
                }
                else
                {
                    damage = damage * criticalPower;
                }

                enemyManager.Damage(damage, isCritical);
            }
        }
    }
}
