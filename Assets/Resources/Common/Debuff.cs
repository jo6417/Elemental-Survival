// using System.Collections;
// using System.Collections.Generic;
// using Lean.Pool;
// using UnityEngine;

// public class Debuff : MonoBehaviour
// {
//     [Header("Buff")]
//     public Transform buffParent; //버프 아이콘 들어가는 부모 오브젝트
//     public IEnumerator hitCoroutine;
//     public IEnumerator burnCoroutine = null;
//     public IEnumerator poisonCoroutine = null;
//     public IEnumerator bleedCoroutine = null;
//     public IEnumerator slowCoroutine = null;
//     public IEnumerator shockCoroutine = null;

//     [Header("CoolTime")]
//     public float particleHitCount = 0; // 파티클 피격 카운트
//     public float hitDelayCount = 0; // 피격 딜레이 카운트
//     public float stopCount = 0; // 시간 정지 카운트
//     public float flatCount = 0; // 납작 디버프 카운트
//     public float oppositeCount = 0; // 스포너 반대편 이동 카운트
//     public float burnCoolCount; // 화상 도트뎀 남은시간
//     public float poisonCoolCount; //독 도트뎀 남은시간
//     public float bleedCoolCount; // 출혈 디버프 남은시간

//     public IEnumerator BurnDotHit(float tickDamage, float duration)
//     {
//         // 화상 데미지 지속시간 넣기
//         burnCoolCount = duration;

//         // 화상 디버프 아이콘
//         Transform burnEffect = null;

//         // 이미 화상 디버프 중 아닐때
//         if (!transform.Find(SystemManager.Instance.burnDebuffEffect.name))
//         {
//             // 화상 디버프 이펙트 붙이기
//             burnEffect = LeanPool.Spawn(SystemManager.Instance.burnDebuffEffect, transform.position, Quaternion.identity, transform).transform;

//             // 포탈 사이즈 배율만큼 이펙트 배율 키우기
//             burnEffect.transform.localScale = Vector3.one * portalSize;
//         }

//         // 화상 데미지 지속시간이 1초 이상 남았을때, 몬스터 살아있을때
//         while (burnCoolCount > 1 && !isDead)
//         {
//             // 한 틱동안 대기
//             yield return new WaitForSeconds(1f);

//             // 화상 데미지 입히기
//             Damage(tickDamage, false);

//             // 화상 데미지 지속시간에서 한틱 차감
//             burnCoolCount -= 1f;
//         }

//         // 화상 이펙트 없에기
//         burnEffect = transform.Find(SystemManager.Instance.burnDebuffEffect.name);
//         if (burnEffect != null)
//             LeanPool.Despawn(burnEffect);

//         // 화상 코루틴 변수 초기화
//         burnCoroutine = null;
//     }

//     public IEnumerator PoisonDotHit(float tickDamage, float duration)
//     {
//         //독 데미지 지속시간 넣기
//         poisonCoolCount = duration;

//         // 포이즌 디버프 이펙트
//         Transform poisonEffect = null;

//         // 이미 포이즌 디버프 중 아닐때
//         if (!transform.Find(SystemManager.Instance.poisonDebuffEffect.name))
//         {
//             //포이즌 디버프 이펙트 붙이기
//             poisonEffect = LeanPool.Spawn(SystemManager.Instance.poisonDebuffEffect, transform.position, Quaternion.identity, transform).transform;

//             // 포탈 사이즈 배율만큼 이펙트 배율 키우기
//             poisonEffect.transform.localScale = Vector3.one * portalSize;
//         }

//         // 독 데미지 지속시간이 1초 이상 남았을때, 몬스터 살아있을때
//         while (poisonCoolCount > 1 && !isDead)
//         {
//             // 한 틱동안 대기
//             yield return new WaitForSeconds(1f);

//             // 독 데미지 입히기
//             Damage(tickDamage, false);

//             // 독 데미지 지속시간에서 한틱 차감
//             poisonCoolCount -= 1f;
//         }

//         // 포이즌 이펙트 없에기
//         poisonEffect = transform.Find(SystemManager.Instance.poisonDebuffEffect.name);
//         if (poisonEffect != null)
//             LeanPool.Despawn(poisonEffect);

//         // 포이즌 코루틴 변수 초기화
//         poisonCoroutine = null;
//     }

//     public IEnumerator BleedDotHit(float tickDamage, float duration)
//     {
//         // 출혈 디버프 아이콘
//         GameObject bleedIcon = null;

//         // 이미 출혈 디버프 중 아닐때
//         if (bleedCoolCount <= 0)
//             //출혈 디버프 아이콘 붙이기
//             bleedIcon = LeanPool.Spawn(SystemManager.Instance.bleedDebuffUI, buffParent.position, Quaternion.identity, buffParent);

//         // 출혈 데미지 지속시간 넣기
//         bleedCoolCount = duration;

//         // 출혈 데미지 지속시간 남았을때 진행
//         while (bleedCoolCount > 0)
//         {
//             // 한 틱동안 대기
//             yield return new WaitForSeconds(1f);

//             // 출혈 데미지 입히기
//             Damage(tickDamage, false);

