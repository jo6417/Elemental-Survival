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
    List<GameObject> onlyOneMagics = new List<GameObject>(); // OnlyOne 소환형 마법 오브젝트 리스트
    List<int> nowCastMagicIDs = new List<int>(); //현재 사용중인 마법
    public List<int> basicMagic = new List<int>(); //기본 마법

    private void Start()
    {
        //기본 마법 추가
        StartCoroutine(CastBasicMagics());
    }

    public void CastAllMagics()
    {
        //플레이어 보유중인 모든 마법 ID
        List<int> hasMagicIDs = new List<int>();
        foreach (var magic in PlayerManager.Instance.hasMagics)
        {
            //active 타입 마법만 사용
            if (magic.castType == "active")
                hasMagicIDs.Add(magic.id);
        }

        // hasMagicIDs 중에 nowCastMagic에 없는 마법 찾기
        List<int> notCastMagic = hasMagicIDs.Except(nowCastMagicIDs).ToList();
        if (notCastMagic.Count != 0 && MagicDB.Instance.loadDone)
        {
            foreach (var magicID in notCastMagic)
            {
                MagicInfo magic = MagicDB.Instance.GetMagicByID(magicID);

                //0등급은 원소젬이므로 캐스팅 안함
                if(magic.grade == 0)
                continue;

                // 마법 프리팹 없으면 넘기기
                var magicPrefab = MagicDB.Instance.magicPrefab.Find(x => x.name == magic.magicName.Replace(" ", "") + "_Prefab");
                if (magicPrefab == null)
                {
                    // print("프리팹 없음");
                    continue;
                }

                // OnlyOne 마법일때
                if (magic.onlyOne)
                {
                    //이미 소환되지 않았을때
                    if (!magic.isSummoned)
                    {
                        magic.isSummoned = true;

                        // 플레이어 위치에 마법 생성
                        GameObject magicObj = LeanPool.Spawn(magicPrefab, transform.position, Quaternion.identity, PlayerManager.Instance.transform);

                        //마법 정보 넣기
                        magicObj.GetComponent<MagicHolder>().magic = magic;

                        //onlyone 마법 오브젝트 리스트에 넣기
                        onlyOneMagics.Add(magicObj);
                    }

                    continue;
                }

                //마법 투사체일때
                if (magicPrefab.TryGetComponent(out MagicProjectile magicPro))
                {
                    // print(magic.magicName + " : 투사체 마법");
                    StartCoroutine(ShotMagic(magicPrefab, magic));
                }
                //소환하는 마법일때
                else if (magicPrefab.TryGetComponent(out MagicFalling magicFall) ||
                magicPrefab.TryGetComponent(out MagicArea magicArea))
                {
                    // print(magic.magicName + " : 소환 마법");
                    StartCoroutine(SummonMagic(magicPrefab, magic));
                }

                //현재 사용중 마법 리스트에 추가
                nowCastMagicIDs.Add(magicID);
            }
        }
    }

    public void ReCastMagics()
    {
        StopAllCoroutines();

        //현재 사용중인 마법 리스트 비우기
        nowCastMagicIDs.Clear();
        //현재 보유 마법 재조사해서 실행하기
        CastAllMagics();

        foreach (var magic in onlyOneMagics)
        {
            // print(magic.name);
            magic.SetActive(false);
            magic.SetActive(true);
        }
    }

    IEnumerator CastBasicMagics()
    {
        // MagicDB 로드 완료까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //TODO 추후 캐릭터 구현 후 기본마법 바꾸고 시작하기
        // 캐릭터 기본 마법 추가
        foreach (var magicID in basicMagic)
        {
            //보유하지 않은 마법일때
            if (!PlayerManager.Instance.hasMagics.Exists(x => x.id == magicID))
            {
                // 플레이어 보유 마법에 해당 마법 추가하기
                PlayerManager.Instance.hasMagics.Add(MagicDB.Instance.GetMagicByID(magicID));
            }

            //보유한 마법의 레벨 올리기
            PlayerManager.Instance.hasMagics.Find(x => x.id == magicID).magicLevel++;
        }

        //플레이어 마법 시작
        CastAllMagics();

        // 보유한 모든 마법 아이콘 갱신
        UIManager.Instance.UpdateMagics();
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
            GameObject magicObj = LeanPool.Spawn(magicPrefab, enemyPos[i], Quaternion.identity, magicPool);

            // 발자국 생성마다 x축 뒤집기
            if (magic.magicName == "Lava Toss")
            {
                if (magicObj.transform.rotation.x == 180)
                    magicObj.transform.rotation = Quaternion.Euler(Vector3.zero);
                else if (magicObj.transform.rotation.x == 0)
                    magicObj.transform.rotation = Quaternion.Euler(new Vector3(180, 0, 0));
            }

            //마법 정보 넣기
            magicObj.GetComponent<MagicHolder>().magic = magic;

            //적 위치 넣기
            magicObj.GetComponent<MagicHolder>().targetPos = enemyPos[i];
        }

        //마법 쿨타임 만큼 대기
        float coolTime = MagicDB.Instance.MagicCoolTime(magic);

        yield return new WaitForSeconds(coolTime);

        StartCoroutine(SummonMagic(magicPrefab, magic));
    }

    //투사체 마법
    IEnumerator ShotMagic(GameObject magicPrefab, MagicInfo magic)
    {
        // 마법id에 해당하는 프리팹 투사체 갯수만큼 생성, onlyOne 속성이 1이면 하나만 발사
        // int projectileNum = magic.onlyOne == 1 ? 1 : PlayerManager.Instance.projectileNum;

        // 랜덤 적 찾기, 투사체 수 이하로
        List<Vector2> enemyPos = MarkEnemyPos(magic);

        for (int i = 0; i < enemyPos.Count; i++)
        {
            // 랜덤 각도로 회전
            // float angle = 0;
            // if (magic.speed == 0)
            // {
            //     angle = Random.Range(0, 360);
            // }

            // 마법 오브젝트 생성
            // GameObject magicObj = LeanPool.Spawn(magicPrefab, transform.position, Quaternion.Euler(new Vector3(0, 0, angle)), magicPool);
            GameObject magicObj = LeanPool.Spawn(magicPrefab, transform.position, Quaternion.identity, magicPool);

            //각도에 따라 스프라이트 뒤집기
            MagicProjectile magicPro = magicObj.GetComponent<MagicProjectile>();
            // if (angle > 90 && angle < 270) // 90 ~ 270
            // {
            //     magicPro.sprite.flipY = true;
            // }
            // else
            // {
            //     magicPro.sprite.flipY = false;
            // }

            //마법 정보 넣기
            magicObj.GetComponent<MagicHolder>().magic = magic;

            //TODO 몬스터 위치 넣기 (안넣으면 랜덤 방향 발사)
            if (magic.speed != 0)
                magicPro.targetPos = MarkEnemyPos(magic)[i];

            yield return new WaitForSeconds(0.1f);
        }

        //마법 쿨타임 만큼 대기
        float coolTime = MagicDB.Instance.MagicCoolTime(magic);

        yield return new WaitForSeconds(coolTime);

        StartCoroutine(ShotMagic(magicPrefab, magic));
    }

    List<Vector2> MarkEnemyPos(MagicInfo magic)
    {
        List<Vector2> enemyPos = new List<Vector2>();

        //캐릭터 주변의 적들
        List<Collider2D> enemyPosList = new List<Collider2D>();
        float range = MagicDB.Instance.MagicRange(magic);
        enemyPosList = Physics2D.OverlapCircleAll(PlayerManager.Instance.transform.position, range, 1 << LayerMask.NameToLayer("Enemy")).ToList();

        // 투사체 개수 (마법 및 플레이어 투사체 버프 합산)
        int magicProjectile = MagicDB.Instance.MagicProjectile(magic);

        // 공격할 적 개수
        // int markNum = colls.Length < magicProjectile ? colls.Length : magicProjectile;

        // 적 위치 리스트에 넣기
        for (int i = 0; i < magicProjectile; i++)
        {
            //중복 제거를 위한 임시 리스트에 넣기
            // List<Vector2> tempList = new List<Vector2>();
            // tempList.Add(enemyPosList[i].transform.position);

            // 범위내 랜덤 위치 벡터 생성
            Vector2 pos = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * range;

            // 범위내 랜덤한 적의 위치
            if (enemyPosList.Count > 0)
            {
                Collider2D col = enemyPosList[Random.Range(0, enemyPosList.Count)];
                pos = col.transform.position;

                //임시 리스트에서 지우기
                enemyPosList.Remove(col);
            }

            // 범위내에 적이 있으면 적위치, 없으면 무작위 위치 넣기
            enemyPos.Add(pos);
        }

        //적의 위치 리스트 리턴
        return enemyPos;
    }
}
