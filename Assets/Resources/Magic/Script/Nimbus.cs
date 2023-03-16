using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Nimbus : MonoBehaviour
{
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] ParticleSystem cloud; // 구름 파티클
    [SerializeField] ParticleManager thunderManager; // 내리치는 번개 파티클 매니저
    [SerializeField] ParticleManager groundElectroManager; // 지면을 타고 흐르는 번개 파티클 매니저
    [SerializeField] CircleCollider2D atkColl; // 공격용 콜라이더
    [SerializeField] GameObject deadzone; // 번개 충돌하는 데드존

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);

        // 감전 시간 갱신
        magicHolder.shockTime = 1 + magicHolder.duration;

        // 공격용 콜라이더 끄기
        atkColl.enabled = false;

        // 타겟 위치로 이동
        transform.position = magicHolder.targetPos;

        // 공격 시작
        StartCoroutine(StartAtk());
    }

    IEnumerator StartAtk()
    {
        // 구름 파티클 켜기
        cloud.gameObject.SetActive(true);

        // 콜라이더 끄기
        atkColl.enabled = false;

        // 구름 나타날때까지 대기
        yield return new WaitForSeconds(0.2f);

        // 구름 좌우로 떨기
        cloud.transform.DOPunchPosition(Vector2.right * 0.5f, 0.3f, 50, 1);

        // 번개 내려치기
        thunderManager.particle.Play();

        // 데드존 켜기
        deadzone.SetActive(true);

        // 번개 치는 시간 대기
        yield return new WaitForSeconds(0.4f);

        // 공격용 콜라이더 스케일 키우기
        DOTween.To(x => atkColl.radius = x, atkColl.radius, magicHolder.range, 0.2f);

        // 지면 번개 파티클 재생
        groundElectroManager.particle.Play();

        // 공격용 콜라이더를 duration 동안 반복 점멸
        yield return StartCoroutine(FlickerColl());

        // 지면 번개 파티클 정지
        groundElectroManager.SmoothStop();

        // 콜라이더 끄기
        atkColl.enabled = false;
        // 데드존 끄기
        deadzone.SetActive(false);

        // 그을음 파티클 끝날때까지 대기
        yield return new WaitForSeconds(2f);

        // 구름 파티클 끄기
        cloud.gameObject.SetActive(false);

        // 디스폰
        StartCoroutine(AutoDespawn());
    }

    IEnumerator FlickerColl()
    {
        // 깜빡일 시간 받기
        float flickCount = magicHolder.duration;
        while (flickCount > 0)
        {
            // 콜라이더 토글
            atkColl.enabled = !atkColl.enabled;

            // 잠깐 대기
            flickCount -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    IEnumerator AutoDespawn(float delay = 0)
    {
        //range 속성만큼 지속시간 부여
        float delayCount = delay;
        while (delayCount > 0)
        {
            delayCount -= Time.deltaTime;
            yield return null;
        }

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }
}
