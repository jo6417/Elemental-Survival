using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using UnityEngine;

public class CastMagic : MonoBehaviour
{
    #region Singleton
    private static CastMagic instance;
    public static CastMagic Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<CastMagic>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<CastMagic>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public Transform magicPool;
    List<int> nowCastMagic = new List<int>(); //현재 사용중인 마법

    public void StartCastMagic()
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
                MagicInfo magic = MagicDB.Instance.GetMagicByID(magicID);

                // 마법 프리팹 없으면 넘기기
                var magicPrefab = MagicDB.Instance.magicPrefab.Find(x => x.name == magic.magicName.Replace(" ", "") + "_Prefab");
                if (magicPrefab == null)
                {
                    // print("프리팹 없음");
                    continue;
                }

                //마법 투사체일때
                if (magicPrefab.TryGetComponent(out MagicProjectile magicPro))
                {
                    // print(magic.magicName + " : 투사체 마법");
                    StartCoroutine(ShotMagic(magicPrefab, magic));
                }
                //소환하는 마법일때
                else if (magicPrefab.TryGetComponent(out MagicFalling magicFall))
                {
                    // print(magic.magicName + " : 소환 마법");
                    StartCoroutine(SummonMagic(magicPrefab, magic));
                }

                //현재 사용중 마법 리스트에 추가
                nowCastMagic.Add(magicID);
            }
        }
    }

    //소환 마법
    IEnumerator SummonMagic(GameObject magicPrefab, MagicInfo magic)
    {
        // 랜덤 적 찾기, 투사체 수 이하로
        List<Vector2> enemyPos = MarkEnemyPos(magic);

        // 찾은 적 리스트 개수만큼 반복
        for (int i = 0; i < enemyPos.Count; i++)
        {
            // 해당 적 위치에 마법 생성
            GameObject magicObj = LeanPool.Spawn(magicPrefab, enemyPos[i], Quaternion.identity);

            MagicFalling magicFall = magicObj.GetComponent<MagicFalling>();
            //마법 정보 넣기
            magicFall.magic = magic;
            // 히트박스 크기 반영
            Vector2 originColSize = magicObj.GetComponent<MagicFalling>().originColScale;
            SetHitBox(magicObj, originColSize, magic);
        }

        //마법 쿨타임 만큼 대기
        float coolTime = magic.coolTime - magic.speed * 0.1f;
        coolTime = coolTime - coolTime * (PlayerManager.Instance.coolTime - 1);
        coolTime = Mathf.Clamp(coolTime, 0.1f, 10f);

        yield return new WaitForSeconds(coolTime);

        StartCoroutine(SummonMagic(magicPrefab, magic));
    }

    //투사체 마법
    IEnumerator ShotMagic(GameObject magicPrefab, MagicInfo magic)
    {
        // 마법id에 해당하는 프리팹 투사체 갯수만큼 생성, onlyOne 속성이 1이면 하나만 발사
        // int projectileNum = magic.onlyOne == 1 ? 1 : PlayerManager.Instance.projectileNum;
        int projectileNum = magic.projectile + PlayerManager.Instance.projectileNum;
        for (int i = 0; i < projectileNum; i++)
        {
            // 마법 오브젝트 생성
            GameObject magicObj = LeanPool.Spawn(
            magicPrefab,
            transform.position,
            Quaternion.identity,
            magicPool);

            // 마법 range 속성으로 히트박스 크기 늘리기
            Vector2 originColSize = magicObj.GetComponent<MagicProjectile>().originColScale;
            SetHitBox(magicObj, originColSize, magic);

            //마법 정보 넣기
            magicObj.GetComponent<MagicProjectile>().magic = magic;
            
            //마법 속도만큼 날리기 (벡터 기본값 5 * 스피드 스탯 * 10 / 100)
            Rigidbody2D rigid = magicObj.GetComponent<Rigidbody2D>();
            Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            rigid.velocity = dir.normalized * 50 * (magic.speed * 0.1f);

            yield return new WaitForSeconds(0.1f);
        }

        //마법 쿨타임 만큼 대기 (기본값 - 스피드 스탯 * 10 / 100)
        yield return new WaitForSeconds(magic.coolTime - magic.speed * 0.1f);

        StartCoroutine(ShotMagic(magicPrefab, magic));
    }

    List<Vector2> MarkEnemyPos(MagicInfo magic)
    {
        List<Vector2> enemyPos = new List<Vector2>();

        //캐릭터 주변의 적들
        Collider2D[] colls = null;
        float range = 2f * magic.range * PlayerManager.Instance.range;
        colls = Physics2D.OverlapCircleAll(PlayerManager.Instance.transform.position, range, 1 << LayerMask.NameToLayer("Enemy"));

        // 투사체 개수 (마법 및 플레이어 투사체 버프 합산)
        int magicProjectile = PlayerManager.Instance.projectileNum + magic.projectile;

        // 공격할 적 개수
        int markNum = colls.Length < magicProjectile ? colls.Length : magicProjectile;

        // 적 위치 리스트에 넣기
        for (int i = 0; i < markNum; i++)
        {
            //중복 제거를 위한 임시 리스트에 넣기
            List<Vector2> tempList = new List<Vector2>();
            tempList.Add(colls[i].transform.position);

            Vector2 pos = tempList[Random.Range(0, tempList.Count)];
            //결과 리스트에 넣기
            enemyPos.Add(pos);
            //임시 리스트에서 지우기
            tempList.Remove(pos);
        }

        //적의 위치 리스트 리턴
        return enemyPos;
    }

    // 콜라이더 사이즈 갱신
    void SetHitBox(GameObject magicObj, Vector2 originSize, MagicInfo magic)
    {
        if (magicObj.TryGetComponent(out BoxCollider2D boxCol))
        {
            boxCol = magicObj.GetComponent<BoxCollider2D>();
            boxCol.size = originSize * magic.range;
        }
        else if (magicObj.TryGetComponent(out CapsuleCollider2D capCol))
        {
            capCol = magicObj.GetComponent<CapsuleCollider2D>();
            capCol.size = originSize * magic.range;
        }
        else if (magicObj.TryGetComponent(out CircleCollider2D circleCol))
        {
            circleCol = magicObj.GetComponent<CircleCollider2D>();
            circleCol.radius = originSize.x * magic.range;
        }
    }
}
