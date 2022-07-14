using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitBox : MonoBehaviour
{
    public EnemyManager enemyManager;

    private void OnParticleCollision(GameObject other)
    {
        // 파티클 피격 딜레이 중이면 리턴
        if (enemyManager.particleHitCount > 0)
            return;

        // 마법 파티클이 충돌했을때
        if (other.transform.CompareTag("Magic") && !enemyManager.isDead)
        {
            StartCoroutine(enemyManager.Hit(other.gameObject));

            //파티클 피격 딜레이 시작
            enemyManager.particleHitCount = 0.2f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 피격 딜레이 중이면 리턴
        if (enemyManager.hitCount > 0)
            return;

        // 마법이 충돌했을때
        if (other.transform.CompareTag("Magic"))
        {
            // 마법 정보 찾기
            MagicHolder magicHolder = other.GetComponent<MagicHolder>();
            MagicInfo magic = magicHolder.magic;

            StartCoroutine(enemyManager.Hit(other.gameObject));
        }

        //적에게 맞았을때
        if (other.transform.CompareTag("Enemy"))
        {
            // 활성화 되어있는 EnemyAtk 컴포넌트 찾기
            if (other.gameObject.TryGetComponent(out EnemyAttack enemyAtk)
            || other.gameObject.TryGetComponent(out MagicHolder magicHolder))
            {
                StartCoroutine(enemyManager.Hit(other.gameObject));
            }
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
                StartCoroutine(enemyManager.Hit(other.gameObject));
        }
    }
}
