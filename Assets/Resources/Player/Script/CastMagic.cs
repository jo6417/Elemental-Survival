using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Text;

public class CastMagic : MonoBehaviour
{
    #region Singleton
    private static CastMagic instance;
    public static CastMagic Instance
    {
        get
        {
            // if (instance == null)
            // {
            //     var obj = FindObjectOfType<CastMagic>();
            //     if (obj != null)
            //     {
            //         instance = obj;
            //     }
            //     // else
            //     // {
            //     //     var newObj = new GameObject().AddComponent<CastMagic>();
            //     //     instance = newObj;
            //     // }
            // }
            return instance;
        }
    }
    #endregion

    public List<GameObject> passiveObjs = new List<GameObject>(); // passive 소환형 마법 오브젝트 리스트
    public List<MagicInfo> nowCastMagics = new List<MagicInfo>(); //현재 사용중인 마법
    public List<DBEnums.MagicDBEnum> testMagics = new List<DBEnums.MagicDBEnum>(); // 테스트용 마법 리스트
    public List<DBEnums.ItemDBEnum> testItems = new List<DBEnums.ItemDBEnum>(); // 테스트용 아이템 리스트
    [SerializeField] bool noMagic; // 마법 없이 테스트
    [SerializeField] bool noItem; // 아이템 없이 테스트

    [Header("Refer")]
    public Transform phone;
    [SerializeField] ParticleSystem playerMagicCastEffect; // 플레이어가 마법 사용시 파티클 실행
    [SerializeField] ParticleSystem phoneMagicCastEffect; // 핸드폰이 마법 사용시 파티클 실행

    [Header("Phone Move")]
    [SerializeField] float spinSpeed = 1f; // 자전하는 속도
    [SerializeField] float orbitAngle = 0f; // 현재 자전 각도
    [SerializeField] float hoverSpeed = 5f; //둥둥 떠서 오르락내리락 하는 속도
    [SerializeField] float hoverRange = 0.5f; // 오르락내리락 하는 범위
    [SerializeField] float hoverHeight = 2f; // 핸드폰이 떠있는 높이

