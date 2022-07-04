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
    public float hpNow = 5; // 체력
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

    public bool godMod = true; //! 플레이어 갓모드
    public NewInput playerInput;
    Sequence damageTextSeq; //데미지 텍스트 시퀀스
    public float camFollowSpeed = 10f;

    [Header("<Input>")]
    Vector2 nowMoveDir;
    public bool isDash; //현재 대쉬중 여부
    public bool isFlat; //깔려서 납작해졌을때
    public float defaultDashSpeed = 1.5f; // 대쉬 속도 기본값
    [HideInInspector]
    public float dashSpeed; //대쉬 버프 속도
    [HideInInspector]
    public float speedDebuff = 1f; //이동속도 디버프
    public Vector3 lastDir; //마지막 바라봤던 방향

    [Header("<Refer>")]
    public GameObject mobSpawner;
    public Animator anim;
    public SpriteRenderer sprite;
    public SpriteRenderer shadow;
    public Rigidbody2D rigid;
    public Light2D playerLight;
    public GameObject bloodPrefab; //플레이어 혈흔 파티클

    [Header("<Stat>")] //플레이어 스탯
    public PlayerStat PlayerStat_Origin; //초기 스탯
    public PlayerStat PlayerStat_Now; //현재 스탯

    [Header("<State>")]
    public float poisonCoolCount; //독 도트뎀 남은시간
    float hitDelayTime = 0.2f; //피격 무적시간
    public float hitCoolCount = 0f;
    public float ultimateCoolTime; //궁극기 마법 쿨타임 값 저장
    public float ultimateCoolCount; //궁극기 마법 쿨타임 카운트
    public Vector2 knockbackDir; //넉백 벡터

    //TODO 피격시 카메라 흔들기
    // public float ShakeTime;
    // public float ShakeIntensity;

    [Header("<Pocket>")]
    public MagicInfo[] hasMergeMagics = new MagicInfo[20]; // merge 보드에 올려진 플레이어 보유 마법
    public List<MagicInfo> hasStackMagics = new List<MagicInfo>(); // 스택에 있는 플레이어 보유 마법
    public List<MagicInfo> ultimateList = new List<MagicInfo>(); //궁극기 마법 리스트
    // public MagicInfo ultimateMagic; //궁극기 마법
    public List<int> hasGems = new List<int>(6); //플레이어가 가진 원소젬
    public List<ItemInfo> hasItems = new List<ItemInfo>(); //플레이어가 가진 아이템

    private void Awake()
    {
        rigid = rigid == null ? GetComponent<Rigidbody2D>() : rigid;
        anim = anim == null ? GetComponent<Animator>() : anim;
        sprite = sprite == null ? GetComponent<SpriteRenderer>() : sprite;

        //플레이어 스탯 인스턴스 생성
        PlayerStat_Now = new PlayerStat();

        //플레이어 초기 스탯 저장
        PlayerStat_Origin = PlayerStat_Now;

        // 입력값 초기화
        InputInitial();
    }

    void InputInitial()
    {
        playerInput = new NewInput();

        // 방향키 버튼 매핑
        playerInput.Player.Move.performed += val =>
        {
            //현재 이동방향 입력
            nowMoveDir = val.ReadValue<Vector2>();

            //대쉬 중 아닐때만
            if (!isDash)
                Move();
        };

        // 방향키 안누를때
        playerInput.Player.Move.canceled += val =>
        {
            //현재 이동방향 입력
            nowMoveDir = val.ReadValue<Vector2>();

            //대쉬 중 아닐때만
            if (!isDash)
                Move();
        };

        // 대쉬 버튼 매핑
        playerInput.Player.Dash.performed += val =>
        {
            if (!isDash && nowMoveDir != Vector2.zero)
                DashToggle();
        };

        // 궁극기 버튼 매핑
        playerInput.Player.Ultimate.performed += val =>
        {
            StartCoroutine(CastMagic.Instance.UseUltimateMagic());
        };
    }

    private void OnEnable()
    {
        playerInput.Enable();
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

        //기본 마법 추가
        StartCoroutine(CastDefaultMagics());
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

        if (ultimateCoolCount > 0)
        {
            //궁극기 쿨타임 카운트 감소
            ultimateCoolCount -= Time.deltaTime;
            //쿨타임 UI 업데이트
            UIManager.Instance.UltimateCooltime();
        }

        //히트 카운트 감소
        if (hitCoolCount > 0)
            hitCoolCount -= Time.deltaTime;
    }

    public void Move()
    {
        // print(nowMoveDir);

        // 깔렸을때 조작불가
        if (isFlat)
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
            anim.speed = 1;
        }

        //애니메이터 스피드 초기화
        anim.speed = 1;

        //이동 입력값 받기
        float horizonInput = nowMoveDir.x;
        float verticalInput = nowMoveDir.y;
        dashSpeed = 1f;

        // x축 이동
        if (horizonInput != 0)
        {
            nowMoveDir.x = horizonInput;

            //방향 따라 캐릭터 회전
            if (horizonInput > 0)
            {
                sprite.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                sprite.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }

        // y축 이동
        if (verticalInput != 0)
        {
            nowMoveDir.y = verticalInput;
        }

        // 방향키 입력에 따라 애니메이터 걷기 변수 입력
        if (horizonInput == 0 && verticalInput == 0)
        {
            anim.SetBool("isWalk", false);
        }
        else
        {
            anim.SetBool("isWalk", true);
        }

        //대쉬 입력에 따라 애니메이터 대쉬 변수 입력
        if (isDash)
        {
            dashSpeed = defaultDashSpeed;
        }

        // 실제 오브젝트 이동해주기
        // nowMoveDir.Normalize();
        rigid.velocity =
        PlayerStat_Now.moveSpeed //플레이어 이동속도
        * nowMoveDir //움직일 방향
        * dashSpeed //대쉬할때 속도 증가
        * speedDebuff //상태이상 걸렸을때 속도 디버프
        * SystemManager.Instance.playerTimeScale //플레이어 개인 타임스케일
        + knockbackDir //넉백 벡터 추가
        ;

        // print(rigid.velocity + "=" + PlayerStat_Now.moveSpeed + "*" + dir + "*" + VarManager.Instance.playerTimeScale);

        //마지막 방향 기억
        if (nowMoveDir != Vector2.zero)
            lastDir = nowMoveDir;
    }

    public void DashToggle()
    {
        isDash = !isDash;
        anim.SetBool("isDash", isDash);

        //대쉬 끝날때 이동 입력확인
        Move();

        // print("Dash : " + isDash);
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        //무언가 충돌되면 움직이는 방향 수정
        Move();

        //적에게 콜라이더 충돌
        if (other.gameObject.CompareTag("EnemyAttack") && hitCoolCount <= 0 && !isDash)
        {
            Hit(other.transform);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // print("OnTriggerEnter2D : " + other.name);

        // 적에게 트리거 충돌
        if (other.gameObject.CompareTag("EnemyAttack") && hitCoolCount <= 0 && !isDash)
        {
            Hit(other.transform);
        }
    }

    // private void OnTriggerStay2D(Collider2D other)
    // {
    //     // print("OnTriggerStay2D : " + other.name);

    //     // 적에게 트리거 충돌
    //     if (other.gameObject.CompareTag("EnemyAttack") && hitCoolCount <= 0 && !isDash)
    //     {
    //         Hit(other.transform);
    //     }
    // }

    // private void OnTriggerExit2D(Collider2D other)
    // {
    //     print("OnTriggerExit2D : " + other.name);

    // }

    void Hit(Transform other)
    {
        // 몬스터 정보 찾기, EnemyAtk 컴포넌트 활성화 되어있을때
        if (other.TryGetComponent(out EnemyAttack enemyAtk) && enemyAtk.enabled)
        {
            EnemyManager enemyManager = enemyAtk.enemyManager;

            //적에게 충돌
            if (enemyManager != null)
            {
                EnemyInfo enemy = enemyManager.enemy;

                // hitCount 갱신되었으면 리턴, 중복 피격 방지
                if (hitCoolCount > 0)
                    return;

                //피격 딜레이 무적시간 시작
                StartCoroutine(HitDelay());

                Damage(enemy.power);

                // 넉백 디버프 있을때
                if (enemyAtk.knockBackDebuff)
                {
                    // print("player knockback");

                    //넉백 방향 벡터 계산
                    Vector2 dir = (transform.position - other.position).normalized * enemy.power;

                    //넉백 디버프 실행
                    StartCoroutine(Knockback(dir));
                }

                // flat 디버프 있을때, 마비 상태 아닐때
                if (enemyAtk.flatDebuff && !isFlat)
                {
                    // print("player flat");

                    // 납작해지고 행동불능
                    StartCoroutine(FlatDebuff());
                }
            }
        }

        //마법 정보 찾기
        if (other.TryGetComponent(out MagicHolder magicHolder))
        {
            //적의 마법에 충돌
            if (magicHolder != null && magicHolder.enabled)
            {
                MagicInfo magic = magicHolder.magic;

                //피격 딜레이 무적
                StartCoroutine(HitDelay());

                //데미지 계산
                float damage = MagicDB.Instance.MagicPower(magic);
                // 고정 데미지에 확률 계산
                damage = Random.Range(damage * 0.8f, damage * 1.2f);

                Damage(damage);
            }
        }
    }

    //HitDelay만큼 시간 지난후 피격무적시간 끝내기
    public IEnumerator HitDelay()
    {
        hitCoolCount = hitDelayTime;

        //머터리얼 변환
        sprite.material = SystemManager.Instance.hitMat;

        //스프라이트 색 변환
        sprite.color = SystemManager.Instance.hitColor;

        yield return new WaitUntil(() => hitCoolCount <= 0);

        //머터리얼 복구
        sprite.material = SystemManager.Instance.spriteUnLitMat;

        //원래 색으로 복구
        sprite.color = Color.white;
    }

    public IEnumerator PoisonDotHit(float tickDamage, float duration)
    {
        //독 데미지 지속시간 넣기
        poisonCoolCount = duration;

        // 포이즌 머터리얼로 변환
        sprite.material = SystemManager.Instance.outLineMat;

        // 보라색으로 스프라이트 색 변환
        sprite.color = SystemManager.Instance.poisonColor;

        // 독 데미지 지속시간 남았을때 진행
        while (poisonCoolCount > 0)
        {
            // 독 데미지 입히기
            Damage(tickDamage);

            // 한 틱동안 대기
            yield return new WaitForSeconds(1f);

            // 독 데미지 지속시간에서 한틱 차감
            poisonCoolCount -= 1f;
        }

        //원래 머터리얼로 복구
        sprite.material = SystemManager.Instance.spriteLitMat;

        //원래 색으로 복구
        sprite.color = Color.white;
    }

    public bool Damage(float damage)
    {
        //! 갓모드 켜져 있으면 데미지 0
        if (godMod && damage > 0)
            damage = 0;

        //데미지 int로 바꾸기
        damage = Mathf.RoundToInt(damage);

        // 데미지 적용
        PlayerStat_Now.hpNow -= damage;

        //체력 범위 제한
        PlayerStat_Now.hpNow = Mathf.Clamp(PlayerStat_Now.hpNow, 0, PlayerStat_Now.hpNow);

        //혈흔 파티클 생성
        LeanPool.Spawn(bloodPrefab, transform.position, Quaternion.identity);

        //데미지 UI 띄우기
        DamageText(damage, false);

        UIManager.Instance.UpdateHp(); //체력 UI 업데이트

        //체력 0 이하가 되면 사망
        if (PlayerStat_Now.hpNow <= 0)
        {
            print("Game Over");
            Dead();

            return true;
        }

        return false;
    }

    void DamageText(float damage, bool isCritical)
    {
        // 데미지 UI 띄우기
        GameObject damageUI = LeanPool.Spawn(SystemManager.Instance.dmgTxtPrefab, transform.position, Quaternion.identity, SystemManager.Instance.overlayPool);
        TextMeshProUGUI dmgTxt = damageUI.GetComponent<TextMeshProUGUI>();

        // 크리티컬 떴을때 추가 강조효과 UI
        if (damage > 0)
        {
            if (isCritical)
            {
                // dmgTxt.color = new Color(200f/255f, 30f/255f, 30f/255f);
            }
            else
            {
                dmgTxt.color = new Color(200f / 255f, 30f / 255f, 30f / 255f);
            }

            dmgTxt.text = damage.ToString();
        }
        // 데미지 없을때
        else if (damage == 0)
        {
            dmgTxt.color = new Color(200f / 255f, 30f / 255f, 30f / 255f);
            dmgTxt.text = "MISS";
        }
        // 데미지가 마이너스일때 (체력회복일때)
        else if (damage < 0)
        {
            dmgTxt.color = Color.green;
            dmgTxt.text = "+" + (-damage).ToString();
        }

        //데미지 UI 애니메이션
        damageTextSeq = DOTween.Sequence();
        damageTextSeq
        .PrependCallback(() =>
        {
            //제로 사이즈로 시작
            damageUI.transform.localScale = Vector3.zero;
        })
        .Append(
            //위로 살짝 올리기
            // damageUI.transform.DOMove((Vector2)damageUI.transform.position + Vector2.up * 1f, 1f)
            damageUI.transform.DOJump((Vector2)damageUI.transform.position + Vector2.left * 2f, 1f, 1, 1f)
            .SetEase(Ease.OutBounce)
        )
        .Join(
            //원래 크기로 늘리기
            damageUI.transform.DOScale(Vector3.one, 0.5f)
        )
        .Append(
            //줄어들어 사라지기
            damageUI.transform.DOScale(Vector3.zero, 0.5f)
        )
        .OnComplete(() =>
        {
            LeanPool.Despawn(damageUI);
        });
    }

    IEnumerator Knockback(Vector2 dir)
    {
        // 넉백 벡터 수정
        knockbackDir = dir;

        // 넉백 벡터 반영하기
        Move();

        yield return new WaitForSeconds(0.5f);

        //넉백 버프 빼기
        knockbackDir = Vector2.zero;

        // 넉백 벡터 반영하기
        Move();
    }

    public IEnumerator FlatDebuff()
    {
        //마비됨
        isFlat = true;
        //플레이어 스프라이트 납작해짐
        transform.localScale = new Vector2(1f, 0.5f);

        //위치 얼리기
        rigid.constraints = RigidbodyConstraints2D.FreezeAll;

        //플레이어 멈추기
        Move();

        yield return new WaitForSeconds(2f);

        //마비 해제
        isFlat = false;
        //플레이어 스프라이트 복구
        transform.localScale = Vector2.one;

        //위치 얼리기 해제
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 플레이어 움직임 다시 재생
        Move();
    }

    void Dead()
    {
        // 시간 멈추기
        Time.timeScale = 0;

        //TODO 게임오버 UI 띄우기
        UIManager.Instance.GameOver();
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

    public void GetMagic(MagicInfo getMagic, bool magicReCast = true)
    {
        // MagicInfo 인스턴스 생성
        MagicInfo magic = new MagicInfo(getMagic);

        //마법의 레벨 초기화
        magic.magicLevel = 1;

        // touchedMagics에 해당 마법 id가 존재하지 않으면
        if (!MagicDB.Instance.touchedMagics.Exists(x => x == magic.id))
        {
            // 보유했던 마법 리스트에 추가
            MagicDB.Instance.touchedMagics.Add(magic.id);
        }

        // 플레이어 보유 마법에 해당 마법 추가하기
        hasStackMagics.Add(magic);

        // 0등급 마법이면 원소젬이므로 스킵
        if (magic.grade == 0)
            return;

        // //TODO 적이 죽으면 발동되는 마법일때 콜백에 함수포함시키기
        // if (magic.magicName == "Life Mushroom")
        // {
        //     SystemManager.Instance.AddDropSeedEvent(magic);
        // }

        //메인 UI에 스마트폰 알림 갱신
        UIManager.Instance.PhoneNotice();

        //플레이어 총 전투력 업데이트
        PlayerStat_Now.playerPower = GetPlayerPower();
    }

    public void EquipUltimate()
    {
        // 궁극기 없으면 리턴
        if (ultimateList.Count <= 0)
            return;

        //해당 마법을 장착
        MagicInfo ultimateMagic = ultimateList[0];
        // print("ultimate : " + ultimateMagic.magicName);

        // 해당 궁극기 쿨타임 저장
        ultimateCoolTime = MagicDB.Instance.MagicCoolTime(ultimateMagic);

        // 해당 마법 쿨타임 카운트 초기화
        ultimateCoolCount = ultimateCoolTime;

        //쿨타임 이미지 갱신
        UIManager.Instance.UltimateCooltime();

        // 궁극기 UI 업데이트
        UIManager.Instance.UpdateUltimateIcon();
    }

    IEnumerator CastDefaultMagics()
    {
        // MagicDB 로드 완료까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //TODO 캐릭터에 따라 defaultMagic 기본마법 넣고 시작
        List<int> magics = new List<int>();

        //! 마법 없이 테스트
        if (CastMagic.Instance.noMagic)
            yield break;

        //! 모든 마법 테스트
        if (CastMagic.Instance.testAllMagic)
        {
            foreach (var value in MagicDB.Instance.magicDB.Values)
            {
                magics.Add(value.id);
            }
        }
        else
        {
            magics = CastMagic.Instance.defaultMagic;
        }

        // 캐릭터 기본 마법 추가
        foreach (int magicID in magics)
        {
            // 마법 찾기
            MagicInfo magic = MagicDB.Instance.GetMagicByID(magicID);

            //마법 획득
            GetMagic(magic, false);
        }

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
        Time.timeScale = 0;

        //레벨업
        PlayerStat_Now.Level++;

        //경험치 초기화
        PlayerStat_Now.ExpNow = 0;

        //경험치 최대치 갱신
        PlayerStat_Now.ExpMax = PlayerStat_Now.Level * PlayerStat_Now.Level + 5;
        //! 테스트용 맥스 경험치
        PlayerStat_Now.ExpMax = 3;

        // 마법 합성 메뉴 띄우기
        UIManager.Instance.PopupUI(UIManager.Instance.mergeMagicPanel);
        // UIManager.Instance.PopupUI(UIManager.Instance.mixMagicPanel);
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

        foreach (var magic in hasStackMagics)
        {
            //총전투력에 해당 마법의 등급*레벨 더하기
            magicPower += magic.grade * magic.magicLevel;

            // print(magicPower + " : " + magic.grade + " * " + magic.magicLevel);
        }

        return magicPower;
    }
}
