using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using UnityEngine;
using DG.Tweening;

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

    public List<GameObject> passiveMagics = new List<GameObject>(); // passive 소환형 마법 오브젝트 리스트
    public List<MagicInfo> nowCastMagics = new List<MagicInfo>(); //현재 사용중인 마법
    public List<int> defaultMagic = new List<int>(); //기본 마법
    public bool testAllMagic; //! 모든 마법 테스트
    // public bool noMagic; //! 마법 없이 테스트

    [Header("Phone Move")]
    public float spinSpeed = 1f; // 자전하는 속도
    float orbitAngle = 0f; // 현재 자전 각도
    public float hoverSpeed = 5f; //둥둥 떠서 오르락내리락 하는 속도
    public float hoverRange = 0.5f; // 오르락내리락 하는 범위

    private void OnEnable()
    {
        // 스케일 초기화
        transform.localScale = Vector3.one * 0.05f;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        SpinAndHovering();
    }

    void SpinAndHovering()
    {
        //오브젝트 각도 초기화, 자전 각도 추가
        orbitAngle += spinSpeed;
        transform.rotation = Quaternion.Euler(new Vector3(0, orbitAngle, 0));

        // 오르락내리락 하는 호버링 무빙
        transform.localPosition = new Vector3(0, Mathf.Sin(Time.time * hoverSpeed) * hoverRange, 0);
    }

    public void CastCheck()
    {
        // Merge 마법에서 레벨 합산해서 리스트 만들기
        List<MagicInfo> castList = new List<MagicInfo>();
        castList.Clear(); //리스트 비우기
        foreach (MagicInfo magic in PlayerManager.Instance.hasMergeMagics)
        {
            //마법이 null이면 넘기기
            if (magic == null)
                continue;
            // print(magic.magicName + " : " + magic.magicLevel);

            // MagicDB에서 해당 마법과 같은 마법 찾기 (마법마다 같은 인스턴스를 써야 쿨타임 및 레벨 공유 가능)
            MagicInfo referMagic = MagicDB.Instance.GetMagicByID(magic.id);

            // ID가 같은 마법이 없으면 (처음 들어가는 마법이면)
            if (!castList.Exists(x => x.id == magic.id))
            {
                //해당 마법 리스트에 추가
                castList.Add(referMagic);
                //마법 레벨 초기화
                referMagic.magicLevel = 0;
            }

            // print(referMagic.magicLevel + " : " + magic.magicLevel);

            // 기존 마법에 레벨 더하기
            referMagic.magicLevel += magic.magicLevel;
        }

        // castList에 있는데 nowCastMagics에 없는 마법 캐스팅하기
        foreach (MagicInfo magic in castList)
        {
            MagicInfo tempMagic = null;

            // 궁극기 마법일때
            if (magic.castType == "ultimate")
            {
                //ID 같은 궁극기 마법 찾기
                tempMagic = PlayerManager.Instance.ultimateList.Find(x => x.id == magic.id);

                // 궁극기가 nowCastMagics에 없으면
                if (tempMagic == null)
                {
                    //TODO 궁극기 리스트에 추가
                    PlayerManager.Instance.ultimateList.Add(magic);
                }

                continue;
            }

            // 일반 마법일때
            if (magic.castType == "passive"
            || magic.castType == "active")
            {
                //ID 같은 일반 마법 찾기
                tempMagic = nowCastMagics.Find(x => x.id == magic.id);

                // 마법이 nowCastMagics에 없으면
                if (tempMagic == null)
                {
                    // 패시브 마법일때
                    if (magic.castType == "passive")
                    {
                        //이미 소환되지 않았을때
                        if (!magic.exist)
                        {
                            //패시브 마법 시전
                            PassiveCast(magic);
                        }
                    }

                    // 자동 시전 마법일때
                    if (magic.castType == "active")
                    {
                        //nowCastMagics에 해당 마법 추가
                        nowCastMagics.Add(magic);

                        // 액티브 마법 시전
                        StartCoroutine(ActiveCast(magic));
                    }

                    continue;
                }
            }

            //현재 실행중인 마법 레벨이 다르면
            if (tempMagic.magicLevel != magic.magicLevel)
            {
                //최근 갱신된 레벨 넣어주기
                tempMagic.magicLevel = magic.magicLevel;
            }
        }

        // 사라진 궁극기 마법 리스트에서 없에기
        foreach (MagicInfo magic in PlayerManager.Instance.ultimateList)
        {
            //ID 같은 마법 찾기
            MagicInfo tempMagic = null;
            tempMagic = castList.Find(x => x.id == magic.id);

            // castList에서 같은 마법을 못찾으면
            if (tempMagic == null)
            {
                // ultimateList에서 해당 마법 제거
                PlayerManager.Instance.ultimateList.Remove(magic);
            }
        }

        // castList에 없는데 nowCastMagics에 있는(이미 시전중인) 일반 마법 찾아서 중단시키기
        for (int i = 0; i < nowCastMagics.Count; i++)
        {
            //ID 같은 마법 찾기
            MagicInfo tempMagic = null;
            tempMagic = castList.Find(x => x.id == nowCastMagics[i].id);

            // 시전 중인 마법을 castList에서 못찾으면 해당 마법 제거
            if (tempMagic == null)
            {
                // 패시브 마법이면 컴포넌트 찾아서 디스폰
                if (nowCastMagics[i].castType == "passive")
                {
                    // passiveMagics에서 해당 패시브 마법 오브젝트 찾기
                    GameObject passiveMagic = passiveMagics.Find(x => x.GetComponent<MagicHolder>().magic.id == nowCastMagics[i].id);

                    // 찾은 오브젝트 디스폰
                    LeanPool.Despawn(passiveMagic);

                    // 패시브 리스트에서 제거
                    passiveMagics.Remove(passiveMagic);
                }

                // nowCastMagics에서 제거, Active 마법은 자동 중단됨
                nowCastMagics.Remove(nowCastMagics[i]);
            }
        }

        //궁극기 장착
        PlayerManager.Instance.EquipUltimate();

        // 인게임 화면 하단에 사용중인 마법 아이콘 나열하기
        UIManager.Instance.UpdateMagics(castList);
    }

    //액티브 마법 소환
    public IEnumerator ActiveCast(MagicInfo magic)
    {
        //마법 프리팹 찾기
        GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);

        //프리팹 없으면 넘기기
        if (magicPrefab == null)
            yield break;

        // 랜덤 적 찾기, 투사체 수 이하로
        List<GameObject> enemyObj = MarkEnemy(magic);

        //해당 마법 쿨타임 불러오기
        float coolTime = MagicDB.Instance.MagicCoolTime(magic);

        // print(magic.magicName + " : " + coolTime);

        for (int i = 0; i < enemyObj.Count; i++)
        {
            // 마법 오브젝트 생성
            GameObject magicObj = LeanPool.Spawn(magicPrefab, transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

            // 태그 및 레이어 마법으로 바꾸기
            magicObj.tag = "Magic";
            magicObj.layer = LayerMask.NameToLayer("Magic");

            //매직 홀더 찾기
            MagicHolder magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

            //타겟 정보 넣기
            magicHolder.SetTarget(MagicHolder.Target.Enemy);

            //마법 정보 넣기
            if (magicHolder.magic == null)
                magicHolder.magic = magic;

            //적 위치 넣기, 있어도 새로 갱신
            magicHolder.targetObj = enemyObj[i];

            //적 오브젝트 넣기, (유도 기능 등에 사용)
            if (enemyObj[i] != null)
                magicHolder.targetPos = enemyObj[i].transform.position;
            else
                magicHolder.targetPos =
                (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle * MagicDB.Instance.MagicRange(magic);

            yield return new WaitForSeconds(0.1f);
        }

        magic.coolCount = coolTime;
        while (magic.coolCount > 0)
        {
            //카운트 차감, 플레이어 자체속도 반영
            magic.coolCount -= Time.deltaTime;

            yield return null;
        }

        //쿨타임 만큼 대기
        // yield return new WaitUntil(() => cooltimeCount <= 0);

        // Merge 목록에 마법 있는지 검사
        if (System.Array.Exists(PlayerManager.Instance.hasMergeMagics, x => x != null && x.id == magic.id))
        {
            //코루틴 재실행
            StartCoroutine(ActiveCast(magic));
        }
        // 사용하던 마법이 사라졌으면
        else
        {
            // 더이상 재실행 하지않고, 현재 사용중 목록에서 제거
            nowCastMagics.Remove(magic);
        }

    }

    void PassiveCast(MagicInfo magic)
    {
        // print("magic Summon : " + magic.magicName);
        magic.exist = true;

        //프리팹 찾기
        GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);

        //프리팹 없으면 넘기기
        if (magicPrefab == null)
            return;

        // 플레이어 위치에 마법 생성
        GameObject magicObj = LeanPool.Spawn(magicPrefab, transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

        //마법 정보 넣기
        magicObj.GetComponentInChildren<MagicHolder>().magic = magic;

        //매직 홀더 찾기
        MagicHolder magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

        //타겟 정보 넣기
        magicHolder.SetTarget(MagicHolder.Target.Enemy);

        //passive 마법 오브젝트 리스트에 넣기
        passiveMagics.Add(magicObj);

        //nowCastMagics에 해당 마법 추가
        nowCastMagics.Add(magic);
    }

    List<GameObject> MarkEnemy(MagicInfo magic)
    {
        List<GameObject> enemyObj = new List<GameObject>();

        //캐릭터 주변의 적들
        List<Collider2D> enemyColList = new List<Collider2D>();
        enemyColList.Clear();
        float range = MagicDB.Instance.MagicRange(magic);
        enemyColList = Physics2D.OverlapCircleAll(PlayerManager.Instance.transform.position, range, 1 << LayerMask.NameToLayer("Enemy")).ToList();

        // 투사체 개수 (마법 및 플레이어 투사체 버프 합산)
        int atkNum = MagicDB.Instance.MagicProjectile(magic);

        // 적 위치 리스트에 넣기
        for (int i = 0; i < atkNum; i++)
        {
            // 플레이어 주변 범위내 랜덤 위치 벡터 생성
            GameObject Obj = null;
            // (Vector2)PlayerManager.Instance.transform.position
            // + Random.insideUnitCircle * range;

            // 플레이어 주변 범위내 랜덤한 적의 위치
            if (enemyColList.Count > 0)
            {
                Collider2D col = enemyColList[Random.Range(0, enemyColList.Count)];
                Obj = col.gameObject;

                //임시 리스트에서 지우기
                enemyColList.Remove(col);

                // print(col.transform.name + col.transform.position);
            }

            // 범위내에 적이 있으면 적위치, 없으면 무작위 위치 넣기
            enemyObj.Add(Obj);
        }

        //적의 위치 리스트 리턴
        return enemyObj;
    }

    public IEnumerator UseUltimateMagic()
    {
        //마법 참조
        MagicInfo magic = null;
        if (PlayerManager.Instance.ultimateList.Count > 0)
            magic = PlayerManager.Instance.ultimateList[0];

        //! Test
        // magic = MagicDB.Instance.GetMagicByID(48);

        //궁극기 없을때, 쿨타임중일때
        if (magic == null || PlayerManager.Instance.ultimateCoolCount > 0)
        {
            UIManager.Instance.ultimateIndicator.DOKill();

            //궁극기 아이콘 인디케이터
            Color baseColor = UIManager.Instance.ultimateIndicator.color;
            Color onColor = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
            Color offColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            //인디케이터 2번 밝히기
            Sequence seq = DOTween.Sequence();
            seq.Append(
                UIManager.Instance.ultimateIndicator.DOColor(onColor, 0.2f)
            )
            .Append(
                UIManager.Instance.ultimateIndicator.DOColor(offColor, 0.2f)
            )
            .SetLoops(2)
            .OnComplete(() =>
            {
                UIManager.Instance.ultimateIndicator.color = offColor;
            });
            seq.Restart();

            yield break;
        }

        //해당 마법 쿨타임 카운트 시작
        PlayerManager.Instance.ultimateCoolCount = PlayerManager.Instance.ultimateCoolTime;

        //프리팹 찾기
        GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);

        // 랜덤 적 찾기, 투사체 수 이하로
        List<GameObject> enemyObj = MarkEnemy(magic);

        for (int i = 0; i < enemyObj.Count; i++)
        {
            // 마법 오브젝트 생성
            GameObject magicObj = LeanPool.Spawn(magicPrefab, transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

            //매직 홀더 찾기
            MagicHolder magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

            //타겟 정보 넣기
            magicHolder.SetTarget(MagicHolder.Target.Enemy);

            //마법 정보 넣기
            if (magicHolder.magic == null)
                magicHolder.magic = magic;

            //적 위치 넣기, 있어도 새로 갱신
            magicHolder.targetObj = enemyObj[i];

            //적 오브젝트 넣기, (유도 기능 등에 사용)
            if (enemyObj[i] != null)
                magicHolder.targetPos = enemyObj[i].transform.position;
            else
                magicHolder.targetPos =
                (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle * MagicDB.Instance.MagicRange(magic);

            yield return new WaitForSeconds(0.1f);
        }
    }
}
