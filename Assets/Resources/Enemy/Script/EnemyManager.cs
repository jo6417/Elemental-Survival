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
    [Header("Initial")]
    public bool initialStart = false;
    public EnemyHitCallback enemyHitCallback; //해당 몬스터 죽을때 실행될 함수들
    public delegate void EnemyHitCallback();

    [SerializeField]
    private List<int> defaultHasItem = new List<int>(); //가진 아이템 기본값
    public List<ItemInfo> nowHasItem = new List<ItemInfo>(); // 현재 가진 아이템
    public EnemyManager referEnemyManager = null;
    public EnemyInfo enemy;
    public float targetResetTime = 3f; //타겟 재설정 시간
    public float targetResetCount = 0; //타겟 재설정 시간 카운트
    public GameObject targetObj; // 공격 목표
    public GameObject TargetObj
    {
        get
        {
            // 타겟이 없거나 비활성화 되어있으면
            if (targetObj == null || !targetObj.activeSelf)
            {
                // 새로운 타겟 찾기
                targetObj = SearchTarget();
            }

            return targetObj;
        }
    }

    [Header("State")]
    public State state; //현재 상태
    public enum State { Idle, Hit, Dead, TimeStop, MagicStop }
    public Action nowAction = Action.Idle; //현재 행동
    public enum Action { Idle, Walk, Jump, Attack }
    public MoveType moveType;
    public enum MoveType
    {
        Walk, // 걸어서 등속도이동
        Jump, // 시간마다 점프
        Dash, // 범위안에 들어오면 대쉬
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
    public float attackRange; // 공격범위
    public bool isGhost = false; // 마법으로 소환된 고스트 몬스터인지 여부

    [Header("Refer")]
    public EnemyAI enemyAI;
    public EnemyAtkTrigger explosionTrigger;
    public Transform spriteObj;
    public List<SpriteRenderer> spriteList = new List<SpriteRenderer>();
    public List<Material> originMatList = new List<Material>(); //변형 전 머터리얼
    public List<Color> originMatColorList = new List<Color>(); //변형 전 머터리얼 색
    public List<Color> originColorList = new List<Color>(); // 변형 전 스프라이트 색
    public List<Animator> animList = new List<Animator>();
    public Rigidbody2D rigid;
    public Collider2D physicsColl; // 물리용 콜라이더
    public List<Collider2D> hitCollList; // 히트박스용 콜라이더

    [Header("Stat")]
    public float hpMax = 0;
    public float HpNow = 0;
    public float power;
    public float speed;
    public float range;

    // [Header("Attack Effect")]
    // public bool friendlyFire = false; // 충돌시 아군 피해 여부
    // public bool flatDebuff = false; //납작해지는 디버프
    // public bool knockBackDebuff = false; //넉백 디버프

    [Header("Debug")]
    [SerializeField]
    string enemyName;
    [SerializeField]
    string enemyType;

    void Awake()
    {
        enemyAI = enemyAI == null ? transform.GetComponent<EnemyAI>() : enemyAI;

        spriteObj = spriteObj == null ? transform : spriteObj;
        rigid = rigid == null ? spriteObj.GetComponentInChildren<Rigidbody2D>(true) : rigid;
        animList = animList.Count == 0 ? GetComponentsInChildren<Animator>().ToList() : animList;

        //히트 콜라이더 없으면 EnemyHitBox로 찾아 넣기
        if (hitCollList.Count == 0)
            foreach (EnemyHitBox hitBox in GetComponentsInChildren<EnemyHitBox>())
            {
                hitCollList.Add(hitBox.GetComponent<Collider2D>());
            }

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

        //초기 타겟은 플레이어
        targetObj = PlayerManager.Instance.gameObject;
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        // 초기화 스위치 켜질때까지 대기
        yield return new WaitUntil(() => initialStart);

        // 모든 스프라이트 색깔,머터리얼 초기화
        for (int i = 0; i < spriteList.Count; i++)
        {
            spriteList[i].color = originColorList[i];
            spriteList[i].material = originMatList[i];
            // spriteList[i].material.color = originMatColorList[i];
        }

        //콜라이더 끄기
        if (hitCollList.Count > 0)
            foreach (Collider2D coll in hitCollList)
            {
                coll.enabled = false;
            }

        // rigid 초기화
        rigid.velocity = Vector3.zero;
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => EnemyDB.Instance.loadDone);

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (enemy == null && referEnemyManager == null)
            //적 정보 찾기
            enemy = EnemyDB.Instance.GetEnemyByName(transform.name.Split('_')[0]);

        //적 정보 인스턴싱, 적 오브젝트마다 따로 EnemyInfo 갖기
        if (enemy != null)
            enemy = new EnemyInfo(enemy);

        // enemy 정보 들어올때까지 대기
        yield return new WaitUntil(() => enemy != null);

        // //enemy 못찾으면 코루틴 종료
        // if (enemy == null)
        //     yield break;

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

        // 고스트일때 체력 절반으로 초기화
        if (isGhost)
            HpNow = enemy.hpMax / 2f; //체력 절반으로 초기화
        else
            HpNow = enemy.hpMax; //체력 초기화

        //죽음 여부 초기화
        isDead = false;

        // idle 상태로 전환
        state = State.Idle;

        //콜라이더 켜기
        if (hitCollList.Count > 0)
            foreach (Collider2D coll in hitCollList)
            {
                coll.enabled = true;
            }

        //! 테스트 확인용
        enemyName = enemy.enemyName;
        enemyType = enemy.enemyType;
        hpMax = enemy.hpMax;
        power = enemy.power;
        speed = enemy.speed;
        range = enemy.range;

        //보스면 체력 UI 띄우기
        if (enemy.enemyType == "boss")
        {
            StartCoroutine(UIManager.Instance.UpdateBossHp(this));
        }

        // 고스트일때
        if (isGhost)
        {
            //todo 모든 스프라이트 유령색으로
            foreach (SpriteRenderer sprite in spriteList)
            {
                sprite.color = new Color(0, 1, 1, 0.5f);
            }

            // 타겟 null로 초기화
            ChangeTarget(null);
        }

        // 초기화 완료되면 초기화 스위치 끄기
        initialStart = false;
    }

    public void ChangeTarget(GameObject newTarget)
    {
        // 타겟 리셋 타임 재설정
        targetResetCount = targetResetTime;

        // 타겟 변경하기
        targetObj = newTarget;

        // 지정 타겟이 null이면 주변 몬스터 타겟 찾아 넣기
        if (targetObj == null)
            targetObj = SearchTarget();
    }

    GameObject SearchTarget()
    {
        // 가장 가까운 적과의 거리
        float closeRange = float.PositiveInfinity;
        GameObject closeEnemy = null;

        //캐릭터 주변의 적들
        List<Collider2D> enemyCollList = Physics2D.OverlapCircleAll(transform.position, 50f, 1 << LayerMask.NameToLayer("Enemy")).ToList();

        // 주변에 아무도 없으면 리턴 
        if (enemyCollList.Count == 0)
            return null;

        // 몬스터 본인 콜라이더는 전부 빼기
        // enemyCollList.Remove(physicsColl);
        foreach (Collider2D coll in hitCollList)
        {
            enemyCollList.Remove(coll);
        }

        for (int i = 0; i < enemyCollList.Count; i++)
        {
            // 히트박스 컴포넌트가 있을때
            if (enemyCollList[i].TryGetComponent(out EnemyHitBox hitBox))
            {
                // 찾은 몬스터도 고스트일때
                if (hitBox.enemyManager.isGhost)
                    // 다음으로 진행
                    continue;
            }
            else
                // 히트박스 컴포넌트가 없을때
                continue;

            // 해당 적이 이전 거리보다 짧으면
            if (Vector2.Distance(transform.position, enemyCollList[i].transform.position) < closeRange)
            {
                // 해당 적을 타겟으로 바꾸기
                closeEnemy = enemyCollList[i].gameObject;
            }
        }

        return closeEnemy;
    }

    private void Update()
    {
        // 파티클 히트 딜레이 차감
        if (particleHitCount > 0)
        {
            particleHitCount -= Time.deltaTime;
        }

        // 고스트일때 타겟이 비활성화되거나 리셋타임이 되면 타겟 재설정
        if (isGhost && (targetResetCount <= 0 || targetObj == null))
            ChangeTarget(null);
    }

    public bool ManageState()
    {
        // 몬스터 정보 없으면 리턴
        if (enemy == null)
            return false;

        // 비활성화 되었으면 리턴
        if (!gameObject.activeSelf)
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

                // 고스트 여부에 따라 복구색 바꾸기
                if (isGhost)
                    spriteList[i].color = new Color(0, 1, 1, 0.5f);
                else
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

            // rigid 초기화
            rigid.velocity = Vector3.zero;
            rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        return true;
    }

    public IEnumerator FlatDebuff()
    {
        //정지 시간 추가
        stopCount = 2f;

        //스케일 납작하게
        transform.localScale = new Vector2(1f, 0.5f);

        //위치 얼리기
        rigid.constraints = RigidbodyConstraints2D.FreezeAll;

        //2초간 깔린채로 대기
        yield return new WaitForSeconds(2f);

        //스케일 복구
        transform.localScale = Vector2.one;

        //위치 얼리기
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void HitMagic(GameObject other)
    {
        if (isDead)
            return;

        // 마법 정보 찾기
        MagicHolder magicHolder = other.GetComponent<MagicHolder>();
        MagicInfo magic = magicHolder.magic;

        // 마법 정보 없으면 리턴
        if (magicHolder == null || magic == null)
            return;

        // 목표가 미설정 되었을때
        if (magicHolder.targetType == MagicHolder.Target.None)
        {
            print("타겟 미설정");
            return;
        }

        // 목표가 몬스터가 아니면 리턴
        if (magicHolder.targetType != MagicHolder.Target.Enemy && magicHolder.targetType != MagicHolder.Target.Both)
            return;

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
                StartCoroutine(Knockback(other, magicHolder.knockbackForce));
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
            StartCoroutine(UIManager.Instance.UpdateBossHp(this));
        }

        // 몬스터 맞았을때 함수 호출 (해당 몬스터만)
        if (enemyHitCallback != null)
            enemyHitCallback();

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

    public IEnumerator Knockback(GameObject attacker, float knockbackForce)
    {
        // 반대 방향 및 넉백파워
        Vector2 knockbackDir = transform.position - attacker.transform.position;
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
        if (hitCollList.Count > 0)
            foreach (Collider2D coll in hitCollList)
            {
                coll.enabled = false;
            }

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

            // 자폭 몬스터일때
            if (selfExplosion)
            {
                // 폭발 반경 표시
                explosionTrigger.atkRangeSprite.enabled = true;
                explosionTrigger.atkRangeSprite.color = new Color(1, 1, 1, 80f / 255f);

                // 폭발 반경 깜빡이기
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

            // 일단 비활성화
            effect.SetActive(false);

            //todo 태그 바꾸기?

            // 태그 몬스터 공격으로 바꾸기
            // effect.tag = "EnemyAttack";
            // effect.layer = LayerMask.NameToLayer("EnemyAttack");

            // Explosion 마법 인스턴스 생성
            MagicInfo magic = new MagicInfo(MagicDB.Instance.GetMagicByName("Explosion"));
            // 마법 데미지에 해당 몬스터 데미지 넣기
            magic.power = enemy.power;

            //폭발에 마법 정보 넣기
            MagicHolder effectHolder = effect.GetComponent<MagicHolder>();
            effectHolder.magic = magic;
            effectHolder.targetType = MagicHolder.Target.Both;

            // 폭발 활성화
            effect.SetActive(true);
        }

        // 고스트가 아닐때
        if (!isGhost)
        {
            //몬스터 총 전투력 빼기
            EnemySpawn.Instance.NowEnemyPower -= enemy.grade;

            //몬스터 킬 카운트 올리기
            SystemManager.Instance.killCount++;
            UIManager.Instance.UpdateKillCount();

            //혈흔 이펙트 생성
            GameObject blood = LeanPool.Spawn(EnemySpawn.Instance.bloodPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            //아이템 드랍
            DropItem();

            // 몬스터 리스트에서 몬스터 본인 빼기
            EnemySpawn.Instance.EnemyDespawn(this);
        }
        else
        {
            // 고스트 여부 초기화
            isGhost = false;
        }

        // 먼지 이펙트 생성
        GameObject dust = LeanPool.Spawn(EnemySpawn.Instance.dustPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
        // dust.tag = "Enemy";

        // 트윈 및 시퀀스 끝내기
        transform.DOKill();

        // 공격 타겟 플레이어로 초기화
        ChangeTarget(PlayerManager.Instance.gameObject);

        // 몬스터 비활성화
        LeanPool.Despawn(gameObject);

        yield return null;
    }

    // 갖고있는 아이템 드랍
    void DropItem()
    {
        //아이템 없으면 원소젬 1개 추가, 최소 젬 1개라도 떨구게
        if (nowHasItem.Count <= 0)
        {
            // 랜덤 원소젬 정보 넣기
            ItemInfo gem = ItemDB.Instance.GetItemByID(Random.Range(0, 6));
            gem.amount = 1;
            nowHasItem.Add(gem);
        }

        //보유한 모든 아이템 드랍
        foreach (ItemInfo item in nowHasItem)
        {
            // print(item.itemName + " : " + item.amount);
            //해당 아이템의 amount 만큼 드랍
            for (int i = 0; i < item.amount; i++)
            {
                //아이템 프리팹 찾기
                GameObject prefab = ItemDB.Instance.GetItemPrefab(item.id);
                //아이템 오브젝트 소환
                GameObject itemObj = LeanPool.Spawn(prefab, transform.position, Quaternion.identity, SystemManager.Instance.itemPool);

                //아이템 정보 넣기
                itemObj.GetComponent<ItemManager>().item = item;

                //아이템 리지드 찾기
                Rigidbody2D itemRigid = itemObj.GetComponent<Rigidbody2D>();

                // 랜덤 방향으로 아이템 날리기
                itemRigid.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * Random.Range(3f, 5f);

                // 아이템 랜덤 회전 시키기
                itemRigid.angularVelocity = Random.value < 0.5f ? 180f : -180f;
            }
        }
    }
}
