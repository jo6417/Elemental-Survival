using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Nimbus : MonoBehaviour
{
    MagicHolder magicHolder;
    [SerializeField] ParticleSystem cloud;
    [SerializeField] ParticleManager thunderManager; // 내리치는 번개 파티클 매니저
    [SerializeField] ParticleManager groundElectroManager; // 지면을 타고 흐르는 번개 파티클 매니저
    [SerializeField] CircleCollider2D atkColl; // 공격용 콜라이더

    float range;
    float duration;

    private void Awake()
    {
        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 공격용 콜라이더 끄기
        atkColl.enabled = false;

        yield return new WaitUntil(() => magicHolder.magic != null);
        // 스탯 불러오기
        range = MagicDB.Instance.MagicRange(magicHolder.magic);
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);

        // 타겟 위치로 이동
        transform.position = magicHolder.targetPos;

        // 공격 시작
        StartCoroutine(StartAtk());
    }

    IEnumerator StartAtk()
    {
        // 콜라이더 끄기
        atkColl.enabled = false;

        // 구름 나타날때까지 대기
        yield return new WaitForSeconds(0.2f);

        // 구름 좌우로 떨기
        cloud.transform.DOPunchPosition(Vector2.right * 0.5f, 0.3f, 50, 1);

        // 파티클 재생
        thunderManager.particle.Play();

        // 공격용 콜라이더 스케일 키우기
        DOTween.To(x => atkColl.radius = x, atkColl.radius, range, duration);

        // 번개 치는 시간 대기
        yield return new WaitForSeconds(0.4f);

        // 지면 번개 파티클 재생
        groundElectroManager.particle.Play();

        // 공격용 콜라이더를 duration 동안 반복 점멸
        yield return StartCoroutine(FlickerColl());

        // 콜라이더 끄기
        atkColl.enabled = false;
        // 전기 퍼지는 이펙트 끝내기
        groundElectroManager.SmoothDisable();

        // 그을음 파티클 끝날때까지 대기
        yield return new WaitForSeconds(2f);

        // 디스폰
        StartCoroutine(AutoDespawn());
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
