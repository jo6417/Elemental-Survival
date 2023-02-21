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

public class PlayerManager : Character
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

    [Header("Input")]
    public NewInput player_Input;
    Vector2 inputMoveDir; // 현재 이동 입력 벡터
    Vector2 nowMoveDir; // 현재 이동 벡터
    public Vector2 lastDir; // 마지막 이동 벡터
    public bool isDash; //현재 대쉬중 여부
    public float defaultDashSpeed = 1.5f; // 대쉬 속도 기본값
    public float dashSpeed = 1; //대쉬 버프 속도
    public PlayerInteracter playerInteracter; //플레이어 상호작용 컴포넌트

    [Header("Refer")]
    // public Transform activeParent; // 액티브 슬롯들 부모 오브젝트
    // public GameObject aimCursor; //! 테스트용 마커
    public GameObject bloodPrefab; //플레이어 혈흔 파티클
    public PlayerHitBox hitBox;
    public GameObject mobSpawner;
    public SpriteRenderer playerSprite; // 몸체 스프라이트
    public SpriteRenderer playerCover; // 플레이어와 같은 이미지로 덮기
    public SpriteRenderer shadowSprite; // 그림자 스프라이트
    public Transform magicParent; // 해당 오브젝트 밑에 마법 붙이기
    public Light2D playerLight;
    // public Rigidbody2D rigid;
    public Collider2D coll;
    public Animator anim;
    public Transform knockbackColl; // 레벨업 시 넉백 콜라이더
    public GameObject lvUpEffectPrefab; // 레벨업 이펙트

    [Header("Stat")] //플레이어 스탯
    // public CharacterStat characterStat; // 현재 스탯
    // private CharacterStat PlayerStat_Default; // 초기 스탯
    public float ExpMax = 5; // 경험치 최대치
    public float ExpNow = 0; // 현재 경험치

    [Header("State")]
    public bool initFinish = false;
    // public float hpNow;
    // public float hpMax;
    // public enum Debuff { Burn, Poison, Bleed, Slow, Shock, Stun, Stop, Flat, Freeze };
    // public IEnumerator[] DebuffList = new IEnumerator[System.Enum.GetValues(typeof(Debuff)).Length];
    IEnumerator expGainCoroutine; // 경험치 획득 코루틴
    // public int remainExp; // 획득 대기중인 경험치
    public List<ItemInfo> remainExpList = new List<ItemInfo>(); // 획득 대기중인 경험치

    [Header("Pocket")]
    [SerializeField] int[] testGems = new int[6]; // 테스트용 초기 원소젬 개수
    [SerializeField] public ItemInfo[] hasGem = new ItemInfo[6]; //플레이어가 가진 원소젬
    public List<ItemInfo> hasArtifact = new List<ItemInfo>(); //플레이어가 가진 아티팩트

    [Header("Sound")]
    int lastStepSound = -1;

    protected override void Awake()
    {
        // Character 의 Awake 코드 실행
        // base.Awake();

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

        // // 플레이어 초기 스탯 저장
        // PlayerStat_Default = new CharacterStat(PlayerStat_Now);

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
            // 대쉬중 아닐때, 현재 이동 중일때
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

        // 초기화 완료
        initFinish = true;
    }

    public Vector3 GetMousePos()
    {
        return Camera.main.ScreenToWorldPoint(SystemManager.Instance.System_Input.Player.MousePosition.ReadValue<Vector2>());
    }

    public Quaternion GetMouseDir()
    {
        Vector2 dir = GetMousePos() - transform.position;
        float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion mouseDir = Quaternion.Euler(Vector3.forward * rotation);

        return mouseDir;
    }

    protected override void OnEnable()
    {
        // Character 의 OnEnable 코드 실행
        // base.OnEnable();

        // 초기화
        StartCoroutine(Init());

        //기본 마법 추가
        StartCoroutine(CastDefaultMagics());
    }

    IEnumerator Init()
    {
        // 플레이어 입력 끄기
        player_Input.Disable();

        // 플레이어 커버 하얗게
        playerCover.color = new Color(1, 1, 1, 1);

        // 플레이어 커버 숨기기
        playerCover.enabled = false;
        // 플레이어 스프라이트 숨기기
        playerSprite.enabled = false;
        // 그림자 스프라이트 숨기기
        shadowSprite.enabled = false;

        yield return new WaitUntil(() => ItemDB.Instance.loadDone);

        // 6종류 gem을 리스트에 넣기
        for (int i = 0; i < testGems.Length; i++)
        {
            ItemInfo gem = new ItemInfo(ItemDB.Instance.GetItemByID(i));
            //! 테스트용 원소젬 개수 넣기
            gem.amount = testGems[i];

            hasGem[i] = gem;
        }

        // 플레이어 버프 업데이트
        // BuffUpdate();

        yield return new WaitUntil(() => UIManager.Instance != null);

        // 원소젬 전체 UI 업데이트
        UIManager.Instance.UpdateGem();

        //능력치 초기화
        UIManager.Instance.InitialStat();

        // 1레벨 경험치 최대치 갱신
        ExpMax = characterStat.Level * characterStat.Level + 5;

        // 소환 위치에 포탈 소환
        GameObject spawner = LeanPool.Spawn(WorldSpawner.Instance.spawnerPrefab, transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);
        Transform beam = spawner.transform.Find("Beam");
        Transform portal = spawner.transform.Find("Portal");
        ParticleSystem beamParticle = spawner.transform.Find("BeamParticle").GetComponent<ParticleSystem>();

        // 포탈 및 빔 사이즈 초기화
        portal.localScale = Vector2.zero;
        beam.localScale = new Vector2(0, 1);

        // 포탈 확장
        portal.DOScale(Vector2.one, 0.5f)
        .SetEase(Ease.OutBack);

        // 동시에 빔 확장
        beam.DOScale(new Vector2(0.5f, 1f), 0.5f)
        .SetEase(Ease.InQuart);

        yield return new WaitForSeconds(0.5f);

        // 플레이어 나타내기
        playerCover.enabled = true;
        playerSprite.enabled = true;
        shadowSprite.enabled = true;

        // 플레이어 입력 켜기
        player_Input.Enable();

        // 빔 파티클 켜기
        beamParticle.gameObject.SetActive(true);

        // 빔 축소
        beam.DOScale(new Vector2(0, 1), 0.3f)
        .SetEase(Ease.OutQuart);
        // 포탈 축소
        portal.DOScale(Vector2.zero, 0.3f)
        .SetEase(Ease.InBack);

        yield return new WaitForSeconds(0.3f);

        // 플레이어 커버 제거
        playerCover.DOColor(new Color(1, 1, 1, 0), 0.5f)
        .SetEase(Ease.OutQuad);

        // 빔 파티클 꺼질때까지 대기
        yield return new WaitUntil(() => !beamParticle.gameObject.activeSelf);
        // 빔 디스폰
        LeanPool.Despawn(spawner);
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
        if (hitDelayCount > 0)
            hitDelayCount -= Time.deltaTime;

        // 플레이어 이동
        Move();

        //! 테스트용 마커 이동
        // aimCursor.transform.position = GetMousePos();
    }

    public void Move()
    {
        // 시간 멈췄으면 리턴
        if (Time.timeScale == 0)
            return;

        // 깔렸을때 조작불가
        if (flatCount > 0)
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

        // 버프 적용된 이동속도 스탯 계산
        float speedStat = GetBuffedStat(nameof(characterStat.moveSpeed));

        Vector2 moveVector =
        characterStat.moveSpeed * 10f //플레이어 이동속도
        * nowMoveDir //움직일 방향
        * dashSpeed //대쉬할때 속도 증가
        * speedStat // 버프 적용된 이동속도 스탯
        * SystemManager.Instance.playerTimeScale //플레이어 개인 타임스케일
        + hitBox.knockbackDir; //넉백 벡터 추가

        // 실제 오브젝트 이동해주기
        rigid.velocity = moveVector;

        // 움직이고 있을때
        if (moveVector.magnitude != 0)
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
        // 시간 멈췄으면 리턴
        if (Time.timeScale == 0)
            return;

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
        characterStat.powerSum = GetPlayerPower();
    }

    // void BuffUpdate()
    // {
    //     //초기 스탯을 임시 스탯으로 복사
    //     CharacterStat PlayerStat_Temp = new CharacterStat(PlayerStat_Default);

    //     //임시 스탯에 현재 아이템의 모든 버프 넣기
    //     foreach (ItemInfo item in hasGem)
    //     {
    //         PlayerStat_Temp.atkNum += item.atkNum * item.amount; // 투사체 개수 버프
    //         PlayerStat_Temp.hpMax += item.hpMax * item.amount; // 최대체력 버프
    //         PlayerStat_Temp.power += item.power * item.amount; // 마법 공격력 버프
    //         PlayerStat_Temp.armor += item.armor * item.amount; // 방어력 버프
    //         PlayerStat_Temp.speed += item.speed * item.amount; // 마법 속도 버프
    //         PlayerStat_Temp.evade += item.evade * item.amount; // 회피율 버프
    //         PlayerStat_Temp.coolTime += item.coolTime * item.amount; // 마법 쿨타임 버프
    //         PlayerStat_Temp.duration += item.duration * item.amount; // 마법 지속시간 버프
    //         PlayerStat_Temp.range += item.range * item.amount; // 마법 범위 버프
    //         PlayerStat_Temp.luck += item.luck * item.amount; // 행운 버프
    //         PlayerStat_Temp.expGain += item.expRate * item.amount; // 경험치 획득량 버프
    //         PlayerStat_Temp.getRage += item.getRage * item.amount; // 아이템 획득거리 버프
    //         PlayerStat_Temp.moveSpeed += item.moveSpeed * item.amount; //이동속도 버프

    //         // PlayerStat_Temp.earth_atk += item.earth * item.amount;
    //         // PlayerStat_Temp.fire_atk += item.fire * item.amount;
    //         // PlayerStat_Temp.life_atk += item.life * item.amount;
    //         // PlayerStat_Temp.lightning_atk += item.lightning * item.amount;
    //         // PlayerStat_Temp.water_atk += item.water * item.amount;
    //         // PlayerStat_Temp.wind_atk += item.wind * item.amount;
    //     }

    //     //현재 스탯에 임시 스탯을 넣기
    //     PlayerStat_Now = PlayerStat_Temp;

    //     // string allBuff = " atkNum : " + PlayerStat_Temp.atkNum + ", " +
    //     //     "\n hpMax : " + PlayerStat_Temp.hpMax + ", " +
    //     //     "\n power : " + PlayerStat_Temp.power + ", " +
    //     //     "\n armor : " + PlayerStat_Temp.armor + ", " +
    //     //     "\n speed : " + PlayerStat_Temp.speed + ", " +
    //     //     "\n evade : " + PlayerStat_Temp.evade + ", " +
    //     //     "\n coolTime : " + PlayerStat_Temp.coolTime + ", " +
    //     //     "\n duration : " + PlayerStat_Temp.duration + ", " +
    //     //     "\n range : " + PlayerStat_Temp.range + ", " +
    //     //     "\n luck : " + PlayerStat_Temp.luck + ", " +
    //     //     "\n expGain : " + PlayerStat_Temp.expGain + ", " +
    //     //     "\n moneyGain : " + PlayerStat_Temp.getRage + ", " +
    //     //     "\n moveSpeed : " + PlayerStat_Temp.moveSpeed;
    //     // print(allBuff);
    // }

    public void AddGem(ItemInfo item, int amount)
    {
        // 해당 원소젬 정보 넣기
        remainExpList.Add(item);

        // 경험치 획득 코루틴이 null 이면
        if (expGainCoroutine == null)
        {
            // 경험치 획득 코루틴 진행
            expGainCoroutine = GetExp();
            StartCoroutine(expGainCoroutine);
        }

        // 플레이어 버프 업데이트
        // BuffUpdate();
    }

    IEnumerator GetExp()
    {
        // 획득 대기 원소젬이 리스트에 남아있으면 반복
        while (remainExpList.Count > 0)
        {
            // 첫번째 원소젬 정보 캐싱
            ItemInfo expGem = remainExpList[0];

            // 개수가 없을때
            if (expGem.amount == 0)
                // 해당 원소젬 리스트에서 제거
                remainExpList.RemoveAt(0);
            // 첫번째 원소젬의 개수가 남아있을때
            else
            {
                // 경험치 1씩 증가
                ExpNow += 1;

                //경험치 다 찼을때 레벨업
                if (ExpNow == ExpMax)
                    yield return StartCoroutine(Levelup());

                // 잔여량 차감
                expGem.amount--;

                // 가격 타입으로 젬 타입 인덱스로 반환
                int gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.ElementNames, x => x == expGem.priceType);
                // 보유 아이템 중 해당 젬 개수 올리기
                hasGem[gemTypeIndex].amount += 1;

                // 해당 젬 UI 인디케이터 밝히기
                UIManager.Instance.GemIndicator(gemTypeIndex, Color.green);
                // 해당 젬 UI 업데이트
                UIManager.Instance.UpdateGem(gemTypeIndex);
                // 경험치, 레벨 UI 갱신
                UIManager.Instance.UpdateExp();
            }

            // yield return null;
        }

        // 모든 경험치 획득 했으면 코루틴 삭제
        expGainCoroutine = null;
    }

    public void GetArtifact(ItemInfo getItem)
    {
        //todo 해당 아티팩트가 이미 있으면 개수 올리기
        ItemInfo alreadyHas = hasArtifact.Find(x => x.id == getItem.id);
        if (alreadyHas != null)
        {
            alreadyHas.amount++;
        }
        //todo 해당 아티팩트가 없으면 추가
        else
        {
            hasArtifact.Add(getItem);
        }

        //todo 아티팩트 그리드 UI 갱신
    }

    IEnumerator Levelup()
    {
        // 시간 멈추기
        SystemManager.Instance.TimeScaleChange(0f);

        //레벨업
        characterStat.Level++;

        //경험치 초기화
        ExpNow = 0;

        //경험치 최대치 갱신
        ExpMax = characterStat.Level * characterStat.Level + 5;
        //! 테스트용 맥스 경험치
        // ExpMax = 3;

        // 죽었으면 리턴
        if (characterStat.hpNow <= 0)
            yield break;

        // 레벨업 이펙트 생성
        LeanPool.Spawn(lvUpEffectPrefab, transform.position, Quaternion.identity, transform);

        // 레벨업 효과음 재생
        SoundManager.Instance.PlaySound("Player_Levelup", 0, 0, 1, false);

        // 제로 사이즈로 시작
        knockbackColl.localScale = Vector2.zero;
        // 넉백 범위 확장
        knockbackColl.DOScale(Vector2.one, 0.5f)
        .SetUpdate(true)
        .SetEase(Ease.OutBack)
        .OnComplete(() =>
        {
            // 다시 제로 사이즈로 초기화
            knockbackColl.localScale = Vector2.zero;
        });

        // 이펙트 딜레이 대기
        yield return new WaitForSecondsRealtime(0.5f);

        // 레벨업 메뉴 띄우기
        UIManager.Instance.PopupUI(UIManager.Instance.levelupPanel);

        // 시간 흐를때까지 대기
        yield return new WaitUntil(() => Time.timeScale > 0);
    }

    public int GetGem(int gemIndex)
    {
        // 해당 젬 개수를 리턴
        return hasGem[gemIndex].amount;
    }

    public void PayGem(int gemIndex, int price)
    {
        //원소젬 지불하기
        hasGem[gemIndex].amount -= price;

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
