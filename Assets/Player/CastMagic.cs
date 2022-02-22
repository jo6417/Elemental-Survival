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
        //미사용 중인 마법 있으면
        List<int> notCastMagic = PlayerManager.Instance.hasMagics.Except(nowCastMagic).ToList();
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
        int projectileNum = magic.onlyOne == 1 ? 1 : PlayerManager.Instance.projectileNum;
        for (int i = 0; i < projectileNum; i++)
        {

            GameObject magicObj = LeanPool.Spawn(
            findPrefab,
            transform.position,
            Quaternion.identity,
            magicPool);

            magicObj.GetComponent<MagicProjectile>().magicID = magicID; //마법 정보 넣기
            Rigidbody2D rigid = magicObj.GetComponent<Rigidbody2D>();

            Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            rigid.velocity = dir.normalized * magic.speed * 5; //마법 날리기

            yield return new WaitForSeconds(0.1f);
        }

        //마법 쿨타임 만큼 대기
        yield return new WaitForSeconds(magic.coolTime);

        StartCoroutine(ShotMagic(magicID));
    }
}
