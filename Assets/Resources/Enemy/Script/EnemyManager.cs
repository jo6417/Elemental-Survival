using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using TMPro;

public class EnemyManager : MonoBehaviour
{
    public List<int> hasItemId = new List<int>(); //가진 아이템
    public EnemyInfo enemy;
    EnemyAI enemyAI;
    public float portalSize = 1f; //포탈 사이즈 지정값
    Collider2D coll;
    public bool isElite; //엘리트 몬스터 여부
    public int eliteClass; //엘리트 클래스 종류
    public bool isDead; //죽음 코루틴 진행중 여부
    public float particleHitCount = 0;
    public float stopCount = 0;
    public float hitCount = 0;
    public float oppositeCount = 0;
    Sequence txtSeq;
    // public Animator anim;

    [Header("Refer")]
    public GameObject damageTxt; //데미지 UI
    [HideInInspector]
    public SpriteRenderer sprite;

    public Material originMat;
    public Color originMatColor; //해당 몬스터 머터리얼 원래 색
    public Color originColor; //해당 몬스터 원래 색

    [Header("Stat")]
    public float HpNow = 2;

    [Header("Attack Effect")]
    public bool isFallAttack = false;

    [Header("Debug")]
    [SerializeField]
    string enemyName;
    [SerializeField]
    string enemyType;
    [SerializeField]
    float hpMax;
    [SerializeField]
    float power;
    [SerializeField]
    float speed;
    [SerializeField]
    float range;

    void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        enemyAI = GetComponent<EnemyAI>();
        coll = GetComponent<Collider2D>();
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
            //적 정보 인스턴싱
            enemy = new EnemyInfo(EnemyDB.Instance.GetEnemyByName(transform.name.Split('_')[0]));

        //enemy 못찾으면 코루틴 종료
        if (enemy == null)
            yield break;

        hitCount = 0; //데미지 카운트 초기화
        stopCount = 0; //시간 정지 카운트 초기화
        oppositeCount = 0; //반대편 전송 카운트 초기화
        HpNow = enemy.hpMax; //체력 초기화
        sprite.color = Color.white; //스프라이트 색깔 초기화

        //머터리얼 정보 저장
        originMat = sprite.material;
        //색상 정보 저장
        originColor = sprite.color;
        //아웃라인이면 머터리얼 색상 저장
        if (sprite.material == SystemManager.Instance.outLineMat)
            originMatColor = sprite.material.color;

        //죽음 여부 초기화
        isDead = false;

        //콜라이더 켜기
        coll.enabled = true;

