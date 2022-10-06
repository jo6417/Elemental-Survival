using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [Header("Refer")]
    public Character character;

    private void Awake()
    {
        // character 찾기
        character = character != null ? character : GetComponent<Character>();
    }

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 초기화 완료시까지 대기
        yield return new WaitUntil(() => character.initialFinish);

        // 고스트 여부에 따라 히트박스 레이어 초기화
        if (character.IsGhost)
            gameObject.layer = SystemManager.Instance.layerList.PlayerHit_Layer;
        else
            gameObject.layer = SystemManager.Instance.layerList.EnemyHit_Layer;
    }

    private void OnParticleCollision(GameObject other)
    {
        // 초기화 안됬으면 리턴
        if (!character.initialFinish)
            return;

        // 파티클 피격 딜레이 중이면 리턴
        if (character.particleHitCount > 0)
            return;

        // 죽었으면 리턴
        if (character.isDead)
            return;

        // 공격 오브젝트와 충돌 했을때
        if (other.TryGetComponent(out Attack attack))
        {
            StartCoroutine(Hit(attack));

            //파티클 피격 딜레이 시작
            character.particleHitCount = 0.2f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 초기화 안됬으면 리턴
        if (!character.initialFinish)
            return;

        // 피격 딜레이 중이면 리턴
        if (character.hitDelayCount > 0)
            return;

        // 죽었으면 리턴
        if (character.isDead)
            return;

        // 공격 오브젝트와 충돌 했을때
        if (other.TryGetComponent(out Attack attack))
        {
            StartCoroutine(Hit(attack));
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 초기화 안됬으면 리턴
        if (!character.initialFinish)
            return;

        // 죽었으면 리턴
        if (character.isDead)
            return;

        // 공격 오브젝트와 충돌 했을때
        if (other.TryGetComponent(out Attack attack))
            // 마법 공격 오브젝트와 충돌 했을때
            if (other.TryGetComponent(out MagicHolder magicHolder))
            {
                // 마법 정보 없으면 리턴
                if (magicHolder.magic == null)
                    return;

                // 다단히트 마법일때만
                if (magicHolder.magic.multiHit)
                    StartCoroutine(Hit(magicHolder));
            }
    }

    public IEnumerator Hit(Attack attacker)
    {
        // 죽었으면 리턴
        if (character.isDead)
            yield break;

        // 피격 위치 산출
        Collider2D attackerColl = attacker.GetComponent<Collider2D>();
        Vector2 hitPos = default;
        // 콜라이더 찾으면 가까운 포인트
        if (attackerColl != null)
            hitPos = attackerColl.ClosestPoint(transform.position);
        // 콜라이더 못찾으면 본인 위치
        else
            hitPos = transform.position;

        // 크리티컬 성공 여부
        bool isCritical = false;
        // 데미지
        float damage = 0;

        // 활성화 되어있는 EnemyAtk 컴포넌트 찾기
        if (attacker.TryGetComponent<EnemyAttack>(out EnemyAttack enemyAtk) && enemyAtk.enabled)
        {
            // 공격한 캐릭터 찾기
            Character atkCharacter = enemyAtk.character;

            // other가 본인일때 리턴
            if (atkCharacter == this)
                yield break;

            // 타격한 적이 비활성화 되었으면 리턴
            // if (!hitEnemyManager.enabled)
            //     return;

            // 고정 데미지가 있으면 아군 피격이라도 적용
            if (enemyAtk.fixedPower > 0)
            {
                Damage(enemyAtk.fixedPower, false, hitPos);
            }

            // 피격 대상이 고스트일때
            if (character.IsGhost)
            {
                //고스트 아닌 적이 때렸을때만 데미지
                if (!atkCharacter.IsGhost)
                    Damage(enemyAtk.character.powerNow, false, hitPos);
            }
            // 피격 대상이 고스트 아닐때
            else
            {
                //고스트가 때렸으면 데미지
                if (atkCharacter.IsGhost)
                    Damage(enemyAtk.character.powerNow, false, hitPos);
            }
        }
        //마법 정보 찾기
        else if (attacker.TryGetComponent(out MagicHolder magicHolder))
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

            // 마법 파워 계산
            float power = MagicDB.Instance.MagicPower(magic);
            //크리티컬 성공 여부 계산
            isCritical = MagicDB.Instance.MagicCritical(magic);
            //크리티컬 데미지 계산
            float criticalPower = MagicDB.Instance.MagicCriticalPower(magic);

            // print(transform.name + " : " + magic.magicName);

            // 데미지가 있으면
            if (power > 0)
            {
                //데미지 계산, 고정 데미지 setPower가 없으면 마법 파워로 계산
                damage = magicHolder.fixedPower > 0 ? magicHolder.fixedPower : power;
                // 고정 데미지에 확률 계산
                damage = Random.Range(damage * 0.8f, damage * 1.2f);

                // 크리티컬이면, 크리티컬 배율 반영시 기존 데미지보다 크면
                if (isCritical)
                {
                    // 크리티컬 파워를 곱해도 데미지가 같으면
                    if (damage == damage * criticalPower)
                        // 데미지 1 상승
                        damage++;
                    // 배율을 해서 데미지가 높아진다면
                    else
                        // 크리티컬 배율 곱한것으로 데미지 결정
                        damage = damage * criticalPower;
                }

                // 도트 피해 옵션 없을때만 데미지 (독, 화상, 출혈)
                if (attacker.poisonTime == 0
                && attacker.burnTime == 0
                && attacker.bleedTime == 0)
                    Damage(damage, isCritical, hitPos);
            }
        }
        // 그냥 Attack 컴포넌트일때
        else
        {
            // 고정 데미지 불러오기
            damage = attacker.fixedPower;

            // 데미지 입기
            Damage(damage, isCritical, hitPos);
        }

        // 디버프 판단해서 적용
        Debuff(attacker, isCritical, damage);

        //피격 딜레이 무적시간 시작
        character.hitCoroutine = HitDelay(damage);
        StartCoroutine(character.hitCoroutine);
    }

    void HitEffect(Vector2 hitPos = default)
    {
        GameObject hitEffect = null;

        // 피격 지점이 기본값으로 들어오면, 히트박스 중심 위치로 지정
        if (hitPos == (Vector2)default)
            hitPos = transform.position;

        // 피격대상이 피격 이펙트 갖고 있을때
        if (character.hitEffect != null)
            hitEffect = character.hitEffect;

        // // 공격자가 타격 이펙트 갖고 있을때
        // if (attack.atkEffect != null)
        //     hitEffect = attack.atkEffect;

        // 피격 지점에 히트 이펙트 소환
        LeanPool.Spawn(hitEffect, hitPos, Quaternion.identity, SystemManager.Instance.effectPool);
    }

    public void Debuff(Attack attacker, bool isCritical, float damage = 0)
    {
        //넉백
        if (attacker.knockbackForce > 0)
        {
            // 보스가 아닌 몬스터일때
            if (character.enemy != null && character.enemy.enemyType != EnemyDB.EnemyType.Boss.ToString()
            // 몬스터가 아닐때 (오브젝트일때)
            || character.enemy == null)
                StartCoroutine(Knockback(attacker, attacker.knockbackForce));
        }

        // 슬로우 디버프, 크리티컬 성공일때
        if (attacker.slowTime > 0 && isCritical)
        {
            // 보스가 아닌 몬스터일때
            if (character.enemy != null && character.enemy.enemyType != EnemyDB.EnemyType.Boss.ToString())
            {
                //이미 슬로우 코루틴 실행중이면 기존 코루틴 취소
                if (character.slowCoroutine != null)
                    StopCoroutine(character.slowCoroutine);

                character.slowCoroutine = SlowDebuff(attacker.slowTime);

                StartCoroutine(character.slowCoroutine);
            }
        }

        //시간 정지
        if (attacker.stopTime > 0)
        {
            // 보스가 아닌 몬스터일때
            if (character.enemy != null && character.enemy.enemyType != EnemyDB.EnemyType.Boss.ToString())
                //몬스터 경직 카운터에 stopTime 만큼 추가
                character.stopCount = attacker.stopTime;
        }

        // 감전 디버프 && 크리티컬일때
        if (attacker.shockTime > 0 && isCritical)
        {
            // 보스가 아닌 몬스터일때
            if (character.enemy != null && character.enemy.enemyType != EnemyDB.EnemyType.Boss.ToString())
            {
                //이미 감전 코루틴 실행중이면 기존 코루틴 취소
                if (character.shockCoroutine != null)
                    StopCoroutine(character.shockCoroutine);

                character.shockCoroutine = ShockDebuff(attacker.shockTime);

                StartCoroutine(character.shockCoroutine);
            }
        }

        // flat 디버프 있을때, flat 상태 아닐때
        if (attacker.flatTime > 0 && character.flatCount <= 0)
        {
            // 보스가 아닌 몬스터일때
            if (character.enemy != null && character.enemy.enemyType != EnemyDB.EnemyType.Boss.ToString())
                // 납작해지고 행동불능
                StartCoroutine(FlatDebuff(attacker.flatTime));
        }

        // 화상 피해 시간 있을때
        if (attacker.burnTime > 0)
        {
            //이미 화상 코루틴 실행중이면 기존 코루틴 취소
            if (character.burnCoroutine != null)
                StopCoroutine(character.burnCoroutine);

            character.burnCoroutine = BurnDebuff(damage, attacker.burnTime);

            StartCoroutine(character.burnCoroutine);
        }

        // 포이즌 피해 시간 있으면 도트 피해
        if (attacker.poisonTime > 0)
        {
            //이미 포이즌 코루틴 실행중이면 기존 코루틴 취소
            if (character.poisonCoroutine != null)
                StopCoroutine(character.poisonCoroutine);

            character.poisonCoroutine = PoisonDotHit(damage, attacker.poisonTime);

            StartCoroutine(character.poisonCoroutine);
        }

        // 출혈 지속시간 있으면 도트 피해
        if (attacker.bleedTime > 0)
        {
            //이미 출혈 코루틴 실행중이면 기존 코루틴 취소
            if (character.bleedCoroutine != null)
                StopCoroutine(character.bleedCoroutine);

            character.bleedCoroutine = BleedDotHit(damage, attacker.bleedTime);

            StartCoroutine(character.bleedCoroutine);
        }
    }

    public IEnumerator HitDelay(float damage)
    {
        // EnemyManager character = character as EnemyManager;

        // Hit 상태로 변경
        this.character.nowState = Character.State.Hit;

        // 몬스터 정보가 있을때
        if (character.enemy != null)
            character.hitDelayCount = character.enemy.hitDelay;
        else
            character.hitDelayCount = 0.2f;

        // 히트 머터리얼 및 색으로 변경
        for (int i = 0; i < this.character.spriteList.Count; i++)
        {
            this.character.spriteList[i].material = SystemManager.Instance.hitMat;

            if (damage > 0)
            {
                // 현재 체력이 max에 가까울수록 빨간색, 0에 가까울수록 흰색
                Color hitColor = Color.Lerp(SystemManager.Instance.hitColor, SystemManager.Instance.DeadColor, this.character.hpNow / this.character.hpMax);

                // 체력 비율에 따라 히트 컬러 넣기
                this.character.spriteList[i].color = hitColor;
            }
            else
                this.character.spriteList[i].color = SystemManager.Instance.healColor;
        }

        yield return new WaitUntil(() => this.character.hitDelayCount <= 0);

        // 죽었으면 복구하지않고 리턴
        if (this.character.isDead)
            yield break;

        // 초기화할 컬러, 머터리얼, 머터리얼 컬러
        Color originColor = default;
        Material originMat = null;
        Color originMatColor = default;

        // 엘리트 몹일때
        if (character.eliteClass != Character.EliteClass.None)
        {
            originMat = SystemManager.Instance.outLineMat;

            //엘리트 종류마다 다른 아웃라인 컬러 적용
            switch ((int)character.eliteClass)
            {
                case 1:
                    originMatColor = Color.green;
                    break;
                case 2:
                    originMatColor = Color.red;
                    break;
                case 3:
                    originMatColor = Color.cyan;
                    break;
                case 4:
                    break;
            }
        }
        // 고스트일때
        if (this.character.IsGhost)
        {
            originMat = SystemManager.Instance.outLineMat;
            originColor = new Color(0, 1, 1, 0.5f);
        }

        // 머터리얼 및 색 초기화
        for (int i = 0; i < this.character.spriteList.Count; i++)
        {
            this.character.spriteList[i].material = this.character.originMatList[i];
            this.character.spriteList[i].color = this.character.originColorList[i];
            this.character.spriteList[i].material.color = this.character.originMatColorList[i];
        }

        // 엘리트나 고스트 색 들어왔으면 넣기
        this.character.spriteList[0].material = originMat != null ? originMat : this.character.originMatList[0];
        this.character.spriteList[0].color = originColor != default ? originColor : this.character.originColorList[0];
        this.character.spriteList[0].material.color = originMatColor != default ? originMatColor : this.character.originMatColorList[0];

        // 코루틴 변수 초기화
        this.character.hitCoroutine = null;
    }

    public void Damage(float damage, bool isCritical, Vector2 hitPos = default)
    {
        // 적 정보 없으면 리턴
        // if (character == null || character.enemy == null)
        //     return;

        // 죽었으면 리턴
        if (character.isDead)
            return;

        // 무적 상태면 리턴
        if (character.invinsible)
            return;

        //데미지 int로 바꾸기
        damage = Mathf.RoundToInt(damage);

        // 데미지 있을때만
        if (damage > 0)
            // 피격 이펙트 재생
            HitEffect(hitPos);

        // 데미지 적용
        character.hpNow -= damage;

        //체력 범위 제한
        character.hpNow = Mathf.Clamp(character.hpNow, 0, character.hpMax);

        //데미지 UI 띄우기
        StartCoroutine(DamageText(damage, isCritical));

        //보스면 체력 UI 띄우기
        if (character.enemy != null && character.enemy.enemyType == EnemyDB.EnemyType.Boss.ToString())
        {
            StartCoroutine(UIManager.Instance.UpdateBossHp(character));
        }

        // 피격시 함수 호출 (해당 몬스터만)
        if (character.hitCallback != null)
            character.hitCallback();

        // 체력 0 이하면 죽음
        if (character.hpNow <= 0)
        {
            // print("Dead Pos : " + transform.position);
            //죽음 시작
            StartCoroutine(Dead());
        }
    }

    public IEnumerator DamageText(float damage, bool isCritical)
    {
        // 데미지 UI 띄우기
        GameObject damageUI = LeanPool.Spawn(UIManager.Instance.dmgTxtPrefab, transform.position, Quaternion.identity, SystemManager.Instance.overlayPool);
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
            dmgTxt.color = Color.green;
            dmgTxt.text = "+" + (-damage).ToString();
        }

        // 데미지 양수일때
        if (damage > 0)
            // 오른쪽으로 DOJump
            damageUI.transform.DOJump((Vector2)damageUI.transform.position + Vector2.right * 2f, 1f, 1, 0.5f)
            .SetEase(Ease.OutBounce);
        // 데미지 음수일때
        else
            // 위로 DoMove
            damageUI.transform.DOMove((Vector2)damageUI.transform.position + Vector2.up * 2f, 0.5f)
            .SetEase(Ease.OutSine);

        //제로 사이즈로 시작
        damageUI.transform.localScale = Vector3.zero;

        //원래 크기로 늘리기
        damageUI.transform.DOScale(Vector3.one, 0.5f);
        yield return new WaitForSeconds(0.8f);

        //줄어들어 사라지기
        damageUI.transform.DOScale(Vector3.zero, 0.2f);
        yield return new WaitForSeconds(0.2f);

        // 데미지 텍스트 디스폰
        LeanPool.Despawn(damageUI);
    }

    public IEnumerator DotHit(float tickDamage, float duration)
    {
        float damageDuration = duration;

        // 도트 데미지 지속시간이 1초 이상 남았을때, 몬스터 살아있을때
        while (damageDuration >= 1 && !character.isDead)
        {
            // 한 틱동안 대기
            yield return new WaitForSeconds(1f);

            // 도트 데미지 입히기
            Damage(tickDamage, false);

            // 남은 지속시간에서 한틱 차감
            damageDuration -= 1f;
        }
    }

    public IEnumerator BurnDebuff(float tickDamage, float duration)
    {
        // 화상 디버프 아이콘
        Transform burnEffect = null;

        // 해당 디버프 아이콘이 없을때
        if (!character.transform.Find(SystemManager.Instance.burnDebuffEffect.name))
        {
            // 화상 디버프 이펙트 붙이기
            burnEffect = LeanPool.Spawn(SystemManager.Instance.burnDebuffEffect, character.transform.position, Quaternion.identity, character.transform).transform;

            // 포탈 사이즈 배율만큼 이펙트 배율 키우기
            burnEffect.transform.localScale = Vector3.one * character.portalSize;
        }

        // 도트 데미지 입히기
        yield return StartCoroutine(DotHit(tickDamage, duration));

        // 화상 이펙트 없에기
        burnEffect = character.transform.Find(SystemManager.Instance.burnDebuffEffect.name);
        if (burnEffect != null)
            LeanPool.Despawn(burnEffect);

        // 화상 코루틴 변수 초기화
        character.burnCoroutine = null;
    }

    public IEnumerator PoisonDotHit(float tickDamage, float duration)
    {
        // 포이즌 디버프 이펙트
        Transform poisonEffect = null;

        // 해당 디버프 아이콘이 없을때
        if (!character.transform.Find(SystemManager.Instance.poisonDebuffEffect.name))
        {
            //포이즌 디버프 이펙트 붙이기
            poisonEffect = LeanPool.Spawn(SystemManager.Instance.poisonDebuffEffect, character.transform.position, Quaternion.identity, character.transform).transform;

            // 포탈 사이즈 배율만큼 이펙트 배율 키우기
            poisonEffect.transform.localScale = Vector3.one * character.portalSize;
        }

        // 도트 데미지 입히기
        yield return StartCoroutine(DotHit(tickDamage, duration));

        // 포이즌 이펙트 없에기
        poisonEffect = character.transform.Find(SystemManager.Instance.poisonDebuffEffect.name);
        if (poisonEffect != null)
            LeanPool.Despawn(poisonEffect);

        // 포이즌 코루틴 변수 초기화
        character.poisonCoroutine = null;
    }

    public IEnumerator BleedDotHit(float tickDamage, float duration)
    {
        // 출혈 디버프 아이콘
        GameObject bleedIcon = null;

        // 해당 디버프 아이콘이 없을때
        if (!character.transform.Find(SystemManager.Instance.bleedDebuffUI.name))
        {
            //출혈 디버프 이펙트 붙이기
            bleedIcon = LeanPool.Spawn(SystemManager.Instance.bleedDebuffUI, character.buffParent.position, Quaternion.identity, character.buffParent);

            // 포탈 사이즈 배율만큼 이펙트 배율 키우기
            bleedIcon.transform.localScale = Vector3.one * character.portalSize;
        }

        // 도트 데미지 입히기
        yield return StartCoroutine(DotHit(tickDamage, duration));

        // 출혈 아이콘 없에기
        bleedIcon = character.buffParent.Find(SystemManager.Instance.bleedDebuffUI.name).gameObject;
        if (bleedIcon != null)
            LeanPool.Despawn(bleedIcon);

        // 코루틴 비우기
        character.bleedCoroutine = null;
    }

    public IEnumerator Knockback(Attack attacker, float knockbackForce)
    {
        // 반대 방향으로 넉백 벡터
        Vector2 knockbackDir = transform.position - attacker.transform.position;
        knockbackDir = knockbackDir.normalized * knockbackForce * PlayerManager.Instance.PlayerStat_Now.knockbackForce;

        // 몬스터 위치에서 피격 반대방향 위치로 이동
        character.transform.DOMove((Vector2)character.transform.position + knockbackDir, 0.1f)
        .SetEase(Ease.OutBack);

        // print(knockbackDir);

        yield return null;
    }

    public IEnumerator SlowDebuff(float slowDuration)
    {
        // 죽었으면 초기화 없이 리턴
        if (character.isDead)
            yield break;

        // 디버프량
        float slowAmount = 0.2f;
        // 슬로우 디버프 아이콘
        Transform slowIcon = null;

        // 애니메이션 속도 저하
        for (int i = 0; i < character.animList.Count; i++)
        {
            character.animList[i].speed = slowAmount;
        }
        // 이동 속도 저하 디버프
        character.moveSpeedDebuff = slowAmount;

        // 이미 슬로우 디버프 중 아닐때
        if (!character.buffParent.Find(SystemManager.Instance.slowDebuffUI.name))
            //슬로우 디버프 아이콘 붙이기
            slowIcon = LeanPool.Spawn(SystemManager.Instance.slowDebuffUI, character.buffParent.position, Quaternion.identity, character.buffParent).transform;

        // 슬로우 시간동안 대기
        yield return new WaitForSeconds(slowDuration);

        // 죽었으면 초기화 없이 리턴
        if (character.isDead)
            yield break;

        // 애니메이션 속도 초기화
        for (int i = 0; i < character.animList.Count; i++)
        {
            character.animList[i].speed = 1f;
        }
        // 이동 속도 저하 디버프 초기화
        character.moveSpeedDebuff = 1f;

        // 슬로우 아이콘 없에기
        slowIcon = character.buffParent.Find(SystemManager.Instance.slowDebuffUI.name);
        if (slowIcon != null)
            LeanPool.Despawn(slowIcon);

        // 코루틴 변수 초기화
        character.slowCoroutine = null;
    }

    public IEnumerator FlatDebuff(float flatTime)
    {
        //정지 시간 추가
        character.flatCount = flatTime;

        //스케일 납작하게
        character.transform.localScale = new Vector2(1f, 0.5f);

        // stopCount 풀릴때까지 대기
        yield return new WaitUntil(() => character.flatCount <= 0);
        // yield return new WaitForSeconds(flatTime);

        //스케일 복구
        character.transform.localScale = Vector2.one;
    }

    public IEnumerator ShockDebuff(float shockDuration)
    {
        // 디버프량
        float slowAmount = 0f;
        // 감전 디버프 이펙트
        Transform shockEffect = null;

        // 애니메이션 속도 저하
        for (int i = 0; i < character.animList.Count; i++)
        {
            character.animList[i].speed = slowAmount;
        }

        // 이동 속도 저하 디버프
        character.moveSpeedDebuff = slowAmount;

        //이동 멈추기
        character.rigid.velocity = Vector2.zero;

        // 이미 감전 디버프 중 아닐때
        if (!character.transform.Find(SystemManager.Instance.shockDebuffEffect.name))
        {
            //감전 디버프 이펙트 붙이기
            shockEffect = LeanPool.Spawn(SystemManager.Instance.shockDebuffEffect, character.transform.position, Quaternion.identity, character.transform).transform;

            // 포탈 사이즈 배율만큼 이펙트 배율 키우기
            shockEffect.transform.localScale = Vector3.one * character.portalSize;
        }

        // 감전 시간동안 대기
        yield return new WaitForSeconds(shockDuration);

        // 죽었으면 초기화 없이 리턴
        if (character.isDead)
            yield break;

        // 애니메이션 속도 초기화
        for (int i = 0; i < character.animList.Count; i++)
        {
            character.animList[i].speed = 1f;
        }

        // 이동 속도 저하 디버프 초기화
        character.moveSpeedDebuff = 1f;

        // 자식중에 감전 이펙트 찾기
        shockEffect = character.transform.Find(SystemManager.Instance.shockDebuffEffect.name);
        if (shockEffect != null)
            LeanPool.Despawn(shockEffect);

        // 코루틴 변수 초기화
        character.shockCoroutine = null;
    }

    public IEnumerator Dead()
    {
        // if (character.enemy == null)
        //     yield break;

        // 경직 시간 추가
        // hitCount += 1f;
        character.nowState = Character.State.Dead;

        // 몬스터 정보가 있을때
        if (character.enemy != null)
        {
            //이동 초기화
            character.rigid.velocity = Vector2.zero;

            // 물리 콜라이더 끄기
            character.physicsColl.enabled = false;
        }

        // 죽음 여부 초기화
        character.isDead = true;

        // 초기화 완료 취소
        character.initialFinish = false;

        // 애니메이션 멈추기
        for (int i = 0; i < character.animList.Count; i++)
        {
            character.animList[i].speed = 0f;
        }

        // 힐 범위 오브젝트가 있을때 디스폰
        if (character.healRange != null)
            LeanPool.Despawn(character.healRange.gameObject);

        // 트윈 멈추기
        transform.DOPause();

        foreach (SpriteRenderer sprite in character.spriteList)
        {
            // 머터리얼 및 색 변경
            sprite.material = SystemManager.Instance.hitMat;
            sprite.color = SystemManager.Instance.hitColor;

            // 색깔 점점 흰색으로
            sprite.DOColor(SystemManager.Instance.DeadColor, 1f)
            .SetEase(Ease.OutQuad);
        }

        // 자폭 몬스터일때
        if (character.selfExplosion)
        {
            // 폭발 반경 표시
            character.enemyAtkTrigger.atkRangeBackground.enabled = true;
            character.enemyAtkTrigger.atkRangeFill.enabled = true;

            // 폭발 반경 인디케이터 사이즈 초기화
            character.enemyAtkTrigger.atkRangeFill.transform.localScale = Vector3.zero;
            // 폭발 반경 인디케이터 사이즈 키우기
            character.enemyAtkTrigger.atkRangeFill.transform.DOScale(Vector3.one, 1f)
            .OnComplete(() =>
            {
                character.enemyAtkTrigger.atkRangeBackground.enabled = false;
                character.enemyAtkTrigger.atkRangeFill.enabled = false;
            });
        }

        // 흰색으로 변하는 시간 대기
        yield return new WaitForSeconds(1f);

        // 고스트가 아닐때
        if (!character.IsGhost)
        {
            // 몬스터 정보가 있을때
            if (character.enemy != null)
            {
                //몬스터 총 전투력 빼기
                WorldSpawner.Instance.NowEnemyPower -= character.enemy.grade;

                //몬스터 킬 카운트 올리기
                SystemManager.Instance.killCount++;
                UIManager.Instance.UpdateKillCount();

                //혈흔 이펙트 생성
                GameObject blood = LeanPool.Spawn(WorldSpawner.Instance.bloodPrefab, character.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
            }

            //아이템 드랍
            character.DropItem();

            // 몬스터 리스트에서 몬스터 본인 빼기
            WorldSpawner.Instance.EnemyDespawn(character);
        }

        //폭발 몬스터면 폭발 시키기
        if (character.selfExplosion)
        {
            // 폭발 이펙트 스폰
            GameObject effect = LeanPool.Spawn(character.enemyAtkTrigger.explosionPrefab, character.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            // 일단 비활성화
            effect.SetActive(false);

            // 폭발 데미지 넣기
            MagicHolder magicHolder = effect.GetComponent<MagicHolder>();
            // 몬스터 정보가 있을때
            if (character.enemy != null)
                magicHolder.fixedPower = character.enemy.power;

            // 고스트 여부에 따라 타겟 및 충돌 레이어 바꾸기
            if (character.IsGhost)
                magicHolder.SetTarget(MagicHolder.Target.Player);
            else
                magicHolder.SetTarget(MagicHolder.Target.Both);

            // 폭발 활성화
            effect.SetActive(true);
        }

        // 모든 디버프 해제
        DebuffRemove();

        // 먼지 이펙트 생성
        GameObject dust = LeanPool.Spawn(WorldSpawner.Instance.dustPrefab, character.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
        // dust.tag = "Enemy";

        // 트윈 및 시퀀스 끝내기
        character.transform.DOKill();

        // 공격 타겟 플레이어로 초기화
        character.TargetObj = PlayerManager.Instance.gameObject;

        // 몬스터 비활성화
        LeanPool.Despawn(character.gameObject);
    }

    public void DebuffRemove()
    {
        // 이동 속도 저하 디버프 초기화
        character.moveSpeedDebuff = 1f;

        // 플랫 디버프 초기화
        character.flatCount = 0f;
        //스케일 복구
        character.transform.localScale = Vector2.one;

        //슬로우 디버프 해제
        // 슬로우 아이콘 없에기
        Transform slowIcon = character.buffParent.Find(SystemManager.Instance.slowDebuffUI.name);
        if (slowIcon != null)
            LeanPool.Despawn(slowIcon);
        // 코루틴 변수 초기화
        character.slowCoroutine = null;

        // 감전 디버프 해제
        // 자식중에 감전 이펙트 찾기
        Transform shockEffect = character.transform.Find(SystemManager.Instance.shockDebuffEffect.name);
        if (shockEffect != null)
            LeanPool.Despawn(shockEffect);
        // 감전 코루틴 변수 초기화
        character.shockCoroutine = null;

        #region DotHit

        // 화상 이펙트 없에기
        Transform burnEffect = character.transform.Find(SystemManager.Instance.burnDebuffEffect.name);
        if (burnEffect != null)
            LeanPool.Despawn(burnEffect);
        // 화상 코루틴 변수 초기화
        character.burnCoroutine = null;

        // 포이즌 이펙트 없에기
        Transform poisonIcon = character.transform.Find(SystemManager.Instance.poisonDebuffEffect.name);
        if (poisonIcon != null)
            LeanPool.Despawn(poisonIcon);
        // 포이즌 코루틴 변수 초기화
        character.poisonCoroutine = null;

        // 출혈 이펙트 없에기
        Transform bleedIcon = character.transform.Find(SystemManager.Instance.bleedDebuffUI.name);
        if (bleedIcon != null)
            LeanPool.Despawn(bleedIcon);
        // 출혈 코루틴 변수 초기화
        character.bleedCoroutine = null;

        #endregion
    }
}