//             // 출혈 데미지 지속시간에서 한틱 차감
//             bleedCoolCount -= 1f;
//         }

//         // 출혈 아이콘 없에기
//         bleedIcon = buffParent.Find(SystemManager.Instance.bleedDebuffUI.name).gameObject;
//         if (bleedIcon != null)
//             LeanPool.Despawn(bleedIcon);

//         // 코루틴 비우기
//         bleedCoroutine = null;
//     }

//     public IEnumerator Knockback(Attack attacker, float knockbackForce)
//     {
//         // 반대 방향으로 넉백 벡터
//         Vector2 knockbackDir = transform.position - attacker.transform.position;
//         knockbackDir = knockbackDir.normalized * knockbackForce * PlayerManager.Instance.PlayerStat_Now.knockbackForce * 2f;

//         // 몬스터 위치에서 피격 반대방향 위치로 이동
//         transform.DOMove((Vector2)transform.position + knockbackDir, 0.1f)
//         .SetEase(Ease.OutBack);

//         // print(knockbackDir);

//         yield return null;
//     }

//     public IEnumerator FlatDebuff(float flatTime)
//     {
//         //정지 시간 추가
//         flatCount = flatTime;

//         //스케일 납작하게
//         transform.localScale = new Vector2(1f, 0.5f);

//         // stopCount 풀릴때까지 대기
//         yield return new WaitUntil(() => flatCount <= 0);
//         // yield return new WaitForSeconds(flatTime);

//         //스케일 복구
//         transform.localScale = Vector2.one;
//     }

//     public IEnumerator SlowDebuff(float slowDuration)
//     {
//         // 죽었으면 초기화 없이 리턴
//         if (isDead)
//             yield break;

//         // 디버프량
//         float slowAmount = 0.2f;
//         // 슬로우 디버프 아이콘
//         Transform slowIcon = null;

//         // 애니메이션 속도 저하
//         for (int i = 0; i < animList.Count; i++)
//         {
//             animList[i].speed = slowAmount;
//         }
//         // 이동 속도 저하 디버프
//         enemyAI.moveSpeedDebuff = slowAmount;

//         // 이미 슬로우 디버프 중 아닐때
//         if (!buffParent.Find(SystemManager.Instance.slowDebuffUI.name))
//             //슬로우 디버프 아이콘 붙이기
//             slowIcon = LeanPool.Spawn(SystemManager.Instance.slowDebuffUI, buffParent.position, Quaternion.identity, buffParent).transform;

//         // 슬로우 시간동안 대기
//         yield return new WaitForSeconds(slowDuration);

//         // 죽었으면 초기화 없이 리턴
//         if (isDead)
//             yield break;

//         // 애니메이션 속도 초기화
//         for (int i = 0; i < animList.Count; i++)
//         {
//             animList[i].speed = 1f;
//         }
//         // 이동 속도 저하 디버프 초기화
//         enemyAI.moveSpeedDebuff = 1f;

//         // 슬로우 아이콘 없에기
//         slowIcon = buffParent.Find(SystemManager.Instance.slowDebuffUI.name);
//         if (slowIcon != null)
//             LeanPool.Despawn(slowIcon);

//         // 코루틴 변수 초기화
//         slowCoroutine = null;
//     }

//     public IEnumerator ShockDebuff(float shockDuration)
//     {
//         // 디버프량
//         float slowAmount = 0f;
//         // 감전 디버프 이펙트
//         Transform shockEffect = null;

//         // 애니메이션 속도 저하
//         for (int i = 0; i < animList.Count; i++)
//         {
//             animList[i].speed = slowAmount;
//         }

//         // 이동 속도 저하 디버프
//         enemyAI.moveSpeedDebuff = slowAmount;

//         //이동 멈추기
//         rigid.velocity = Vector2.zero;

//         // 이미 감전 디버프 중 아닐때
//         if (!transform.Find(SystemManager.Instance.shockDebuffEffect.name))
//         {
//             //감전 디버프 이펙트 붙이기
//             shockEffect = LeanPool.Spawn(SystemManager.Instance.shockDebuffEffect, transform.position, Quaternion.identity, transform).transform;

//             // 포탈 사이즈 배율만큼 이펙트 배율 키우기
//             shockEffect.transform.localScale = Vector3.one * portalSize;
//         }

//         // 감전 시간동안 대기
//         yield return new WaitForSeconds(shockDuration);

//         // 죽었으면 초기화 없이 리턴
//         if (isDead)
//             yield break;

//         // 애니메이션 속도 초기화
//         for (int i = 0; i < animList.Count; i++)
//         {
//             animList[i].speed = 1f;
//         }

//         // 이동 속도 저하 디버프 초기화
//         enemyAI.moveSpeedDebuff = 1f;

//         // 자식중에 감전 이펙트 찾기
//         shockEffect = transform.Find(SystemManager.Instance.shockDebuffEffect.name);
//         if (shockEffect != null)
//             LeanPool.Despawn(shockEffect);

//         // 코루틴 변수 초기화
//         shockCoroutine = null;
//     }
// }