        //! 테스트 확인용
        enemyName = enemy.enemyName;
        enemyType = enemy.enemyType;
        hpMax = enemy.hpMax;
        power = enemy.power;
        speed = enemy.speed;
        range = enemy.range;
    }

    private void Update()
    {
        if (particleHitCount > 0)
        {
            particleHitCount -= Time.deltaTime;
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.transform.CompareTag("Magic") && !isDead && particleHitCount <= 0)
        {
            HitMagic(other.gameObject);

            //파티클 피격 딜레이 시작
            particleHitCount = 0.2f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.CompareTag("Magic"))
        {
            HitMagic(other.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 계속 마법 콜라이더 안에 있을때
        if (other.transform.CompareTag("Magic") && hitCount <= 0)
        {
            // 마법 정보 찾기
            MagicHolder magicHolder = other.GetComponent<MagicHolder>();
            MagicInfo magic = magicHolder.magic;

            // 다단히트 마법일때만
            if (magic.multiHit)
                HitMagic(other.gameObject);
        }
    }

    void HitMagic(GameObject other)
    {
        if (isDead)
            return;

        // 마법 정보 찾기
        MagicHolder magicHolder = other.GetComponent<MagicHolder>();
        MagicInfo magic = magicHolder.magic;

        // print(transform.name + " : " + magic.magicName);

        // 체력 감소
        if (magic.power > 0)
            StartCoroutine(Damaged(magicHolder));

        //넉백
        if (magicHolder.knockbackForce > 0)
        {
            StartCoroutine(Knockback(magicHolder.knockbackForce));
        }

        //시간 정지
        if (magicHolder.isStop)
        {
            //몬스터 경직 카운터에 duration 만큼 추가
            stopCount = MagicDB.Instance.MagicDuration(magic);

            // 해당 위치에 고정
            // enemyAI.rigid.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isDead)
            return;

        if (other.transform.CompareTag("Magic"))
        {
            // 마법 정보 찾기
            MagicHolder magicHolder = other.GetComponent<MagicHolder>();
            MagicInfo magic = magicHolder.magic;

            //경직 풀기
            if (magicHolder.isStop)
            {
                // 카운터를 0으로 만들기
                // stopCount = 0;

                // 위치 고정 해제
                // enemyAI.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }

    IEnumerator Damaged(MagicHolder magicHolder)
    {
        if (enemy == null || isDead)
            yield break;

        MagicInfo magic = magicHolder.magic;

        //크리티컬 성공 여부
        bool isCritical = MagicDB.Instance.MagicCritical(magic);
        //크리티컬 데미지 계산
        float criticalPower = MagicDB.Instance.MagicCriticalPower(magic);

        //데미지 계산
        float damage = MagicDB.Instance.MagicPower(magic);
        // 고정 데미지에 확률 계산
        damage = Random.Range(damage * 0.8f, damage * 1.2f);

        //크리티컬 곱해도 데미지가 그대로면 크리티컬 아님
        if (Mathf.RoundToInt(damage) >= Mathf.RoundToInt(damage * criticalPower))
        {
            isCritical = false;
            damage = Mathf.RoundToInt(damage);
        }
        else
        {
            damage = Mathf.RoundToInt(damage * criticalPower);
        }

        // 데미지가 0이 아닐때 최소 데미지 1 보장
        if (damage != 0)
            damage = Mathf.Clamp(damage, 1, damage);

        // 체력 감소
        HpNow -= damage;

        // 경직 시간 추가
        hitCount = enemy.hitDelay;

        //데미지 UI 띄우기
        DamageText(damage, isCritical);

        //보스면 체력 UI 띄우기
        if (enemy.enemyType == "boss")
        {
            UIManager.Instance.UpdateBossHp(HpNow, hpMax, enemyName);
        }

        // print(HpNow + " / " + enemy.HpMax);
        // 체력 0 이하면 죽음
        if (HpNow <= 0)
        {
            // print("Dead Pos : " + transform.position);
            //죽음 코루틴 시작
            StartCoroutine(Dead());
        }
    }

    void DamageText(float damage, bool isCritical)
    {
        // 데미지 UI 띄우기
        GameObject damageUI = LeanPool.Spawn(damageTxt, transform.position, Quaternion.identity, UIManager.Instance.overlayPool);
        TextMeshProUGUI dmgTxt = damageUI.GetComponent<TextMeshProUGUI>();

        //데미지 텍스트, 데미지 0일때 miss 처리
        dmgTxt.text = damage == 0 ? "MISS" : damage.ToString();

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
        txtSeq = DOTween.Sequence();
        txtSeq
        .PrependCallback(() =>
        {
            //제로 사이즈로 시작
            damageUI.transform.localScale = Vector3.zero;
        })
        .Append(
            //위로 살짝 올리기
            damageUI.transform.DOMove((Vector2)damageUI.transform.position + Vector2.up * 1f, 1f)
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

    IEnumerator Knockback(float knockbackForce)
    {
        // 반대 방향 및 넉백파워
        Vector2 knockbackDir = transform.position - PlayerManager.Instance.transform.position;
        Vector2 knockbackBuff = knockbackDir.normalized * ((knockbackForce * 0.1f) + (PlayerManager.Instance.PlayerStat_Now.knockbackForce - 1));
        knockbackDir = knockbackDir.normalized + knockbackBuff;

        // 피격 반대방향으로 이동
        transform.DOMove((Vector2)transform.position + knockbackDir, enemy.hitDelay)
        .SetEase(Ease.OutExpo);

        // print(knockbackDir);

        yield return null;
    }

    IEnumerator Dead()
    {
        if (enemy == null)
            yield break;

        // 경직 시간 추가
        // hitCount += 1f;

        isDead = true;

        //콜라이더 끄기
        coll.enabled = false;

        //몬스터 총 전투력 빼기
        EnemySpawn.Instance.NowEnemyPower -= enemy.grade;

        //몬스터 킬 카운트 올리기
        UIManager.Instance.killCount++;
        UIManager.Instance.UpdateKillCount();

        // 머터리얼 및 색 변경
        sprite.material = SystemManager.Instance.hitMat;
        sprite.color = SystemManager.Instance.hitColor;

        // 색깔 점점 흰색으로
        sprite.DOColor(SystemManager.Instance.DeadColor, 1f);

        //색 변경 완료 될때까지 대기
        yield return new WaitUntil(() => sprite.color == SystemManager.Instance.DeadColor);
        // while (sprite.color != EnemySpawn.Instance.DeadColor)
        // {
        //     sprite.DOPlay();

        //     yield return null;
        // }

        //전역 시간 속도가 멈춰있다면 복구될때까지 대기
        yield return new WaitUntil(() => SystemManager.Instance.timeScale > 0);

        // 먼지 이펙트 생성
        GameObject dust = LeanPool.Spawn(EnemySpawn.Instance.dustPrefab, transform.position, Quaternion.identity, EnemySpawn.Instance.effectPool);
        dust.tag = "Enemy";
        // 2초후 디스폰
        // LeanPool.Despawn(dust, 2f);

        //혈흔 이펙트 생성
        GameObject blood = LeanPool.Spawn(EnemySpawn.Instance.bloodPrefab, transform.position, Quaternion.identity, EnemySpawn.Instance.effectPool);
        // 10초후 디스폰
        // LeanPool.Despawn(blood, 10f);

        if (enemy.dropRate >= Random.Range(0, 1) && hasItemId.Count > 0)
        {
            //아이템 드랍
            DropItem();
        }

        // 몬스터 리스트에서 몬스터 본인 빼기
        EnemySpawn.Instance.spawnEnemyList.Remove(gameObject);

        // 몬스터 비활성화
        LeanPool.Despawn(gameObject);

        yield return null;
    }

    // 갖고있는 아이템 드랍
    void DropItem()
    {
        Transform itemPool = ObjectPool.Instance.transform.Find("ItemPool");

        //보유한 모든 아이템 드랍
        foreach (var id in hasItemId)
        {
            int itemId = id;
            // -1이면 랜덤 원소젬 뽑기
            if (id == -1)
                itemId = Random.Range(0, 5);

            //아이템 프리팹 찾기
            GameObject prefab = ItemDB.Instance.GetItemPrefab(itemId);
            //아이템 오브젝트 소환
            GameObject itemObj = LeanPool.Spawn(prefab, transform.position, Quaternion.identity, itemPool);

            //TODO 랜덤 방향으로 아이템 날리기
            Vector2 pos = (Vector2)transform.position + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 3f;
            itemObj.transform.DOMove(pos, 1f)
            .SetEase(Ease.OutExpo);
        }

        //몬스터 죽을때 함수 호출, 체력 씨앗 드랍
        if (SystemManager.Instance.enemyDeadCallback != null)
            SystemManager.Instance.enemyDeadCallback(transform.position);
    }
}
