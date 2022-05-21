using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyExplosion : MonoBehaviour
{
    RaycastHit2D[] hitObjects;
    public CircleCollider2D coll;
    bool explodeTriggerOn;

    private void OnTriggerEnter2D(Collider2D other)
    {
        //TODO 플레이어 들어오면 트리거 작동
        if (other.CompareTag("Player") && !explodeTriggerOn)
        {
            explodeTriggerOn = true;

            Explosion();
        }
    }

    IEnumerator Explosion()
    {
        //TODO 범위 내 플레이어, 몬스터 모두 데미지 및 넉백
        int castNum = coll.Cast(Vector2.zero, hitObjects);
        print(castNum);
        foreach (var obj in hitObjects)
        {
            print(obj.collider.name);
        }
        
        //TODO 폭발 이펙트 남기기
        //TODO 해당 몬스터 디스폰

        yield return null;
    }
}
