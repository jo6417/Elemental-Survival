using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class PlayerHitBox : HitBox
{
    [Header("Refer")]
    PlayerManager playerManager;
    public IEnumerator hitDelayCoroutine;
    [SerializeField] GameObject hitEffect;
    [SerializeField] GameObject healEffect;
    [SerializeField] GameObject deathEffect;
    [SerializeField] AudioSource deadAudio; // 사망시 사운드

    [Header("<State>")]
    // float hitDelayTime = 0.2f; //피격 무적시간
    // public float hitCoolCount = 0f; // 피격 무적시간 카운트
    public Vector2 knockbackDir; //넉백 벡터
    // public bool isFlat; //깔려서 납작해졌을때
    // public float particleHitCount = 0; // 파티클 피격 카운트
    // public float stopCount = 0; // 시간 정지 카운트
    // public float flatCount = 0; // 납작 디버프 카운트
    // public float oppositeCount = 0; // 스포너 반대편 이동 카운트

    [Header("<Buff>")]
    public Transform buffParentObj; //버프 아이콘 들어가는 부모 오브젝트
    public IEnumerator hitCoroutine;
    public IEnumerator burnCoroutine = null;
    public IEnumerator poisonCoroutine = null;
    public IEnumerator bleedCoroutine = null;
    public IEnumerator slowCoroutine = null;
    public IEnumerator shockCoroutine = null;

    private void Awake()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => PlayerManager.Instance != null);

        playerManager = PlayerManager.Instance;

        // 죽음 이펙트 끄기
        deathEffect.SetActive(false);
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (character.hitDelayCount > 0 || playerManager.isDash)
            return;

        //무언가 충돌되면 움직이는 방향 수정
        playerManager.Move();

        // 공격 오브젝트와 충돌 했을때
        Attack attack = other.transform.GetComponent<Attack>();
        if (attack != null && attack.enabled)
        {
            StartCoroutine(Hit(attack));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // print("OnTriggerEnter2D : " + other.name);

        if (character.hitDelayCount > 0 || playerManager.isDash)
            return;

        // 공격 오브젝트와 충돌 했을때
        Attack attack = other.GetComponent<Attack>();
        if (attack != null && attack.enabled)
        {
            StartCoroutine(Hit(attack));
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        // print("OnParticleCollision : " + other.name);

        if (character.hitDelayCount > 0 || playerManager.isDash)
            return;

        // 공격 오브젝트와 충돌 했을때
        Attack attack = other.GetComponent<Attack>();
        if (attack != null && attack.enabled)
        {
            StartCoroutine(Hit(attack));
        }
    }

    public override IEnumerator Hit(Attack attack)
    {
        // 피격 위치 산출
        Vector2 hitPos = default;
        // 콜라이더 찾으면 가까운 포인트
        if (attack.atkColl != null)
            hitPos = attack.atkColl.ClosestPoint(transform.position);
        // 콜라이더 못찾으면 본인 위치
        else
            hitPos = transform.position;

        //크리티컬 성공 여부
        bool isCritical = false;
        // 데미지
        float damage = 0;

        // 몬스터 정보 찾기, EnemyAtk 컴포넌트 활성화 되어있을때
        if (attack.TryGetComponent(out EnemyAttack enemyAtk) && enemyAtk.enabled)
        {
            // 몬스터 정보 찾기
            Character character = enemyAtk.character;

            // 몬스터 정보 없을때, 고스트일때 리턴
            if (character == null || character.enemy == null || character.IsGhost)
            {
                Debug.Log($"enemy is null");
                yield break;
            }

            // hitCount 갱신되었으면 리턴, 중복 피격 방지
            if (character.hitDelayCount > 0)
                yield break;

            // 데미지 적용
            Damage(enemyAtk.character.powerNow, false, hitPos);
        }
        //마법 정보 찾기
        else if (attack.TryGetComponent(out MagicHolder magicHolder))
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
            if (magicHolder.targetType == MagicHolder.TargetType.None)
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

            // 데미지 계산, 고정 데미지 setPower가 없으면 마법 파워로 계산
            damage = magicHolder.power != 0 ? magicHolder.power : power;
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
        // 그냥 Attack 컴포넌트일때
        else
        {
            // 고정 데미지 불러오기
            damage = attack.power;

            // 데미지 입기
            Damage(damage, isCritical, hitPos);
        }

        // 디버프 판단해서 적용
        AfterEffect(attack, isCritical, damage);
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
        LeanPool.Spawn(hitEffect, hitPos, Quaternion.identity, ObjectPool.Instance.effectPool);
    }

    // public void AfterEffect(Attack attack, bool isCritical, float damage = 0)
    // {
    //     //시간 정지
    //     if (attack.stopTime > 0)
    //     {
    //         // 경직 카운터에 stopTime 만큼 추가
    //         // stopCount = attacker.stopTime;

    //         // 해당 위치에 고정
    //         // enemyAI.rigid.constraints = RigidbodyConstraints2D.FreezeAll;
    //     }

    //     //넉백
    //     if (attack.knockbackForce > 0)
    //     {
    //         StartCoroutine(Knockback(attack, attack.knockbackForce));
    //     }

    //     // 슬로우
    //     if (attack.slowTime > 0)
    //     {
    //         //이미 슬로우 코루틴 중이면 기존 코루틴 취소
    //         if (slowCoroutine != null)
    //             StopCoroutine(slowCoroutine);

    //         // slowCoroutine = SlowDebuff(attack.slowTime);
    //         slowCoroutine = character.characterStat.BuffCoroutine(
    //                Debuff.Slow.ToString(), nameof(character.characterStat.speed), true, 0.2f, attack.slowTime,
    //                character.buffParent, SystemManager.Instance.slowDebuffUI, slowCoroutine);

    //         StartCoroutine(slowCoroutine);
    //     }

    //     // 감전 디버프 && 크리티컬일때
    //     if (attack.shockTime > 0)
    //     {
    //         //이미 감전 코루틴 중이면 기존 코루틴 취소
    //         if (shockCoroutine != null)
    //             StopCoroutine(shockCoroutine);

    //         // shockCoroutine = ShockDebuff(attack.shockTime);
    //         shockCoroutine = character.characterStat.BuffCoroutine(
    //              Debuff.Slow.ToString(), nameof(character.characterStat.speed), true, 0.2f, attack.slowTime,
    //              character.buffParent, SystemManager.Instance.slowDebuffUI, shockCoroutine);

    //         StartCoroutine(shockCoroutine);
    //     }

    //     // flat 디버프 있을때, flat 상태 아닐때
    //     if (attack.flatTime > 0 && !isFlat)
    //     {
    //         // print("player flat");

    //         // 납작해지고 행동불능
    //         StartCoroutine(FlatDebuff(attack.flatTime));
    //     }

    //     // 화상 피해 시간 있을때
    //     if (attack.burnTime > 0)
    //     {
    //         // 도트 데미지 실행
    //         DotHit(damage, isCritical, attack.burnTime, transform,
    //         SystemManager.Instance.burnDebuffEffect, Debuff.Burn);
    //     }

    //     // 포이즌 피해 시간 있으면 도트 피해
    //     if (attack.poisonTime > 0)
    //     {
    //         // 도트 데미지 실행
    //         DotHit(damage, isCritical, attack.poisonTime, transform,
    //         SystemManager.Instance.poisonDebuffEffect, Debuff.Poison);
    //     }

    //     // 출혈 지속시간 있으면 도트 피해
    //     if (attack.bleedTime > 0)
    //     {
    //         // 도트 데미지 실행
    //         DotHit(damage, isCritical, attack.bleedTime, buffParentObj,
    //         SystemManager.Instance.bleedDebuffUI, Debuff.Bleed);
    //     }
    // }

    public override void Damage(float damage, bool isCritical, Vector2 hitPos = default)
    {
        // //! 갓모드 켜져 있으면 데미지 0
        // if (SystemManager.Instance.godMod && damage > 0)
        //     damage = 0;

        //데미지 int로 바꾸기
        damage = Mathf.RoundToInt(damage);

        // 이미 피격 딜레이 코루틴 중이었으면 취소
        if (hitDelayCoroutine != null)
            StopCoroutine(hitDelayCoroutine);

        // 데미지 0 아닐때
        if (damage != 0)
        {
            //피격 딜레이 무적시간 시작
            hitDelayCoroutine = HitDelay(damage);
            StartCoroutine(hitDelayCoroutine);
        }

        // 무적 상태일때, 방어
        if (character.invinsible)
        {
            // 데미지 Block 
            UIManager.Instance.DamageUI(UIManager.DamageType.Block, damage, isCritical, hitPos);

            // 데미지 제로
            damage = 0;
        }
        // 회피 성공했을때 or 데미지가 0일때
        else if (damage == 0 || playerManager.characterStat.evade > Random.value)
        {
            // 데미지 표시
            UIManager.Instance.DamageUI(UIManager.DamageType.Miss, damage, isCritical, hitPos);

            SoundManager.Instance.PlaySound("Miss");
        }
        // 데미지 양수일때, 피격
        else if (damage > 0)
        {
            // 데미지 표시
            UIManager.Instance.DamageUI(UIManager.DamageType.Damaged, damage, isCritical, hitPos);

            // 혈흔 파티클 생성
            LeanPool.Spawn(playerManager.bloodPrefab, transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

            // 데미지 사운드 재생
            SoundManager.Instance.PlaySound("Hit");

            // 피격 이펙트 재생
            HitEffect(hitPos);
        }
        // 데미지 음수일때, 회복
        else if (damage < 0)
        {
            // 데미지 표시
            UIManager.Instance.DamageUI(UIManager.DamageType.Heal, damage, isCritical, hitPos);

            // 힐 사운드 재생
            SoundManager.Instance.PlaySound("Heal");

            // 힐 이펙트 생성
            LeanPool.Spawn(healEffect, transform.position, Quaternion.identity, transform);
        }

        // 데미지 적용
        playerManager.characterStat.hpNow -= damage;
        // 체력 범위 제한
        playerManager.characterStat.hpNow = Mathf.Clamp(playerManager.characterStat.hpNow, 0, playerManager.characterStat.hpMax);

        UIManager.Instance.UpdateHp(); //체력 UI 업데이트

        //체력 0 이하가 되면 사망
        if (playerManager.characterStat.hpNow <= 0)
        {
            print("Game Over");
            StartCoroutine(Dead());
        }
    }

    public override IEnumerator Knockback(Attack attacker, float knockbackForce)
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

    public override IEnumerator FlatDebuff(float flatTime)
    {
        // //마비됨
        // isFlat = true;
        //플레이어 스프라이트 납작해짐
        playerManager.transform.localScale = new Vector2(1f, 0.5f);

        //위치 얼리기
        // playerManager.rigid.constraints = RigidbodyConstraints2D.FreezeAll;

        //플레이어 멈추기
        playerManager.Move();

        // 디버프 시간동안 대기
        yield return new WaitForSeconds(flatTime);

        // //마비 해제
        // isFlat = false;
        //플레이어 스프라이트 복구
        playerManager.transform.localScale = Vector2.one;

        //위치 얼리기 해제
        // playerManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 플레이어 움직임 다시 재생
        playerManager.Move();
    }

    public IEnumerator Dead()
    {
        // 히트 딜레이 코루틴 끄기
        StopCoroutine(hitDelayCoroutine);

        // 플레이어 조작 끄기
        playerManager.player_Input.Disable();

        // 플레이어 충돌 콜라이더 끄기
        playerManager.coll.enabled = false;
        // 플레이어 히트 콜라이더 끄기
        Collider2D[] hitColls = GetComponents<Collider2D>();
        for (int i = 0; i < hitColls.Length; i++)
            hitColls[i].enabled = false;

        //todo 핸드폰이 플레이어에게 가면서
        //todo 핸드폰 천천히 하얗게 변함
        //todo 핸드폰에서 새어나오는 빛줄기 파티클 이펙트

        // 타임스케일 멈추는 시간
        float stopTime = 3f;

        // // 플레이어 머터리얼 변환
        // playerManager.playerSprite.material = SystemManager.Instance.hitMat;

        // 플레이어 하얗게 변환
        // playerManager.playerSprite.material.color = SystemManager.Instance.hitColor;
        playerManager.playerSprite.material.DOColor(SystemManager.Instance.DeadColor, "_Tint", stopTime / 2f)
        .SetEase(Ease.OutQuad)
        .SetUpdate(true);

        // 시간 천천히 멈추기
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0f, stopTime / 2f)
        .SetEase(Ease.OutQuad)
        .SetUpdate(true);
        // SystemManager.Instance.TimeScaleChange(0, stopTime);

        // 사운드 느려지다가 정지
        SoundManager.Instance.SoundTimeScale(0, stopTime, false);

        yield return new WaitForSecondsRealtime(stopTime / 2f);

        //todo 핸드폰 미니 폭파
        //todo 핸드폰 미니 폭파음

        // 플레이어 사망 사운드 재생
        deadAudio.Play();

        // 플레이어에서 하얀 빛 파티클 터짐
        deathEffect.SetActive(true);

        // 핸드폰 입력 막기
        PhoneMenu.Instance.Phone_Input.Disable();

        // 플레이어, 핸드폰 끄기
        playerManager.playerSprite.enabled = false;
        CastMagic.Instance.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(1f);

        // 게임 오버 UI 켜기
        SystemManager.Instance.GameOverPanelOpen(false);
    }

    // public void DebuffRemove()
    // {
    //     // 이동 속도 저하 디버프 초기화
    //     playerManager.speedBuff = 1f;

    //     // 플랫 디버프 초기화
    //     flatCount = 0;
    //     playerManager.transform.localScale = Vector2.one;

    //     //슬로우 디버프 해제
    //     // 슬로우 아이콘 없에기
    //     Transform slowIcon = buffParentObj.Find(SystemManager.Instance.slowDebuffUI.name);
    //     if (slowIcon != null)
    //         LeanPool.Despawn(slowIcon);
    //     // 코루틴 변수 초기화
    //     slowCoroutine = null;

    //     // 감전 디버프 해제
    //     // 자식중에 감전 이펙트 찾기
    //     Transform shockEffect = transform.Find(SystemManager.Instance.shockDebuffEffect.name);
    //     if (shockEffect != null)
    //         LeanPool.Despawn(shockEffect);
    //     // 감전 코루틴 변수 초기화
    //     shockCoroutine = null;

    //     // 화상 이펙트 없에기
    //     Transform burnEffect = transform.Find(SystemManager.Instance.burnDebuffEffect.name);
    //     if (burnEffect != null)
    //         LeanPool.Despawn(burnEffect);
    //     // 화상 코루틴 변수 초기화
    //     burnCoroutine = null;

    //     // 포이즌 아이콘 없에기
    //     Transform poisonIcon = transform.Find(SystemManager.Instance.poisonDebuffEffect.name);
    //     if (poisonIcon != null)
    //         LeanPool.Despawn(poisonIcon);
    //     // 포이즌 코루틴 변수 초기화
    //     poisonCoroutine = null;

    //     // 출혈 아이콘 없에기
    //     Transform bleedIcon = transform.Find(SystemManager.Instance.bleedDebuffUI.name);
    //     if (bleedIcon != null)
    //         LeanPool.Despawn(bleedIcon);
    //     // 출혈 코루틴 변수 초기화
    //     bleedCoroutine = null;
    // }
}
