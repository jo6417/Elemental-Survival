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
    [SerializeField] Transform shadow; // 그림자

    [Header("Stat")]
    float range;
    float duration;

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        // 스탯 초기화
        range = MagicDB.Instance.MagicRange(magicHolder.magic);
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);

        // 디버프 초기화
        magicHolder.shockTime = 1f;

        //콜라이더 끄기
        atkColl.enabled = false;

        // 플레이어가 쓴 마법일때
        if (magicHolder.GetTarget() == Attack.TargetType.Enemy)
            if (magicHolder.isManualCast)
                // 타겟 위치로 이동
                transform.position = magicHolder.targetPos;
            else
                // 범위 내 랜덤 위치로 이동
                transform.position = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * range;

        // 그림자 사이즈 초기화
        shadow.localScale = new Vector3(1, 0.4f, 1);

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

        //콜라이더 끄기
        atkColl.enabled = false;

        // 그림자 사이즈 줄이기
        shadow.DOScale(Vector3.zero, 0.05f);

        // 파티클 끄고 디스폰
        energyBall.SmoothDespawn();
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
    }
}
