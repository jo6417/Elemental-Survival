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

[System.Serializable]
public class PlayerStat
{
    public int playerPower; //플레이어 전투력
    public float hpMax = 100; // 최대 체력
    public float hpNow = 100; // 체력
    public float Level = 1; //레벨
    public float moveSpeed = 10; //이동속도

    public int atkNum = 0; // 공격 횟수
    public int pierce = 0; // 관통 횟수
    public float power = 1; //마법 공격력
    public float armor = 1; //방어력
    public float speed = 1; //마법 공격속도
    public float evade = 0f; // 회피율
    public float knockbackForce = 1; //넉백 파워
    public float coolTime = 1; //마법 쿨타임
    public float duration = 1; //마법 지속시간
    public float range = 1; //마법 범위
    public float luck = 1; //행운
    public float expGain = 1; //경험치 획득량
    public float getRage = 1; // 아이템 획득 범위

    //원소 공격력
    public float earth_atk = 1;
    public float fire_atk = 1;
    public float life_atk = 1;
    public float lightning_atk = 1;
    public float water_atk = 1;
    public float wind_atk = 1;

    public PlayerStat() { }
    public PlayerStat(PlayerStat playerStat)
    {
        this.playerPower = playerStat.playerPower; //플레이어 전투력
        this.hpMax = playerStat.hpMax; // 최대 체력
        this.hpNow = playerStat.hpNow; // 체력
        this.Level = playerStat.Level; //레벨
        this.moveSpeed = playerStat.moveSpeed; //이동속도
        this.atkNum = playerStat.atkNum; // 공격 횟수
        this.pierce = playerStat.pierce; // 관통 횟수
        this.power = playerStat.power; //마법 공격력
        this.armor = playerStat.armor; //방어력
        this.speed = playerStat.speed; //마법 공격속도
        this.evade = playerStat.evade; // 회피율
        this.knockbackForce = playerStat.knockbackForce; //넉백 파워
        this.coolTime = playerStat.coolTime; //마법 쿨타임
        this.duration = playerStat.duration; //마법 지속시간
        this.range = playerStat.range; //마법 범위
        this.luck = playerStat.luck; //행운
        this.expGain = playerStat.expGain; //경험치 획득량
        this.getRage = playerStat.getRage; // 아이템 획득 범위

        // 원소 공격력
        this.earth_atk = playerStat.earth_atk;
        this.fire_atk = playerStat.fire_atk;
        this.life_atk = playerStat.life_atk;
        this.lightning_atk = playerStat.lightning_atk;
        this.water_atk = playerStat.water_atk;
        this.wind_atk = playerStat.wind_atk;
    }
}

public class PlayerManager : MonoBehaviour
{
    #region Singleton
    private static PlayerManager instance;
    public static PlayerManager Instance
    {
        get
        {
            // if (instance == null)
            // {
            //     var obj = FindObjectOfType<PlayerManager>();
            //     if (obj != null)
            //     {
            //         instance = obj;
            //     }
            //     // else
            //     // {
            //     //     var newObj = new GameObject().AddComponent<PlayerManager>();
            //     //     instance = newObj;
            //     // }
            // }
            return instance;
        }
    }
    #endregion

    [Header("<Input>")]
    public NewInput player_Input;
    Vector2 inputMoveDir; // 현재 이동 입력 벡터
    Vector2 nowMoveDir; // 현재 이동 벡터
    public Vector2 lastDir; // 마지막 이동 벡터
    public bool isDash; //현재 대쉬중 여부
    public float defaultDashSpeed = 1.5f; // 대쉬 속도 기본값
    public float dashSpeed = 1; //대쉬 버프 속도
    public float speedBuff = 1f; //이동속도 디버프
    public PlayerInteracter playerInteracter; //플레이어 상호작용 컴포넌트

    [Header("<Refer>")]
    // public Transform activeParent; // 액티브 슬롯들 부모 오브젝트
    // public GameObject aimCursor; //! 테스트용 마커
    public GameObject bloodPrefab; //플레이어 혈흔 파티클
    public PlayerHitBox hitBox;
    public GameObject mobSpawner;
    public SpriteRenderer playerSprite; // 몸체 스프라이트
    public SpriteRenderer playerCover; // 플레이어와 같은 이미지로 덮기
    public SpriteRenderer shadowSprite; // 그림자 스프라이트
    public Light2D playerLight;
    public Rigidbody2D rigid;
    public Collider2D coll;
    public Animator anim;