    private void Awake()
    {
        // 다른 오브젝트가 이미 있을때
        if (instance != null)
        {
            // 파괴 후 리턴
            Destroy(gameObject);
            return;
        }
        // 최초 생성 됬을때
        else
        {
            instance = this;

            // 파괴되지 않게 설정
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        // 스케일 초기화
        phone.localScale = Vector3.one * 0.05f;
    }

    private void FixedUpdate()
    {
        SpinAndHovering();
    }

    void SpinAndHovering()
    {
        // 폰메뉴 켜져있으면 리턴
        if (PhoneMenu.Instance.isOpen)
            return;

        //오브젝트 각도 초기화, 자전 각도 추가
        orbitAngle += spinSpeed;
        phone.rotation = Quaternion.Euler(new Vector3(0, orbitAngle, 0));

        // 오르락내리락 하는 호버링 무빙
        phone.localPosition = new Vector3(0, Mathf.Sin(Time.time * hoverSpeed) * hoverRange + hoverHeight, -0.5f);
    }

    public List<MagicInfo> hasAllMagic()
    {
        // 인벤토리에서 레벨 합산해서 리스트 만들기
        List<MagicInfo> magicList = new List<MagicInfo>();

        // 액티브 슬롯의 마법 수집
        MagicInfo activeMagic = null;
        activeMagic = UIManager.Instance.activeSlot_A.slotInfo as MagicInfo;
        if (activeMagic != null) magicList.Add(activeMagic);
        activeMagic = UIManager.Instance.activeSlot_B.slotInfo as MagicInfo;
        if (activeMagic != null) magicList.Add(activeMagic);
        activeMagic = UIManager.Instance.activeSlot_C.slotInfo as MagicInfo;
        if (activeMagic != null) magicList.Add(activeMagic);

        // 인벤토리에서 마법 찾기
        for (int i = 0; i < PhoneMenu.Instance.invenSlots.Count; i++)
        {
            // 마법 정보 불러오기
            MagicInfo magic = PhoneMenu.Instance.invenSlots[i].slotInfo as MagicInfo;

            // 마법이 있으면
            if (magic != null)
                magicList.Add(magic);
        }

        // 인벤토리에서 레벨 합산해서 리스트 만들기
        List<MagicInfo> returnMagics = new List<MagicInfo>();

        // 같은 마법끼리 합산해서 통합
        for (int i = 0; i < magicList.Count; i++)
        {
            // 마법 정보 불러오기
            MagicInfo magic = magicList[i];

            // 기존의 합산 리스트에서 마법 찾기
            MagicInfo findMagic = returnMagics.Find(x => x.id == magic.id);

            // ID가 같은 마법이 없으면 (처음 들어가는 마법이면)
            if (findMagic == null)
            {
                // 해당 마법으로 새 인스턴스 생성
                MagicInfo referMagic = new MagicInfo(magic);

                // 마법 레벨 초기화
                referMagic.magicLevel = magic.magicLevel;

                // 리턴 마법 리스트에 추가
                returnMagics.Add(referMagic);
            }
            // 이미 합산 리스트에 마법이 있을때
            else
            {
                // 기존 사용중이던 마법에 레벨만 더하기
                findMagic.magicLevel += magic.magicLevel;
            }
        }

        // 최종 마법 리스트를 리턴
        return returnMagics;
    }

    public void CastCheck()
    {
        // 인벤토리에서 레벨 합산해서 리스트 만들기
        List<InventorySlot> slotList = new List<InventorySlot>();
        List<MagicInfo> magicList = new List<MagicInfo>();
        slotList.Clear(); //리스트 비우기

        // 액티브 슬롯의 패시브 마법도 포함
        MagicInfo activeMagic = UIManager.Instance.activeSlot_A.slotInfo as MagicInfo;
        if (activeMagic != null && activeMagic.castType == MagicDB.CastType.passive.ToString())
        {
            magicList.Add(UIManager.Instance.activeSlot_A.slotInfo as MagicInfo);
            slotList.Add(UIManager.Instance.activeSlot_A);
        }
        activeMagic = UIManager.Instance.activeSlot_B.slotInfo as MagicInfo;
        if (activeMagic != null && activeMagic.castType == MagicDB.CastType.passive.ToString())
        {
            magicList.Add(UIManager.Instance.activeSlot_B.slotInfo as MagicInfo);
            slotList.Add(UIManager.Instance.activeSlot_B);
        }
        activeMagic = UIManager.Instance.activeSlot_C.slotInfo as MagicInfo;
        if (activeMagic != null && activeMagic.castType == MagicDB.CastType.passive.ToString())
        {
            magicList.Add(UIManager.Instance.activeSlot_C.slotInfo as MagicInfo);
            slotList.Add(UIManager.Instance.activeSlot_C);
        }

        // 인벤토리에서 마법 찾기
        for (int i = 0; i < PhoneMenu.Instance.invenSlots.Count; i++)
        {
            // 마법 정보 불러오기
            MagicInfo magic = PhoneMenu.Instance.invenSlots[i].slotInfo as MagicInfo;

            //마법이 null이면 넘기기
            if (magic == null)
                continue;
            // print(magic.magicName + " : " + magic.magicLevel);

            // 기존의 합산 리스트에서 마법 찾기
            MagicInfo findMagic = magicList.Find(x => x.id == magic.id);

            // ID가 같은 마법이 없으면 (처음 들어가는 마법이면)
            if (findMagic == null)
            {
                // 해당 마법으로 새 인스턴스 생성
                MagicInfo referMagic = new MagicInfo(magic);

                //마법 레벨 초기화
                referMagic.magicLevel = magic.magicLevel;

                // 해당 슬롯 리스트에 추가
                slotList.Add(PhoneMenu.Instance.invenSlots[i]);
                // 해당 마법 리스트에 추가
                magicList.Add(referMagic);
            }
            // 이미 합산 리스트에 마법이 있을때
            else
            {
                // 기존 사용중이던 마법에 레벨만 더하기
                findMagic.magicLevel += magic.magicLevel;

                // print(findMagic.name + " : " + findMagic.magicLevel);
            }
        }

        // castList에 있는데 nowCastMagics에 없는 마법 캐스팅하기
        for (int i = 0; i < magicList.Count; i++)
        {
            MagicInfo magic = magicList[i];
            MagicInfo tempMagic = null;

            //ID 같은 마법 찾기
            tempMagic = nowCastMagics.Find(x => x.id == magic.id);

            // 마법이 현재 실행 마법 리스트에 없으면, 신규 마법이면
            if (tempMagic == null)
            {
                // 패시브 마법일때
                if (magic.castType == MagicDB.CastType.passive.ToString())
                {
                    MagicInfo globalMagic = MagicDB.Instance.GetMagicByID(magic.id);
                    //이미 소환되지 않았을때
                    if (!globalMagic.exist)
                    {
                        // 소환 여부 활성화
                        globalMagic.exist = true;

                        //패시브 마법 시전
                        PassiveCast(magic);
                    }

                    // passiveMagics에서 해당 패시브 마법 오브젝트 찾기
                    GameObject passiveObj = passiveObjs.Find(
                        x => x.GetComponent<MagicHolder>().magic.id
                        == magic.id);

                    if (passiveObj != null)
                    {
                        // 해당 슬롯이 인벤토리 슬롯일때
                        if (PhoneMenu.Instance.invenSlots.Exists(x => x == slotList[i]))
                            // 마법 사용 자동으로 설정
                            passiveObj.GetComponent<MagicHolder>().isManualCast = false;
                        // 해당 슬롯이 액티브  슬롯일때
                        else
                            // 마법 사용 수동으로 설정
                            passiveObj.GetComponent<MagicHolder>().isManualCast = true;
                    }
                }

                // 해당 마법이 쿨타임 중일때 넘기기 (이미 반복 코루틴 실행중이므로)
                if (MagicDB.Instance.GetMagicByID(magic.id).coolCount > 0)
                    continue;

                // 액티브 마법일때
                if (magic.castType == MagicDB.CastType.active.ToString())
                {
                    //nowCastMagics에 해당 마법 추가
                    nowCastMagics.Add(magic);

                    // 액티브 마법 시전
                    StartCoroutine(AutoCast(magic));
                }

                continue;
            }
            // 현재 실행중 마법일때
            else
            {
                // passiveMagics에서 해당 패시브 마법 오브젝트 찾기
                GameObject passiveObj = passiveObjs.Find(
                    x => x.GetComponent<MagicHolder>().magic.id
                    == magic.id);

                if (passiveObj != null)
                {
                    // 해당 슬롯이 인벤토리 슬롯일때
                    if (PhoneMenu.Instance.invenSlots.Exists(x => x == slotList[i]))
                        // 마법 사용 자동으로 설정
                        passiveObj.GetComponent<MagicHolder>().isManualCast = false;
                    // 해당 슬롯이 액티브  슬롯일때
                    else
                        // 마법 사용 수동으로 설정
                        passiveObj.GetComponent<MagicHolder>().isManualCast = true;
                }
            }

            //현재 실행중인 마법 레벨이 다르면 (마법 레벨업일때)
            if (tempMagic.magicLevel != magic.magicLevel)
            {
                //최근 갱신된 레벨 넣어주기
                tempMagic.magicLevel = magic.magicLevel;

                // print($"Name : {tempMagic.name} / Level : {tempMagic.magicLevel}");

                // 패시브 마법이면
                if (tempMagic.castType == MagicDB.CastType.passive.ToString())
                {
                    // passiveMagics에서 해당 패시브 마법 오브젝트 찾기
                    GameObject passiveObj = passiveObjs.Find(x => x.GetComponentInChildren<MagicHolder>().magic.id == tempMagic.id);

                    // 패시브 오브젝트 껐다켜서 초기화
                    passiveObj.SetActive(false);
                    passiveObj.SetActive(true);
                }
            }
        }

        // castList에 없는데 nowCastMagics에 있는(이미 시전중인) 마법 찾아서 중단시키기
        for (int i = 0; i < nowCastMagics.Count; i++)
        {
            //ID 같은 마법 찾기
            InventorySlot tempSlot = slotList.Find(x => x.slotInfo.id == nowCastMagics[i].id);
            SlotInfo tempMagic = null;
            if (tempSlot != null)
                tempMagic = tempSlot.slotInfo;

            // 시전 중인 마법을 castList에서 못찾으면 해당 마법 제거
            if (tempMagic as MagicInfo == null)
            {
                // 패시브 마법이면 컴포넌트 찾아서 디스폰
                if (nowCastMagics[i].castType == MagicDB.CastType.passive.ToString())
                {
                    // print(nowCastMagics[i].name);

                    // passiveMagics에서 해당 패시브 마법 오브젝트 찾기
                    GameObject passiveMagic = passiveObjs.Find(x => x.GetComponentInChildren<MagicHolder>(true).magic.id == nowCastMagics[i].id);

                    // 찾은 오브젝트 디스폰
                    LeanPool.Despawn(passiveMagic);

                    // 패시브 리스트에서 제거
                    passiveObjs.Remove(passiveMagic);

                    // 현재 소환여부 초기화
                    MagicDB.Instance.GetMagicByID(nowCastMagics[i].id).exist = false;
                }

                // 패시브 마법을 nowCastMagics에서 제거, Active 마법은 자동 중단됨
                nowCastMagics.Remove(nowCastMagics[i]);
            }
        }

        // 인게임 화면 하단에 사용중인 마법 아이콘 리스트 갱신
        StartCoroutine(UIManager.Instance.UpdateMagics(magicList));
    }

    public IEnumerator ManualCast(InventorySlot invenSlot, MagicInfo magic)
    {
        // 마법 정보가 없거나 쿨타임 중이면 실패 인디케이터 실행
        if (magic == null || MagicDB.Instance.GetActiveMagicByID(magic.id).coolCount > 0)
        {
            // 스프라이트 2회 켜기
            invenSlot.BlinkSlot(4);

            yield break;
        }

        // 플레이어 마법 시전 이펙트 플레이
        playerMagicCastEffect.Play();

        //todo 마법 시전 효과음 플레이
        // SoundManager.Instance.SoundPlay("UseMagic");

        MagicHolder magicHolder = null;

        // 패시브일때
        if (magic.castType == MagicDB.CastType.passive.ToString())
        {
            // passiveMagics에서 해당 패시브 마법 오브젝트 찾기
            GameObject magicObj = passiveObjs.Find(x => x.GetComponentInChildren<MagicHolder>(true).magic.id == magic.id);

            magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

            // 패시브 오브젝트에서 magicHolder 찾기
            if (magicObj != null)
            {
                // 수동 실행
                magicHolder.isManualCast = true;

                // 해당 마법의 콜백 실행
                if (magicHolder.magicCastCallback != null)
                    magicHolder.magicCastCallback();
            }
        }
        // 액티브일때
        else
        {
            //마법 프리팹 찾기
            GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);

            //프리팹 없으면 넘기기
            if (magicPrefab == null)
                yield break;

            // 마우스 위치 근처 공격 지점 리스트
            List<Vector2> attackPos = MarkEnemyPos(magic, PlayerManager.Instance.GetMousePos());

            // 공격 횟수만큼 시전
            StartCoroutine(Cast(true, attackPos, magic, magicPrefab, magicHolder));
        }

        // 해당 마법의 액티브 쿨타임 갱신
        MagicInfo activeMagic = MagicDB.Instance.GetActiveMagicByID(magic.id);
        float fixCoolTime = -1;

        // // 쿨타임 수동 제어일때
        // if (!magicHolder.autoCoolDown)
        // {
        //     // 쿨타임 스위치 끄기
        //     magicHolder.fixCoolTime = -1;
        //     // 쿨타임 스위치 켜질때까지 대기
        //     yield return new WaitUntil(() => magicHolder.fixCoolTime != -1);

        //     fixCoolTime = magicHolder.fixCoolTime;
        // }
        // // 쿨타임 자동 제어일때
        // else

        // 해당 마법 쿨타임 계산
        fixCoolTime = MagicDB.Instance.MagicCoolTime(activeMagic);
        // 쿨타입 입력
        activeMagic.coolCount = fixCoolTime;
        // 쿨타임 0 이하가 될때까지 대기
        yield return new WaitUntil(() => activeMagic.coolCount <= 0);

        // // 액티브 쿨타임 차감
        // yield return StartCoroutine(Cooldown(activeMagic, fixCoolTime));

        // // 패시브일때
        // if (magic.castType == MagicDB.CastType.passive.ToString())
        // {
        //     // 쿨타임 체크를 위한 전역 마법 정보 불러오기
        //     MagicInfo globalMagic = MagicDB.Instance.GetMagicByID(magic.id);

        //     // 글로벌 쿨타임 차감
        //     yield return StartCoroutine(Cooldown(globalMagic, fixCoolTime));
        // }
    }

