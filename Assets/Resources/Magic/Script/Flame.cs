using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Flame : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] ParticleManager particleManager;
    [SerializeField] MagicHolder magicHolder;
    public Color fireColor;
    public Material fireMaterial;

    [Header("Spec")]
    float range;
    float duration;

    private void Awake()
    {
        // 파티클 기본 색깔
        fireColor = new Color(1f, 80f / 255f, 30f / 255f, 1f);
        // 파티클 기본 머터리얼
        fireMaterial = SystemManager.Instance.HDR3_Mat;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 콜라이더 끄기
        magicHolder.coll.enabled = false;

        //magic 불러올때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        range = MagicDB.Instance.MagicRange(magicHolder.magic);
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);

        // 파티클 색 초기화
        ParticleSystem.MainModule particleMain = particleManager.particle.main;
        particleMain.startColor = fireColor;
        // 파티클 머터리얼 초기화
        ParticleSystemRenderer particleRenderer = particleManager.particle.GetComponent<ParticleSystemRenderer>();
        particleRenderer.material = fireMaterial;

        // magicHolder에서 targetPos 받아와서 해당 위치로 이동
        transform.position = magicHolder.targetPos;

        // 범위만큼 스케일 반영
        transform.localScale = Vector2.one * range;

        // 콜라이더 켜기
        magicHolder.coll.enabled = true;

        // 화염 파티클 재생
        particleManager.particle.Play();

        // duration 만큼 시간 지나면 디스폰 시작
        yield return new WaitForSeconds(duration);

        // 파티클 끄고 디스폰
        particleManager.SmoothDespawn();

        // 파티클 사라지는 시간 절반만큼 대기
        yield return new WaitForSeconds(1f);

        // 콜라이더 끄기
        magicHolder.coll.enabled = false;
    }

    private void OnDisable()
    {
        // 파티클 기본 색깔
        fireColor = new Color(1f, 80f / 255f, 30f / 255f, 1f);
        // 파티클 기본 머터리얼
        fireMaterial = SystemManager.Instance.HDR3_Mat;
    }
}
