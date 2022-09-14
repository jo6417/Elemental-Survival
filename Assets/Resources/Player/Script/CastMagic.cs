using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

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

    public List<GameObject> passiveObjs = new List<GameObject>(); // passive 소환형 마법 오브젝트 리스트
    public List<MagicInfo> nowCastMagics = new List<MagicInfo>(); //현재 사용중인 마법
    public bool noMagic; // 마법 없이 테스트
    public List<int> testMagics = new List<int>(); // 테스트용 마법 리스트
    public bool noItem; // 아이템 없이 테스트
    public List<int> testItems = new List<int>(); // 테스트용 아이템 리스트

    [Header("Refer")]
    [SerializeField] ParticleSystem playerMagicCastEffect; // 플레이어가 마법 사용시 파티클 실행
    [SerializeField] ParticleSystem phoneMagicCastEffect; // 핸드폰이 마법 사용시 파티클 실행

    [Header("Phone Move")]
    [SerializeField] float spinSpeed = 1f; // 자전하는 속도
    [SerializeField] float orbitAngle = 0f; // 현재 자전 각도
    [SerializeField] float hoverSpeed = 5f; //둥둥 떠서 오르락내리락 하는 속도
    [SerializeField] float hoverRange = 0.5f; // 오르락내리락 하는 범위
    [SerializeField] float hoverHeight = 2f; // 핸드폰이 떠있는 높이

    private void OnEnable()
    {
        // 스케일 초기화
        transform.localScale = Vector3.one * 0.05f;
    }

    private void FixedUpdate()
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
        transform.localPosition = new Vector3(0, Mathf.Sin(Time.time * hoverSpeed) * hoverRange + hoverHeight, -0.5f);
    }

    public IEnumerator ManualCast(InventorySlot invenSlot, MagicInfo magic)
    {
        // 마법 정보가 없거나 쿨타임 중이면 실패 인디케이터 실행
        if (magic == null || MagicDB.Instance.GetActiveMagicByID(magic.id).coolCount > 0)
        {
            // 스프라이트 2회 켜기
            invenSlot.FailBlink(2);

            yield break;
        }

        // 해당 마법 투사체 개수 불러오기
        float projectileNum = MagicDB.Instance.MagicProjectile(magic);
        // 해당 마법 쿨타임 불러오기
        float coolTime = MagicDB.Instance.MagicCoolTime(magic);
        // 해당 마법 범위 불러오기
        float range = MagicDB.Instance.MagicRange(magic);

        // 플레이어 마법 시전 이펙트 플레이
        playerMagicCastEffect.Play();

        //마법 프리팹 찾기
        GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);

        //프리팹 없으면 넘기기
        if (magicPrefab == null)
            yield break;

        // 마우스 위치 근처 공격 지점 리스트
        List<Vector2> attackPos = new List<Vector2>();

        // 투사체 개수만큼 마우스 근처 포지션 지정
        for (int i = 0; i < projectileNum; i++)
        {
            Vector3 atkPos = PlayerManager.Instance.mouseWorldPos + (Vector3)Random.insideUnitCircle * range;
            attackPos.Add(atkPos);
        }

        // 공격지점 개수만큼 마법 시전
        for (int i = 0; i < attackPos.Count; i++)
        {
            // 마법 오브젝트 생성
            GameObject magicObj = LeanPool.Spawn(magicPrefab, PlayerManager.Instance.transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

            // 레이어 바꾸기
            magicObj.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;

            //매직 홀더 찾기
            MagicHolder magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

            //타겟 정보 넣기
            magicHolder.SetTarget(MagicHolder.Target.Enemy);

            //마법 정보 넣기
            if (magicHolder.magic == null)
                magicHolder.magic = magic;

            //적 오브젝트 넣기, (유도 기능 등에 사용)
            // magicHolder.targetObj = attackPos[i];

            //적 위치 넣기
            if (attackPos[i] != null)
                magicHolder.targetPos = attackPos[i];

            yield return new WaitForSeconds(0.1f);
        }

        // 해당 마법의 전역 쿨타임 갱신
        MagicInfo activeMagic = MagicDB.Instance.GetActiveMagicByID(magic.id);
        activeMagic.coolCount = coolTime;

        // 해당 마법 전역 쿨타임 카운트 감소
        while (activeMagic.coolCount > 0)
        {
            //카운트 차감, 플레이어 자체속도 반영
            activeMagic.coolCount -= Time.deltaTime;

            yield return null;
        }
    }

    public void CastCheck()
    {
        // 인벤토리에서 레벨 합산해서 리스트 만들기
        List<MagicInfo> castList = new List<MagicInfo>();
        castList.Clear(); //리스트 비우기

        // 인벤토리에서 마법 찾기
        for (int i = 0; i < PhoneMenu.Instance.invenSlots.Count; i++)
        {
            // 액티브 마법이면 넘기기
            if (i >= 20)
                continue;

            // 마법 정보 불러오기
            MagicInfo magic = PhoneMenu.Instance.invenSlots[i].slotInfo as MagicInfo;

            //마법이 null이면 넘기기
            if (magic == null)
                continue;
            // print(magic.magicName + " : " + magic.magicLevel);

            // 기존의 합산 리스트에서 마법 찾기
            MagicInfo findMagic = castList.Find(x => x.id == magic.id);

            // ID가 같은 마법이 없으면 (처음 들어가는 마법이면)
            if (findMagic == null)
            {
                // 해당 마법으로 새 인스턴스 생성
                MagicInfo referMagic = new MagicInfo(magic);

                //마법 레벨 초기화
                referMagic.magicLevel = magic.magicLevel;

                //해당 마법 리스트에 추가
                castList.Add(referMagic);
            }
            // 이미 합산 리스트에 마법이 있을때
            else
                // 기존 사용중이던 마법에 레벨만 더하기
                findMagic.magicLevel += magic.magicLevel;

            // print(referMagic.magicLevel + " : " + magic.magicLevel);
        }

        // castList에 있는데 nowCastMagics에 없는 마법 캐스팅하기
        foreach (MagicInfo magic in castList)
        {
            MagicInfo tempMagic = null;

            // 일반 마법일때
            if (magic.castType == MagicDB.MagicType.passive.ToString()
            || magic.castType == MagicDB.MagicType.active.ToString())
            {
                //ID 같은 일반 마법 찾기
                tempMagic = nowCastMagics.Find(x => x.id == magic.id);

                // 마법이 현재 실행 마법 리스트에 없으면, 신규 마법이면
                if (tempMagic == null)
                {
                    // 해당 마법이 쿨타임 중일때 넘기기 (이미 반복 코루틴 실행중이므로)
                    if (MagicDB.Instance.GetMagicByID(magic.id).coolCount > 0)
                        continue;

                    // 패시브 마법일때
                    if (magic.castType == MagicDB.MagicType.passive.ToString())
                    {
                        //이미 소환되지 않았을때
                        if (!magic.exist)
                        {
                            //패시브 마법 시전
                            PassiveCast(magic);
                        }
                    }

                    // 액티브 마법일때
                    if (magic.castType == MagicDB.MagicType.active.ToString())
                    {
                        //nowCastMagics에 해당 마법 추가
                        nowCastMagics.Add(magic);

                        // 액티브 마법 시전
                        StartCoroutine(ActiveCast(magic));
                    }

                    continue;
                }
            }

            //현재 실행중인 마법 레벨이 다르면 (마법 레벨업일때)
            if (tempMagic.magicLevel != magic.magicLevel)
            {
                //최근 갱신된 레벨 넣어주기
                tempMagic.magicLevel = magic.magicLevel;

                // print($"Name : {tempMagic.name} / Level : {tempMagic.magicLevel}");

                // 패시브 마법이면
                if (tempMagic.castType == MagicDB.MagicType.passive.ToString())
                {
                    // passiveMagics에서 해당 패시브 마법 오브젝트 찾기
                    GameObject passiveObj = passiveObjs.Find(x => x.GetComponentInChildren<MagicHolder>().magic.id == tempMagic.id);

                    // 패시브 오브젝트 껐다켜서 초기화
                    passiveObj.SetActive(false);
                    passiveObj.SetActive(true);
                }
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
                if (nowCastMagics[i].castType == MagicDB.MagicType.passive.ToString())
                {
                    print(nowCastMagics[i].name);

                    // passiveMagics에서 해당 패시브 마법 오브젝트 찾기
                    GameObject passiveMagic = passiveObjs.Find(x => x.GetComponentInChildren<MagicHolder>(true).magic.id == nowCastMagics[i].id);

                    // 찾은 오브젝트 디스폰
                    LeanPool.Despawn(passiveMagic);

                    // 패시브 리스트에서 제거
                    passiveObjs.Remove(passiveMagic);
                }

                // 패시브 마법을 nowCastMagics에서 제거, Active 마법은 자동 중단됨
                nowCastMagics.Remove(nowCastMagics[i]);
            }
        }

        // 인게임 화면 하단에 사용중인 마법 아이콘 리스트 갱신
        UIManager.Instance.UpdateMagics(castList);
    }

    //액티브 마법 소환
    public IEnumerator ActiveCast(MagicInfo magic)
    {
        // 핸드폰 마법 시전 이펙트 플레이
        phoneMagicCastEffect.Play();

        //마법 프리팹 찾기
        GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);

        //프리팹 없으면 넘기기
        if (magicPrefab == null)
            yield break;

        // 랜덤 적 찾기, 투사체 수 이하로
        List<EnemyManager> enemyObj = MarkEnemies(magic);

        //해당 마법 쿨타임 불러오기
        float coolTime = MagicDB.Instance.MagicCoolTime(magic);

        // print(magic.magicName + " : " + coolTime);

        for (int i = 0; i < enemyObj.Count; i++)
        {
            // 마법 오브젝트 생성
            GameObject magicObj = LeanPool.Spawn(magicPrefab, transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

            // 레이어 바꾸기
            magicObj.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;

            //매직 홀더 찾기
            MagicHolder magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

            //타겟 정보 넣기
            magicHolder.SetTarget(MagicHolder.Target.Enemy);

            //마법 정보 넣기
            if (magicHolder.magic == null)
                magicHolder.magic = magic;

            // 적 오브젝트가 null이 아닐때
            if (enemyObj[i] != null)
            {
                //적 위치 넣기, 있어도 새로 갱신
                magicHolder.targetObj = enemyObj[i].gameObject;

                //적 오브젝트 위치 넣기, (유도 기능 등에 사용)
                magicHolder.targetPos = enemyObj[i].transform.position;
            }
            else
                // 오브젝트 없으면 범위내 랜덤 위치 넣기
                magicHolder.targetPos =
                (Vector2)transform.position + Random.insideUnitCircle.normalized * MagicDB.Instance.MagicRange(magic);

            yield return new WaitForSeconds(0.1f);
        }

        // 쿨타임 체크를 위한 전역 마법 정보 불러오기
        MagicInfo globalMagic = MagicDB.Instance.GetMagicByID(magic.id);
        // 해당 마법의 전역 쿨타임 갱신
        globalMagic.coolCount = coolTime;

        while (globalMagic.coolCount > 0)
        {
            //카운트 차감, 플레이어 자체속도 반영
            globalMagic.coolCount -= Time.deltaTime;

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 인벤토리에서 해당 마법이 있는 인덱스 찾기
        int slotIndex = -1;
        slotIndex = PhoneMenu.Instance.invenSlots.FindIndex(
        x => x != null &&
        // 마법인지 검사
        x.slotInfo as MagicInfo != null &&
        // 해당 마법과 같은 id를 가진 마법 있는지 검사
        x.slotInfo.id == magic.id);

        // 인벤토리에 있을때
        if (slotIndex != -1)
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
        GameObject magicObj = LeanPool.Spawn(magicPrefab, PlayerManager.Instance.transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

        //마법 정보 넣기
        magicObj.GetComponentInChildren<MagicHolder>().magic = magic;

        //매직 홀더 찾기
        MagicHolder magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

        //타겟 정보 넣기
        magicHolder.SetTarget(MagicHolder.Target.Enemy);

        //passive 마법 오브젝트 리스트에 넣기
        passiveObjs.Add(magicObj);

        //nowCastMagics에 해당 마법 추가
        nowCastMagics.Add(magic);
    }

    public List<EnemyManager> MarkEnemies(MagicInfo magic)
    {
        // 마법 범위 계산
        float range = MagicDB.Instance.MagicRange(magic);
        // 투사체 개수 계산
        int atkNum = MagicDB.Instance.MagicProjectile(magic);

        //리턴할 적 오브젝트 리스트
        List<EnemyManager> enemyObjs = new List<EnemyManager>();

        //범위 안의 모든 적 콜라이더 리스트에 담기
        List<Collider2D> enemyCollList = new List<Collider2D>();
        enemyCollList.Clear();
        enemyCollList = Physics2D.OverlapCircleAll(PlayerManager.Instance.transform.position, range, 1 << SystemManager.Instance.layerList.EnemyHit_Layer).ToList();

        // 찾은 적과 투사체 개수 중 많은 쪽만큼 반복
        int findNum = Mathf.Max(enemyCollList.Count, atkNum);
        for (int i = 0; i < findNum; i++)
        {
            // 투사체 개수만큼 채워지면 반복문 끝내기
            if (enemyObjs.Count >= atkNum)
                break;

            EnemyManager enemyManager = null;
            Collider2D targetColl = null;

            if (enemyCollList.Count > 0)
            {
                // 리스트 내에서 랜덤으로 선택
                targetColl = enemyCollList[Random.Range(0, enemyCollList.Count)];
                // 적 히트박스 찾기
                EnemyHitBox targetHitBox = targetColl.GetComponent<EnemyHitBox>();
                if (targetHitBox != null)
                    // 적 매니저 찾기
                    enemyManager = targetHitBox.enemyManager;

                // 이미 들어있는 오브젝트일때
                if (enemyObjs.Exists(x => x == enemyManager)
                // 해당 몬스터가 유령일때
                || (enemyManager && enemyManager.IsGhost))
                {
                    // 임시 리스트에서 지우기
                    enemyCollList.Remove(targetColl);

                    // 넘기기
                    continue;
                }
            }

            // 적 오브젝트 변수에 담기
            enemyObjs.Add(enemyManager);

            // 임시 리스트에서 지우기
            if (targetColl != null)
                enemyCollList.Remove(targetColl);
        }

        //적의 위치 리스트 리턴
        return enemyObjs;
    }

    #region UseUltimateMagic
    // public IEnumerator UseUltimateMagic()
    // {
    //     //마법 참조
    //     MagicInfo magic = null;
    //     if (PlayerManager.Instance.ultimateList.Count > 0)
    //         magic = PlayerManager.Instance.ultimateList[0];

    //     // Test
    //     // magic = MagicDB.Instance.GetMagicByID(48);
    //     ultimateMagicCastEffect.Play();

    //     // 궁극기 없을때, 쿨타임중일때
    //     if (magic == null || PlayerManager.Instance.ultimateCoolCount > 0)
    //     {
    //         UIManager.Instance.ultimateIndicator.DOKill();

    //         //궁극기 아이콘 인디케이터
    //         Color baseColor = UIManager.Instance.ultimateIndicator.color;
    //         Color onColor = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
    //         Color offColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

    //         //인디케이터 2번 밝히기
    //         Sequence seq = DOTween.Sequence();
    //         seq.Append(
    //             UIManager.Instance.ultimateIndicator.DOColor(onColor, 0.2f)
    //         )
    //         .Append(
    //             UIManager.Instance.ultimateIndicator.DOColor(offColor, 0.2f)
    //         )
    //         .SetLoops(2)
    //         .OnComplete(() =>
    //         {
    //             UIManager.Instance.ultimateIndicator.color = offColor;
    //         });
    //         seq.Restart();

    //         yield break;
    //     }

    //     //해당 마법 쿨타임 카운트 시작
    //     PlayerManager.Instance.ultimateCoolCount = PlayerManager.Instance.ultimateCoolTime;

    //     //프리팹 찾기
    //     GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);

    //     // 랜덤 적 찾기, 투사체 수 이하로
    //     List<GameObject> enemyObj = MarkEnemyObj(magic);

    //     for (int i = 0; i < enemyObj.Count; i++)
    //     {
    //         // 마법 오브젝트 생성
    //         GameObject magicObj = LeanPool.Spawn(magicPrefab, transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

    //         //매직 홀더 찾기
    //         MagicHolder magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

    //         //타겟 정보 넣기
    //         magicHolder.SetTarget(MagicHolder.Target.Enemy);

    //         //마법 정보 넣기
    //         if (magicHolder.magic == null)
    //             magicHolder.magic = magic;

    //         //적 위치 넣기, 있어도 새로 갱신
    //         magicHolder.targetObj = enemyObj[i];

    //         //적 오브젝트 넣기, (유도 기능 등에 사용)
    //         if (enemyObj[i] != null)
    //             magicHolder.targetPos = enemyObj[i].transform.position;
    //         else
    //             magicHolder.targetPos =
    //             (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle * MagicDB.Instance.MagicRange(magic);

    //         yield return new WaitForSeconds(0.1f);
    //     }
    // }
    #endregion
}
