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
    public bool noMagic; //! 마법 없이 테스트

    [Header("Phone Spin")]
    public float spinSpeed = 1f; // 자전하는 속도
    public float rotationSpeed = 50f; // 공전하는 속도
    public float spinRange = 5f; //회전 반경
    public float followSpeed = 5f; //플레이어 따라가는 속도
    float orbitAngle = 0f; // 현재 자전 각도
    Vector3 slowFollowPos;
    Vector3 spinOffset;

    private void OnEnable()
    {
        // 스케일 초기화
        transform.localScale = Vector3.one * 0.05f;

        transform.position = slowFollowPos + Vector3.up * spinRange;
        spinOffset = transform.position - slowFollowPos;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        SpinObject();
    }

    void SpinObject()
    {
        // 중심점 벡터 slowFollowPos 가 플레이어 천천히 따라가기
        slowFollowPos = Vector3.Lerp(slowFollowPos, PlayerManager.Instance.transform.position, Time.deltaTime * followSpeed);

        // 중심점 기준으로 마법 오브젝트 위치 보정
        transform.position = slowFollowPos + spinOffset;

        // 중심점 기준 공전위치로 회전
        float speed = Time.deltaTime * 50f;
        transform.RotateAround(slowFollowPos, Vector3.back, speed);

        //z축 위치 보정
        transform.position = new Vector3(transform.position.x, transform.position.y, -1f);

        // 중심점 벡터 기준으로 오프셋 재설정
        spinOffset = transform.position - slowFollowPos;

        //오브젝트 각도 초기화, 자전 각도 추가
        orbitAngle += spinSpeed;
        transform.rotation = Quaternion.Euler(new Vector3(0, orbitAngle, 0));
    }

    public void CastCheck()
    {
        // Merge 마법에서 레벨 합산해서 리스트 만들기
        List<MagicInfo> castList = new List<MagicInfo>();
        foreach (MagicInfo magic in PlayerManager.Instance.hasMergeMagics)
        {
            //마법이 null이면 넘기기
            if (magic == null)
                continue;

            print(magic.magicName + " : " + magic.magicLevel);

            MagicInfo tempMagic = null;
            tempMagic = castList.Find(x => x.id == magic.id);
            // ID가 같은 마법이 없으면
            if (tempMagic == null)
            {
                //해당 마법 리스트에 추가
                castList.Add(magic);
            }
            // 이미 ID 같은 마법이 들어있으면
            else
            {
                //해당 마법 레벨 더하기
                tempMagic.magicLevel += magic.magicLevel;
            }
        }

        // castList에 있는데 nowCastMagics에 없는 마법 캐스팅하기
        foreach (MagicInfo magic in castList)
        {
            //ID 같은 마법 찾기
            MagicInfo tempMagic = null;
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

                if (magic.castType == "auto")
                {
                    // 액티브 마법 시전
                    StartCoroutine(ActiveCast(magic));
                }

                continue;
            }

            //현재 실행중인 마법 레벨이 다르면
            if (tempMagic.magicLevel != magic.magicLevel)
            {
                //최근 갱신된 레벨 넣어주기
                tempMagic.magicLevel = magic.magicLevel;
            }
        }

        print(passiveMagics.Count + " : " + nowCastMagics.Count);

        // castList에 없는데 nowCastMagics에 있는(이미 시전중인) 마법 찾아서 중단시키기
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

                // nowCastMagics에서 제거
                nowCastMagics.Remove(nowCastMagics[i]);
            }
        }
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
        List<Vector2> enemyPos = MarkEnemyPos(magic);

        //해당 마법 쿨타임 불러오기
        float coolTime = MagicDB.Instance.MagicCoolTime(magic);

        for (int i = 0; i < enemyPos.Count; i++)
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
            magicHolder.targetPos = enemyPos[i];

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

        //passive 마법 오브젝트 리스트에 넣기
        passiveMagics.Add(magicObj);

        //nowCastMagics에 해당 마법 추가
        nowCastMagics.Add(magic);
    }

    List<Vector2> MarkEnemyPos(MagicInfo magic)
    {
        List<Vector2> enemyPos = new List<Vector2>();

        //캐릭터 주변의 적들
        List<Collider2D> enemyColList = new List<Collider2D>();
        enemyColList.Clear();
        float range = MagicDB.Instance.MagicRange(magic);
        enemyColList = Physics2D.OverlapCircleAll(PlayerManager.Instance.transform.position, range, 1 << LayerMask.NameToLayer("Enemy")).ToList();

        // 투사체 개수 (마법 및 플레이어 투사체 버프 합산)
        int magicProjectile = MagicDB.Instance.MagicProjectile(magic);

        // 적 위치 리스트에 넣기
        for (int i = 0; i < magicProjectile; i++)
        {
            // 플레이어 주변 범위내 랜덤 위치 벡터 생성
            Vector2 pos =
            (Vector2)PlayerManager.Instance.transform.position
            + Random.insideUnitCircle.normalized * range;

            // 플레이어 주변 범위내 랜덤한 적의 위치
            if (enemyColList.Count > 0)
            {
                Collider2D col = enemyColList[Random.Range(0, enemyColList.Count)];
                pos = col.transform.position;

                //임시 리스트에서 지우기
                enemyColList.Remove(col);

                // print(col.transform.name + col.transform.position);
            }

            // 범위내에 적이 있으면 적위치, 없으면 무작위 위치 넣기
            enemyPos.Add(pos);
        }

        //적의 위치 리스트 리턴
        return enemyPos;
    }

    public IEnumerator UseUltimateMagic()
    {
        MagicInfo magic = PlayerManager.Instance.ultimateMagic;

        //! Test
        // magic = MagicDB.Instance.GetMagicByID(48);

        //궁극기 없을때, 쿨타임중일때
        if (magic == null || PlayerManager.Instance.ultimateCoolCount > 0)
        {
            print("궁극기 실패");

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

        GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);
        float cooltime = MagicDB.Instance.MagicCoolTime(magic);

        // 랜덤 적 찾기, 투사체 수 이하로
        List<Vector2> enemyPos = MarkEnemyPos(magic);

        //해당 마법 쿨타임 불러오기
        float coolTime = MagicDB.Instance.MagicCoolTime(magic);

        for (int i = 0; i < enemyPos.Count; i++)
        {
            // 마법 오브젝트 생성
            GameObject magicObj = LeanPool.Spawn(magicPrefab, transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

            //매직 홀더 찾기
            MagicHolder magicHolder = magicObj.GetComponentInChildren<MagicHolder>();

            //마법 정보 넣기
            if (magicHolder.magic == null)
                magicHolder.magic = magic;

            //적 위치 넣기, 있어도 새로 갱신
            magicHolder.targetPos = enemyPos[i];

            yield return new WaitForSeconds(0.1f);
        }

        //해당 마법 쿨타임 카운트 시작
        PlayerManager.Instance.ultimateCoolCount = cooltime;
    }
}
