using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using TMPro;
using System.Linq;

public class EnemyManager : MonoBehaviour
{
    [SerializeField]
    private List<int> defaultHasItem = new List<int>(); //가진 아이템 기본값
    public List<ItemInfo> nowHasItem = new List<ItemInfo>(); // 현재 가진 아이템
    public EnemyManager referEnemyManager = null;
    public EnemyInfo enemy;

    [Header("State")]
    public State state; //현재 상태
    public enum State { Idle, Hit, Dead, TimeStop, MagicStop }
    public Action nowAction = Action.Idle; //현재 행동
    public Action nextAction = Action.Idle; //다음에 할 행동 예약
    public enum Action { Idle, Walk, Jump, Attack }
    public MoveType moveType;
    public enum MoveType
    {
        Walk, // 걸어서 등속도이동
        Jump, // 시간마다 점프
        Dash, // 시간마다 대쉬
        Teleport, // 시간마다 플레이어 주변 위치로 텔레포트
        Follow // 플레이어와 일정거리 고정하며 따라다님
    };

    public float portalSize = 1f; //포탈 사이즈 지정값
    public bool isElite; //엘리트 몬스터 여부
    public int eliteClass; //엘리트 클래스 종류
    public bool isDead; //죽음 코루틴 진행중 여부
    public float particleHitCount = 0;
    public float stopCount = 0;
    public float hitCount = 0;
    public float oppositeCount = 0;
    Sequence damageTextSeq;
    public bool selfExplosion = false; //죽을때 자폭 여부
    public bool statusEffect = false; //상태이상으로 색 변형 했는지 여부

    [Header("Refer")]
    public EnemyAtkTrigger explosionTrigger;
    public Transform spriteObj;
    public List<SpriteRenderer> spriteList = new List<SpriteRenderer>();
    public List<Material> originMatList = new List<Material>(); //변형 전 머터리얼
    public List<Color> originMatColorList = new List<Color>(); //변형 전 머터리얼 색
    public List<Color> originColorList = new List<Color>(); // 변형 전 스프라이트 색
    public List<Animator> animList = new List<Animator>();
    public Rigidbody2D rigid;
    public Collider2D physicsColl; // 물리용 콜라이더
    public Collider2D hitColl; // 히트박스용 콜라이더
    EnemyAI enemyAI;

    [Header("Stat")]
    public float hpMax;
    public float HpNow = 2;
    public float power;
    public float speed;
    public float range;

    [Header("Attack Effect")]
    public bool friendlyFire = false; // 충돌시 아군 피해 여부
    public bool flatDebuff = false; //납작해지는 디버프
    public bool knockBackDebuff = false; //넉백 디버프

    [Header("Debug")]
    [SerializeField]
    string enemyName;
    [SerializeField]
    string enemyType;

