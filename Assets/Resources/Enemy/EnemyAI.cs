using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using TMPro;

public class EnemyAI : MonoBehaviour
{
    public EnemyInfo enemy;
    public string enemyName;

    [Header("Refer")]
    // public Enemy enemy;
    Transform player;
    public float speed;
    public GameObject damageTxt; //데미지 UI
    Rigidbody2D rigid;
    SpriteRenderer sprite;
    public GameObject[] hasItem; //가진 아이템

    [Header("Stat")]
    public float hitCount = 0;
    public float HpNow = 2;

    void Start()
    {
        player = PlayerManager.Instance.transform;
        rigid = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => EnemyDB.Instance.loadDone);

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (enemy == null)
            enemy = EnemyDB.Instance.GetEnemyByName(transform.name.Split('_')[0]);

        //enemy 못찾으면 코루틴 종료
        if (enemy == null)
            yield break;

        hitCount = 0; //데미지 쿨타임 초기화
        HpNow = enemy.hpMax; //체력 초기화
        sprite.color = Color.white; //스프라이트 색깔 초기화
        rigid.velocity = Vector2.zero; //속도 초기화

        enemyName = enemy.name;
    }

    void Update()
    {
        rigid.velocity = Vector2.zero; //이동 초기화

        if (hitCount <= 0)
        {
            sprite.color = Color.white;

            Vector2 dir = player.position - transform.position;
            rigid.velocity = dir.normalized * speed;
        }
        // 맞고나서 경직 시간일때
        else
        {
            // 적 색깔 변화
            sprite.color = Color.gray;

            // 경직 시간 카운트
            hitCount -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 마법 투사체와 충돌 했을때
        if (other.transform.CompareTag("Magic"))
        {
            // print("마법과 충돌");

            // 마법 정보 찾기
            MagicHolder magicHolder = other.GetComponent<MagicHolder>();
            MagicInfo magic = magicHolder.magic;

            // 체력 감소
            Damaged(magicHolder);

            //넉백
            if (magicHolder.knockbackForce > 0 && gameObject.activeSelf)
            {
                StartCoroutine(Knockback(magicHolder.knockbackForce));
            }
        }
    }

    void Damaged(MagicHolder magicHolder)
    {
        if (enemy == null)
            return;

        MagicInfo magic = magicHolder.magic;

        //크리티컬 확률 계산
        bool isCritical = MagicDB.Instance.MagicCritical(magic) >= Random.value ? true : false;
        //크리티컬 데미지 계산
        float criticalDamage = isCritical ? MagicDB.Instance.MagicCriticalDamage(magic) : 1f;

        //데미지 계산
        float damage = MagicDB.Instance.MagicPower(magic);
        // 고정 데미지에 확률 계산 및 크리티컬 데미지 곱해서 int로 반올림
        damage = Mathf.RoundToInt(Random.Range(damage * 0.8f, damage * 1.2f) * criticalDamage);

        // 데미지가 0이 아닐때 최소 데미지 1 보장
        if (damage != 0)
            damage = Mathf.Clamp(damage, 1, damage);

        // 체력 감소
        HpNow -= damage;

        // 경직 시간 추가
        hitCount = enemy.hitDelay;

        // 데미지 UI 띄우기
        Transform damageCanvas = ObjectPool.Instance.transform.Find("OverlayUI");
        var damageUI = LeanPool.Spawn(damageTxt, transform.position, Quaternion.identity, damageCanvas);
        TextMeshProUGUI dmgTxt = damageUI.GetComponent<TextMeshProUGUI>();
        dmgTxt.text = damage.ToString();

        // 크리티컬 떴을때 추가 강조효과 UI
        if (isCritical && damage != 0)
        {
            dmgTxt.fontSize = 120;
            dmgTxt.color = new Color(1, 100 / 255, 100 / 255);
        }
        else
        {
            dmgTxt.fontSize = 100;
            dmgTxt.color = Color.white;
        }

        //데미지 UI 애니메이션
        damageUI.transform.DOMove((Vector2)damageUI.transform.position + Vector2.up * 1f, 1f);
        damageUI.transform.DOScale(Vector3.zero, 1f).SetEase(Ease.InOutBack);
        LeanPool.Despawn(damageUI, 1f);

        // print(HpNow + " / " + enemy.HpMax);
        // 체력 0 이하면 죽음
        if (HpNow <= 0)
            Dead();
    }

    IEnumerator Knockback(float knockbackForce)
    {
        // 반대 방향 및 넉백파워
        Vector2 knockbackDir = transform.position - player.position;
        Vector2 knockbackBuff = knockbackDir.normalized * ((knockbackForce * 0.1f) + (PlayerManager.Instance.knockbackForce - 1));
        knockbackDir = knockbackDir.normalized + knockbackBuff;

        //반대방향으로 이동
        transform.DOMove((Vector2)transform.position + knockbackDir, enemy.hitDelay)
        .SetEase(Ease.OutExpo);

        // print(knockbackDir);

        yield return null;
    }

    void Dead()
    {
        if (enemy == null)
            return;

        if (enemy.dropRate >= Random.Range(0, 1) && hasItem.Length > 0)
        {
            //아이템 드랍
            DropItem();
        }

        // 몬스터 비활성화
        LeanPool.Despawn(gameObject);
    }

    // 갖고있는 아이템 드랍
    void DropItem()
    {
        Transform itemPool = ObjectPool.Instance.transform.Find("ItemPool");
        LeanPool.Spawn(hasItem[Random.Range(0, hasItem.Length)], transform.position, Quaternion.identity, itemPool);

        //체력 씨앗 드랍
        DropHealSeed();
    }

    void DropHealSeed()
    {
        // 플레이어가 HealSeed 마법을 갖고 있을때
        MagicInfo magic = PlayerManager.Instance.hasMagics.Find(x => x.magicName == "Heal Seed");
        if (magic != null)
        {            
            // 크리티컬 확률 = 드랍 확률
            // print(MagicDB.Instance.MagicCritical(magic));
            bool isDrop = MagicDB.Instance.MagicCritical(magic) >= Random.value ? true : false;
            //크리티컬 데미지만큼 회복
            int healAmount = Mathf.RoundToInt(MagicDB.Instance.MagicCriticalDamage(magic));
            healAmount = (int)Mathf.Clamp(healAmount, 1f, healAmount); //최소 회복량 1f 보장

            // HealSeed 마법 크리티컬 확률에 따라 드랍
            if (isDrop)
            {
                Transform itemPool = ObjectPool.Instance.transform.Find("ItemPool");
                GameObject healSeed = LeanPool.Spawn(ItemDB.Instance.heartSeed, transform.position, Quaternion.identity, itemPool);

                // 아이템에 체력 회복량 넣기
                healSeed.GetComponent<ItemManager>().amount = healAmount;
            }
        }
    }
}
