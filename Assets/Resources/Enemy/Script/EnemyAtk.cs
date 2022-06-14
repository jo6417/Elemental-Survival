using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAtk : MonoBehaviour
{
    public EnemyManager enemyManager;
    public string enemyName;

    [Header("Attack Effect")]
    public bool friendlyFire = false; // 충돌시 아군 피해 여부
    public bool flatDebuff = false; //납작해지는 디버프
    public bool knockBackDebuff = false; //넉백 디버프

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => enemyManager.enemy != null);

        // 적 정보 들어오면 이름 표시
        enemyName = enemyManager.enemy.enemyName;
    }
}
