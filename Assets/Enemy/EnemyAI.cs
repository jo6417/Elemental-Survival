using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;

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
    // public float atkPower = 1;
    // public float dropRate = 0.5f; //아이템 드롭 확률
    // public float hitDelay = 1f;
    // public float HpMax = 2;
    // public float knockbackForce = 1;

    void Start()
    {
        player = PlayerManager.Instance.transform;
        rigid = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

        //프리팹 이름으로 아이템 정보 찾아 넣기
        enemy = EnemyDB.Instance.GetEnemyByName(transform.name.Split('_')[0]);
        enemyName = enemy.name;

        Initial();
    }

    void Initial()
    {
        hitCount = 0; //데미지 쿨타임 초기화
        HpNow = enemy.hpMax; //체력 초기화
        sprite.color = Color.white; //스프라이트 색깔 초기화
        rigid.velocity = Vector2.zero; //속도 초기화
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

            // 체력 감소
            MagicInfo magic = MagicDB.Instance.GetMagicByID(other.GetComponent<MagicProjectile>().magicID);
            Damaged(magic);
        }
    }

    void Damaged(MagicInfo magic)
    {
        //크리티컬 확률 추가
        bool isCritical = magic.criticalRate >= Random.value ? true : false;
        float criticalAtk = isCritical ? 1.5f : 1f;
        int damage = (int)(Random.Range(magic.damage * 0.8f, magic.damage * 1.2f) * criticalAtk);
        damage = Mathf.Clamp(damage, 1, damage);

        // 체력 감소
        HpNow -= damage;

        // 경직 시간 추가
        hitCount = enemy.hitDelay;

        // 넉백 효과
        if (magic.knockbackForce > 0 && gameObject.activeSelf)
        {
            StartCoroutine(Knockback(magic.knockbackForce));
        }

        // 데미지 UI 띄우기
        Transform damageCanvas = ObjectPool.Instance.transform.Find("OverlayUI");
        var damageUI = LeanPool.Spawn(damageTxt, transform.position, Quaternion.identity, damageCanvas);
        Text dmgTxt = damageUI.GetComponent<Text>();
        dmgTxt.text = damage.ToString();

        // 크리티컬 떴을때 추가 강조효과 UI
        if (isCritical)
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
        // 반대로 이동
        Vector2 dir = transform.position - player.position;
        // Vector2 dir = Vector2.zero;
        // rigid.velocity = dir.normalized * knockbackForce;
        rigid.AddForce(dir.normalized * knockbackForce * 0.1f);

        yield return null;
    }

    void Dead()
    {
        if (enemy.dropRate >= Random.Range(0, 1))
        {
            //아이템 드랍
            DropItem();
        }

        // 몬스터 초기화
        Initial();

        // 몬스터 비활성화
        LeanPool.Despawn(gameObject);
    }

    // 갖고있는 아이템 드랍
    void DropItem()
    {
        Transform itemPool = ObjectPool.Instance.transform.Find("ItemPool");
        LeanPool.Spawn(hasItem[Random.Range(0, hasItem.Length)], transform.position, Quaternion.identity, itemPool);
    }
}
