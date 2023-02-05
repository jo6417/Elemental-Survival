using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class ElectroBolt : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] Collider2D atkColl; // 공격용 콜라이더
    [SerializeField] ParticleManager energyBall; // 에너지볼 이펙트 파티클

    [Header("Stat")]
    float range;
    float duration;
    float scale;

    private void OnEnable()
    {
        // 구체 사이즈 초기화
        transform.localScale = Vector2.zero;

        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        // 스탯 초기화
        range = MagicDB.Instance.MagicRange(magicHolder.magic);
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);
        scale = MagicDB.Instance.MagicScale(magicHolder.magic);

        // 디버프 초기화
        magicHolder.shockTime = 1f;

        // 플레이어가 쓴 마법일때
        if (magicHolder.GetTarget() == Attack.TargetType.Enemy)
            // 수동 시전일때
            if (magicHolder.isManualCast)
                // 타겟 위치로 이동
                transform.position = magicHolder.targetPos;
            else
                // 범위 내 랜덤 위치로 이동
                transform.position = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle * range;

        // 레벨만큼 구체 사이즈 확대
        transform.DOScale(Vector2.one * scale, 0.2f);

        // 공격 시작
        StartCoroutine(StartAtk());
    }

    IEnumerator StartAtk()
    {
        // 파티클 켜기
        energyBall.particle.Play();

        // 사운드 반복 딜레이
        float loopDelay = 0.2f;
        // 사운드 반복 횟수
        int loopNum = Mathf.RoundToInt(duration / loopDelay);
        // 전기 사운드 반복 재생
        SoundManager.Instance.PlaySound("ElectroBolt", transform.position, 0, loopDelay, loopNum, true);

        // duartion 동안 콜라이더 점멸 반복
        yield return StartCoroutine(FlickerColl());

        // 사이즈 줄이기
        transform.DOScale(Vector2.zero, 0.2f);
        yield return new WaitForSeconds(0.2f);

        // 디스폰
        LeanPool.Despawn(transform);
    }

    IEnumerator FlickerColl()
    {
        // 깜빡일 시간 받기
        float flickCount = duration;
        while (flickCount > 0)
        {
            // 콜라이더 토글
            atkColl.enabled = !atkColl.enabled;

            // 잠깐 대기
            flickCount -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 콜라이더 끄기
        atkColl.enabled = false;
    }
}
