using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

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

    [Header("Refer")]
    public GameObject mobSpawner;
    private Animator animator;
    private SpriteRenderer sprite;
    public GameObject levelupPopup;
    public GameObject OverlayUI;

    [Header("Stat")] //기본 스탯
    public float hpMax = 20; // 최대 체력
    public float hpNow = 20; // 체력
    public float mpMax = 10; // 최대 마나
    public float mpNow = 10; // 마나
    public float Level = 1; //레벨
    public float ExpMax = 5; // 경험치 최대치
    public float ExpNow = 0; // 현재 경험치
    public float moveSpeed = 5; //이동속도

    [Header("Damaged")]
    public float HitDelay = 0.1f; //피격 무적시간
    public float flickSpeed = 10f; //깜빡이는 속도
    public float ShakeTime;
    public float ShakeIntensity;
    private Color originColor;
    private bool isDamage = false;

    [Header("Buff")] // 능력치 추가 계수 (곱연산 기본값 : 1 / 합연산 기본값 : 0)
    public int projectileNum = 1; // 투사체 개수
    public float power = 1; //마법 공격력
    public float armor = 1; //방어력
    public float rateFire = 1; //마법 공격속도
    public float cooltime = 1; //마법 쿨타임
    public float duration = 1; //마법 지속시간
    public float range = 1; //마법 범위
    public float luck = 1; //행운
    public float expGain = 1; //경험치 획득량
    public float moneyGain = 1; //원소젬 획득량

    [Header("Item")]
    public List<int> hasMagics = new List<int>(); //플레이어가 가진 마법
    public List<int> hasItems = new List<int>(); //플레이어가 가진 아이템
    public int Earth_Gem = 0;
    public int Fire_Gem = 0;
    public int Life_Gem = 0;
    public int Lightning_Gem = 0;
    public int Water_Gem = 0;
    public int Wind_Gem = 0;

    void Start()
    {
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        originColor = sprite.color;

        // 원소젬 UI 업데이트
        UIManager.Instance.updateGem();

        //경험치 최대치 갱신
        ExpMax = Level * Level + 5;

        //능력치 초기화
        UIManager.Instance.InitialStat();
    }

    private void Update()
    {
        //카메라 따라오기
        Camera.main.transform.position = transform.position + new Vector3(0, 0, -10);
        
        //몬스터 스포너 따라오기
        mobSpawner.transform.position = transform.position;

        //이동
        Move();
    }

    void Move()
    {
        //이동 입력값 받기
        Vector2 dir = Vector2.zero;
        float horizonInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // x축 이동
        if (horizonInput != 0)
        {
            dir.x = horizonInput;

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

        // 애니메이터
        if (horizonInput == 0 && verticalInput == 0)
        {
            animator.SetBool("isWalking", false);
        }
        else
        {
            animator.SetBool("isWalking", true);
        }

        dir.Normalize();
        GetComponent<Rigidbody2D>().velocity = moveSpeed * dir;
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        //적에게 충돌
        if (other.gameObject.CompareTag("Enemy") && !isDamage)
        {
            // print("적 충돌");

            Enemy enemy = other.gameObject.GetComponent<EnemyAI>().enemy;

            //피격 딜레이 무적
            IEnumerator hitDelay = HitDelayCoroutine();
            StartCoroutine(hitDelay);

            Damage(enemy.atkPower);
        }
    }

    //HitDelay만큼 시간 지난후 피격무적시간 끝내기
    IEnumerator HitDelayCoroutine()
    {
        isDamage = true;

        sprite.color = new Color(255, 20, 20); //스프라이트 색 변환

        yield return new WaitForSeconds(HitDelay);

        sprite.color = originColor; //원래 색으로 복구

        isDamage = false;
    }

    void Damage(float damage)
    {
        // 체력 감소
        hpNow -= damage;

        //체력 0 이하가 되면 사망
        if (hpNow <= 0)
        {
            // print("Game Over");
            // Dead();
        }

        UIManager.Instance.updateHp(); //체력 UI 업데이트        
    }

    void Dead(){
        // 시간 멈추기
        Time.timeScale = 0;
        
        //TODO 게임오버 씬 띄우기
        // gameOverUI.SetActive(true);
    }

    public void GainItem(ItemInfo item)
    {
        // print(item.itemType + " : " + item.itemName);
        // 아이템이 젬 타입일때
        if (item.itemType == "Gem")
        {
            //플레이어 소지 젬 갯수 올리기
            AddGem(item);
        }

        // 아이템이 스크롤일때
        if (item.itemType == "Scroll")
        {            
            // print("아이템 합성");

            //TODO 아이템 합성 메뉴 띄우기
            UIManager.Instance.ScrollMenu();
        }
    }

    public void AddGem(ItemInfo item)
    {
        // 어떤 원소든지 경험치 증가
        ExpNow++;

        //경험치 다 찼을때
        if (ExpNow == ExpMax)
        {
            //레벨업
            Levelup();
        }

        if (item.earth)
        {
            Earth_Gem++;
        }
        else if (item.fire)
        {
            Fire_Gem++;
        }
        else if (item.life)
        {
            Life_Gem++;
        }
        else if (item.lightning)
        {
            Lightning_Gem++;
        }
        else if (item.water)
        {
            Water_Gem++;
        }
        else if (item.wind)
        {
            Wind_Gem++;
        }

        // UI 업데이트
        UIManager.Instance.updateGem();

        //경험치 및 레벨 갱신
        UIManager.Instance.updateExp();
    }

    void Levelup()
    {
        //레벨업
        Level++;

        //경험치 초기화
        ExpNow = 0;

        //경험치 최대치 갱신
        ExpMax = Level * Level + 5;

        // 시간 멈추기
        Time.timeScale = 0;

        // 팝업 선택메뉴 띄우기
        levelupPopup.SetActive(true);
    }

}