    void Awake()
    {
        spriteObj = spriteObj == null ? transform : spriteObj;
        rigid = rigid == null ? spriteObj.GetComponentInChildren<Rigidbody2D>(true) : rigid;
        hitColl = hitColl == null ? spriteObj.GetComponentInChildren<Collider2D>(true) : hitColl;
        animList = animList.Count == 0 ? GetComponentsInChildren<Animator>().ToList() : animList;

        enemyAI = GetComponent<EnemyAI>();

        // 스프라이트 리스트에 아무것도 없으면 찾아 넣기
        if (spriteList.Count == 0)
        {
            spriteList = spriteObj.GetComponentsInChildren<SpriteRenderer>().ToList();
        }

        // 초기 스프라이트 정보 수집
        foreach (SpriteRenderer sprite in spriteList)
        {
            originColorList.Add(sprite.color);
            originMatList.Add(sprite.material);
            originMatColorList.Add(sprite.material.color);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        //콜라이더 끄기
        if (hitColl != null)
            hitColl.enabled = false;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => EnemyDB.Instance.loadDone);

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (enemy == null && referEnemyManager == null)
            //적 정보 찾기
            enemy = EnemyDB.Instance.GetEnemyByName(transform.name.Split('_')[0]);

        //적 정보 인스턴싱
        if (enemy != null)
            enemy = new EnemyInfo(enemy);

        // enemy 정보 들어올때까지 대기
        yield return new WaitUntil(() => enemy != null);

        // //enemy 못찾으면 코루틴 종료
        // if (enemy == null)
        //     yield break;

        //보스면 체력 UI 띄우기
        if (enemy.enemyType == "boss")
        {
            UIManager.Instance.UpdateBossHp(HpNow, hpMax, enemyName);
        }

        //ItemDB 로드 될때까지 대기
        yield return new WaitUntil(() => ItemDB.Instance.loadDone);

        //보유 아이템 초기화
        nowHasItem.Clear();
        foreach (var itemId in defaultHasItem)
        {
            // id 할당을 위해 변수 선언
            int id = itemId;

            // -1이면 랜덤 원소젬 뽑기
            if (id == -1)
                id = Random.Range(0, 5);

            // item 인스턴스 생성 및 amount 초기화
            ItemInfo item = new ItemInfo(ItemDB.Instance.GetItemByID(id));
            item.amount = 1;

            //item 정보 넣기
            nowHasItem.Add(item);
        }

        hitCount = 0; //데미지 카운트 초기화
        stopCount = 0; //시간 정지 카운트 초기화
        oppositeCount = 0; //반대편 전송 카운트 초기화
        HpNow = enemy.hpMax; //체력 초기화

        //죽음 여부 초기화
        isDead = false;

        //콜라이더 켜기
        if (hitColl != null)
            hitColl.enabled = true;

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

    public bool ManageState()
    {
        // 몬스터 정보 없으면 리턴
        if (enemy == null)
            return false;

        //죽음 애니메이션 중일때
        if (isDead)
        {
            // 상태이상 변수 true
            statusEffect = true;

            state = State.Dead;

            rigid.velocity = Vector2.zero; //이동 초기화
            rigid.constraints = RigidbodyConstraints2D.FreezeAll;

            if (animList.Count > 0)
            {
                foreach (Animator anim in animList)
                {
                    anim.speed = 0f;
                }
            }

            transform.DOPause();

            return false;
        }

        //전역 타임스케일이 0 일때
        if (SystemManager.Instance.globalTimeScale == 0)
        {
            // 상태이상 변수 true
            statusEffect = true;

            state = State.MagicStop;

            // 애니메이션 멈추기
            if (animList.Count > 0)
            {
                foreach (Animator anim in animList)
                {
                    anim.speed = 0f;
                }
            }

            // 이동 멈추기
            rigid.velocity = Vector2.zero;

            transform.DOPause();

            return false;
        }

        // 멈춤 디버프일때
        if (stopCount > 0)
        {
            // 상태이상 변수 true
            statusEffect = true;

            state = State.TimeStop;

            rigid.velocity = Vector2.zero; //이동 초기화
            rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            // 애니메이션 멈추기
            if (animList.Count > 0)
            {
                foreach (Animator anim in animList)
                {
                    anim.speed = 0f;
                }
            }

            //시간 멈춤 머터리얼 및 색으로 바꾸기
            for (int i = 0; i < spriteList.Count; i++)
            {
                spriteList[i].material = originMatList[i];
                spriteList[i].color = SystemManager.Instance.stopColor;
            }

            transform.DOPause();

            stopCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;

            return false;
        }

        //맞고 경직일때
        if (hitCount > 0)
        {
            // 상태이상 변수 true
            statusEffect = true;

            state = State.Hit;

            rigid.velocity = Vector2.zero; //이동 초기화

            // 머터리얼 및 색 변경
            foreach (SpriteRenderer sprite in spriteList)
            {
                sprite.material = SystemManager.Instance.hitMat;
                sprite.color = SystemManager.Instance.hitColor;
            }

            hitCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;

            return false;
        }

        //스폰 콜라이더에 닿아 반대편으로 보내질때 잠시대기
        if (oppositeCount > 0)
        {
            rigid.velocity = Vector2.zero; //이동 초기화

            oppositeCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;

            return false;
        }

        //모든 문제 없으면 idle 상태로 전환
        state = State.Idle;

        // 상태이상 걸렸으면
        if (statusEffect)
        {
            //상태이상 해제됨
            statusEffect = false;

            // rigid, sprite, 트윈, 애니메이션 상태 초기화
            for (int i = 0; i < spriteList.Count; i++)
            {
                spriteList[i].material = originMatList[i];
                spriteList[i].color = originColorList[i];
            }

            transform.DOPlay();

            // 애니메이션 속도 초기화
            if (animList.Count > 0)
            {
                foreach (Animator anim in animList)
                {
                    anim.speed = 1f;
                }
            }
        }

        return true;
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
        if (other.CompareTag("Magic"))
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

        //적에게 맞았을때
        if (other.transform.CompareTag("Enemy"))
        {
            if (other.gameObject.TryGetComponent<EnemyManager>(out EnemyManager hitEnemy))
            {
                if (hitEnemy.enabled)
                {
                    // 아군 피해 줄때
                    if (hitEnemy.friendlyFire)
                    {
                        // print("enemy damage");

                        // 데미지 입기
                        Damage(hitEnemy.enemy.power, false);
                    }

                    // 넉백 디버프 있을때
                    if (hitEnemy.knockBackDebuff)
                    {
                        // print("enemy knock");

                        // 넉백
                        StartCoroutine(Knockback(hitEnemy.enemy.power));
                    }

                    // flat 디버프 있을때, stop 카운트 중 아닐때
                    if (hitEnemy.flatDebuff && stopCount <= 0)
                    {
                        // print("enemy flat");

                        // 납작해지고 행동불능
                        StartCoroutine(FlatDebuff());
                    }
                }
            }
        }
    }

    IEnumerator FlatDebuff()
    {
        //정지 시간 추가
        stopCount = 2f;

        //스케일 납작하게
        transform.localScale = new Vector2(1f, 0.5f);

        //2초간 깔린채로 대기
        yield return new WaitForSeconds(2f);

        //스케일 복구
        transform.localScale = Vector2.one;
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
        {
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
            }
            else
            {
                damage = damage * criticalPower;
            }

            //데미지 적용
            Damage(damage, isCritical);
        }

        //넉백
        if (magicHolder.knockbackForce > 0)
        {
            if (nowAction != Action.Jump && nowAction != Action.Attack)
            {
                StartCoroutine(Knockback(magicHolder.knockbackForce));
            }
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

    public void Damage(float damage, bool isCritical)
    {
        if (enemy == null || isDead)
            return;

        //데미지 int로 바꾸기
        damage = Mathf.RoundToInt(damage);

        // 데미지 적용
        HpNow -= damage;

        //체력 범위 제한
        HpNow = Mathf.Clamp(HpNow, 0, hpMax);

        // 경직 시간 추가
        if (damage > 0)
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
        GameObject damageUI = LeanPool.Spawn(SystemManager.Instance.dmgTxtPrefab, transform.position, Quaternion.identity, SystemManager.Instance.overlayPool);
        TextMeshProUGUI dmgTxt = damageUI.GetComponent<TextMeshProUGUI>();

        // 크리티컬 떴을때 추가 강조효과 UI
        if (damage > 0)
        {
            if (isCritical)
            {
                dmgTxt.color = Color.yellow;
            }
            else
            {
                dmgTxt.color = Color.white;
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
            dmgTxt.color = new Color(0, 100f / 255f, 1);
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
            //오른쪽으로 dojump
            damageUI.transform.DOJump((Vector2)damageUI.transform.position + Vector2.right * 2f, 1f, 1, 1f)
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

    public IEnumerator Knockback(float knockbackForce)
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

    public IEnumerator Dead()
    {
        if (enemy == null)
            yield break;

        // 경직 시간 추가
        // hitCount += 1f;

        isDead = true;

        //콜라이더 끄기
        if (hitColl != null)
            hitColl.enabled = false;

        //몬스터 총 전투력 빼기
        EnemySpawn.Instance.NowEnemyPower -= enemy.grade;

        //몬스터 킬 카운트 올리기
        SystemManager.Instance.killCount++;
        UIManager.Instance.UpdateKillCount();

        if (spriteList != null)
        {
            foreach (SpriteRenderer sprite in spriteList)
            {
                // 머터리얼 및 색 변경
                sprite.material = SystemManager.Instance.hitMat;
                sprite.color = SystemManager.Instance.hitColor;

                // 색깔 점점 흰색으로
                sprite.DOColor(SystemManager.Instance.DeadColor, 1f)
                .SetEase(Ease.OutQuad);
            }

            // 폭발 반경 표시
            if (selfExplosion)
            {
                explosionTrigger.atkRangeSprite.enabled = true;
                explosionTrigger.atkRangeSprite.color = new Color(1, 1, 1, 80f / 255f);

                explosionTrigger.atkRangeSprite.DOColor(new Color(1, 0, 0, 80f / 255f), 1f)
                .SetEase(Ease.Flash, 3, 0)
                .OnComplete(() =>
                {
                    explosionTrigger.atkRangeSprite.color = new Color(1, 1, 1, 80f / 255f);
                    explosionTrigger.atkRangeSprite.enabled = false;
                });
            }

            //색 변경 완료 될때까지 대기
            yield return new WaitUntil(() => spriteList[0].color == SystemManager.Instance.DeadColor);
        }

        //전역 시간 속도가 멈춰있다면 복구될때까지 대기
        yield return new WaitUntil(() => SystemManager.Instance.globalTimeScale > 0);

        //폭발 몬스터면 폭발 시키기
        if (selfExplosion)
        {
            // 폭발 이펙트 스폰
            GameObject effect = LeanPool.Spawn(explosionTrigger.explosionPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            // enemy 데이터 넣어주기
            effect.GetComponent<EnemyManager>().enemy = enemy;
        }

        // 먼지 이펙트 생성
        GameObject dust = LeanPool.Spawn(EnemySpawn.Instance.dustPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
        dust.tag = "Enemy";
        // 2초후 디스폰
        // LeanPool.Despawn(dust, 2f);

        //혈흔 이펙트 생성
        GameObject blood = LeanPool.Spawn(EnemySpawn.Instance.bloodPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
        // 10초후 디스폰
        // LeanPool.Despawn(blood, 10f);

        if (enemy.dropRate >= Random.Range(0, 1) && nowHasItem.Count > 0)
        {
            //아이템 드랍
            DropItem();
        }

        // 몬스터 리스트에서 몬스터 본인 빼기
        EnemySpawn.Instance.spawnEnemyList.Remove(gameObject);

        // 트윈 및 시퀀스 끝내기
        transform.DOKill();

        // 몬스터 비활성화
        LeanPool.Despawn(gameObject);

        yield return null;
    }

    // 갖고있는 아이템 드랍
    void DropItem()
    {
        //보유한 모든 아이템 드랍
        foreach (var item in nowHasItem)
        {
            print(item.itemName + " : " + item.amount);
            //해당 아이템의 amount 만큼 드랍
            for (int i = 0; i < item.amount; i++)
            {
                //아이템 프리팹 찾기
                GameObject prefab = ItemDB.Instance.GetItemPrefab(item.id);
                //아이템 오브젝트 소환
                GameObject itemObj = LeanPool.Spawn(prefab, transform.position, Quaternion.identity, SystemManager.Instance.itemPool);

                //아이템 정보 넣기
                itemObj.GetComponent<ItemManager>().item = item;

                // 랜덤 방향으로 아이템 날리기
                Vector2 pos = (Vector2)transform.position + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 3f;
                itemObj.transform.DOMove(pos, 1f)
                .SetEase(Ease.OutExpo);
            }
        }

        //몬스터 죽을때 함수 호출, 체력 씨앗 드랍
        if (SystemManager.Instance.enemyDeadCallback != null)
            SystemManager.Instance.enemyDeadCallback(transform.position);
    }
}
