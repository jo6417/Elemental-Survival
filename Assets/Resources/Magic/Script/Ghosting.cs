using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using DG.Tweening;

public class Ghosting : MonoBehaviour
{
    MagicHolder magicHolder;
    [SerializeField] GameObject wifiEffect; // 와이파이 모양 버프 이펙트

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
    }

    IEnumerator Init()
    {
        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);

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
        {
            SystemManager.Instance.globalEnemyDeadCallback -= SummonGhost;
        }
    }

    // 몬스터 유령 생성하기
    public void SummonGhost(Character character)
    {
        // 캐릭터 정보 없거나 이미 고스트면 리턴
        if (character == null || character.enemy == null || character.IsGhost) return;
        // 보스일때 리턴
        if (character.enemy.enemyType == EnemyDB.EnemyType.Boss.ToString()) return;

        // 크리티컬 확률 = 소환 확률
        float summonRate = MagicDB.Instance.MagicCriticalRate(magicHolder.magic);
        // 크리티컬 아니면 고스트 소환 실패, 리턴
        if (Random.value > summonRate)
            return;

        //몬스터 프리팹 찾기
        GameObject ghostPrefab = EnemyDB.Instance.GetPrefab(character.enemy.id);
        // 몬스터 프리팹 소환 및 비활성화
        GameObject ghostObj = LeanPool.Spawn(ghostPrefab, character.transform.position, Quaternion.identity, ObjectPool.Instance.enemyPool);
        // 고스트 매니저 찾기
        Character ghostCharacter = ghostObj.GetComponent<Character>();

        // 해당 유령은 고스트로 바꾸기 예약
        ghostCharacter.changeGhost = true;

        // 버프 이용해서 머리위에 와이파이 이펙트 붙이기
        ghostCharacter.SetBuff("Ghosting", "", true, 1, -1, false, ghostCharacter.buffParent, wifiEffect);

        // 일정 시간 이후 해당 고스트는 죽음
        StartCoroutine(ghostCharacter.AutoKill(magicHolder.duration));
    }
}
