using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using UnityEngine;

public class CastMagic : MonoBehaviour
{
    public Transform magicPool;
    List<int> nowCastMagic = new List<int>(); //현재 사용중인 마법

    void Update()
    {
        //플레이어 보유중인 모든 마법 ID
        List<int> hasMagicIDs = new List<int>();
        foreach (var magic in PlayerManager.Instance.hasMagics)
        {
            hasMagicIDs.Add(magic.id);
        }

        //미사용 중인 마법 있으면
        List<int> notCastMagic = hasMagicIDs.Except(nowCastMagic).ToList();
        if (notCastMagic.Count != 0 && MagicDB.Instance.loadDone)
        {
            foreach (var magicID in notCastMagic)
            {
                StartCoroutine(ShotMagic(magicID));

                //현재 사용중 마법 리스트에 추가
                nowCastMagic.Add(magicID);
            }
        }
    }

    IEnumerator ShotMagic(int magicID)
    {
        MagicInfo magic = MagicDB.Instance.GetMagicByID(magicID);

        // 마법 프리팹 없으면 넘기기
        var findPrefab = MagicDB.Instance.magicPrefab.Find(x => x.name == magic.magicName.Replace(" ", "") + "_Prefab");
        if (findPrefab == null)
        {
            // print("프리팹 없음, 코루틴 중단");
            yield break;
        }

        // 마법id에 해당하는 프리팹 투사체 갯수만큼 생성, onlyOne 속성이 1이면 하나만 발사
        // int projectileNum = magic.onlyOne == 1 ? 1 : PlayerManager.Instance.projectileNum;
        int projectileNum = magic.projectile + PlayerManager.Instance.projectileNum;
        for (int i = 0; i < projectileNum; i++)
        {
            GameObject magicObj = LeanPool.Spawn(
            findPrefab,
            transform.position,
            Quaternion.identity,
            magicPool);

            magicObj.GetComponent<MagicProjectile>().magic = magic; //마법 정보 넣기
            Rigidbody2D rigid = magicObj.GetComponent<Rigidbody2D>();

            Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            //마법 속도만큼 날리기 (벡터 기본값 5 * 스피드 스탯 * 10 / 100)
            rigid.velocity = dir.normalized * 50 *(magic.speed * 0.1f);

            yield return new WaitForSeconds(0.1f);
        }

        //마법 쿨타임 만큼 대기 (기본값 1초 - 스피드 스탯 * 10 / 100)
        yield return new WaitForSeconds(1 - magic.speed * 0.1f);

        StartCoroutine(ShotMagic(magicID));
    }
}