    public IEnumerator AutoCast(MagicInfo magic)
    {
        // 핸드폰 마법 시전 이펙트 플레이
        phoneMagicCastEffect.Play();

        MagicHolder magicHolder = null;

        // 패시브일때
        if (magic.castType == MagicDB.CastType.passive.ToString())
        {
            // // passiveMagics에서 해당 패시브 마법 오브젝트 찾기
            // GameObject magicObj = passiveObjs.Find(x => x.GetComponentInChildren<MagicHolder>(true).magic.id == magic.id);

            // magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

            // // 패시브 오브젝트에서 magicHolder 찾기
            // if (magicHolder != null)
            // {
            //     // 자동 실행
            //     magicHolder.isManualCast = false;

            //     // 해당 마법의 콜백 실행
            //     if (magicHolder.magicCastCallback != null)
            //         magicHolder.magicCastCallback();
            // }
        }
        // 액티브일때
        else
        {
            //마법 프리팹 찾기
            GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);

            //프리팹 없으면 넘기기
            if (magicPrefab == null)
                yield break;

            // 공격 지점 찾기
            List<Vector2> attackPos = MarkEnemyPos(magic);

            // 마법 시전
            StartCoroutine(Cast(false, attackPos, magic, magicPrefab, magicHolder));
        }

