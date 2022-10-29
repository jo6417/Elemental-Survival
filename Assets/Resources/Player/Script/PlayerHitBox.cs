using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class PlayerHitBox : MonoBehaviour
{
    [Header("Refer")]
    PlayerManager playerManager;
    public IEnumerator hitDelayCoroutine;
    Sequence damageTextSeq; //데미지 텍스트 시퀀스
    [SerializeField] GameObject hitEffect;

    [Header("<State>")]
    float hitDelayTime = 0.2f; //피격 무적시간
    public float hitCoolCount = 0f; // 피격 무적시간 카운트
    public Vector2 knockbackDir; //넉백 벡터
    public bool isFlat; //깔려서 납작해졌을때
    public float particleHitCount = 0; // 파티클 피격 카운트
    public float hitDelayCount = 0; // 피격 딜레이 카운트
    public float stopCount = 0; // 시간 정지 카운트
    public float flatCount = 0; // 납작 디버프 카운트
    public float oppositeCount = 0; // 스포너 반대편 이동 카운트

    [Header("<Buff>")]
    public Transform buffParent; //버프 아이콘 들어가는 부모 오브젝트
    public IEnumerator hitCoroutine;
    public IEnumerator burnCoroutine = null;
    public IEnumerator poisonCoroutine = null;
    public IEnumerator bleedCoroutine = null;
    public IEnumerator slowCoroutine = null;
    public IEnumerator shockCoroutine = null;

    private void Awake()
    {
        playerManager = PlayerManager.Instance;
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (hitCoolCount > 0 || playerManager.isDash)
            return;

        //무언가 충돌되면 움직이는 방향 수정
        playerManager.Move();

        // 공격 오브젝트와 충돌 했을때
        if (other.gameObject.TryGetComponent(out Attack attack))
        {
            StartCoroutine(Hit(attack));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // print("OnTriggerEnter2D : " + other.name);

        if (hitCoolCount > 0 || playerManager.isDash)
            return;

        // 공격 오브젝트와 충돌 했을때
        if (other.TryGetComponent(out Attack attack))
        {
            StartCoroutine(Hit(attack));
        }
    }

    public IEnumerator Hit(Attack attacker)
    {
        // 피격 위치 산출
        Collider2D attackerColl = attacker.GetComponent<Collider2D>();
        Vector2 hitPos = default;
        // 콜라이더 찾으면 가까운 포인트
        if (attackerColl != null)
            hitPos = attackerColl.ClosestPoint(transform.position);
        // 콜라이더 못찾으면 본인 위치
        else
            hitPos = transform.position;

        //크리티컬 성공 여부
        bool isCritical = false;

        // 몬스터 정보 찾기, EnemyAtk 컴포넌트 활성화 되어있을때
        if (attacker.TryGetComponent(out EnemyAttack enemyAtk) && enemyAtk.enabled)
        {
            // 몬스터 정보 찾기
            Character enemyManager = enemyAtk.character;

            // 몬스터 정보 없을때, 고스트일때 리턴
            if (enemyManager == null || enemyManager.enemy == null || enemyManager.IsGhost)
            {
                Debug.Log($"enemy is null : {enemyManager.transform.position}");
                yield break;
            }

            // hitCount 갱신되었으면 리턴, 중복 피격 방지
            if (hitCoolCount > 0)
                yield break;

            // 이미 피격 딜레이 코루틴 중이었으면 취소
            if (hitDelayCoroutine != null)
                StopCoroutine(hitDelayCoroutine);

            // 데미지 적용
            Damage(enemyAtk.character.powerNow, false, hitPos);
        }

        //마법 정보 찾기
        if (attacker.TryGetComponent(out MagicHolder magicHolder))
        {
            // 마법 정보 찾기
            MagicInfo magic = magicHolder.magic;

            // 마법 정보 없으면 리턴
            if (magicHolder == null || magic == null)
            {
                print($"magic is null : {gameObject}");
                yield break;
            }

            // 목표가 미설정 되었을때
            if (magicHolder.targetType == MagicHolder.Target.None)
            {
                // print("타겟 미설정");
                yield break;
            }

            // 해당 마법이 무한관통 아니고, 관통횟수 남아있을때
            if (magicHolder.pierceCount != -1 && magicHolder.pierceCount > 0)
                // 관통 횟수 차감
                magicHolder.pierceCount--;

            // 데미지 계산
            float power = MagicDB.Instance.MagicPower(magic);
            // 크리티컬 성공 여부 계산
            isCritical = MagicDB.Instance.MagicCritical(magic);
            // 크리티컬 데미지 계산
            float criticalPower = MagicDB.Instance.MagicCriticalPower(magic);

            // 이미 피격 딜레이 코루틴 중이었으면 취소
            if (hitDelayCoroutine != null)
                StopCoroutine(hitDelayCoroutine);

            // 데미지 계산, 고정 데미지 setPower가 없으면 마법 파워로 계산
            float damage = magicHolder.fixedPower == 0 ? power : magicHolder.fixedPower;
            // 고정 데미지에 확률 계산
            damage = Random.Range(damage * 0.8f, damage * 1.2f);

            // 크리티컬이면 크리티컬 데미지 배율 반영
            if (isCritical)
            {
                // 크리티컬 파워를 곱해도 데미지가 같으면
                if (damage == damage * criticalPower)
                    // 데미지 1 상승
                    damage++;
                // 데미지가 높아진다면
                else
                    // 크리티컬 배율 곱한것으로 데미지 결정
                    damage = damage * criticalPower;
            }

            //데미지 입기
            Damage(damage, false, hitPos);
        }

        // 디버프 판단해서 적용
        Debuff(attacker, isCritical);
    }

    void HitEffect(Vector2 hitPos = default)
    {
        GameObject hitEffect = null;

        // 피격 지점이 기본값으로 들어오면, 히트박스 중심 위치로 지정
        if (hitPos == (Vector2)default)
            hitPos = transform.position;

        // 플레이어 피격 이펙트 갖고 있을때
        if (this.hitEffect != null)
            hitEffect = this.hitEffect;

        // // 공격자가 타격 이펙트 갖고 있을때
        // if (attack.atkEffect != null)
        //     hitEffect = attack.atkEffect;

        // 피격 지점에 히트 이펙트 소환
        LeanPool.Spawn(hitEffect, hitPos, Quaternion.identity, SystemManager.Instance.effectPool);
    }

    public void Debuff(Attack attacker, bool isCritical)
    {
        //시간 정지
        if (attacker.stopTime > 0)
        {
            // 경직 카운터에 stopTime 만큼 추가
            // stopCount = attacker.stopTime;

            // 해당 위치에 고정
            // enemyAI.rigid.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        //넉백
        if (attacker.knockbackForce > 0)
        {
            StartCoroutine(Knockback(attacker, attacker.knockbackForce));
        }

        // 슬로우 디버프, 크리티컬 성공일때
        if (attacker.slowTime > 0 && isCritical)
        {
            //이미 슬로우 코루틴 중이면 기존 코루틴 취소
            if (slowCoroutine != null)
                StopCoroutine(slowCoroutine);

            slowCoroutine = SlowDebuff(attacker.slowTime);

            StartCoroutine(slowCoroutine);
        }

        // 감전 디버프 && 크리티컬일때
        if (attacker.shockTime > 0 && isCritical)
        {
            //이미 감전 코루틴 중이면 기존 코루틴 취소
            if (shockCoroutine != null)
                StopCoroutine(shockCoroutine);

            shockCoroutine = ShockDebuff(attacker.shockTime);

            StartCoroutine(shockCoroutine);
        }

        // flat 디버프 있을때, flat 상태 아닐때
        if (attacker.flatTime > 0 && !isFlat)
        {
            // print("player flat");

            // 납작해지고 행동불능
            StartCoroutine(FlatDebuff(attacker.flatTime));
        }

        // 화상 피해 시간 있으면 도트 피해
        if (attacker.burnTime > 0)
        {
            //이미 화상 코루틴 중이면 기존 코루틴 취소
            if (burnCoroutine != null)
                StopCoroutine(burnCoroutine);

            burnCoroutine = BurnDotHit(1, attacker.burnTime);

            StartCoroutine(burnCoroutine);
        }

        // 포이즌 피해 시간 있으면 도트 피해
        if (attacker.poisonTime > 0)
        {
            //이미 포이즌 코루틴 중이면 기존 코루틴 취소
            if (poisonCoroutine != null)
                StopCoroutine(poisonCoroutine);

            poisonCoroutine = PoisonDotHit(1, attacker.poisonTime);

            StartCoroutine(poisonCoroutine);
        }

        // 출혈 지속시간 있으면 도트 피해
        if (attacker.bleedTime > 0)
        {
            //이미 출혈 코루틴 중이면 기존 코루틴 취소
            if (bleedCoroutine != null)
                StopCoroutine(bleedCoroutine);

            bleedCoroutine = BleedDotHit(1, attacker.bleedTime);

            StartCoroutine(bleedCoroutine);
        }
    }

    public void Damage(float damage, bool isCritical, Vector2 hitPos = default)
    {
        // 피격 이펙트 재생
        HitEffect(hitPos);

        //피격 딜레이 무적시간 시작
        hitDelayCoroutine = HitDelay(damage);
        StartCoroutine(hitDelayCoroutine);

        //! 갓모드 켜져 있으면 데미지 0
        if (SystemManager.Instance.godMod && damage > 0)
            damage = 0;

        // 회피율에 따라 데미지 0
        if (playerManager.PlayerStat_Now.evade > Random.value && damage > 0)
            damage = 0;

        //데미지 int로 바꾸기
        damage = Mathf.RoundToInt(damage);

        // 데미지 사운드 재생
        if (damage > 0)
            SoundManager.Instance.Play("Hit");
        // 회피 사운드 재생
        if (damage == 0)
            SoundManager.Instance.Play("Miss");
        // 힐 사운드 재생
        if (damage < 0)
            SoundManager.Instance.Play("Heal");

        // 데미지 적용
        playerManager.PlayerStat_Now.hpNow -= damage;

        //체력 범위 제한
        playerManager.PlayerStat_Now.hpNow = Mathf.Clamp(playerManager.PlayerStat_Now.hpNow, 0, playerManager.PlayerStat_Now.hpMax);

        //혈흔 파티클 생성
        if (damage > 0)
            LeanPool.Spawn(playerManager.bloodPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        //데미지 UI 띄우기
        StartCoroutine(DamageText(damage, false));

        UIManager.Instance.UpdateHp(); //체력 UI 업데이트

        //체력 0 이하가 되면 사망
        if (playerManager.PlayerStat_Now.hpNow <= 0)
        {
            print("Game Over");
            Dead();
        }
    }

    public IEnumerator DamageText(float damage, bool isCritical)
    {
        // 데미지 UI 띄우기
        GameObject damageUI = LeanPool.Spawn(UIManager.Instance.dmgTxtPrefab, transform.position, Quaternion.identity, SystemManager.Instance.overlayPool);
        TextMeshProUGUI dmgTxt = damageUI.GetComponent<TextMeshProUGUI>();

        // 데미지가 양수일때
        if (damage > 0)
        {
            // 크리티컬 떴을때 추가 강조효과 UI
            if (isCritical)
            {
                // dmgTxt.color = new Color(200f / 255f, 30f / 255f, 30f / 255f);
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
        // 데미지가 음수일때 (체력회복일때)
        else if (damage < 0)
        {
            dmgTxt.color = Color.green;
            dmgTxt.text = "+" + (-damage).ToString();
        }

        // 데미지 양수일때
        if (damage > 0)
            // 왼쪽으로 DOJump
            damageUI.transform.DOJump((Vector2)damageUI.transform.position - Vector2.right * 2f, 1f, 1, 1f)
            .SetEase(Ease.OutBounce);
        // 데미지 없거나 음수일때(체력회복일때)
        else
            // 위로 DoMove
            damageUI.transform.DOMove((Vector2)damageUI.transform.position + Vector2.up * 2f, 1f)
            .SetEase(Ease.OutSine);

        //제로 사이즈로 시작
        damageUI.transform.localScale = Vector3.zero;

        //원래 크기로 늘리기
        damageUI.transform.DOScale(Vector3.one, 0.5f);
        yield return new WaitForSeconds(1f);

        //줄어들어 사라지기
        damageUI.transform.DOScale(Vector3.zero, 0.5f);
        yield return new WaitForSeconds(0.5f);

        // 데미지 텍스트 디스폰
        LeanPool.Despawn(damageUI);
    }

    //HitDelay만큼 시간 지난후 피격무적시간 끝내기
    public IEnumerator HitDelay(float damage)
    {
        hitCoolCount = hitDelayTime;

        //머터리얼 변환
        playerManager.sprite.material = SystemManager.Instance.hitMat;

        //스프라이트 색 변환
        if (damage > 0)
            playerManager.sprite.color = SystemManager.Instance.hitColor;
        else
            playerManager.sprite.color = SystemManager.Instance.healColor;

        yield return new WaitUntil(() => hitCoolCount <= 0);

        //머터리얼 복구
        playerManager.sprite.material = SystemManager.Instance.spriteUnLitMat;

        //원래 색으로 복구
        playerManager.sprite.color = Color.white;

        // 코루틴 변수 초기화
        hitDelayCoroutine = null;
    }

    public IEnumerator DotHit(float tickDamage, float duration)
    {
        float damageDuration = duration;

        // 도트 데미지 지속시간이 1초 이상 남았을때
        while (damageDuration >= 1)
        {
            // 한 틱동안 대기
            yield return new WaitForSeconds(1f);

            // 도트 데미지 입히기
            Damage(tickDamage, false);

            // 남은 지속시간에서 한틱 차감
            damageDuration -= 1f;
        }
    }

    public IEnumerator BurnDotHit(float tickDamage, float duration)
    {
        // 화상 디버프 아이콘
        Transform burnEffect = null;

        // 이미 화상 디버프 중 아닐때
        if (!transform.Find(SystemManager.Instance.burnDebuffEffect.name))
        {
            // 화상 디버프 이펙트 붙이기
            burnEffect = LeanPool.Spawn(SystemManager.Instance.burnDebuffEffect, transform.position, Quaternion.identity, transform).transform;
        }

        // 도트 데미지 입히기
        yield return StartCoroutine(DotHit(tickDamage, duration));

        // 화상 이펙트 없에기
        burnEffect = transform.Find(SystemManager.Instance.burnDebuffEffect.name);
        if (burnEffect != null)
            LeanPool.Despawn(burnEffect);

        // 화상 코루틴 변수 초기화
        burnCoroutine = null;
    }

    public IEnumerator PoisonDotHit(float tickDamage, float duration)
    {
        // 포이즌 디버프 이펙트
        Transform poisonEffect = null;

        // 이미 포이즌 디버프 중 아닐때
        if (!transform.Find(SystemManager.Instance.poisonDebuffEffect.name))
        {
            //포이즌 디버프 이펙트 붙이기
            poisonEffect = LeanPool.Spawn(SystemManager.Instance.poisonDebuffEffect, transform.position, Quaternion.identity, transform).transform;
        }

        // 도트 데미지 입히기
        yield return StartCoroutine(DotHit(tickDamage, duration));

        // 포이즌 이펙트 없에기
        poisonEffect = transform.Find(SystemManager.Instance.poisonDebuffEffect.name);
        if (poisonEffect != null)
            LeanPool.Despawn(poisonEffect);

        // 포이즌 코루틴 변수 초기화
        poisonCoroutine = null;
    }

    public IEnumerator BleedDotHit(float tickDamage, float duration)
    {
        // 출혈 디버프 이펙트
        Transform bleedIcon = null;

        // 이미 출혈 디버프 중 아닐때
        if (!transform.Find(SystemManager.Instance.bleedDebuffUI.name))
        {
            //출혈 디버프 이펙트 붙이기
            bleedIcon = LeanPool.Spawn(SystemManager.Instance.bleedDebuffUI, transform.position, Quaternion.identity, buffParent).transform;
        }

        // 도트 데미지 입히기
        yield return StartCoroutine(DotHit(tickDamage, duration));

        // 출혈 아이콘 없에기
        bleedIcon = buffParent.Find(SystemManager.Instance.bleedDebuffUI.name);
        if (bleedIcon != null)
            LeanPool.Despawn(bleedIcon);

        // 코루틴 비우기
        bleedCoroutine = null;
    }

    public IEnumerator Knockback(Attack attacker, float knockbackForce)
    {
        // 플레이어 방향으로 넉백 방향 계산
        Vector2 dir = transform.position - attacker.transform.position;

        // 넉백 벡터 수정
        knockbackDir = dir.normalized * knockbackForce * 2f;

        // 넉백 벡터 반영하기
        playerManager.Move();

        yield return new WaitForSeconds(0.1f);

        //넉백 버프 빼기
        knockbackDir = Vector2.zero;

        // 넉백 벡터 반영하기
        playerManager.Move();
    }

    public IEnumerator FlatDebuff(float flatTime)
    {
        //마비됨
        isFlat = true;
        //플레이어 스프라이트 납작해짐
        playerManager.transform.localScale = new Vector2(1f, 0.5f);

        //위치 얼리기
        playerManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;

        //플레이어 멈추기
        playerManager.Move();

        // 디버프 시간동안 대기
        yield return new WaitForSeconds(flatTime);

        //마비 해제
        isFlat = false;
        //플레이어 스프라이트 복구
        playerManager.transform.localScale = Vector2.one;

        //위치 얼리기 해제
        playerManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 플레이어 움직임 다시 재생
        playerManager.Move();
    }

    public IEnumerator SlowDebuff(float slowDuration)
    {
        // 디버프량
        float slowAmount = 0.5f;
        // 슬로우 디버프 아이콘
        GameObject slowIcon = null;

        // 애니메이션 속도 저하
        playerManager.anim.speed = slowAmount;

        // 이동 속도 저하 디버프
        playerManager.speedDeBuff = slowAmount;

        // 이미 슬로우 디버프 중 아닐때
        if (!buffParent.Find(SystemManager.Instance.slowDebuffUI.name))
            //슬로우 디버프 아이콘 붙이기
            slowIcon = LeanPool.Spawn(SystemManager.Instance.slowDebuffUI, buffParent.position, Quaternion.identity, buffParent);

        // 슬로우 시간동안 대기
        yield return new WaitForSeconds(slowDuration);

        // 애니메이션 속도 초기화
        playerManager.anim.speed = 1f;

        // 이동 속도 저하 디버프 초기화
        playerManager.speedDeBuff = 1f;

        // 슬로우 아이콘 없에기
        slowIcon = buffParent.Find(SystemManager.Instance.slowDebuffUI.name).gameObject;
        if (slowIcon != null)
            LeanPool.Despawn(slowIcon);

        // 코루틴 변수 초기화
        slowCoroutine = null;
    }

    public IEnumerator ShockDebuff(float shockDuration)
    {
        // 디버프량
        float slowAmount = 0f;
        // 감전 디버프 이펙트
        GameObject shockEffect = null;

        // 애니메이션 속도 저하
        playerManager.anim.speed = slowAmount;

        // 이동 속도 저하 디버프
        playerManager.speedDeBuff = slowAmount;

        // 이미 감전 디버프 중 아닐때
        if (!playerManager.transform.Find(SystemManager.Instance.shockDebuffEffect.name))
            //감전 디버프 이펙트 붙이기
            shockEffect = LeanPool.Spawn(SystemManager.Instance.shockDebuffEffect, playerManager.transform.position, Quaternion.identity, playerManager.transform);

        // 감전 시간동안 대기
        yield return new WaitForSeconds(shockDuration);

        // 애니메이션 속도 초기화
        playerManager.anim.speed = 1f;

        // 이동 속도 저하 디버프 초기화
        playerManager.speedDeBuff = 1f;

        // 자식중에 감전 이펙트 찾기
        shockEffect = playerManager.transform.Find(SystemManager.Instance.shockDebuffEffect.name).gameObject;
        if (shockEffect != null)
            LeanPool.Despawn(shockEffect);

        // 코루틴 변수 초기화
        shockCoroutine = null;
    }

    public IEnumerator Dead()
    {
        // 시간 멈추기
        Time.timeScale = 0;

        //TODO 게임오버 UI 띄우기
        UIManager.Instance.GameOver();

        yield return null;
    }

    public void DebuffRemove()
    {
        // 이동 속도 저하 디버프 초기화
        playerManager.speedDeBuff = 1f;

        // 플랫 디버프 초기화
        flatCount = 0;
        playerManager.transform.localScale = Vector2.one;

        //슬로우 디버프 해제
        // 슬로우 아이콘 없에기
        Transform slowIcon = buffParent.Find(SystemManager.Instance.slowDebuffUI.name);
        if (slowIcon != null)
            LeanPool.Despawn(slowIcon);
        // 코루틴 변수 초기화
        slowCoroutine = null;

        // 감전 디버프 해제
        // 자식중에 감전 이펙트 찾기
        Transform shockEffect = transform.Find(SystemManager.Instance.shockDebuffEffect.name);
        if (shockEffect != null)
            LeanPool.Despawn(shockEffect);
        // 감전 코루틴 변수 초기화
        shockCoroutine = null;

        // 화상 이펙트 없에기
        Transform burnEffect = transform.Find(SystemManager.Instance.burnDebuffEffect.name);
        if (burnEffect != null)
            LeanPool.Despawn(burnEffect);
        // 화상 코루틴 변수 초기화
        burnCoroutine = null;

        // 포이즌 아이콘 없에기
        Transform poisonIcon = transform.Find(SystemManager.Instance.poisonDebuffEffect.name);
        if (poisonIcon != null)
            LeanPool.Despawn(poisonIcon);
        // 포이즌 코루틴 변수 초기화
        poisonCoroutine = null;

        // 출혈 아이콘 없에기
        Transform bleedIcon = transform.Find(SystemManager.Instance.bleedDebuffUI.name);
        if (bleedIcon != null)
            LeanPool.Despawn(bleedIcon);
        // 출혈 코루틴 변수 초기화
        bleedCoroutine = null;
    }
}
