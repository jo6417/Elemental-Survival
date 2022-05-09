using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;
using System.Linq;
using TMPro;
using UnityEngine.Experimental.Rendering.Universal;

public class PlayerStat
{
    public int playerPower; //플레이어 전투력
    public float hpMax = 100; // 최대 체력
    public float hpNow = 100; // 체력
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

public class PlayerManager : MonoBehaviour
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

    bool isDash; //현재 대쉬중 여부

    [Header("<Refer>")]
    public GameObject mobSpawner;
    private Animator animator;
    public SpriteRenderer sprite;
    public Rigidbody2D rigid;
    public Vector3 lastDir; //마지막 바라봤던 방향
    public GameObject txtPrefab; //체력 회복 텍스트 UI
    public Light2D playerLight;

    [Header("<Stat>")] //플레이어 스탯
    public PlayerStat PlayerStat_Origin; //초기 스탯
    public PlayerStat PlayerStat_Now; //현재 스탯

    [Header("<Damaged>")]
    float hitCount = 0f;
    public float HitDelay = 0.5f; //피격 무적시간
    //TODO 피격시 카메라 흔들기
    // public float ShakeTime;
    // public float ShakeIntensity;

    [Header("<Pocket>")]
    public List<int> hasGems = new List<int>(6); //플레이어가 가진 원소젬
    public List<MagicInfo> hasMagics = new List<MagicInfo>(); //플레이어가 가진 마법
    public MagicInfo ultimateMagic; //궁극기 마법
    public float ultimateCoolCount; //궁극기 마법 쿨타임 카운트
    public List<ItemInfo> hasItems = new List<ItemInfo>(); //플레이어가 가진 아이템

    private void Awake()
    {
        //플레이어 스탯 인스턴스 생성
        PlayerStat_Now = new PlayerStat();

        //플레이어 초기 스탯 저장
        PlayerStat_Origin = PlayerStat_Now;
    }

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();

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

