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
            // 활성화 되어있는 EnemyAtk 컴포넌트 찾기
            if (other.gameObject.TryGetComponent<EnemyAttack>(out EnemyAttack enemyAtk) && enemyAtk.enabled)
            {
                EnemyManager hitEnemy = enemyAtk.enemyManager;

                // other가 본인일때 리턴
                if (hitEnemy == enemyManager || hitEnemy == enemyManager.referEnemyManager)
                {
                    // print("본인 타격");
                    return;
                }

                if (hitEnemy.enabled)
                {
                    // 아군 피해 줄때
                    if (enemyAtk.friendlyFire)
                    {
                        // print("enemy damage");

                        // 데미지 입기
                        enemyManager.Damage(hitEnemy.enemy.power, false);
                    }

                    // 넉백 디버프 있을때
                    if (enemyAtk.knockBackDebuff)
                    {
                        // print("enemy knock");

                        // 넉백
                        StartCoroutine(enemyManager.Knockback(hitEnemy.enemy.power));
                    }

                    // flat 디버프 있을때, stop 카운트 중 아닐때
                    if (enemyAtk.flatDebuff && enemyManager.stopCount <= 0)
                    {
                        // print("enemy flat");

                        // 납작해지고 행동불능
                        StartCoroutine(enemyManager.FlatDebuff());
                    }
                }
            }
        }
    }
}
