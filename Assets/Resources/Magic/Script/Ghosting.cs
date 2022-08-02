using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using DG.Tweening;

public class Ghosting : MonoBehaviour
{
    MagicHolder magicHolder;
    MagicInfo magic;

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
    }

    IEnumerator Init()
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
        StartCoroutine(Init());
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
        if (!enemyManager.IsGhost && enemyManager.enemy.enemyType != "boss")
        {
            //몬스터 프리팹 찾기
            GameObject ghostPrefab = EnemyDB.Instance.GetPrefab(enemyManager.enemy.id);

            // 몬스터 프리팹 소환 및 비활성화
            GameObject ghostObj = LeanPool.Spawn(ghostPrefab, enemyManager.transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

            // 몬스터 매니저 찾기
            EnemyManager ghostManager = ghostObj.GetComponent<EnemyManager>();

            // 해당 유령은 고스트로 바꾸기 예약
            ghostManager.changeGhost = true;

            for (int i = 0; i < ghostManager.spriteList.Count; i++)
            {
                // 스프라이트 투명하게
                ghostManager.spriteList[i].color = Color.clear;
                // 머터리얼 초기화
                ghostManager.spriteList[i].material = SystemManager.Instance.outLineMat;
            }

            // 타겟 null로 초기화
            ghostManager.TargetObj = null;

            for (int i = 0; i < ghostManager.spriteList.Count; i++)
            {
                // 스프라이트 유령색으로 바꾸기
                // ghostManager.spriteList[i].
                ghostManager.spriteList[i].DOColor(new Color(0, 1, 1, 0.5f), 2f)
                .OnComplete(() =>
                {
                    // 마지막 스프라이트일때
                    if (i == ghostManager.spriteList.Count)
                    {
                        // 소환된 몬스터 초기화 시작
                        ghostManager.initialStart = true;
                    }
                });
            }
        }
    }
}