        // 쿨타임 체크를 위한 전역 마법 정보 불러오기
        MagicInfo globalMagic = MagicDB.Instance.GetMagicByID(magic.id);
        // 해당 마법 쿨타임 계산
        float fixCoolTime = MagicDB.Instance.MagicCoolTime(globalMagic);
        // 쿨타입 입력
        globalMagic.coolCount = fixCoolTime;

        // 쿨타임 차감
        yield return StartCoroutine(CooldownCoroutine(globalMagic, fixCoolTime));

        // 쿨타임 0 이하가 될때까지 대기
        yield return new WaitUntil(() => globalMagic.coolCount <= 0);

        int slotIndex = -1;
        // 인벤토리에서 해당 마법이 있는 인덱스 찾기
        slotIndex = PhoneMenu.Instance.invenSlots.FindIndex(x => x != null &&
        // 마법인지 검사
        x.slotInfo as MagicInfo != null &&
        // 해당 마법과 같은 id를 가진 마법 있는지 검사
        x.slotInfo.id == magic.id);

        // 인벤토리에 있을때
        if (slotIndex != -1)
        {
            //코루틴 재실행
            StartCoroutine(AutoCast(magic));
        }
        // 사용하던 마법이 사라졌으면
        else
        {
            // 더이상 재실행 하지않고, 현재 사용중 목록에서 제거
            nowCastMagics.Remove(magic);
        }
    }

    IEnumerator Cast(bool isManual, List<Vector2> attackPos, MagicInfo magic, GameObject magicPrefab, MagicHolder magicHolder)
    {
        // // 공격지점 개수만큼 마법 시전
        // for (int i = 0; i < attackPos.Count; i++)
        // {           
        // }

        // 생성위치 구분
        Vector2 spawnPos = isManual ? PlayerManager.Instance.transform.position : phone.transform.position;

        // 마법 오브젝트 생성
        GameObject magicObj = LeanPool.Spawn(magicPrefab, spawnPos, Quaternion.identity, ObjectPool.Instance.magicPool);

        // 레이어 바꾸기
        magicObj.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;

        //매직 홀더 찾기
        magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

        // 수동 시전 여부 넣기
        magicHolder.isManualCast = isManual;

        //타겟 정보 넣기
        magicHolder.SetTarget(MagicHolder.TargetType.Enemy);

        //마법 정보 넣기
        magicHolder.magic = magic;

        //적 오브젝트 넣기, (유도 기능 등에 사용)
        // magicHolder.targetObj = attackPos[i];

        //적 위치 넣기
        if (attackPos[0] != null)
            magicHolder.targetPos = attackPos[0];

        // 고정 시간을 시전 개수만큼 나누기
        yield return new WaitForSeconds(0.5f / attackPos.Count);
    }

    public void Cooldown(MagicInfo magic, bool isManualCast, float fixCoolTime = -1)
    {
        // 시전 방식에 따라 다른 마법 정보 찾기
        if (isManualCast)
            magic = MagicDB.Instance.GetActiveMagicByID(magic.id);
        else
            magic = MagicDB.Instance.GetMagicByID(magic.id);

        // 코루틴 대리 실행
        StartCoroutine(CooldownCoroutine(magic, fixCoolTime));
    }

    IEnumerator CooldownCoroutine(MagicInfo magic, float fixCoolTime = -1)
    {
        // 고정 쿨타임 들어오면 넣기
        float cooltime = fixCoolTime != -1 ? fixCoolTime : MagicDB.Instance.MagicCoolTime(magic);
        magic.coolCount = cooltime;

        // 쿨타임중이면 반복
        while (magic.coolCount > 0)
        {
            // yield return new WaitForSeconds(Time.deltaTime);
            yield return new WaitForEndOfFrame();

            // 전역 쿨타임 차감
            magic.coolCount -= Time.deltaTime;
        }
    }

    void PassiveCast(MagicInfo magic)
    {
        // print("magic Summon : " + magic.magicName);

        //프리팹 찾기
        GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);

        //프리팹 없으면 넘기기
        if (magicPrefab == null)
            return;

        // 플레이어 위치에 마법 생성
        GameObject magicObj = LeanPool.Spawn(magicPrefab, PlayerManager.Instance.transform.position, Quaternion.identity, ObjectPool.Instance.magicPool);

        //마법 정보 넣기
        magicObj.GetComponentInChildren<MagicHolder>().magic = magic;

        //매직 홀더 찾기
        MagicHolder magicHolder = magicObj.GetComponentInChildren<MagicHolder>(true);

        //타겟 정보 넣기
        magicHolder.SetTarget(MagicHolder.TargetType.Enemy);

        //passive 마법 오브젝트 리스트에 넣기
        passiveObjs.Add(magicObj);

        //nowCastMagics에 해당 마법 추가
        nowCastMagics.Add(magic);
    }

    public List<Vector2> MarkEnemyPos(MagicInfo magic, Vector2 targetPos = default)
    {
        // 마법 범위 계산
        float range = MagicDB.Instance.MagicRange(magic);
        // 투사체 개수 계산
        int atkNum = MagicDB.Instance.MagicAtkNum(magic);

        // 프리팹 찾기
        GameObject magicPrefab = MagicDB.Instance.GetMagicPrefab(magic.id);

        // 리턴할 적 위치 리스트
        List<Vector2> enemyPosList = new List<Vector2>();

        // 적 오브젝트 리스트
        List<Character> enemyObjs = new List<Character>();

        // 기준점을 정해주지 않으면 주변의 적 찾기
        if (targetPos == default)
        {
            //범위 안의 모든 적 콜라이더 리스트에 담기
            List<Collider2D> enemyCollList = new List<Collider2D>();
            enemyCollList.Clear();
            enemyCollList = Physics2D.OverlapCircleAll(PlayerManager.Instance.transform.position, range, 1 << SystemManager.Instance.layerList.EnemyHit_Layer).ToList();

            // 적 못찾으면 범위내 랜덤 위치 잡기
            targetPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * range;

            // 가장 가까운 적의 위치를 기준으로 잡기
            foreach (Collider2D enemyColl in enemyCollList)
            {
                // 적 히트박스 찾기
                HitBox targetHitBox = enemyColl.GetComponent<HitBox>();
                // 히트박스 찾으면 끝내기
                if (targetHitBox != null)
                {
                    targetPos = enemyColl.transform.position;
                    break;
                }
            }
        }

        // 투사체 개수만큼 반복
        for (int i = 0; i < atkNum; i++)
        {
            Vector2 pos = targetPos;

            // 2번째 투사체부터
            if (i > 1)
                // 마법 월드 사이즈에 비례해서 위치 퍼뜨리기
                pos = targetPos + Random.insideUnitCircle.normalized * magicPrefab.transform.lossyScale.x * 3f;

            // 적 위치 변수에 담기
            enemyPosList.Add(pos);
        }

        // 적의 위치 리스트 리턴
        return enemyPosList;
    }

    public List<Character> MarkEnemies(MagicInfo magic)
    {
        // 마법 범위 계산
        float range = MagicDB.Instance.MagicRange(magic);
        // 투사체 개수 계산
        int atkNum = MagicDB.Instance.MagicAtkNum(magic);

        //리턴할 적 오브젝트 리스트
        List<Character> enemyObjs = new List<Character>();

        // 마우스를 중심으로 범위 안의 모든 적 콜라이더 리스트에 담기
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

            Character character = null;
            Collider2D targetColl = null;

            if (enemyCollList.Count > 0)
            {
                // 리스트 내에서 랜덤으로 선택
                targetColl = enemyCollList[Random.Range(0, enemyCollList.Count)];
                // 적 히트박스 찾기
                HitBox targetHitBox = targetColl.GetComponent<HitBox>();
                if (targetHitBox != null)
                    // 적 매니저 찾기
                    character = targetHitBox.character;

                // 이미 들어있는 오브젝트일때
                if (enemyObjs.Exists(x => x == character)
                // 해당 몬스터가 유령일때
                || (character && character.IsGhost))
                {
                    // 임시 리스트에서 지우기
                    enemyCollList.Remove(targetColl);

                    // 넘기기
                    continue;
                }
            }

            // 적 오브젝트 변수에 담기
            enemyObjs.Add(character);

            // 임시 리스트에서 지우기
            if (targetColl != null)
                enemyCollList.Remove(targetColl);
        }

        //적의 위치 리스트 리턴
        return enemyObjs;
    }
}