        //기본 마법 추가
        StartCoroutine(CastBasicMagics());
    }

    private void Update()
    {
        if(Time.timeScale == 0f)
        return;

        //카메라 따라오기
        foreach (var cam in SystemManager.Instance.camList)
        {
            cam.transform.position = transform.position + new Vector3(0, 0, -10);
        }

        //몬스터 스포너 따라오기
        if (mobSpawner.activeSelf)
            mobSpawner.transform.position = transform.position;

        if (ultimateCoolCount > 0)
        {
            //궁극기 쿨타임 카운트 감소
            ultimateCoolCount -= Time.deltaTime;
            //쿨타임 UI 업데이트
            UIManager.Instance.UltimateCooltime();
        }

        // 쿨타임 가능하고 스페이스바 눌렀을때
        if (Input.GetKeyDown(KeyCode.Space) && PlayerManager.Instance.ultimateCoolCount <= 0)
        {
            StartCoroutine(CastMagic.Instance.UseUltimateMagic());
        }

        //히트 카운트 감소
        if (hitCount > 0)
            hitCount -= Time.deltaTime;

        //이동
        Move();
    }

    void Move()
    {
        //대쉬 중에는 이동 조작불가
        if (isDash)
            return;

        //이동 입력값 받기
        Vector2 dir = Vector2.zero;
        float horizonInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        float dashBoost = 1;

        // x축 이동
        if (horizonInput != 0)
        {
            dir.x = horizonInput;

            //방향 따라 캐릭터 회전
            if (horizonInput > 0)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }

        // y축 이동
        if (verticalInput != 0)
        {
            dir.y = verticalInput;
        }

        // 방향키 입력에 따라 애니메이터 걷기 변수 입력
        if (horizonInput == 0 && verticalInput == 0)
        {
            animator.SetBool("isWalk", false);
        }
        else
        {
            //대쉬 입력에 따라 애니메이터 대쉬 변수 입력
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                DashToggle();
                dashBoost = 2f;
            }
            else
            {
                animator.SetBool("isWalk", true);
            }
        }

        dir.Normalize();
        rigid.velocity = PlayerStat_Now.moveSpeed * dir * dashBoost;

        // print(rigid.velocity + "=" + PlayerStat_Now.moveSpeed + "*" + dir + "*" + VarManager.Instance.playerTimeScale);

        //마지막 방향 기억
        if (dir != Vector2.zero)
            lastDir = dir;
    }

    public void DashToggle()
    {
        isDash = !isDash;
        animator.SetBool("isDash", isDash);
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        //적에게 충돌
        if (other.gameObject.CompareTag("Enemy") && hitCount <= 0 && !isDash)
        {
            // print("적 충돌");

            EnemyInfo enemy = other.gameObject.GetComponent<EnemyManager>().enemy;

            //피격 딜레이 무적
            IEnumerator hitDelay = HitDelayCoroutine();
            StartCoroutine(hitDelay);

            Damage(enemy.power);
        }
    }

    //HitDelay만큼 시간 지난후 피격무적시간 끝내기
    IEnumerator HitDelayCoroutine()
    {
        hitCount = HitDelay;

        //머터리얼 변환
        sprite.material = SystemManager.Instance.hitMat;

        //스프라이트 색 변환
        // sprite.color = VarManager.Instance.hitColor;

        yield return new WaitUntil(() => hitCount <= 0);

        //머터리얼 복구
        sprite.material = SystemManager.Instance.spriteMat;

        //원래 색으로 복구
        sprite.color = Color.white;
    }

    void Damage(float damage)
    {
        // 체력 감소
        PlayerStat_Now.hpNow -= damage;

        //체력 0 이하가 되면 사망
        if (PlayerStat_Now.hpNow <= 0)
        {
            PlayerStat_Now.hpNow = 0;
            // print("Game Over");
            // Dead();
        }

        UIManager.Instance.UpdateHp(); //체력 UI 업데이트        
    }

    void Dead()
    {
        // 시간 멈추기
        Time.timeScale = 0;

        //TODO 게임오버 UI 띄우기
        // gameOverUI.SetActive(true);
    }

    public void GetItem(ItemInfo getItem)
    {
        // print(getItem.itemType + " : " + getItem.itemName);

        // 아이템이 스크롤일때
        if (getItem.itemType == "Scroll")
        {
            // 아이템 합성 메뉴 띄우기
            // UIManager.Instance.PopupUI(UIManager.Instance.magicMixUI);

            //보유하지 않은 아이템일때
            if (!hasItems.Exists(x => x.id == getItem.id))
            {
                // 플레이어 보유 아이템에 해당 아이템 추가하기
                hasItems.Add(getItem);
            }

            //보유한 아이템의 개수만 늘려주기
            hasItems.Find(x => x.id == getItem.id).amount++;

            //TODO 스크롤 획득 메시지 UI 띄우기
            print("마법 합성이 " + getItem.amount + "회 가능합니다.");
        }

        if (getItem.itemType == "Artifact")
        {
            // print("아티팩트 획득");

            //보유하지 않은 아이템일때
            if (!hasItems.Exists(x => x.id == getItem.id))
            {
                // 플레이어 보유 아이템에 해당 아이템 추가하기
                hasItems.Add(getItem);
            }

            //보유한 아이템의 개수만 늘려주기
            hasItems.Find(x => x.id == getItem.id).amount++;
            // getItem.hasNum++;

            // 보유한 모든 아이템 아이콘 갱신
            // UIManager.Instance.UpdateItems();

            // 모든 아이템 버프 갱신
            buffUpdate();
        }
    }

    public IEnumerator GetHeal(int amount)
    {
        //플레이어 체력 회복하기
        PlayerStat_Now.hpNow += amount;

        //초과 회복 방지
        if (PlayerStat_Now.hpNow > PlayerStat_Now.hpMax)
            PlayerStat_Now.hpNow = PlayerStat_Now.hpMax;

        //UI 업데이트
        UIManager.Instance.UpdateHp();

        Vector2 startPos = transform.position;
        Vector2 endPos = (Vector2)transform.position + Vector2.up * 1f;

        // 회복 텍스트 띄우기
        GameObject healUI = LeanPool.Spawn(txtPrefab, startPos, Quaternion.identity, UIManager.Instance.overlayCanvas);
        TextMeshProUGUI healTxt = healUI.GetComponent<TextMeshProUGUI>();
        healTxt.color = Color.green;
        healTxt.text = "+ " + amount.ToString();

        // 회복량 UI 애니메이션
        Sequence txtSeq = DOTween.Sequence();
        txtSeq
        .PrependCallback(() =>
        {
            //제로 사이즈로 시작
            healUI.transform.localScale = Vector3.zero;
        })
        .Append(
            //위로 살짝 올리기
            healUI.transform.DOMove(endPos, 1f)
        )
        .Join(
            //원래 크기로 늘리기
            healUI.transform.DOScale(Vector3.one, 0.5f)
        )
        .Append(
            //줄어들어 사라지기
            healUI.transform.DOScale(Vector3.zero, 0.5f)
            .SetEase(Ease.InBack)
        )
        .OnComplete(() =>
        {
            LeanPool.Despawn(healUI);
        });

        yield return null;
    }

    public void GetMagic(MagicInfo getMagic, bool resetAllMagic = true)
    {
        //보유하지 않은 마법일때
        if (!hasMagics.Exists(x => x.id == getMagic.id))
        {
            // 플레이어 보유 마법에 해당 마법 추가하기
            hasMagics.Add(getMagic);
        }

        // 0등급 마법이면 원소젬이므로 스킵
        if (getMagic.grade == 0)
            return;

        //보유한 마법의 레벨 올리기
        hasMagics.Find(x => x.id == getMagic.id).magicLevel++;

        //TODO 적이 죽으면 발동되는 마법일때 콜백에 함수포함시키기
        if (getMagic.magicName == "Life Seed")
        {
            SystemManager.Instance.AddDropSeedEvent(getMagic);
        }

        // 보유한 모든 마법 아이콘 갱신
        UIManager.Instance.UpdateMagics();

        // 마법 캐스팅 다시 시작
        if (resetAllMagic)
            CastMagic.Instance.ReCastMagics();

        //플레이어 총 전투력 업데이트
        PlayerStat_Now.playerPower = GetPlayerPower();
    }

    public void GetUltimateMagic(MagicInfo magic)
    {
        //해당 마법을 장착
        ultimateMagic = magic;
        print("ultimate : " + ultimateMagic.magicName);

        //해당 마법 쿨타임 카운트 초기화
        ultimateCoolCount = MagicDB.Instance.MagicCoolTime(ultimateMagic);
        UIManager.Instance.UltimateCooltime();

        // 궁극기 UI 업데이트
        UIManager.Instance.UpdateUltimateIcon();
    }

    IEnumerator CastBasicMagics()
    {
        // MagicDB 로드 완료까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //TODO 캐릭터에 따라 basicMagic에 기본마법 넣고 시작
        List<int> magics = new List<int>();

        if (CastMagic.Instance.testAllMagic)
        {
            foreach (var value in MagicDB.Instance.magicDB.Values)
            {
                magics.Add(value.id);
            }
        }
        else
        {
            magics = CastMagic.Instance.basicMagic;
        }

        // 캐릭터 기본 마법 추가
        foreach (var magicID in magics)
        {
            // //보유하지 않은 마법일때
            // if (!hasMagics.Exists(x => x.id == magicID))
            // {
            //     // 플레이어 보유 마법에 해당 마법 추가하기
            //     hasMagics.Add(MagicDB.Instance.GetMagicByID(magicID));
            // }

            // //보유한 마법의 레벨 올리기
            // hasMagics.Find(x => x.id == magicID).magicLevel++;

            // 마법 찾기
            MagicInfo magic = MagicDB.Instance.GetMagicByID(magicID);

            //마법 획득
            GetMagic(magic, false);
        }

        //플레이어 마법 시작
        CastMagic.Instance.CastAllMagics();

        // 보유한 모든 마법 아이콘 갱신
        // UIManager.Instance.UpdateMagics();

        // 보유한 궁극기 마법 아이콘 갱신
        UIManager.Instance.UpdateUltimateIcon();
        UIManager.Instance.UltimateCooltime();

        //플레이어 총 전투력 업데이트
        PlayerStat_Now.playerPower = GetPlayerPower();
    }

    void buffUpdate()
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
        int gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.elementNames, x => x == item.priceType);

        // 젬 타입 인덱스로 해당 젬과 같은 마법 찾아서 획득
        GetMagic(MagicDB.Instance.GetMagicByID(gemTypeIndex));

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
        Time.timeScale = 0;

        //레벨업
        PlayerStat_Now.Level++;

        //경험치 초기화
        PlayerStat_Now.ExpNow = 0;

        //경험치 최대치 갱신
        PlayerStat_Now.ExpMax = PlayerStat_Now.Level * PlayerStat_Now.Level + 5;
        //! 테스트용 맥스 경험치
        PlayerStat_Now.ExpMax = 3;

        // 팝업 선택메뉴 띄우기
        UIManager.Instance.PopupUI(UIManager.Instance.magicMixPanel);
        // levelupPopup.SetActive(true);
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

        foreach (var magic in hasMagics)
        {
            //총전투력에 해당 마법의 등급*레벨 더하기
            magicPower += magic.grade * magic.magicLevel;

            // print(magicPower + " : " + magic.grade + " * " + magic.magicLevel);
        }

        return magicPower;
    }
}