    [Header("<Stat>")] //플레이어 스탯
    public PlayerStat PlayerStat_Now; // 현재 스탯
    private PlayerStat PlayerStat_Default; // 초기 스탯
    public float ExpMax = 5; // 경험치 최대치
    public float ExpNow = 0; // 현재 경험치

    [Header("<State>")]
    public bool initFinish = false;
    public float hpNow;
    public float hpMax;
    public enum Debuff { Burn, Poison, Bleed, Slow, Shock, Stun, Stop, Flat, Freeze };
    public IEnumerator[] DebuffList = new IEnumerator[System.Enum.GetValues(typeof(Debuff)).Length];

    [Header("<Pocket>")]
    [SerializeField] List<int> hasGems = new List<int>(); // 테스트용 초기 원소젬 개수
    public List<ItemInfo> hasItems = new List<ItemInfo>(); //플레이어가 가진 아이템

    [Header("Sound")]
    int lastStepSound = -1;

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

        // 위치 초기화
        transform.position = Vector3.zero;

        rigid = rigid == null ? GetComponent<Rigidbody2D>() : rigid;
        anim = anim == null ? GetComponent<Animator>() : anim;
        playerSprite = playerSprite == null ? GetComponent<SpriteRenderer>() : playerSprite;

        // 플레이어 초기 스탯 저장
        PlayerStat_Default = new PlayerStat(PlayerStat_Now);

