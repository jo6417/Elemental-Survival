using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class DeathMineSpawner : MonoBehaviour
{
    MagicHolder magicHolder;
    MagicInfo magic;
    public GameObject minePrefab; //지뢰 프리팹

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 적이 죽을때 함수를 호출하도록 델리게이트에 넣기
        SystemManager.Instance.globalEnemyDeadCallback += DropMine;

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
        SystemManager.Instance.globalEnemyDeadCallback -= DropMine;
    }

    // 지뢰 드랍하기
    public void DropMine(Vector2 eventPos)
    {
        // print(MagicDB.Instance.MagicCritical(magic));

        // 크리티컬 확률 = 드랍 확률
        bool isDrop = MagicDB.Instance.MagicCritical(magic);

        //크리티컬 데미지 = 회복량
        int healAmount = Mathf.RoundToInt(MagicDB.Instance.MagicCriticalPower(magic));
        healAmount = (int)Mathf.Clamp(healAmount, 1f, healAmount); //최소 회복량 1f 보장

        // 마법 크리티컬 확률에 따라 지뢰 생성
        if (isDrop)
        {
            GameObject mushroom = LeanPool.Spawn(minePrefab, eventPos, Quaternion.identity, SystemManager.Instance.itemPool);
        }
    }
}
