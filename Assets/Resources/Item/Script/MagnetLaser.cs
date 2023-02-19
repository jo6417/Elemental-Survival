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
    [SerializeField] EdgeCollider2D coll;
    [SerializeField] Transform enemyPush; // 몬스터 밀어내는 콜라이더
    [SerializeField] float startTime = 0.5f;
    [SerializeField] float expandTime = 0.2f;
    [SerializeField] float pulseDelay = 0.1f;
    [SerializeField] float durationTime = 5f;
    float targetWidth;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 레이저 및 에너지볼 끄기
        laser.gameObject.SetActive(false);
        enemyPush.gameObject.SetActive(false);

        yield return new WaitUntil(() => PlayerManager.Instance != null);
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
        // 레이저 콜라이더 초기화
        coll.edgeRadius = 0;
        // 에너지볼 사이즈 초기화
        enemyPush.localScale = Vector2.zero;

        // 레이저 및 에너지볼 켜기
        laser.gameObject.SetActive(true);
        enemyPush.gameObject.SetActive(true);

        // 레이저 사운드 켜기
        AudioSource laserSound = SoundManager.Instance.PlaySound("MagnetBeam", transform, expandTime, 0, -1, true);

        float initWidth = 0.1f;
        float defaultWidth = 3f;

        // 초반 레이저 얇은 굵기로
        DOTween.To(() => laser.startWidth, x => laser.startWidth = x, initWidth, startTime);
        DOTween.To(() => laser.endWidth, x => laser.endWidth = x, initWidth, startTime);
        // 에너지볼 사이즈 동기화
        enemyPush.DOScale(initWidth / 3f * 2.5f, startTime);
        yield return new WaitForSeconds(startTime);

        // 레이저 굵기 키우기
        DOTween.To(() => laser.startWidth, x => laser.startWidth = x, defaultWidth, expandTime);
        DOTween.To(() => laser.endWidth, x => laser.endWidth = x, defaultWidth, expandTime);
        // 레이저 콜라이더 키우기
        DOTween.To(() => coll.edgeRadius, x => coll.edgeRadius = x, defaultWidth / 2f, expandTime);
        // 에너지볼 사이즈 동기화
        enemyPush.DOScale(defaultWidth / 3f * 2.5f, expandTime);
        yield return new WaitForSeconds(expandTime);

        // 에너지볼 끄기
        energyBall.Stop();

        //todo 에너지볼로 몬스터 밀어내기 반복
        // // 초기 위치에서 시작 
        // enemyPush.transform.localPosition = energyStartPos;
        // // 레이저 끝까지 이동
        // enemyPush.transform.DOLocalMove(energyEndPos, 0.5f)
        // .OnStart(() =>
        // {
        //     enemyPush.transform.localScale = Vector2.zero;
        //     //todo 에너지볼 사이즈 확장
        //     enemyPush.DOScale(targetWidth / 3f * 2.5f, pulseDelay / 2f).SetEase(Ease.OutQuint);
        // })
        // .SetLoops(-1, LoopType.Restart);

        // 레이저 굵기 딜레이마다 반복
        float durationCount = durationTime;
        while (durationCount > 0)
        {
            // 기본 사이즈와 작은 사이즈 번갈아 뽑기
            targetWidth = targetWidth != 3f ? 3f : Random.Range(defaultWidth * 0.6f, defaultWidth * 0.9f);

            // 펄스 딜레이 시간 동안 굵기 변화
            DOTween.To(() => laser.startWidth, x => laser.startWidth = x, targetWidth, pulseDelay);
            DOTween.To(() => laser.endWidth, x => laser.endWidth = x, targetWidth, pulseDelay);

            // 펄스 딜레이 절반만큼 대기
            yield return new WaitForSeconds(pulseDelay);

            durationCount -= pulseDelay;
        }

        // 레이저 사운드 끝내기
        SoundManager.Instance.StopSound(laserSound, expandTime);

        // 자석 스프라이트 줄어들기
        magnet.transform.DOScale(Vector3.zero, expandTime)
        .SetEase(Ease.InBack);

        // 레이저 굵기 0으로 줄어들기
        DOTween.To(() => laser.startWidth, x => laser.startWidth = x, 0f, expandTime);
        DOTween.To(() => laser.endWidth, x => laser.endWidth = x, 0f, expandTime);
        // 에너지볼 굵기 0으로 줄어들기
        enemyPush.DOScale(0f, expandTime);
        yield return new WaitForSeconds(expandTime);

        // 레이저 끄기
        laser.gameObject.SetActive(false);

        // 디스폰
        LeanPool.Despawn(transform);
    }

    // IEnumerator PushEnemy()
    // {
    //     // 에너지볼 시작 위치 = 에너지볼 반지름
    //     Vector2 energyStartPos = Vector3.right * 2.5f;
    //     // 에너지볼 끝 위치 = 레이저 길이 - 에너지볼 반지름
    //     Vector2 energyEndPos = Vector3.right * (2.5f + 30f);
    //     //!
    //     print(energyStartPos + " : " + energyEndPos);

    //     // 초기 위치에서 시작 
    //     enemyPush.transform.localPosition = energyStartPos;
    //     // 레이저 끝까지 이동
    //     enemyPush.transform.DOLocalMove(energyEndPos, 1f)
    //     .OnStart(() =>
    //     {
    //         enemyPush.transform.localScale = Vector2.zero;
    //         //todo 에너지볼 사이즈 확장
    //         enemyPush.DOScale(targetWidth / 3f * 2.5f, 0.2f).SetEase(Ease.OutQuint);
    //     })
    //     .SetLoops(-1, LoopType.Restart);
    // }

    private void Update()
    {
        // 시간 멈췄으면 리턴
        if (Time.timeScale == 0)
            return;

        // 마우스 방향 계산
        Vector2 mouseDir = PlayerManager.Instance.GetMousePos() - transform.position;
        float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;

        // 마우스 위치로 회전
        transform.rotation = Quaternion.Euler(Vector3.forward * angle);
    }
}