        // 입력값 초기화
        StartCoroutine(InputInit());
    }

    IEnumerator InputInit()
    {
        player_Input = new NewInput();

        // 방향키 눌렀을때
        player_Input.Player.Move.performed += val =>
        {
            //! 사운드 테스트
            // SoundManager.Instance.SoundPlay("Test");

            //현재 이동방향 입력
            inputMoveDir = val.ReadValue<Vector2>();

            //대쉬 중 아닐때만
            Move();
        };

        // 방향키 안누를때
        player_Input.Player.Move.canceled += val =>
        {
            //현재 이동방향 입력
            inputMoveDir = val.ReadValue<Vector2>();

            //대쉬 중 아닐때만
            Move();
        };

        // 대쉬 버튼 매핑
        player_Input.Player.Dash.performed += val =>
        {
            // 대쉬중 아닐때, 현재 이동 정지 아닐때
            if (!isDash && inputMoveDir != Vector2.zero)
                DashToggle();
        };

        // 상호작용 버튼 눌렀을때
        player_Input.Player.Interact.performed += val =>
        {
            // 현재 상호작용 가능한 오브젝트 상호작용 하기
            InteractSubmit(true);
        };

        // 상호작용 버튼 뗐을때
        player_Input.Player.Interact.canceled += val =>
        {
            // 현재 상호작용 가능한 오브젝트 상호작용 하기
            InteractSubmit(false);
        };

        yield return new WaitUntil(() => UIManager.Instance != null && CastMagic.Instance != null);

        // A슬롯 마법 시전
        player_Input.Player.ActiveMagic_A.performed += val =>
        {
            // 0번째 액티브 슬롯 마법 불러오기
            MagicInfo magic = UIManager.Instance.activeSlot_A.slotInfo as MagicInfo;

            // 수동 마법 시전
            StartCoroutine(CastMagic.Instance.ManualCast(UIManager.Instance.activeSlot_A, magic));
        };
        // B슬롯 마법 시전
        player_Input.Player.ActiveMagic_B.performed += val =>
        {
            // 1번째 액티브 슬롯 마법 불러오기
            MagicInfo magic = UIManager.Instance.activeSlot_B.slotInfo as MagicInfo;

            // 수동 마법 시전
            StartCoroutine(CastMagic.Instance.ManualCast(UIManager.Instance.activeSlot_B, magic));
        };
        // C슬롯 마법 시전
        player_Input.Player.ActiveMagic_C.performed += val =>
        {
            // 2번째 액티브 슬롯 마법 불러오기
            MagicInfo magic = UIManager.Instance.activeSlot_C.slotInfo as MagicInfo;

            // 수동 마법 시전
            StartCoroutine(CastMagic.Instance.ManualCast(UIManager.Instance.activeSlot_C, magic));
        };

        // 플레이어 입력 켜기
        player_Input.Enable();

        // 초기화 완료
        initFinish = true;
    }

    public Vector2 GetMousePos()
    {
        return Camera.main.ScreenToWorldPoint(PlayerManager.Instance.player_Input.Player.MousePosition.ReadValue<Vector2>());
    }

    public Vector2 GetMouseDir()
    {
        return GetMousePos() - (Vector2)transform.position;
    }

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());

        //기본 마법 추가
        StartCoroutine(CastDefaultMagics());
    }

    IEnumerator Init()
    {
        // 플레이어 커버 색 초기화
        playerCover.color = new Color(1, 1, 1, 0);

        yield return new WaitUntil(() => ItemDB.Instance.loadDone);

        // 보유 아이템 리스트 초기화
        hasItems.Clear();
        // 6종류 gem을 리스트에 넣기
        for (int i = 0; i < hasGems.Count; i++)
        {
            ItemInfo gem = new ItemInfo(ItemDB.Instance.GetItemByID(i));
            //! 테스트용 원소젬 개수 넣기
            gem.amount = hasGems[i];

            hasItems.Add(gem);
        }

        // 플레이어 버프 업데이트
        BuffUpdate();

        yield return new WaitUntil(() => UIManager.Instance != null);

        // 원소젬 전체 UI 업데이트
        UIManager.Instance.UpdateGem();

        //능력치 초기화
        UIManager.Instance.InitialStat();

        // 1레벨 경험치 최대치 갱신
        ExpMax = PlayerStat_Now.Level * PlayerStat_Now.Level + 5;
    }

    private void OnDestroy()
    {
        if (player_Input != null)
        {
            player_Input.Disable();
            player_Input.Dispose();
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        // 스프라이트 복제
        playerCover.sprite = playerSprite.sprite;

        //몬스터 스포너 따라오기
        if (mobSpawner.activeSelf)
            mobSpawner.transform.position = transform.position;

        //히트 카운트 감소
        if (hitBox.hitCoolCount > 0)
            hitBox.hitCoolCount -= Time.deltaTime;

        // 플레이어 이동
        Move();

        //! 테스트용 마커 이동
        // aimCursor.transform.position = GetMousePos();
    }

    public void Move()
    {
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

        // x축 이동에 따라 회전
        if (inputMoveDir.x != 0)
        {
            //방향 따라 캐릭터 회전
            if (inputMoveDir.x > 0)
            {
                playerSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                playerSprite.transform.rotation = Quaternion.Euler(0, 180, 0);
            }

            // 그림자는 회전값 유지
            shadowSprite.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        //대쉬 입력에 따라 애니메이터 대쉬 변수 입력
        dashSpeed = isDash ? defaultDashSpeed : 1f;

        // 대쉬중 아닐때
        if (!isDash)
            // 이동 입력값을 실제 이동 벡터에 담기
            nowMoveDir = inputMoveDir;

        // 방향키 입력에 따라 애니메이터 걷기 변수 입력
        if (inputMoveDir == Vector2.zero)
        {
            anim.SetBool("isWalk", false);
        }
        else
        {
            anim.SetBool("isWalk", true);
        }

        // 마지막 이동 방향 기억
        if (nowMoveDir != Vector2.zero)
            lastDir = nowMoveDir;

        Vector2 moveVector =
        PlayerStat_Now.moveSpeed * 10f //플레이어 이동속도
        * nowMoveDir //움직일 방향
        * dashSpeed //대쉬할때 속도 증가
        * speedBuff // 속도 버프
        * SystemManager.Instance.playerTimeScale //플레이어 개인 타임스케일
        + hitBox.knockbackDir; //넉백 벡터 추가

        // 실제 오브젝트 이동해주기
        rigid.velocity = moveVector;

        // 대쉬중 아닐때
        if (!isDash)
            // 이동 속도를 애니메이션 속도에 적용
            anim.speed = 1 * SystemManager.Instance.playerTimeScale * moveVector.magnitude * 0.1f;
        else
            // 일반 속도로 적용
            anim.speed = 1 * SystemManager.Instance.playerTimeScale;
    }

    public void DashToggle()
    {
        isDash = !isDash;
        anim.SetBool("isDash", isDash);

        // 대쉬 토글시 체력바 가리기
        UIManager.Instance.dodgeBar.alpha = isDash ? 1f : 0f;

        //대쉬 끝날때 이동 입력확인
        Move();

        // print("Dash : " + isDash);
    }

    public void InteractSubmit(bool isPress = true)
    {
        // 가장 가까운 상호작용 오브젝트와 상호작용 실행
        if (playerInteracter.nearInteracter != null && playerInteracter.nearInteracter.interactSubmitCallback != null)
            playerInteracter.nearInteracter.interactSubmitCallback(isPress);
    }

    IEnumerator CastDefaultMagics()
    {
        // MagicDB 로드 완료까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //! 인스펙터의 테스트 마법 획득
        for (int i = 0; i < CastMagic.Instance.testMagics.Count; i++)
        {
            string name = CastMagic.Instance.testMagics[i].ToString();
            MagicInfo magic = MagicDB.Instance.GetMagicByName(name);
            PhoneMenu.Instance.GetMagic(magic);

            yield return null;
        }

        //! 인스펙터의 테스트 아이템 획득
        for (int i = 0; i < CastMagic.Instance.testItems.Count; i++)
        {
            string name = CastMagic.Instance.testItems[i].ToString();
            ItemInfo item = ItemDB.Instance.GetItemByName(name);
            PhoneMenu.Instance.GetItem(item);

            yield return null;
        }

        // 인벤토리에서 마법 찾아 자동 시전하기
        CastMagic.Instance.CastCheck();

        //플레이어 총 전투력 업데이트
        PlayerStat_Now.playerPower = GetPlayerPower();
    }

    void BuffUpdate()
    {
        //초기 스탯을 임시 스탯으로 복사
        PlayerStat PlayerStat_Temp = new PlayerStat(PlayerStat_Default);

        //임시 스탯에 현재 아이템의 모든 버프 넣기
        foreach (ItemInfo item in hasItems)
        {
            PlayerStat_Temp.atkNum += item.atkNum * item.amount; // 투사체 개수 버프
            PlayerStat_Temp.hpMax += item.hpMax * item.amount; // 최대체력 버프
            PlayerStat_Temp.power += item.power * item.amount; // 마법 공격력 버프
            PlayerStat_Temp.armor += item.armor * item.amount; // 방어력 버프
            PlayerStat_Temp.speed += item.speed * item.amount; // 마법 속도 버프
            PlayerStat_Temp.evade += item.evade * item.amount; // 회피율 버프
            PlayerStat_Temp.coolTime += item.coolTime * item.amount; // 마법 쿨타임 버프
            PlayerStat_Temp.duration += item.duration * item.amount; // 마법 지속시간 버프
            PlayerStat_Temp.range += item.range * item.amount; // 마법 범위 버프
            PlayerStat_Temp.luck += item.luck * item.amount; // 행운 버프
            PlayerStat_Temp.expGain += item.expRate * item.amount; // 경험치 획득량 버프
            PlayerStat_Temp.getRage += item.getRage * item.amount; // 아이템 획득거리 버프
            PlayerStat_Temp.moveSpeed += item.moveSpeed * item.amount; //이동속도 버프

            // PlayerStat_Temp.earth_atk += item.earth * item.amount;
            // PlayerStat_Temp.fire_atk += item.fire * item.amount;
            // PlayerStat_Temp.life_atk += item.life * item.amount;
            // PlayerStat_Temp.lightning_atk += item.lightning * item.amount;
            // PlayerStat_Temp.water_atk += item.water * item.amount;
            // PlayerStat_Temp.wind_atk += item.wind * item.amount;
        }

        //현재 스탯에 임시 스탯을 넣기
        PlayerStat_Now = PlayerStat_Temp;

        // string allBuff = " atkNum : " + PlayerStat_Temp.atkNum + ", " +
        //     "\n hpMax : " + PlayerStat_Temp.hpMax + ", " +
        //     "\n power : " + PlayerStat_Temp.power + ", " +
        //     "\n armor : " + PlayerStat_Temp.armor + ", " +
        //     "\n speed : " + PlayerStat_Temp.speed + ", " +
        //     "\n evade : " + PlayerStat_Temp.evade + ", " +
        //     "\n coolTime : " + PlayerStat_Temp.coolTime + ", " +
        //     "\n duration : " + PlayerStat_Temp.duration + ", " +
        //     "\n range : " + PlayerStat_Temp.range + ", " +
        //     "\n luck : " + PlayerStat_Temp.luck + ", " +
        //     "\n expGain : " + PlayerStat_Temp.expGain + ", " +
        //     "\n moneyGain : " + PlayerStat_Temp.getRage + ", " +
        //     "\n moveSpeed : " + PlayerStat_Temp.moveSpeed;
        // print(allBuff);
    }

    public void AddGem(ItemInfo item, int amount)
    {
        // 어떤 원소든지 젬 개수만큼 경험치 증가
        ExpNow += amount;

        //경험치 다 찼을때
        if (ExpNow >= ExpMax)
        {
            //레벨업
            Levelup();
        }

        // 가격 타입으로 젬 타입 인덱스로 반환
        int gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.ElementNames, x => x == item.priceType);
        // 보유 아이템 중 해당 젬 개수 올리기
        hasItems[gemTypeIndex].amount += amount;

        // 플레이어 버프 업데이트
        // BuffUpdate();

        //해당 젬 UI 인디케이터
        UIManager.Instance.GemIndicator(gemTypeIndex, Color.green);

        // UI 업데이트
        UIManager.Instance.UpdateGem(gemTypeIndex);

        //경험치 및 레벨 갱신
        UIManager.Instance.UpdateExp();
    }

    public void GetArtifact(ItemInfo getItem)
    {
        //todo 해당 아티팩트가 이미 있으면 개수 올리기
        ItemInfo alreadyHas = hasItems.Find(x => x.id == getItem.id);
        if (alreadyHas != null)
        {
            alreadyHas.amount++;
        }
        //todo 해당 아티팩트가 없으면 추가
        else
        {
            hasItems.Add(getItem);
        }

        //todo 아티팩트 그리드 UI 갱신
    }

    void Levelup()
    {
        //레벨업
        PlayerStat_Now.Level++;

        //경험치 초기화
        ExpNow = 0;

        //경험치 최대치 갱신
        ExpMax = PlayerStat_Now.Level * PlayerStat_Now.Level + 5;
        //! 테스트용 맥스 경험치
        // ExpMax = 3;

        // 죽었으면 리턴
        if (PlayerStat_Now.hpNow <= 0)
            return;

        // 레벨업 메뉴 띄우기
        UIManager.Instance.PopupUI(UIManager.Instance.levelupPanel);
    }

    public int GetGem(int gemIndex)
    {
        // 해당 젬 개수를 리턴
        return hasItems[gemIndex].amount;
    }

    public void PayGem(int gemIndex, int price)
    {
        //원소젬 지불하기
        hasItems[gemIndex].amount -= price;

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

    public void StepSound()
    {
        string soundName = "";

        // 발자국 소리 인덱스 풀 만들기
        List<int> stepSounds = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            stepSounds.Add(i);
        }

        // 마지막 발자국 소리 인덱스 삭제
        if (lastStepSound != -1)
            stepSounds.Remove(lastStepSound);

        // 새로운 발자국 소리 뽑기
        lastStepSound = stepSounds[Random.Range(0, stepSounds.Count)];

        switch (lastStepSound)
        {
            case 0:
                soundName = "Step1";
                break;
            case 1:
                soundName = "Step2";
                break;
            case 2:
                soundName = "Step3";
                break;
        }

        SoundManager.Instance.PlaySound(soundName);
    }

    public void PlaySound(string name)
    {
        SoundManager.Instance.PlaySound(name);
    }
}
