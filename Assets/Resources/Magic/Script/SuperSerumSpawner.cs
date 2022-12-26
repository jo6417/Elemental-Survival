using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class SuperSerumSpawner : MonoBehaviour
{
    MagicHolder mineMagicHolder;
    MagicInfo magic;
    public GameObject serumOrbPrefab; //슈퍼 세럼 알갱이 프리팹

    private void Awake()
    {
        mineMagicHolder = GetComponent<MagicHolder>();
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => mineMagicHolder.magic != null);
        magic = mineMagicHolder.magic;

        // 적이 죽을때 함수를 호출하도록 델리게이트에 넣기
        SystemManager.Instance.globalEnemyDeadCallback += DropSerumOrb;

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
        SystemManager.Instance.globalEnemyDeadCallback -= DropSerumOrb;
    }

    // 슈퍼세럼 오브 드랍하기
    public void DropSerumOrb(Character character)
    {
        // print(MagicDB.Instance.MagicCritical(magic));

        // 크리티컬 확률 = 드랍 확률
        bool isDrop = MagicDB.Instance.MagicCritical(magic);

        if (isDrop)
        {
            // 적을 체력 오브 드랍
            GameObject serumOrb = LeanPool.Spawn(serumOrbPrefab, character.transform.position, Quaternion.identity, ObjectPool.Instance.itemPool);

            // 매직홀더 찾기
            MagicHolder orbMagicHolder = serumOrb.GetComponentInChildren<MagicHolder>();

            // 마법 정보 전달
            orbMagicHolder.magic = magic;
        }
    }
}
