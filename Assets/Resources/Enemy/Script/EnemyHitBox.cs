using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class EnemyHitBox : MonoBehaviour
{
    [Header("Refer")]
    public EnemyManager enemyManager;
    // public EnemyInfo enemy;

    private void OnParticleCollision(GameObject other)
    {
        // 파티클 피격 딜레이 중이면 리턴
        if (enemyManager.particleHitCount > 0)
            return;

        // 마법 파티클이 충돌했을때
        if (other.transform.CompareTag(SystemManager.TagNameList.Magic.ToString()) && !enemyManager.isDead)
        {
            StartCoroutine(Hit(other.gameObject));

            //파티클 피격 딜레이 시작
            enemyManager.particleHitCount = 0.2f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 피격 딜레이 중이면 리턴
        if (enemyManager.hitCount > 0)
            return;

        // 마법이 충돌했을때
        if (other.transform.CompareTag(SystemManager.TagNameList.Magic.ToString()))
        {
            // 마법 정보 찾기
            MagicHolder magicHolder = other.GetComponent<MagicHolder>();
            MagicInfo magic = magicHolder.magic;

            StartCoroutine(Hit(other.gameObject));
        }

        //적에게 맞았을때
        if (other.transform.CompareTag(SystemManager.TagNameList.Enemy.ToString()))
        {
            // 활성화 되어있는 EnemyAtk 컴포넌트 찾기
            if (other.gameObject.TryGetComponent(out EnemyAttack enemyAtk)
            || other.gameObject.TryGetComponent(out MagicHolder magicHolder))
            {
                StartCoroutine(Hit(other.gameObject));
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 계속 마법 트리거 콜라이더 안에 있을때
        if (other.transform.CompareTag(SystemManager.TagNameList.Magic.ToString()) && enemyManager.hitCount <= 0)
        {
            // 마법 정보 찾기
            MagicHolder magicHolder = other.GetComponent<MagicHolder>();
            MagicInfo magic = magicHolder.magic;

            // 다단히트 마법일때만
            if (magic.multiHit)
                StartCoroutine(Hit(other.gameObject));
        }
    }

    public IEnumerator Hit(GameObject other)
    {
        // 죽었으면 리턴
        if (enemyManager.isDead)
            yield break;

        // 활성화 되어있는 EnemyAtk 컴포넌트 찾기
        if (other.gameObject.TryGetComponent<EnemyAttack>(out EnemyAttack enemyAtk) && enemyAtk.enabled)
        {
            // 공격한 몹 매니저
            EnemyManager atkEnemyManager = enemyAtk.enemyManager;

            // 공격한 몹의 정보 찾기
            yield return new WaitUntil(() => enemyAtk.enemy != null);
            EnemyInfo atkEnemy = enemyAtk.enemy;

            // other가 본인일때 리턴
            if (atkEnemyManager == this)
            {
                // print("본인 타격");
                yield break;
            }

            // 타격한 적이 비활성화 되었으면 리턴
            // if (!hitEnemyManager.enabled)
            //     return;

            //피격 대상이 고스트일때
            if (enemyManager.IsGhost)
            {
                //고스트 아닌 적이 때렸을때만 데미지
                if (!atkEnemyManager.IsGhost)
                    Damage(atkEnemy.power, false);
            }
            //피격 대상이 고스트 아닐때
            else
            {
                //고스트가 때렸으면 데미지
                if (atkEnemyManager.IsGhost)
                    Damage(atkEnemy.power, false);

                // 아군 피해 옵션 켜져있을때
                // if (enemyAtk.friendlyFire)
                //     Damage(atkEnemy.power, false);
            }

            // 넉백 디버프 있을때
            if (enemyAtk.knockBackDebuff)
            {
                // print("enemy knock");

                // 넉백
                StartCoroutine(Knockback(other.gameObject, atkEnemyManager.enemy.power));
            }

            // flat 디버프 있을때, stop 카운트 중 아닐때
            if (enemyAtk.flatDebuff && enemyManager.stopCount <= 0)
            {
                // print("enemy flat");

                // 납작해지고 행동불능
                StartCoroutine(FlatDebuff());
            }
        }

        //마법 정보 찾기
        if (other.TryGetComponent(out MagicHolder magicHolder))
        {
            // 마법 정보 찾기
            MagicInfo magic = magicHolder.magic;

            // 마법 파워 계산
            float power = MagicDB.Instance.MagicPower(magic);
            // 마법 지속시간 계산
            float duration = MagicDB.Instance.MagicDuration(magic);
            //크리티컬 성공 여부
            bool isCritical = MagicDB.Instance.MagicCritical(magic);
            //크리티컬 데미지 계산
            float criticalPower = MagicDB.Instance.MagicCriticalPower(magic);

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

            // print(transform.name + " : " + magic.magicName);

            // 마법 데미지가 있으면
            if (magic.power > 0)
            {
                //데미지 계산, 고정 데미지 setPower가 없으면 마법 파워로 계산
                float damage = magicHolder.setPower == 0 ? power : magicHolder.setPower;
                // 고정 데미지에 확률 계산
                damage = Random.Range(damage * 0.8f, damage * 1.2f);

                // 크리티컬 데미지 배율 반영
                damage = damage * criticalPower;

                //데미지 적용
                Damage(damage, isCritical);
            }

            //시간 정지
            if (magicHolder.isStop)
            {
                //몬스터 경직 카운터에 duration 만큼 추가
                enemyManager.stopCount = duration;

                // 해당 위치에 고정
                // enemyAI.rigid.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            //넉백
            if (magicHolder.knockbackForce > 0)
            {
                if (enemyManager.nowAction != EnemyManager.Action.Jump && enemyManager.nowAction != EnemyManager.Action.Attack)
                {
                    StartCoroutine(Knockback(other, magicHolder.knockbackForce));
                }
            }

            // 슬로우 디버프, 크리티컬 성공일때
            if (magicHolder.slowTime > 0 && isCritical)
            {
                //이미 슬로우 코루틴 중이면 기존 코루틴 취소
                if (enemyManager.slowCoroutine != null)
                    StopCoroutine(enemyManager.slowCoroutine);

                enemyManager.slowCoroutine = SlowDebuff(magicHolder.slowTime);

                StartCoroutine(enemyManager.slowCoroutine);
            }

            // 감전 디버프 && 크리티컬일때
            if (magicHolder.shockTime > 0 && isCritical)
            {
                //이미 감전 코루틴 중이면 기존 코루틴 취소
                if (enemyManager.shockCoroutine != null)
                    StopCoroutine(enemyManager.shockCoroutine);

                enemyManager.shockCoroutine = ShockDebuff(magicHolder.shockTime);

                StartCoroutine(enemyManager.shockCoroutine);
            }
        }
    }

    public IEnumerator HitDelay()
    {
        enemyManager.hitCount = enemyManager.enemy.hitDelay;

        // 머터리얼 및 색 변경
        foreach (SpriteRenderer sprite in enemyManager.spriteList)
        {
            sprite.material = SystemManager.Instance.hitMat;
            sprite.color = SystemManager.Instance.hitColor;
        }

        yield return new WaitUntil(() => enemyManager.hitCount <= 0);

        // 머터리얼 및 색 초기화
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
        //피격 딜레이 무적시간 시작
        enemyManager.hitCoroutine = HitDelay();
        StartCoroutine(enemyManager.hitCoroutine);

        if (enemyManager.enemy == null || enemyManager.isDead)
            return;

        //데미지 int로 바꾸기
        damage = Mathf.RoundToInt(damage);

        // 데미지 적용
        enemyManager.HpNow -= damage;

        //체력 범위 제한
        enemyManager.HpNow = Mathf.Clamp(enemyManager.HpNow, 0, enemyManager.hpMax);

        // 경직 시간 추가
        if (damage > 0)
            enemyManager.hitCount = enemyManager.enemy.hitDelay;

        //데미지 UI 띄우기
        DamageText(damage, isCritical);

        //보스면 체력 UI 띄우기
        if (enemyManager.enemy.enemyType == "boss")
        {
            StartCoroutine(UIManager.Instance.UpdateBossHp(enemyManager));
        }

        // 몬스터 맞았을때 함수 호출 (해당 몬스터만)
        if (enemyManager.enemyHitCallback != null)
            enemyManager.enemyHitCallback();

        // print(HpNow + " / " + enemy.HpMax);
        // 체력 0 이하면 죽음
        if (enemyManager.HpNow <= 0)
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

    public IEnumerator FlatDebuff()
    {
        //정지 시간 추가
        enemyManager.stopCount = 2f;

        //스케일 납작하게
        transform.localScale = new Vector2(1f, 0.5f);

        //위치 얼리기
        enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;

        //2초간 깔린채로 대기
        yield return new WaitForSeconds(2f);

        //스케일 복구
        transform.localScale = Vector2.one;

        //위치 얼리기
        enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public IEnumerator Knockback(GameObject attacker, float knockbackForce)
    {
        // 반대 방향 및 넉백파워
        Vector2 knockbackDir = transform.position - attacker.transform.position;
        Vector2 knockbackBuff = knockbackDir.normalized * ((knockbackForce * 0.1f) + (PlayerManager.Instance.PlayerStat_Now.knockbackForce - 1));
        knockbackDir = knockbackDir.normalized + knockbackBuff;

        // 피격 반대방향으로 이동
        transform.DOMove((Vector2)transform.position + knockbackDir, enemyManager.enemy.hitDelay)
        .SetEase(Ease.OutExpo);

        // print(knockbackDir);

        yield return null;
    }

    public IEnumerator SlowDebuff(float slowDuration)
    {
        // 디버프량
        float slowAmount = 0.2f;
        // 슬로우 디버프 아이콘
        GameObject slowIcon = null;

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
            slowIcon = LeanPool.Spawn(SystemManager.Instance.slowDebuffUI, enemyManager.buffParent.position, Quaternion.identity, enemyManager.buffParent);

        // 슬로우 시간동안 대기
        yield return new WaitForSeconds(slowDuration);

        // 애니메이션 속도 초기화
        for (int i = 0; i < enemyManager.animList.Count; i++)
        {
            enemyManager.animList[i].speed = 1f;
        }
        // 이동 속도 저하 디버프 초기화
        enemyManager.enemyAI.moveSpeedDebuff = 1f;

        // 슬로우 아이콘 없에기
        slowIcon = enemyManager.buffParent.Find(SystemManager.Instance.slowDebuffUI.name).gameObject;
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
        GameObject shockEffect = null;

        // 애니메이션 속도 저하
        for (int i = 0; i < enemyManager.animList.Count; i++)
        {
            enemyManager.animList[i].speed = slowAmount;
        }

        // 이동 속도 저하 디버프
        enemyManager.enemyAI.moveSpeedDebuff = slowAmount;

        // 이미 감전 디버프 중 아닐때
        if (!transform.Find(SystemManager.Instance.shockDebuffEffect.name))
        {
            //감전 디버프 이펙트 붙이기
            shockEffect = LeanPool.Spawn(SystemManager.Instance.shockDebuffEffect, transform.position, Quaternion.identity, transform);

            // 포탈 사이즈 배율만큼 이펙트 배율 키우기
            shockEffect.transform.localScale = Vector3.one * enemyManager.portalSize;
        }

        // 감전 시간동안 대기
        yield return new WaitForSeconds(shockDuration);

        // 애니메이션 속도 초기화
        for (int i = 0; i < enemyManager.animList.Count; i++)
        {
            enemyManager.animList[i].speed = 1f;
        }

        // 이동 속도 저하 디버프 초기화
        enemyManager.enemyAI.moveSpeedDebuff = 1f;

        // 자식중에 감전 이펙트 찾기
        shockEffect = transform.Find(SystemManager.Instance.shockDebuffEffect.name).gameObject;
        if (shockEffect != null)
            LeanPool.Despawn(shockEffect);

        // 코루틴 변수 초기화
        enemyManager.shockCoroutine = null;
    }

    public IEnumerator Dead()
    {
        if (enemyManager.enemy == null)
            yield break;

        // 경직 시간 추가
        // hitCount += 1f;

        enemyManager.isDead = true;

        //콜라이더 전부 끄기
        if (enemyManager.hitCollList.Count > 0)
            foreach (Collider2D coll in enemyManager.hitCollList)
            {
                coll.enabled = false;
            }

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
                enemyManager.explosionTrigger.atkRangeBackground.enabled = true;
                enemyManager.explosionTrigger.atkRangeFill.enabled = true;

                // 폭발 반경 인디케이터 사이즈 초기화
                enemyManager.explosionTrigger.atkRangeFill.transform.localScale = Vector3.zero;
                // 폭발 반경 인디케이터 사이즈 키우기
                enemyManager.explosionTrigger.atkRangeFill.transform.DOScale(Vector3.one, 1f)
                .OnComplete(() =>
                {
                    enemyManager.explosionTrigger.atkRangeBackground.enabled = false;
                    enemyManager.explosionTrigger.atkRangeFill.enabled = false;
                });
            }

            //색 변경 완료 될때까지 대기
            yield return new WaitUntil(() => enemyManager.spriteList[0].color == SystemManager.Instance.DeadColor);
        }

        //전역 시간 속도가 멈춰있다면 복구될때까지 대기
        yield return new WaitUntil(() => SystemManager.Instance.globalTimeScale > 0);

        // 고스트가 아닐때
        if (!enemyManager.IsGhost)
        {
            //몬스터 총 전투력 빼기
            EnemySpawn.Instance.NowEnemyPower -= enemyManager.enemy.grade;

            //몬스터 킬 카운트 올리기
            SystemManager.Instance.killCount++;
            UIManager.Instance.UpdateKillCount();

            //혈흔 이펙트 생성
            GameObject blood = LeanPool.Spawn(EnemySpawn.Instance.bloodPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            //아이템 드랍
            enemyManager.DropItem();

            // 몬스터 리스트에서 몬스터 본인 빼기
            EnemySpawn.Instance.EnemyDespawn(enemyManager);
        }
        else
        {
        }

        //폭발 몬스터면 폭발 시키기
        if (enemyManager.selfExplosion)
        {
            // 폭발 이펙트 스폰
            GameObject effect = LeanPool.Spawn(enemyManager.explosionTrigger.explosionPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            // 일단 비활성화
            effect.SetActive(false);

            // 폭발 데미지 넣기
            MagicHolder magicHolder = effect.GetComponent<MagicHolder>();
            magicHolder.setPower = enemyManager.enemy.power;

            // 고스트 여부에 따라 충돌 레이어 바꾸기
            if (enemyManager.IsGhost)
                effect.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
            else
                effect.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;

            // 폭발 활성화
            effect.SetActive(true);
        }

        // 먼지 이펙트 생성
        GameObject dust = LeanPool.Spawn(EnemySpawn.Instance.dustPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
        // dust.tag = "Enemy";

        // 트윈 및 시퀀스 끝내기
        transform.DOKill();

        // 공격 타겟 플레이어로 초기화
        enemyManager.ChangeTarget(PlayerManager.Instance.gameObject);

        // 몬스터 비활성화
        LeanPool.Despawn(enemyManager.gameObject);

        yield return null;
    }
}
