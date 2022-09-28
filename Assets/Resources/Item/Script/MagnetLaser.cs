using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class MagnetLaser : MonoBehaviour
{
    [SerializeField] SpriteRenderer magnet;
    [SerializeField] ParticleSystem energyBall;
    [SerializeField] LineRenderer laser;
    [SerializeField] float startTime = 0.5f;
    [SerializeField] float expandTime = 0.2f;
    [SerializeField] float pulseDelay = 0.1f;
    [SerializeField] float durationTime = 5f;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform);
        transform.localPosition = Vector3.zero;

        // 에너지볼 켜기
        energyBall.Play();

        // 자석 스프라이트 커지기
        magnet.transform.DOScale(Vector3.one, expandTime)
        .SetEase(Ease.OutBack);
        yield return new WaitForSeconds(expandTime);

        // 레이저 굵기 초기화
        laser.startWidth = 0;
        laser.endWidth = 0;
        laser.gameObject.SetActive(true);

        // 초반 레이저 얇은 굵기로
        DOTween.To(() => laser.startWidth, x => laser.startWidth = x, 0.1f, startTime);
        DOTween.To(() => laser.endWidth, x => laser.endWidth = x, 0.1f, startTime);
        yield return new WaitForSeconds(startTime);

        // 레이저 굵기 키우기
        DOTween.To(() => laser.startWidth, x => laser.startWidth = x, 3f, expandTime);
        DOTween.To(() => laser.endWidth, x => laser.endWidth = x, 3f, expandTime);
        yield return new WaitForSeconds(expandTime);

        // 에너지볼 끄기
        energyBall.Stop();

        // 레이저 굵기 딜레이마다 반복
        float durationCount = durationTime;
        while (durationCount > 0)
        {
            float randomWidth = Random.Range(2.8f, 3.2f);

            // 펄스 딜레이 시간 동안 굵기 변화
            DOTween.To(() => laser.startWidth, x => laser.startWidth = x, randomWidth, pulseDelay);
            DOTween.To(() => laser.endWidth, x => laser.endWidth = x, randomWidth, pulseDelay);

            // 펄스 딜레이 만큼 대기
            yield return new WaitForSeconds(pulseDelay);
            durationCount -= pulseDelay;
        }

        // 자석 스프라이트 줄어들기
        magnet.transform.DOScale(Vector3.zero, expandTime)
        .SetEase(Ease.InBack);

        // 굵기 0으로 줄어들기
        DOTween.To(() => laser.startWidth, x => laser.startWidth = x, 0f, expandTime);
        DOTween.To(() => laser.endWidth, x => laser.endWidth = x, 0f, expandTime);
        yield return new WaitForSeconds(expandTime);

        // 레이저 끄기
        laser.gameObject.SetActive(false);

        // 디스폰
        LeanPool.Despawn(transform);
    }

    private void Update()
    {
        // 마우스 방향 계산
        Vector2 mouseDir = PlayerManager.Instance.GetMousePos() - (Vector2)transform.position;
        float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;

        // 마우스 위치로 회전
        transform.rotation = Quaternion.Euler(Vector3.forward * angle);
    }
}
