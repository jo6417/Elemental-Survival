using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class Ghosting : MonoBehaviour
{
    MagicHolder magicHolder;
    MagicInfo magic;
    public GameObject ghostPrefab; //지뢰 프리팹

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 적이 죽을때 함수를 호출하도록 델리게이트에 넣기
        SystemManager.Instance.globalEnemyDeadCallback += SummonGhost;

        //플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform);
        transform.localPosition = Vector3.zero;
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    private void OnDisable()
    {
        // 해당 마법 장착 해제되면 델리게이트에서 함수 빼기
        if (SystemManager.Instance != null)
            SystemManager.Instance.globalEnemyDeadCallback -= SummonGhost;
    }

    // 몬스터 유령 생성하기
    public void SummonGhost(EnemyManager enemyManager)
    {
        // print(MagicDB.Instance.MagicCritical(magic));

        // 크리티컬 확률 = 소환 확률
        bool isDrop = MagicDB.Instance.MagicCritical(magic);

        //크리티컬 데미지 = 소환 몬스터 체력 추가
        int healAmount = Mathf.RoundToInt(MagicDB.Instance.MagicCriticalPower(magic));

        // 이미 유령 아닐때, 보스 아닐때
        if (!enemyManager.isGhost && enemyManager.enemy.enemyType != "boss")
            // 포탈에서 몬스터 유령 소환
            StartCoroutine(EnemySpawn.Instance.PortalSpawn(enemyManager.enemy, false, enemyManager.transform.position, null, true));
    }
}
