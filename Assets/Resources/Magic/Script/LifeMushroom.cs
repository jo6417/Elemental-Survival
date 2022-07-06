using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class LifeMushroom : MonoBehaviour
{
    MagicHolder magicHolder;
    MagicInfo magic;
    public GameObject lifeMushroom; // 회복 버섯

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 적이 죽을때 함수를 호출하도록 델리게이트에 넣기
        SystemManager.Instance.globalEnemyDeadCallback += DropLifeSeed;

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
        SystemManager.Instance.globalEnemyDeadCallback -= DropLifeSeed;
    }

    // Life Seed 드랍하기
    public void DropLifeSeed(Vector2 eventPos)
    {
        // print(MagicDB.Instance.MagicCritical(magic));

        // 크리티컬 확률 = 드랍 확률
        bool isDrop = MagicDB.Instance.MagicCritical(magic);

        //크리티컬 데미지 = 회복량
        int healAmount = Mathf.RoundToInt(MagicDB.Instance.MagicCriticalPower(magic));
        healAmount = (int)Mathf.Clamp(healAmount, 1f, healAmount); //최소 회복량 1f 보장

        // HealSeed 마법 크리티컬 확률에 따라 드랍
        if (isDrop)
        {
            GameObject mushroom = LeanPool.Spawn(lifeMushroom, eventPos, Quaternion.identity, SystemManager.Instance.itemPool);

            // 아이템에 체력 회복량 넣기
            mushroom.GetComponent<ItemManager>().amount = healAmount;

            //아이템 리지드 찾기
            Rigidbody2D itemRigid = mushroom.GetComponent<Rigidbody2D>();

            // 랜덤 방향으로 아이템 날리기
            itemRigid.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * Random.Range(3f, 5f);

            // 아이템 랜덤 회전 시키기
            itemRigid.angularVelocity = Random.value < 0.5f ? 360f : -360f;
        }
    }
}
