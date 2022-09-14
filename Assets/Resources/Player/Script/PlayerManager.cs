using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;
using System.Linq;
using TMPro;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerStat
{
    public int playerPower; //플레이어 전투력
    public float hpMax = 10000; // 최대 체력
    public float hpNow = 10000; // 체력
    public float Level = 1; //레벨
    public float ExpMax = 5; // 경험치 최대치
    public float ExpNow = 0; // 현재 경험치
    public float moveSpeed = 10; //이동속도

    public int projectileNum = 0; // 투사체 개수
    public int pierce = 0; // 관통 횟수
    public float power = 1; //마법 공격력
    public float armor = 1; //방어력
    public float knockbackForce = 1; //넉백 파워
    public float speed = 1; //마법 공격속도
    public float coolTime = 1; //마법 쿨타임
    public float duration = 1; //마법 지속시간
    public float range = 1; //마법 범위
    public float luck = 1; //행운
    public float expGain = 1; //경험치 획득량
    public float moneyGain = 1; //원소젬 획득량

    //원소 공격력
    public float earth_atk = 1;
    public float fire_atk = 1;
    public float life_atk = 1;
    public float lightning_atk = 1;
    public float water_atk = 1;
    public float wind_atk = 1;
}

public class PlayerManager : Character
{
    #region Singleton
    private static PlayerManager instance;
    public static PlayerManager Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<PlayerManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<PlayerManager>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("<Input>")]
    public NewInput playerInput;
    Vector2 inputMoveDir; // 현재 이동 입력 벡터
    Vector2 nowMoveDir; // 현재 이동 벡터
    public Vector2 lastDir; // 마지막 이동 벡터
    public Vector3 mouseWorldPos; // 현재 마우스 위치
    public bool isDash; //현재 대쉬중 여부
    public float defaultDashSpeed = 1.5f; // 대쉬 속도 기본값
    public float dashSpeed = 1; //대쉬 버프 속도
    public float speedDeBuff = 1f; //이동속도 디버프
    public PlayerInteracter playerInteracter; //플레이어 상호작용 컴포넌트

    [Header("<Refer>")]
    // public Transform activeParent; // 액티브 슬롯들 부모 오브젝트
    public GameObject marker; //! 테스트용 마커
    public GameObject bloodPrefab; //플레이어 혈흔 파티클
    public PlayerHitBox hitBox;
    public GameObject mobSpawner;
    public SpriteRenderer sprite; // 몸체 스프라이트
    public SpriteRenderer shadowSprite; // 그림자 스프라이트
    public Light2D playerLight;

    [Header("<Stat>")] //플레이어 스탯
    public PlayerStat PlayerStat_Origin; //초기 스탯
    public PlayerStat PlayerStat_Now; //현재 스탯

    [Header("<State>")]
    public bool initFinish = false;
    public float camFollowSpeed = 10f; // 캠 따라오는 속도

    //TODO 피격시 카메라 흔들기
    // public float ShakeTime;
    // public float ShakeIntensity;

    [Header("<Pocket>")]
    // public SlotInfo[] inventory = new SlotInfo[23]; // 플레이어 인벤토리
    public List<int> hasGems = new List<int>(6); //플레이어가 가진 원소젬
    public List<ItemInfo> hasItems = new List<ItemInfo>(); //플레이어가 가진 아이템
    public InventorySlot activeSlot_A;
    public InventorySlot activeSlot_B;
    public InventorySlot activeSlot_C;

    private void Awake()
    {
        // 위치 초기화
        transform.position = Vector3.zero;

        rigid = rigid == null ? GetComponent<Rigidbody2D>() : rigid;
        anim = anim == null ? GetComponent<Animator>() : anim;
        sprite = sprite == null ? GetComponent<SpriteRenderer>() : sprite;

        //플레이어 스탯 인스턴스 생성
        PlayerStat_Now = new PlayerStat();

        //플레이어 초기 스탯 저장
        PlayerStat_Origin = PlayerStat_Now;

        // 입력값 초기화
        InputInit();
    }

    void InputInit()
    {
        playerInput = new NewInput();

        // 방향키 눌렀을때
        playerInput.Player.Move.performed += val =>
        {
            //현재 이동방향 입력
            inputMoveDir = val.ReadValue<Vector2>();

            //대쉬 중 아닐때만
            Move();
        };

        // 방향키 안누를때
        playerInput.Player.Move.canceled += val =>
        {
            //현재 이동방향 입력
            inputMoveDir = val.ReadValue<Vector2>();

            //대쉬 중 아닐때만
            Move();
        };

        // 대쉬 버튼 매핑
        playerInput.Player.Dash.performed += val =>
        {
            // 대쉬중 아닐때, 현재 이동 정지 아닐때
            if (!isDash && inputMoveDir != Vector2.zero)
                DashToggle();
        };

        // 상호작용 버튼 매핑
        playerInput.Player.Interact.performed += val =>
        {
            // 현재 상호작용 가능한 오브젝트 상호작용 하기
            InteractSubmit();
        };

        // 마우스 위치 입력
        playerInput.Player.MousePosition.performed += val =>
        {
            mouseWorldPos = Camera.main.ScreenToWorldPoint(val.ReadValue<Vector2>());
            mouseWorldPos.z = 0;

            // print(mouseWorldPos);
        };

        // 마법 시전 버튼 매핑
        playerInput.Player.ActiveMagic_A.performed += val =>
        {
            // 0번째 액티브 슬롯 마법 불러오기
            // MagicInfo magic = inventory[20] as MagicInfo;
            MagicInfo magic = activeSlot_A.slotInfo as MagicInfo;

            // // 해당 액티브 슬롯 실패 인디케이터 찾기
            // InventorySlot invenSlot = PlayerManager.Instance.activeParent.GetChild(0).GetComponent<InventorySlot>();

            // 수동 마법 시전
            StartCoroutine(CastMagic.Instance.ManualCast(activeSlot_A, magic));
        };
        playerInput.Player.ActiveMagic_B.performed += val =>
        {
            // 1번째 액티브 슬롯 마법 불러오기
            // MagicInfo magic = inventory[21] as MagicInfo;
            MagicInfo magic = activeSlot_B.slotInfo as MagicInfo;

            // // 해당 액티브 슬롯 실패 인디케이터 찾기
            // InventorySlot invenSlot = PlayerManager.Instance.activeParent.GetChild(1).GetComponent<InventorySlot>();

            // 수동 마법 시전
            StartCoroutine(CastMagic.Instance.ManualCast(activeSlot_B, magic));
        };
        playerInput.Player.ActiveMagic_C.performed += val =>
        {
            // 2번째 액티브 슬롯 마법 불러오기
            // MagicInfo magic = inventory[22] as MagicInfo;
            MagicInfo magic = activeSlot_C.slotInfo as MagicInfo;

            // // 해당 액티브 슬롯 실패 인디케이터 찾기
            // InventorySlot invenSlot = PlayerManager.Instance.activeParent.GetChild(2).GetComponent<InventorySlot>();

            // 수동 마법 시전
            StartCoroutine(CastMagic.Instance.ManualCast(activeSlot_C, magic));
        };

        // 초기화 완료
        initFinish = true;
    }

    private void OnEnable()
    {
        playerInput.Enable();

        //기본 마법 추가
        StartCoroutine(CastDefaultMagics());
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }

    void Start()
    {
        // 원소젬 UI 업데이트
        for (int i = 0; i < 6; i++)
        {
            // hasGems.Add(0);
            UIManager.Instance.UpdateGem(i);
        }

        //경험치 최대치 갱신
        PlayerStat_Now.ExpMax = PlayerStat_Now.Level * PlayerStat_Now.Level + 5;

        //능력치 초기화
        UIManager.Instance.InitialStat();
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        // 카메라 플레이어 부드럽게 따라오기
        SystemManager.Instance.camParent.position = Vector3.Lerp(SystemManager.Instance.camParent.position, transform.position + new Vector3(0, 0, -50), Time.deltaTime * camFollowSpeed);

        //몬스터 스포너 따라오기
        if (mobSpawner.activeSelf)
            mobSpawner.transform.position = transform.position;

        //히트 카운트 감소
        if (hitBox.hitCoolCount > 0)
            hitBox.hitCoolCount -= Time.deltaTime;

        // 플레이어 이동
        Move();

        //! 테스트용 마커 이동
        marker.transform.position = mouseWorldPos;
    }

    public void Move()
    {
        // print(nowMoveDir);

        // 깔렸을때 조작불가
        if (hitBox.isFlat)
        {
            //대쉬 중이었으면 취소
            isDash = false;

            //이동 멈추기
            rigid.velocity = Vector2.zero;

            //Idle 애니메이션으로 바꾸고 멈추기
            anim.SetBool("isWalk", false);
            anim.SetBool("isDash", false);
            anim.speed = 0;

            return;
        }
        else
        {
            anim.speed = 1 * SystemManager.Instance.playerTimeScale;
        }

        // x축 이동에 따라 회전
        if (inputMoveDir.x != 0)
        {
            //방향 따라 캐릭터 회전
            if (inputMoveDir.x > 0)
            {
                sprite.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                sprite.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }

        // 방향키 입력에 따라 애니메이터 걷기 변수 입력
        if (inputMoveDir == Vector2.zero)
        {
            anim.SetBool("isWalk", false);
        }
        else
        {
            anim.SetBool("isWalk", true);
        }

        //대쉬 입력에 따라 애니메이터 대쉬 변수 입력
        dashSpeed = isDash ? defaultDashSpeed : 1f;

        // 대쉬중 아닐때
        if (!isDash)
            // 이동 입력값을 실제 이동 벡터에 담기
            nowMoveDir = inputMoveDir;

        // 마지막 이동 방향 기억
        if (nowMoveDir != Vector2.zero)
            lastDir = nowMoveDir;

        // 실제 오브젝트 이동해주기
        rigid.velocity =
        PlayerStat_Now.moveSpeed //플레이어 이동속도
        * nowMoveDir //움직일 방향
        * dashSpeed //대쉬할때 속도 증가
        * speedDeBuff // 속도 버프
        * SystemManager.Instance.playerTimeScale //플레이어 개인 타임스케일
        + hitBox.knockbackDir //넉백 벡터 추가
        ;
    }

    public void DashToggle()
    {
        isDash = !isDash;
        anim.SetBool("isDash", isDash);

        //대쉬 끝날때 이동 입력확인
        Move();

        // print("Dash : " + isDash);
    }

    public void InteractSubmit()
    {
        // 가장 가까운 상호작용 오브젝트와 상호작용 실행
        if (playerInteracter.nearInteracter != null && playerInteracter.nearInteracter.interactSubmitCallback != null)
            playerInteracter.nearInteracter.interactSubmitCallback();
    }

    IEnumerator CastDefaultMagics()
    {
        // MagicDB 로드 완료까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //! 인스펙터의 테스트 마법 획득
        for (int i = 0; i < CastMagic.Instance.testMagics.Count; i++)
        {
            int id = CastMagic.Instance.testMagics[i];
            MagicInfo magic = MagicDB.Instance.GetMagicByID(id);
            PhoneMenu.Instance.GetMagic(magic);
        }

        //! 인스펙터의 테스트 아이템 획득
        for (int i = 0; i < CastMagic.Instance.testItems.Count; i++)
        {
            int id = CastMagic.Instance.testItems[i];
            ItemInfo item = ItemDB.Instance.GetItemByID(id);
            PhoneMenu.Instance.GetItem(item);
        }

        // 인벤토리에서 마법 찾아 자동 시전하기
        CastMagic.Instance.CastCheck();

        //플레이어 총 전투력 업데이트
        PlayerStat_Now.playerPower = GetPlayerPower();
    }

    void BuffUpdate()
    {
        //초기 스탯 복사
        PlayerStat PlayerStat_Temp = new PlayerStat();
        PlayerStat_Temp = PlayerStat_Origin;

        //임시 스탯에 현재 아이템의 모든 버프 넣기
        foreach (var item in hasItems)
        {
            PlayerStat_Temp.projectileNum += item.projectileNum * item.amount; // 투사체 개수 버프
            PlayerStat_Temp.hpMax += item.hpMax * item.amount; //최대체력 버프
            PlayerStat_Temp.power += item.power * item.amount; //마법 공격력 버프
            PlayerStat_Temp.armor += item.armor * item.amount; //방어력 버프
            PlayerStat_Temp.speed += item.rateFire * item.amount; //마법 공격속도 버프
            PlayerStat_Temp.coolTime += item.coolTime * item.amount; //마법 쿨타임 버프
            PlayerStat_Temp.duration += item.duration * item.amount; //마법 지속시간 버프
            PlayerStat_Temp.range += item.range * item.amount; //마법 범위 버프
            PlayerStat_Temp.luck += item.luck * item.amount; //행운 버프
            PlayerStat_Temp.expGain += item.expGain * item.amount; //경험치 획득량 버프
            PlayerStat_Temp.moneyGain += item.moneyGain * item.amount; //원소젬 획득량 버프
            PlayerStat_Temp.moveSpeed += item.moveSpeed / item.amount; //이동속도 버프

            PlayerStat_Temp.earth_atk += item.earth * item.amount;
            PlayerStat_Temp.fire_atk += item.fire * item.amount;
            PlayerStat_Temp.life_atk += item.life * item.amount;
            PlayerStat_Temp.lightning_atk += item.lightning * item.amount;
            PlayerStat_Temp.water_atk += item.water * item.amount;
            PlayerStat_Temp.wind_atk += item.wind * item.amount;
        }

        //현재 스탯에 임시 스탯을 넣기
        PlayerStat_Now = PlayerStat_Temp;
    }

    public void AddGem(ItemInfo item, int amount)
    {
        // 어떤 원소든지 젬 개수만큼 경험치 증가
        PlayerStat_Now.ExpNow += amount;

        //경험치 다 찼을때
        if (PlayerStat_Now.ExpNow >= PlayerStat_Now.ExpMax)
        {
            //레벨업
            Levelup();
        }
        // print(item.itemName.Split(' ')[0]);

        // 가격 타입으로 젬 타입 인덱스로 반환
        int gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.ElementNames, x => x == item.priceType);

        // 젬 타입 인덱스로 해당 젬과 같은 마법 찾아서 획득
        // GetMagic(MagicDB.Instance.GetMagicByID(gemTypeIndex));

        //해당 젬 갯수 올리기
        hasGems[gemTypeIndex] = hasGems[gemTypeIndex] + amount;
        // print(hasGems[gemTypeIndex] + " : " + amount);

        //해당 젬 UI 인디케이터
        UIManager.Instance.GemIndicator(gemTypeIndex);

        // UI 업데이트
        UIManager.Instance.UpdateGem(gemTypeIndex);

        //경험치 및 레벨 갱신
        UIManager.Instance.UpdateExp();
    }

    void Levelup()
    {
        // 시간 멈추기
        // Time.timeScale = 0;

        //레벨업
        PlayerStat_Now.Level++;

        //경험치 초기화
        PlayerStat_Now.ExpNow = 0;

        //경험치 최대치 갱신
        PlayerStat_Now.ExpMax = PlayerStat_Now.Level * PlayerStat_Now.Level + 5;
        //! 테스트용 맥스 경험치
        PlayerStat_Now.ExpMax = 3;

        // 마법 합성 메뉴 띄우기
        // UIManager.Instance.PopupUI(UIManager.Instance.mergeMagicPanel);
    }

    public void PayGem(int gemIndex, int price)
    {
        //원소젬 지불하기
        hasGems[gemIndex] -= price;

        //젬 UI 업데이트
        UIManager.Instance.UpdateGem(gemIndex);
    }

    public int GetPlayerPower()
    {
        //플레이어의 총 전투력
        int magicPower = 0;

        foreach (InventorySlot invenSlot in PhoneMenu.Instance.invenSlots)
        {
            // 마법 찾기
            MagicInfo magic = invenSlot.slotInfo as MagicInfo;

            // 마법 정보 없으면 리턴
            if (magic == null)
                continue;

            //총전투력에 해당 마법의 등급*레벨 더하기
            magicPower += magic.grade * magic.magicLevel;

            // print(magicPower + " : " + magic.grade + " * " + magic.magicLevel);
        }

        return magicPower;
    }
}
