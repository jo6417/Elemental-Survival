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
        SystemManager.Instance.globalEnemyDeadCallback -= SummonGhost;
    }

    // 몬스터 유령 생성하기
    public void SummonGhost(Vector2 eventPos)
    {
        // print(MagicDB.Instance.MagicCritical(magic));

        // 크리티컬 확률 = 소환 확률
        bool isDrop = MagicDB.Instance.MagicCritical(magic);

        //크리티컬 데미지 = 소환 몬스터 체력 추가
        int healAmount = Mathf.RoundToInt(MagicDB.Instance.MagicCriticalPower(magic));

        //todo 이 함수를 실행하는 몬스터의 enemyManager에서 포탈을 생성해야함, enemy 정보가 필요

        // 마법 크리티컬 확률에 따라 유령 소환
        // if (isDrop)
        // {
        //     //todo 유령 포탈에서 소환
        //     //todo 포탈 스프라이트는 끄기
        //     GameObject ghost = LeanPool.Spawn(ghostPrefab, eventPos, Quaternion.identity, SystemManager.Instance.itemPool);

        //     // 매니저 찾기
        //     EnemyManager enemyManager = ghost.GetComponent<EnemyManager>();

        //     //todo 모든 스프라이트 유령색으로
        //     foreach (SpriteRenderer sprite in enemyManager.spriteList)
        //     {
        //         sprite.color = new Color(0, 1, 1, 0.5f);
        //     }

        //     //todo 초기화 하기전에 유령 공격 타겟 변경
        //     enemyManager.ChangeTarget(null);

        //     // 몬스터 초기화 시작
        //     enemyManager.initialStart = true;
        // }
    }
}
