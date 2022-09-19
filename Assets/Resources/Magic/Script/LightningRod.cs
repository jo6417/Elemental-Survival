using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LightningRod : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] LineRenderer lightningLinePrefab;
    [SerializeField] ParticleManager spikeSpark;
    [SerializeField] MagicHolder magicHolder;
    List<ParticleSystem> electroBalls = new List<ParticleSystem>(); // 적의 위치에 생성될 전기 구체 리스트
    List<LineRenderer> electroLines = new List<LineRenderer>(); // 전기 구체 사이마다 들어갈 전기 라인

    [Header("Spec")]
    float range;
    float duration;
    float atkNum;

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
        atkNum = MagicDB.Instance.MagicAtkNum(magicHolder.magic);

        // magicHolder에서 targetPos 받아와서 해당 위치로 이동
        transform.position = magicHolder.targetPos;

        //todo 해당 위치에 못박기
        StartCoroutine(SpikeRod());
    }

    IEnumerator SpikeRod()
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f);

        //todo 한번 반짝 빛나는 이펙트 - 로컬

        // 못 회전하기
        transform.DORotate(Vector3.up * 360f, 1f, RotateMode.LocalAxisAdd);

        yield return new WaitForSeconds(1f);

        // 못 domove inback으로 박기
        transform.DOMove(magicHolder.targetPos, 1f)
        .SetEase(Ease.InBack);

        // 박힐때 흙 튀기기
        // 바닥에 꽂힐때 전기 파티클 재생
        spikeSpark.particle.Play();

        //todo 범위 내 적 모두 찾아서 리스트업

        // 리스트 개수만큼 반복
        //todo 리스트중 가장 가까운적 찾아서 새 리스트에 넣기
        //todo 리스트에서 마지막 찾은 적과 가장 가까운적 찾아서 리스트에 넣기

        //todo 리스트의 모든 적 위치마다 전기 구체 소환
        //todo 각 포인트 사이마다 전기 라인 프리팹 소환하고 전기라인 리스트에 추가
        //todo 전기 라인 렌더러의 시작,끝 지점 옮기기
        //todo 라인 렌더러 포인트대로 엣지 콜라이더 포인트 갱신
    }
}
