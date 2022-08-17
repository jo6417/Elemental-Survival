using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class EnemyHitBox : MonoBehaviour, IHitBox
{
    [Header("Refer")]
    public EnemyManager enemyManager;

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 초기화 완료시까지 대기
        yield return new WaitUntil(() => enemyManager.initialFinish);

        // 고스트 여부에 따라 히트박스 레이어 초기화
        if (enemyManager.IsGhost)
            gameObject.layer = SystemManager.Instance.layerList.PlayerHit_Layer;
        else
            gameObject.layer = SystemManager.Instance.layerList.EnemyHit_Layer;
    }

    private void OnParticleCollision(GameObject other)
    {
        // 초기화 안됬으면 리턴
        if (!enemyManager.initialFinish)
            return;

        // 파티클 피격 딜레이 중이면 리턴
        if (enemyManager.particleHitCount > 0)
            return;

        // 죽었으면 리턴
        if (enemyManager.isDead)
            return;

        // 공격 오브젝트와 충돌 했을때
        if (other.TryGetComponent(out Attack attack))
        {
            StartCoroutine(Hit(attack));

            //파티클 피격 딜레이 시작
            enemyManager.particleHitCount = 0.2f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 초기화 안됬으면 리턴
        if (!enemyManager.initialFinish)
            return;

        // 피격 딜레이 중이면 리턴
        if (enemyManager.hitCount > 0)
            return;

        // 죽었으면 리턴
        if (enemyManager.isDead)
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
        if (!enemyManager.initialFinish)
            return;

        // 죽었으면 리턴
        if (enemyManager.isDead)
            return;

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
        if (enemyManager.isDead)
            yield break;

        //크리티컬 성공 여부
        bool isCritical = false;

        // 활성화 되어있는 EnemyAtk 컴포넌트 찾기
        if (attacker.TryGetComponent<EnemyAttack>(out EnemyAttack enemyAtk) && enemyAtk.enabled)
        {
            // 공격한 몹 매니저
            EnemyManager atkEnemyManager = enemyAtk.enemyManager;

            // 공격한 몹의 정보 찾기
            yield return new WaitUntil(() => enemyAtk.enemy != null);
            EnemyInfo atkEnemy = enemyAtk.enemy;

            // other가 본인일때 리턴
            if (atkEnemyManager == this)
            {
                print(enemyManager.enemy.enemyName + " 본인 타격");
                yield break;
            }

            // 타격한 적이 비활성화 되었으면 리턴
            // if (!hitEnemyManager.enabled)
            //     return;

            // 피격 대상이 고스트일때
            if (enemyManager.IsGhost)
            {
                //고스트 아닌 적이 때렸을때만 데미지
                if (!atkEnemyManager.IsGhost)
                    Damage(atkEnemy.power, false);
            }
            // 피격 대상이 고스트 아닐때
            else
            {
                //고스트가 때렸으면 데미지
                if (atkEnemyManager.IsGhost)
                    Damage(atkEnemy.power, false);
            }
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
                float damage = magicHolder.fixedPower == 0 ? power : magicHolder.fixedPower;
                // 고정 데미지에 확률 계산
                damage = Random.Range(damage * 0.8f, damage * 1.2f);

                // 크리티컬이면 크리티컬 데미지 배율 반영
                if (isCritical)
                    damage = damage * criticalPower;

                // 도트 피해 옵션 없을때만 데미지 (독, 화상, 출혈, 감전)
                if (attacker.poisonTime == 0)
                    Damage(damage, isCritical);
            }
        }

        // 디버프 판단해서 적용
        Debuff(attacker, isCritical);
    }

    public void Debuff(Attack attacker, bool isCritical)
    {
        // 보스가 아닐때 디버프
        if (enemyManager.enemy.enemyType != EnemyDB.EnemyType.Boss.ToString())
            return;

        //시간 정지
        if (attacker.stopTime > 0)
        {
            //몬스터 경직 카운터에 stopTime 만큼 추가
            enemyManager.stopCount = attacker.stopTime;

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
            if (enemyManager.slowCoroutine != null)
                StopCoroutine(enemyManager.slowCoroutine);

            enemyManager.slowCoroutine = SlowDebuff(attacker.slowTime);

            StartCoroutine(enemyManager.slowCoroutine);
        }

        // 감전 디버프 && 크리티컬일때
        if (attacker.shockTime > 0 && isCritical)
        {
            //이미 감전 코루틴 중이면 기존 코루틴 취소
            if (enemyManager.shockCoroutine != null)
                StopCoroutine(enemyManager.shockCoroutine);

            enemyManager.shockCoroutine = ShockDebuff(attacker.shockTime);

            StartCoroutine(enemyManager.shockCoroutine);
        }

        // flat 디버프 있을때, flat 상태 아닐때
        if (attacker.flatTime > 0 && enemyManager.flatCount <= 0)
        {
            // print("player flat");

            // 납작해지고 행동불능
            StartCoroutine(FlatDebuff(attacker.flatTime));
        }

        // 포이즌 피해 시간 있으면 도트 피해
        if (attacker.poisonTime > 0)
        {
            //이미 포이즌 코루틴 중이면 기존 코루틴 취소
            if (enemyManager.poisonCoroutine != null)
                StopCoroutine(enemyManager.poisonCoroutine);

            enemyManager.poisonCoroutine = PoisonDotHit(1, attacker.poisonTime);

            StartCoroutine(enemyManager.poisonCoroutine);
        }

        // 출혈 지속시간 있으면 도트 피해
        if (attacker.bleedTime > 0)
        {
            //이미 출혈 코루틴 중이면 기존 코루틴 취소
            if (enemyManager.bleedCoroutine != null)
                StopCoroutine(enemyManager.bleedCoroutine);

            enemyManager.bleedCoroutine = BleedDotHit(1, attacker.bleedTime);

            StartCoroutine(enemyManager.bleedCoroutine);
        }
    }

    public IEnumerator HitDelay()
    {
        // Hit 상태로 변경
        enemyManager.nowState = EnemyManager.State.Hit;

        enemyManager.hitCount = enemyManager.enemy.hitDelay;

        // 히트 머터리얼 및 색으로 변경
        for (int i = 0; i < enemyManager.spriteList.Count; i++)
        {
            enemyManager.spriteList[i].material = SystemManager.Instance.hitMat;
            enemyManager.spriteList[i].color = SystemManager.Instance.hitColor;
        }

        yield return new WaitUntil(() => enemyManager.hitCount <= 0);

        // 죽었으면 복구하지않고 리턴
        if (enemyManager.isDead)
            yield break;

        // 고스트일때
        if (enemyManager.IsGhost)
            // 유령 머터리얼 및 색으로 초기화
            for (int i = 0; i < enemyManager.spriteList.Count; i++)
            {
                enemyManager.spriteList[i].material = SystemManager.Instance.outLineMat;
                enemyManager.spriteList[i].color = new Color(0, 1, 1, 0.5f);
            }
        // 일반 몹일때
        else
            // 일반몹 머터리얼 및 색으로 초기화
            for (int i = 0; i < enemyManager.spriteList.Count; i++)
            {
                enemyManager.spriteList[i].material = enemyManager.originMatList[i];
                enemyManager.spriteList[i].color = enemyManager.originColorList[i];
            }

        // 코루틴 변수 초기화
        enemyManager.hitCoroutine = null;
    }

    public void Damage(float damage, bool isCritical)
    {
        // 적 정보 없으면 리턴
        if (enemyManager == null || enemyManager.enemy == null)
            return;

        // 죽었으면 리턴
        if (enemyManager.isDead)
            return;

        //피격 딜레이 무적시간 시작
        enemyManager.hitCoroutine = HitDelay();
        StartCoroutine(enemyManager.hitCoroutine);

        //데미지 int로 바꾸기
        damage = Mathf.RoundToInt(damage);

        // 데미지 적용
        enemyManager.hpNow -= damage;

        //체력 범위 제한
        enemyManager.hpNow = Mathf.Clamp(enemyManager.hpNow, 0, enemyManager.hpMax);

        // 경직 시간 추가
        if (damage > 0)
            enemyManager.hitCount = enemyManager.enemy.hitDelay;

        //데미지 UI 띄우기
        DamageText(damage, isCritical);

        //보스면 체력 UI 띄우기
        if (enemyManager.enemy.enemyType == EnemyDB.EnemyType.Boss.ToString())
        {
            StartCoroutine(UIManager.Instance.UpdateBossHp(enemyManager));
        }

        // 몬스터 맞았을때 함수 호출 (해당 몬스터만)
        if (enemyManager.enemyHitCallback != null)
            enemyManager.enemyHitCallback();

        // print(HpNow + " / " + enemy.HpMax);
        // 체력 0 이하면 죽음
        if (enemyManager.hpNow <= 0)
        {
            // print("Dead Pos : " + transform.position);
            //죽음 시작
            Dead();
        }
    }

    public void DamageText(float damage, bool isCritical)
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
        enemyManager.damageTextSeq = DOTween.Sequence();
        enemyManager.damageTextSeq
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

    public IEnumerator PoisonDotHit(float tickDamage, float duration)
    {
        //독 데미지 지속시간 넣기
        enemyManager.poisonCoolCount = duration;

        // 포이즌 디버프 아이콘
        Transform poisonEffect = null;

        // 이미 포이즌 디버프 중 아닐때
        if (!enemyManager.transform.Find(SystemManager.Instance.poisonDebuffEffect.name))
        {
            //포이즌 디버프 이펙트 붙이기
            poisonEffect = LeanPool.Spawn(SystemManager.Instance.poisonDebuffEffect, enemyManager.transform.position, Quaternion.identity, enemyManager.transform).transform;

            // 포탈 사이즈 배율만큼 이펙트 배율 키우기
            poisonEffect.transform.localScale = Vector3.one * enemyManager.portalSize;
        }

        // 독 데미지 지속시간이 1초 이상 남았을때, 몬스터 살아있을때
        while (enemyManager.poisonCoolCount > 1 && !enemyManager.isDead)
        {
            // 한 틱동안 대기
            yield return new WaitForSeconds(1f);

            // 독 데미지 입히기
            Damage(tickDamage, false);

            // 독 데미지 지속시간에서 한틱 차감
            enemyManager.poisonCoolCount -= 1f;
        }

        // 포이즌 아이콘 없에기
        poisonEffect = enemyManager.transform.Find(SystemManager.Instance.poisonDebuffEffect.name);
        if (poisonEffect != null)
            LeanPool.Despawn(poisonEffect);

        // 포이즌 코루틴 변수 초기화
        enemyManager.poisonCoroutine = null;
    }

    public IEnumerator BleedDotHit(float tickDamage, float duration)
    {
        // 출혈 디버프 아이콘
        GameObject bleedIcon = null;

        // 이미 출혈 디버프 중 아닐때
        if (enemyManager.bleedCoolCount <= 0)
            //출혈 디버프 아이콘 붙이기
            bleedIcon = LeanPool.Spawn(SystemManager.Instance.bleedDebuffUI, enemyManager.buffParent.position, Quaternion.identity, enemyManager.buffParent);

        // 출혈 데미지 지속시간 넣기
        enemyManager.bleedCoolCount = duration;

        // 출혈 데미지 지속시간 남았을때 진행
        while (enemyManager.bleedCoolCount > 0)
        {
            // 한 틱동안 대기
            yield return new WaitForSeconds(1f);

            // 출혈 데미지 입히기
            Damage(tickDamage, false);

            // 출혈 데미지 지속시간에서 한틱 차감
            enemyManager.bleedCoolCount -= 1f;
        }

        // 출혈 아이콘 없에기
        bleedIcon = enemyManager.buffParent.Find(SystemManager.Instance.bleedDebuffUI.name).gameObject;
        if (bleedIcon != null)
            LeanPool.Despawn(bleedIcon);

        // 코루틴 비우기
        enemyManager.bleedCoroutine = null;
    }

    public IEnumerator Knockback(Attack attacker, float knockbackForce)
    {
        // 반대 방향으로 넉백 벡터
        Vector2 knockbackDir = transform.position - attacker.transform.position;
        knockbackDir = knockbackDir.normalized * knockbackForce * PlayerManager.Instance.PlayerStat_Now.knockbackForce * 2f;

        // 몬스터 위치에서 피격 반대방향 위치로 이동
        enemyManager.transform.DOMove((Vector2)enemyManager.transform.position + knockbackDir, 0.1f)
        .SetEase(Ease.OutBack);

        // print(knockbackDir);

        yield return null;
    }

    public IEnumerator FlatDebuff(float flatTime)
    {
        //정지 시간 추가
        enemyManager.flatCount = flatTime;

        //스케일 납작하게
        enemyManager.transform.localScale = new Vector2(1f, 0.5f);

        // stopCount 풀릴때까지 대기
        yield return new WaitUntil(() => enemyManager.flatCount <= 0);
        // yield return new WaitForSeconds(flatTime);

        //스케일 복구
        enemyManager.transform.localScale = Vector2.one;
    }

    public IEnumerator SlowDebuff(float slowDuration)
    {
        // 죽었으면 초기화 없이 리턴
        if (enemyManager.isDead)
            yield break;

        // 디버프량
        float slowAmount = 0.2f;
        // 슬로우 디버프 아이콘
        Transform slowIcon = null;

        // 애니메이션 속도 저하
        for (int i = 0; i < enemyManager.animList.Count; i++)
        {
            enemyManager.animList[i].speed = slowAmount;
        }
        // 이동 속도 저하 디버프
        enemyManager.enemyAI.moveSpeedDebuff = slowAmount;

        // 이미 슬로우 디버프 중 아닐때
        if (!enemyManager.buffParent.Find(SystemManager.Instance.slowDebuffUI.name))
            //슬로우 디버프 아이콘 붙이기
            slowIcon = LeanPool.Spawn(SystemManager.Instance.slowDebuffUI, enemyManager.buffParent.position, Quaternion.identity, enemyManager.buffParent).transform;

        // 슬로우 시간동안 대기
        yield return new WaitForSeconds(slowDuration);

        // 죽었으면 초기화 없이 리턴
        if (enemyManager.isDead)
            yield break;

        // 애니메이션 속도 초기화
        for (int i = 0; i < enemyManager.animList.Count; i++)
        {
            enemyManager.animList[i].speed = 1f;
        }
        // 이동 속도 저하 디버프 초기화
        enemyManager.enemyAI.moveSpeedDebuff = 1f;

        // 슬로우 아이콘 없에기
        slowIcon = enemyManager.buffParent.Find(SystemManager.Instance.slowDebuffUI.name);
        if (slowIcon != null)
            LeanPool.Despawn(slowIcon);

        // 코루틴 변수 초기화
        enemyManager.slowCoroutine = null;
    }

    public IEnumerator ShockDebuff(float shockDuration)
    {
        // 디버프량
        float slowAmount = 0f;
        // 감전 디버프 이펙트
        Transform shockEffect = null;

        // 애니메이션 속도 저하
        for (int i = 0; i < enemyManager.animList.Count; i++)
        {
            enemyManager.animList[i].speed = slowAmount;
        }

        // 이동 속도 저하 디버프
        enemyManager.enemyAI.moveSpeedDebuff = slowAmount;

        //이동 멈추기
        enemyManager.rigid.velocity = Vector2.zero;

        // 이미 감전 디버프 중 아닐때
        if (!enemyManager.transform.Find(SystemManager.Instance.shockDebuffEffect.name))
        {
            //감전 디버프 이펙트 붙이기
            shockEffect = LeanPool.Spawn(SystemManager.Instance.shockDebuffEffect, enemyManager.transform.position, Quaternion.identity, enemyManager.transform).transform;

            // 포탈 사이즈 배율만큼 이펙트 배율 키우기
            shockEffect.transform.localScale = Vector3.one * enemyManager.portalSize;
        }

        // 감전 시간동안 대기
        yield return new WaitForSeconds(shockDuration);

        // 죽었으면 초기화 없이 리턴
        if (enemyManager.isDead)
            yield break;

        // 애니메이션 속도 초기화
        for (int i = 0; i < enemyManager.animList.Count; i++)
        {
            enemyManager.animList[i].speed = 1f;
        }

        // 이동 속도 저하 디버프 초기화
        enemyManager.enemyAI.moveSpeedDebuff = 1f;

        // 자식중에 감전 이펙트 찾기
        shockEffect = enemyManager.transform.Find(SystemManager.Instance.shockDebuffEffect.name);
        if (shockEffect != null)
            LeanPool.Despawn(shockEffect);

        // 코루틴 변수 초기화
        enemyManager.shockCoroutine = null;
    }

    public void Dead()
    {
        if (enemyManager.enemy == null)
            return;

        // 경직 시간 추가
        // hitCount += 1f;
        enemyManager.nowState = EnemyManager.State.Dead;

        enemyManager.rigid.velocity = Vector2.zero; //이동 초기화

        enemyManager.isDead = true;

        // 초기화 완료 취소
        enemyManager.initialFinish = false;

        // 애니메이션 멈추기
        for (int i = 0; i < enemyManager.animList.Count; i++)
        {
            enemyManager.animList[i].speed = 0f;
        }

        // 트윈 멈추기
        transform.DOPause();

        if (enemyManager.spriteList != null)
        {
            foreach (SpriteRenderer sprite in enemyManager.spriteList)
            {
                // 머터리얼 및 색 변경
                sprite.material = SystemManager.Instance.hitMat;
                sprite.color = SystemManager.Instance.hitColor;

                // 색깔 점점 흰색으로
                sprite.DOColor(SystemManager.Instance.DeadColor, 1f)
                .SetEase(Ease.OutQuad);
            }

            // 자폭 몬스터일때
            if (enemyManager.selfExplosion)
            {
                // 폭발 반경 표시
                enemyManager.enemyAtkTrigger.atkRangeBackground.enabled = true;
                enemyManager.enemyAtkTrigger.atkRangeFill.enabled = true;

                // 폭발 반경 인디케이터 사이즈 초기화
                enemyManager.enemyAtkTrigger.atkRangeFill.transform.localScale = Vector3.zero;
                // 폭발 반경 인디케이터 사이즈 키우기
                enemyManager.enemyAtkTrigger.atkRangeFill.transform.DOScale(Vector3.one, 1f)
                .OnComplete(() =>
                {
                    enemyManager.enemyAtkTrigger.atkRangeBackground.enabled = false;
                    enemyManager.enemyAtkTrigger.atkRangeFill.enabled = false;
                });
            }

            //색 변경 완료 될때까지 대기
            // yield return new WaitUntil(() => enemyManager.spriteList[0].color == SystemManager.Instance.DeadColor);
        }

        // 고스트가 아닐때
        if (!enemyManager.IsGhost)
        {
            //몬스터 총 전투력 빼기
            EnemySpawn.Instance.NowEnemyPower -= enemyManager.enemy.grade;

            //몬스터 킬 카운트 올리기
            SystemManager.Instance.killCount++;
            UIManager.Instance.UpdateKillCount();

            //혈흔 이펙트 생성
            GameObject blood = LeanPool.Spawn(EnemySpawn.Instance.bloodPrefab, enemyManager.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            //아이템 드랍
            enemyManager.DropItem();

            // 몬스터 리스트에서 몬스터 본인 빼기
            EnemySpawn.Instance.EnemyDespawn(enemyManager);
        }

        //폭발 몬스터면 폭발 시키기
        if (enemyManager.selfExplosion)
        {
            // 폭발 이펙트 스폰
            GameObject effect = LeanPool.Spawn(enemyManager.enemyAtkTrigger.explosionPrefab, enemyManager.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            // 일단 비활성화
            effect.SetActive(false);

            // 폭발 데미지 넣기
            MagicHolder magicHolder = effect.GetComponent<MagicHolder>();
            magicHolder.fixedPower = enemyManager.enemy.power;

            // 고스트 여부에 따라 타겟 및 충돌 레이어 바꾸기
            if (enemyManager.IsGhost)
                magicHolder.SetTarget(MagicHolder.Target.Player);
            else
                magicHolder.SetTarget(MagicHolder.Target.Both);

            // 폭발 활성화
            effect.SetActive(true);
        }

        // 모든 디버프 해제
        if (enemyManager.enemy.enemyType != EnemyDB.EnemyType.Boss.ToString())
            DebuffRemove();

        // 먼지 이펙트 생성
        GameObject dust = LeanPool.Spawn(EnemySpawn.Instance.dustPrefab, enemyManager.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
        // dust.tag = "Enemy";

        // 트윈 및 시퀀스 끝내기
        enemyManager.transform.DOKill();

        // 공격 타겟 플레이어로 초기화
        enemyManager.TargetObj = PlayerManager.Instance.gameObject;

        // 몬스터 비활성화
        LeanPool.Despawn(enemyManager.gameObject);
    }

    public void DebuffRemove()
    {
        // 이동 속도 저하 디버프 초기화
        enemyManager.enemyAI.moveSpeedDebuff = 1f;

        //슬로우 디버프 해제
        // 슬로우 아이콘 없에기
        Transform slowIcon = enemyManager.buffParent.Find(SystemManager.Instance.slowDebuffUI.name);
        if (slowIcon != null)
            LeanPool.Despawn(slowIcon);
        // 코루틴 변수 초기화
        enemyManager.slowCoroutine = null;

        // 감전 디버프 해제
        // 자식중에 감전 이펙트 찾기
        Transform shockEffect = enemyManager.transform.Find(SystemManager.Instance.shockDebuffEffect.name);
        if (shockEffect != null)
            LeanPool.Despawn(shockEffect);
        // 감전 코루틴 변수 초기화
        enemyManager.shockCoroutine = null;

        // 포이즌 아이콘 없에기
        Transform poisonIcon = enemyManager.transform.Find(SystemManager.Instance.poisonDebuffEffect.name);
        if (poisonIcon != null)
            LeanPool.Despawn(poisonIcon);
        // 포이즌 코루틴 변수 초기화
        enemyManager.poisonCoroutine = null;
    }
}
