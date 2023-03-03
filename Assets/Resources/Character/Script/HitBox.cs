using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [Header("Refer")]
    public Character character;
    public List<Collider2D> hitColls;

    // [Header("State")]
    // List<Buff> buffList = new List<Buff>();

    private void Awake()
    {
        // character 찾기
        character = character != null ? character : GetComponent<Character>();

        // 피격 콜라이더 모두 찾기
        hitColls = GetComponents<Collider2D>().ToList();
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

        // 히트 콜라이더 모두 켜기
        foreach (Collider2D hitColl in hitColls)
        {
            hitColl.enabled = true;
        }
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

        // 피격 딜레이 중이면 리턴
        if (character.hitDelayCount > 0)
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

    public virtual IEnumerator Hit(Attack attack)
    {
        // 죽었으면 리턴
        if (character.isDead)
            yield break;

        // 피격 위치 산출
        Collider2D attackerColl = attack.GetComponent<Collider2D>();
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
        if (attack.TryGetComponent<EnemyAttack>(out EnemyAttack enemyAtk) && enemyAtk.enabled)
        {
            // 공격한 캐릭터 찾기
            Character atkCharacter = enemyAtk.character;

            // other가 본인일때 리턴
            if (atkCharacter == this)
                yield break;

            // 공격자를 타겟으로 변경
            character.TargetObj = atkCharacter.gameObject;

            // 크리티컬 성공 여부 계산
            isCritical = Random.value > 0.5f ? true : false;

            // 고정 데미지가 있으면 아군 피격이라도 적용
            if (enemyAtk.power > 0)
            {
                // 데미지 갱신
                damage = enemyAtk.power;

                Damage(damage, false, hitPos);
            }

            // 피격 대상이 고스트일때
            if (character.IsGhost)
            {
                //고스트 아닌 적이 때렸을때만 데미지
                if (!atkCharacter.IsGhost)
                {
                    // 데미지 갱신
                    damage = enemyAtk.character.powerNow;

                    Damage(damage, false, hitPos);
                }
            }
            // 피격 대상이 고스트 아닐때
            else
            {
                //고스트가 때렸으면 데미지
                if (atkCharacter.IsGhost)
                {
                    // 데미지 갱신
                    damage = enemyAtk.character.powerNow;

                    Damage(damage, false, hitPos);
                }
            }
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

            // 목표가 미설정 혹은 플레이어일때
            if (magicHolder.targetType == MagicHolder.TargetType.None
            || magicHolder.targetType == MagicHolder.TargetType.Player)
            {
                // print("타겟 미설정");
                yield break;
            }

            // 캐릭터가 타겟일때
            if (magicHolder.targetType == MagicHolder.TargetType.Enemy
            || magicHolder.targetType == MagicHolder.TargetType.Both)
            {
                // 히트 콜백 있으면 실행
                if (magicHolder.hitAction != null)
                    magicHolder.hitAction.Invoke();

                // 해당 마법이 무한관통 아니고, 관통횟수 남아있을때
                if (magicHolder.pierceCount > 0)
                    // 관통 횟수 차감
                    magicHolder.pierceCount--;

                // 마법 파워 계산
                float power = MagicDB.Instance.MagicPower(magic);
                //크리티컬 성공 여부 계산
                isCritical = MagicDB.Instance.MagicCritical(magic);
                //크리티컬 데미지 계산
                float criticalPower = MagicDB.Instance.MagicCriticalPower(magic);

                // print(attacker.gameObject.name + " : " + magic.name);

                // 데미지가 있으면
                if (power > 0)
                {
                    //데미지 계산, 고정 데미지 setPower가 없으면 마법 파워로 계산
                    damage = magicHolder.power > 0 ? magicHolder.power : power;
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

                    // // 도트 피해 옵션 없을때만 데미지 (독, 화상, 출혈)
                    // if (attacker.poisonTime == 0
                    // && attacker.burnTime == 0
                    // && attacker.bleedTime == 0)
                    // 데미지 주기
                    Damage(damage, isCritical, hitPos);
                }
            }
        }
        // 그냥 Attack 컴포넌트일때
        else
        {
            // 고정 데미지 불러오기
            damage = attack.power;

            // 데미지 입기
            Damage(damage, isCritical, hitPos);
        }

        // 무적 아닐때
        if (!character.invinsible)
        {
            // 디버프 판단해서 적용
            AfterEffect(attack, isCritical, damage);
        }
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

        // 현재 무적 상태면
        if (character.invinsible)
            // block 이펙트로 교체
            hitEffect = WorldSpawner.Instance.blockEffect;

        // 피격 지점에 히트 이펙트 소환
        LeanPool.Spawn(hitEffect, hitPos, Quaternion.identity, ObjectPool.Instance.effectPool);
    }

    // public IEnumerator StatBuff(string statName, bool isMultiple, float power, float duration,
    // Transform buffParent, GameObject debuffEffect, IEnumerator coroutine)
    // {
    //     //todo 스탯이름으로 해당 스탯 값 불러오기
    //     var statField = PlayerManager.Instance.PlayerStat_Now.GetType().GetField(statName);
    //     var stat = statField.GetValue(PlayerManager.Instance.PlayerStat_Now);

    //     //todo 스탯이름 없으면 에러
    //     if (statField == null || stat == null)
    //     {
    //         print(statName + " : 는 존재하지 않는 스탯 이름");
    //         yield break;
    //     }

    //     // Buff buff = AddBuff(statName, isMultiple, power);
    //     // 버프 인스턴스 생성
    //     Buff buff = new Buff();
    //     // 스탯 이름 전달
    //     buff.statName = statName;
    //     // 연산 종류 전달
    //     buff.isMultiple = isMultiple;
    //     // 버프량 전달
    //     buff.amount = power;
    //     // 해당 버프 리스트에 넣기
    //     buffList.Add(buff);

    //     // 디버프 아이콘
    //     Transform debuffUI = null;
    //     // 이미 슬로우 디버프 중 아닐때
    //     if (!buffParent.Find(debuffEffect.name))
    //         //슬로우 디버프 아이콘 붙이기
    //         debuffUI = LeanPool.Spawn(debuffEffect, buffParent.position, Quaternion.identity, buffParent).transform;

    //     // 일정 시간 대기
    //     yield return new WaitForSeconds(duration);

    //     // 해당 리스트에서 버프 없에기
    //     // RemoveBuff(buff);
    //     buffList.Remove(buff);

    //     // 슬로우 아이콘 없에기
    //     debuffUI = buffParent.Find(debuffEffect.name);
    //     if (debuffUI != null)
    //         LeanPool.Despawn(debuffUI);
    // }

    public void AfterEffect(Attack attack, bool isCritical, float damage = 0)
    {
        // 보스가 아닌 몬스터일때, 몬스터 아닐때(플레이어, 사물)
        if (character.enemy == null
        || (character.enemy != null && character.enemy.enemyType != EnemyDB.EnemyType.Boss.ToString()))
        {
            //넉백
            if (attack.knockbackForce > 0)
                StartCoroutine(Knockback(attack, attack.knockbackForce));

            //시간 정지
            if (attack.stopTime > 0)
                StartCoroutine(TimeStop(attack.stopTime));
            // //캐릭터 경직 카운터에 stopTime 만큼 추가
            // character.stopCount = attack.stopTime;

            // 슬로우 디버프, 크리티컬 성공일때
            if (attack.slowTime > 0)
                // 버프 적용
                character.SetBuff(Debuff.Slow.ToString(), nameof(character.characterStat.moveSpeed), true, 0.5f, attack.slowTime,
                   false, character.buffParent, SystemManager.Instance.slowDebuffUI);

            // 스턴
            if (attack.stunTime > 0)
                // 버프 적용
                character.SetBuff(Debuff.Stun.ToString(), nameof(character.characterStat.moveSpeed), true, 0, attack.stunTime,
                  false, character.buffParent, SystemManager.Instance.stunDebuffEffect);

            // 감전 디버프 && 크리티컬일때
            if (attack.shockTime > 0)
                // 버프 적용
                character.SetBuff(Debuff.Shock.ToString(), nameof(character.characterStat.moveSpeed), true, 0, attack.shockTime,
                  false, character.transform, SystemManager.Instance.shockDebuffEffect);

            // flat 디버프 있을때, flat 상태 아닐때
            if (attack.flatTime > 0 && character.flatCount <= 0)
                // 납작해지고 행동불능
                StartCoroutine(FlatDebuff(attack.flatTime));
        }

        #region DotHit

        // 화상 피해 시간 있을때
        if (attack.burnTime > 0)
            // 도트 데미지 실행
            character.SetBuff(Debuff.Burn.ToString(), "", true, attack.power, attack.burnTime,
             true, character.transform, SystemManager.Instance.burnDebuffEffect);

        // 포이즌 피해 시간 있으면 도트 피해
        if (attack.poisonTime > 0)
            // 도트 데미지 실행
            character.SetBuff(Debuff.Poison.ToString(), "", true, attack.power, attack.poisonTime,
             true, character.transform, SystemManager.Instance.poisonDebuffEffect);

        // 출혈 지속시간 있으면 도트 피해
        if (attack.bleedTime > 0)
            // 도트 데미지 실행
            character.SetBuff(Debuff.Bleed.ToString(), "", true, attack.power, attack.bleedTime,
             true, character.buffParent, SystemManager.Instance.bleedDebuffUI);

        #endregion
    }

    public IEnumerator HitDelay(float damage)
    {
        // 캐릭터 정보가 있을때
        if (character.enemy != null)
            character.hitDelayCount = character.enemy.hitDelay;
        else
            character.hitDelayCount = 0.2f;

        // 히트 머터리얼 및 색으로 변경
        for (int i = 0; i < character.spriteList.Count; i++)
        {
            if (damage > 0)
            {
                // 현재 체력이 max에 가까울수록 빨간색, 0에 가까울수록 흰색
                Color hitColor = Color.Lerp(SystemManager.Instance.hitColor, SystemManager.Instance.DeadColor, character.characterStat.hpNow / character.characterStat.hpMax);

                // 체력 비율에 따라 히트 컬러 넣기
                character.spriteList[i].material.SetColor("_Tint", hitColor);
            }
            if (damage == 0)
                // 회피 또는 방어
                character.spriteList[i].material.SetColor("_Tint", Color.blue);
            if (damage < 0)
                // 회복
                character.spriteList[i].material.SetColor("_Tint", SystemManager.Instance.healColor);
        }

        // 히트 딜레이 대기
        yield return new WaitUntil(() => character.hitDelayCount <= 0);

        // 죽었으면 복구하지않고 리턴
        if (character.isDead)
            yield break;

        for (int i = 0; i < character.spriteList.Count; i++)
        {
            // 고스트일때
            if (character.IsGhost)
                // 고스트 틴트색으로 초기화
                character.spriteList[i].material.SetColor("_Tint", new Color(0, 1, 1, 0.5f));
            // 멈춤 디버프 중일때
            else if (character.stopCount > 0)
                // 회색으로 초기화
                character.spriteList[i].material.SetColor("_Tint", SystemManager.Instance.stopColor);
            else
                // 투명하게 초기화
                character.spriteList[i].material.SetColor("_Tint", new Color(1, 1, 1, 0));
        }

        // 코루틴 변수 초기화
        character.hitCoroutine = null;
    }

    public virtual void Damage(float damage, bool isCritical, Vector2 hitPos = default)
    {
        // 적 정보 없으면 리턴
        // if (character == null || character.enemy == null)
        //     return;

        // 죽었으면 리턴
        if (character.isDead)
            return;

        // 피격 위치가 있을때만
        if (hitPos != default)
            // 데미지가 0 이상일때
            if (damage >= 0)
                // 피격 이펙트 재생
                HitEffect(hitPos);

        //피격 딜레이 무적시간 시작
        character.hitCoroutine = HitDelay(damage);
        StartCoroutine(character.hitCoroutine);

        // 피격 위치안들어오면 현재위치
        if (hitPos == default)
            hitPos = transform.position;

        //데미지 int로 바꾸기
        damage = Mathf.RoundToInt(damage);

        // 무적 상태일때, 방어
        if (character.invinsible)
            UIManager.Instance.DamageUI(UIManager.DamageType.Block, damage, isCritical, hitPos);
        // 데미지 0일때, 회피
        else if (damage == 0)
            UIManager.Instance.DamageUI(UIManager.DamageType.Miss, damage, isCritical, hitPos);
        // 데미지 양수일때, 피격
        else if (damage > 0)
            UIManager.Instance.DamageUI(UIManager.DamageType.Damaged, damage, isCritical, hitPos);
        // 데미지 음수일때, 회복
        else if (damage < 0)
            UIManager.Instance.DamageUI(UIManager.DamageType.Heal, damage, isCritical, hitPos);


        // 무적 아닐때
        if (!character.invinsible)
            // 데미지 적용
            character.characterStat.hpNow -= damage;

        //체력 범위 제한
        character.characterStat.hpNow = Mathf.Clamp(character.characterStat.hpNow, 0, character.characterStat.hpMax);

        //보스면 체력 UI 띄우기
        if (character.enemy != null && character.enemy.enemyType == EnemyDB.EnemyType.Boss.ToString())
        {
            StartCoroutine(UIManager.Instance.UpdateBossHp(character));
        }

        // 피격시 함수 호출 (해당 캐릭터만)
        if (character.hitCallback != null)
            character.hitCallback();

        // 체력 0 이하면 죽음
        if (character.characterStat.hpNow <= 0)
        {
            // print("Dead Pos : " + transform.position);
            //죽음 시작
            StartCoroutine(Dead(character.deadDelay));
        }
    }

    public virtual IEnumerator Knockback(Attack attacker, float knockbackForce)
    {
        // 반대 방향으로 넉백 벡터
        Vector2 knockbackDir = transform.position - attacker.transform.position;
        knockbackDir = knockbackDir.normalized * knockbackForce * PlayerManager.Instance.characterStat.knockbackForce;

        // 캐릭터 위치에서 피격 반대방향 위치로 이동
        character.transform.DOMove((Vector2)character.transform.position + knockbackDir, 0.1f)
        .SetEase(Ease.OutBack);

        // // 해당 방향으로 캐릭터 밀기
        // character.rigid.AddForce(knockbackDir);

        yield return null;
    }

    public virtual IEnumerator FlatDebuff(float flatTime)
    {
        // 정지 시간 초기화
        character.stopCount = flatTime;

        //스케일 납작하게
        character.transform.localScale = new Vector2(1f, 0.5f);

        // stopCount 풀릴때까지 대기
        yield return new WaitUntil(() => character.stopCount <= 0);
        // yield return new WaitForSeconds(flatTime);

        //스케일 복구
        character.transform.localScale = Vector2.one;
    }

    public IEnumerator TimeStop(float stopTime)
    {
        // 정지 시간 초기화
        character.stopCount = stopTime;

        // 모든 스프라이트 회색으로
        for (int i = 0; i < character.spriteList.Count; i++)
            character.spriteList[i].material.SetColor("_Tint", SystemManager.Instance.stopColor);

        // stopCount 풀릴때까지 대기
        yield return new WaitUntil(() => character.stopCount <= 0);

        // 틴트 없에기
        for (int i = 0; i < character.spriteList.Count; i++)
        {
            // 아직 회색이면
            if (character.spriteList[i].material.GetColor("_Tint") == SystemManager.Instance.stopColor)
                // 틴트색 초기화
                character.spriteList[i].material.SetColor("_Tint", new Color(1, 1, 1, 0));
        }
    }

    public IEnumerator Dead(float deadDelay)
    {
        // if (character.enemy == null)
        //     yield break;

        // 죽음 여부 초기화
        character.isDead = true;

        // 경직 시간 추가
        // hitCount += 1f;

        // 캐릭터 정보가 있을때
        if (character.enemy != null)
        {
            //이동 초기화
            character.rigid.velocity = Vector2.zero;

            // 물리 콜라이더 끄기
            if (character.physicsColl != null)
                character.physicsColl.enabled = false;
        }

        // 피격 콜라이더 모두 끄기
        foreach (Collider2D hitColl in hitColls)
        {
            hitColl.enabled = false;
        }

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
            // 빨간색으로 변경
            sprite.material.SetColor("_Tint", SystemManager.Instance.hitColor);

            // 색깔 점점 흰색으로
            sprite.material.DOColor(SystemManager.Instance.DeadColor, "_Tint", deadDelay)
            .SetEase(Ease.OutQuad);
        }

        // 죽음 딜레이 대기
        yield return new WaitForSeconds(deadDelay);

        // 고스트가 아닐때
        if (!character.IsGhost)
        {
            // 캐릭터 정보가 있을때
            if (character.enemy != null)
            {
                //캐릭터 총 전투력 빼기
                WorldSpawner.Instance.NowEnemyPower -= character.enemy.grade;

                //캐릭터 킬 카운트 올리기
                SystemManager.Instance.killCount++;
                UIManager.Instance.UpdateKillCount();

                //혈흔 이펙트 생성
                GameObject blood = LeanPool.Spawn(WorldSpawner.Instance.bloodPrefab, character.transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);
            }

            //아이템 드랍
            character.DropItem();

            // 캐릭터 리스트에서 캐릭터 본인 빼기
            WorldSpawner.Instance.EnemyDespawn(character);
        }

        // 모든 디버프 해제
        DebuffRemove();

        // 먼지 이펙트 생성
        GameObject dust = LeanPool.Spawn(WorldSpawner.Instance.dustPrefab, character.transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);
        // dust.tag = "Enemy";

        // 죽을때 콜백 호출
        if (character.deadCallback != null)
            character.deadCallback(character);

        // 트윈 및 시퀀스 끝내기
        character.transform.DOKill();

        // 공격 타겟 플레이어로 초기화
        character.TargetObj = PlayerManager.Instance.gameObject;

        // 캐릭터 비활성화
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

        // 모든 버프 해제
        for (int i = 0; i < character.buffList.Count; i++)
            StartCoroutine(character.StopBuff(character.buffList[i], character.buffList[i].duration));
    }
}