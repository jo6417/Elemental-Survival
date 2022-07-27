using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class PlayerHitBox : MonoBehaviour
{
    [Header("<State>")]
    float hitDelayTime = 0.2f; //피격 무적시간
    public float hitCoolCount = 0f; // 피격 무적시간 카운트
    public IEnumerator hitDelayCoroutine;
    Sequence damageTextSeq; //데미지 텍스트 시퀀스

    [Header("<Buff>")]
    public Transform buffParent; // 버프 아이콘 부모 오브젝트
    public Vector2 knockbackDir; //넉백 벡터
    public bool isFlat; //깔려서 납작해졌을때
    public float poisonCoolCount; //독 도트뎀 남은시간
    public IEnumerator slowCoroutine;
    public IEnumerator shockCoroutine;

    private void OnCollisionStay2D(Collision2D other)
    {
        if (hitCoolCount > 0 || PlayerManager.Instance.isDash)
            return;

        //무언가 충돌되면 움직이는 방향 수정
        PlayerManager.Instance.Move();

        //적에게 콜라이더 충돌
        if (other.gameObject.CompareTag(SystemManager.TagNameList.Enemy.ToString()) || other.gameObject.CompareTag(SystemManager.TagNameList.Magic.ToString()))
        {
            StartCoroutine(Hit(other.transform));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // print("OnTriggerEnter2D : " + other.name);

        if (hitCoolCount > 0 || PlayerManager.Instance.isDash)
            return;

        // 적에게 트리거 충돌
        if (other.gameObject.CompareTag(SystemManager.TagNameList.Enemy.ToString()) || other.gameObject.CompareTag(SystemManager.TagNameList.Magic.ToString()))
        {
            StartCoroutine(Hit(other.transform));
        }
    }

    public IEnumerator Hit(Transform other)
    {
        // 몬스터 정보 찾기, EnemyAtk 컴포넌트 활성화 되어있을때
        if (other.TryGetComponent(out EnemyAttack enemyAtk) && enemyAtk.enabled)
        {
            // 몬스터 정보 찾기
            EnemyManager enemyManager = enemyAtk.enemyManager;

            // 몬스터 정보 없을때, 고스트일때 리턴
            if (enemyManager == null || enemyManager.enemy == null || enemyManager.IsGhost)
            {
                print($"enemy is null : {gameObject}");
                yield break;
            }

            // hitCount 갱신되었으면 리턴, 중복 피격 방지
            if (hitCoolCount > 0)
                yield break;

            // 이미 피격 딜레이 코루틴 중이었으면 취소
            if (hitDelayCoroutine != null)
                StopCoroutine(hitDelayCoroutine);

            //피격 딜레이 무적시간 시작
            hitDelayCoroutine = HitDelay();
            StartCoroutine(hitDelayCoroutine);

            yield return new WaitUntil(() => enemyAtk.enemy != null);
            EnemyInfo enemy = enemyAtk.enemy;

            Damage(enemy.power);

            // 넉백 디버프 있을때
            if (enemyAtk.knockBackDebuff)
            {
                // print("player knockback");

                //넉백 방향 벡터 계산
                Vector2 dir = (transform.position - other.position).normalized * enemy.power;

                //넉백 디버프 실행
                StartCoroutine(Knockback(dir));
            }

            // flat 디버프 있을때, 마비 상태 아닐때
            if (enemyAtk.flatTime > 0 && !isFlat)
            {
                // print("player flat");

                // 납작해지고 행동불능
                StartCoroutine(FlatDebuff(enemyAtk.flatTime));
            }
        }

        //마법 정보 찾기
        if (other.TryGetComponent(out MagicHolder magicHolder))
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

            // 마법 스탯 계산
            float power = MagicDB.Instance.MagicPower(magic);
            bool isCritical = MagicDB.Instance.MagicCritical(magic);

            // 이미 피격 딜레이 코루틴 중이었으면 취소
            if (hitDelayCoroutine != null)
                StopCoroutine(hitDelayCoroutine);

            //피격 딜레이 무적
            hitDelayCoroutine = HitDelay();
            StartCoroutine(hitDelayCoroutine);

            // 데미지 계산, 고정 데미지 setPower가 없으면 마법 파워로 계산
            float damage = magicHolder.fixedPower == 0 ? power : magicHolder.fixedPower;
            // 고정 데미지에 확률 계산
            damage = Random.Range(damage * 0.8f, damage * 1.2f);

            // 도트 피해 옵션 없을때만 데미지 (독, 화상, 출혈, 감전)
            if (magicHolder.poisonTime == 0)
                //데미지 입기
                Damage(damage);

            // 독 피해 시간 있으면 도트 피해
            if (magicHolder.poisonTime > 0)
                StartCoroutine(PoisonDotHit(damage, magicHolder.poisonTime));

            // 슬로우 디버프 시간이 있을때
            if (magicHolder.slowTime > 0)
            {
                // 디버프 성공일때, 혹은 타겟이 플레이어일때
                if (isCritical || magicHolder.targetType == MagicHolder.Target.Player)
                {
                    //이미 슬로우 코루틴 중이면 기존 코루틴 취소
                    if (slowCoroutine != null)
                        StopCoroutine(slowCoroutine);

                    slowCoroutine = SlowDebuff(magicHolder.slowTime);

                    StartCoroutine(slowCoroutine);
                }
            }

            // 감전 디버프 && 크리티컬일때
            if (magicHolder.shockTime > 0)
            {
                // 디버프 성공일때, 혹은 타겟이 플레이어일때
                if (isCritical || magicHolder.targetType == MagicHolder.Target.Player)
                {
                    //이미 감전 코루틴 중이면 기존 코루틴 취소
                    if (shockCoroutine != null)
                        StopCoroutine(shockCoroutine);

                    shockCoroutine = ShockDebuff(magicHolder.shockTime);

                    StartCoroutine(shockCoroutine);
                }
            }
        }
    }

    public bool Damage(float damage)
    {
        //! 갓모드 켜져 있으면 데미지 0
        if (PlayerManager.Instance.godMod && damage > 0)
            damage = 0;

        //데미지 int로 바꾸기
        damage = Mathf.RoundToInt(damage);

        // 데미지 적용
        PlayerManager.Instance.PlayerStat_Now.hpNow -= damage;

        //체력 범위 제한
        PlayerManager.Instance.PlayerStat_Now.hpNow = Mathf.Clamp(PlayerManager.Instance.PlayerStat_Now.hpNow, 0, PlayerManager.Instance.PlayerStat_Now.hpMax);

        //혈흔 파티클 생성
        if (damage > 0)
            LeanPool.Spawn(PlayerManager.Instance.bloodPrefab, transform.position, Quaternion.identity);

        //데미지 UI 띄우기
        DamageText(damage, false);

        UIManager.Instance.UpdateHp(); //체력 UI 업데이트

        //체력 0 이하가 되면 사망
        if (PlayerManager.Instance.PlayerStat_Now.hpNow <= 0)
        {
            print("Game Over");
            Dead();

            return true;
        }

        return false;
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
                // dmgTxt.color = new Color(200f/255f, 30f/255f, 30f/255f);
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
        // 데미지가 마이너스일때 (체력회복일때)
        else if (damage < 0)
        {
            dmgTxt.color = Color.green;
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
            //위로 살짝 올리기
            // damageUI.transform.DOMove((Vector2)damageUI.transform.position + Vector2.up * 1f, 1f)
            damageUI.transform.DOJump((Vector2)damageUI.transform.position + Vector2.left * 2f, 1f, 1, 1f)
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

    //HitDelay만큼 시간 지난후 피격무적시간 끝내기
    public IEnumerator HitDelay()
    {
        hitCoolCount = hitDelayTime;

        //머터리얼 변환
        PlayerManager.Instance.sprite.material = SystemManager.Instance.hitMat;

        //스프라이트 색 변환
        PlayerManager.Instance.sprite.color = SystemManager.Instance.hitColor;

        yield return new WaitUntil(() => hitCoolCount <= 0);

        //머터리얼 복구
        PlayerManager.Instance.sprite.material = SystemManager.Instance.spriteUnLitMat;

        //원래 색으로 복구
        PlayerManager.Instance.sprite.color = Color.white;

        // 코루틴 변수 초기화
        hitDelayCoroutine = null;
    }

    public IEnumerator PoisonDotHit(float tickDamage, float duration)
    {
        //독 데미지 지속시간 넣기
        poisonCoolCount = duration;

        // 독 데미지 지속시간 남았을때 진행
        while (poisonCoolCount > 0)
        {
            // 포이즌 머터리얼로 변환
            PlayerManager.Instance.sprite.material = SystemManager.Instance.outLineMat;

            // 보라색으로 스프라이트 색 변환
            PlayerManager.Instance.sprite.color = SystemManager.Instance.poisonColor;

            // 독 데미지 입히기
            Damage(tickDamage);

            // 한 틱동안 대기
            yield return new WaitForSeconds(1f);

            // 독 데미지 지속시간에서 한틱 차감
            poisonCoolCount -= 1f;
        }

        //원래 머터리얼로 복구
        PlayerManager.Instance.sprite.material = SystemManager.Instance.spriteLitMat;

        //원래 색으로 복구
        PlayerManager.Instance.sprite.color = Color.white;
    }

    public IEnumerator Knockback(Vector2 dir)
    {
        // 넉백 벡터 수정
        knockbackDir = dir;

        // 넉백 벡터 반영하기
        PlayerManager.Instance.Move();

        yield return new WaitForSeconds(0.5f);

        //넉백 버프 빼기
        knockbackDir = Vector2.zero;

        // 넉백 벡터 반영하기
        PlayerManager.Instance.Move();
    }

    public IEnumerator FlatDebuff(float flatTime)
    {
        //마비됨
        isFlat = true;
        //플레이어 스프라이트 납작해짐
        PlayerManager.Instance.transform.localScale = new Vector2(1f, 0.5f);

        //위치 얼리기
        PlayerManager.Instance.rigid.constraints = RigidbodyConstraints2D.FreezeAll;

        //플레이어 멈추기
        PlayerManager.Instance.Move();

        // 디버프 시간동안 대기
        yield return new WaitForSeconds(flatTime);

        //마비 해제
        isFlat = false;
        //플레이어 스프라이트 복구
        PlayerManager.Instance.transform.localScale = Vector2.one;

        //위치 얼리기 해제
        PlayerManager.Instance.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 플레이어 움직임 다시 재생
        PlayerManager.Instance.Move();
    }

    public IEnumerator SlowDebuff(float slowDuration)
    {
        // 디버프량
        float slowAmount = 0.5f;
        // 슬로우 디버프 아이콘
        GameObject slowIcon = null;

        // 애니메이션 속도 저하
        PlayerManager.Instance.anim.speed = slowAmount;

        // 이동 속도 저하 디버프
        PlayerManager.Instance.speedDeBuff = slowAmount;

        // 이미 슬로우 디버프 중 아닐때
        if (!buffParent.Find(SystemManager.Instance.slowDebuffUI.name))
            //슬로우 디버프 아이콘 붙이기
            slowIcon = LeanPool.Spawn(SystemManager.Instance.slowDebuffUI, buffParent.position, Quaternion.identity, buffParent);

        // 슬로우 시간동안 대기
        yield return new WaitForSeconds(slowDuration);

        // 애니메이션 속도 초기화
        PlayerManager.Instance.anim.speed = 1f;

        // 이동 속도 저하 디버프 초기화
        PlayerManager.Instance.speedDeBuff = 1f;

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
        PlayerManager.Instance.anim.speed = slowAmount;

        // 이동 속도 저하 디버프
        PlayerManager.Instance.speedDeBuff = slowAmount;

        // 이미 감전 디버프 중 아닐때
        if (!PlayerManager.Instance.transform.Find(SystemManager.Instance.shockDebuffEffect.name))
            //감전 디버프 이펙트 붙이기
            shockEffect = LeanPool.Spawn(SystemManager.Instance.shockDebuffEffect, PlayerManager.Instance.transform.position, Quaternion.identity, PlayerManager.Instance.transform);

        // 감전 시간동안 대기
        yield return new WaitForSeconds(shockDuration);

        // 애니메이션 속도 초기화
        PlayerManager.Instance.anim.speed = 1f;

        // 이동 속도 저하 디버프 초기화
        PlayerManager.Instance.speedDeBuff = 1f;

        // 자식중에 감전 이펙트 찾기
        shockEffect = PlayerManager.Instance.transform.Find(SystemManager.Instance.shockDebuffEffect.name).gameObject;
        if (shockEffect != null)
            LeanPool.Despawn(shockEffect);

        // 코루틴 변수 초기화
        shockCoroutine = null;
    }

    void Dead()
    {
        // 시간 멈추기
        Time.timeScale = 0;

        //TODO 게임오버 UI 띄우기
        UIManager.Instance.GameOver();
    }
}
