using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Egg : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] Collider2D coll;
    public KeepDistanceMove keepDistanceMove;
    public MagicHolder magicHolder;
    public SpriteRenderer eggSprite;
    public SpriteRenderer shadow;
    [SerializeField] Color readyColor; // 투척 준비시 색깔
    [SerializeField] Color shotColor; // 투척시 색깔
    [SerializeField] GameObject explosionPrefab; // 폭발 프리팹
    [SerializeField] ParticleManager trailEffect;

    [Header("State")]
    float range;
    float speed;
    float duration;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 마법 스탯 초기화
        yield return new WaitUntil(() => magicHolder.magic != null);

        range = MagicDB.Instance.MagicRange(magicHolder.magic);
        speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, false);
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);

        // 파티클 트레일 끄기
        trailEffect.gameObject.SetActive(false);

        // 콜라이더 끄기
        coll.enabled = false;
        // 스프라이트 켜기
        eggSprite.enabled = true;
        // 그림자 끄기
        shadow.enabled = true;
    }

    public IEnumerator SingleShot(int index)
    {
        // 달걀 따라오기 그만
        keepDistanceMove.enabled = false;
        keepDistanceMove.rigid.velocity = Vector3.zero;

        // 파티클 트레일 켜기
        trailEffect.gameObject.SetActive(true);

        // 달걀 인덱스에 따라 딜레이
        yield return new WaitForSeconds(index * 0.1f);

        // 달걀 색 변경
        eggSprite.color = shotColor;

        // 타겟 위치
        Vector2 targetPos = magicHolder.targetPos;

        // range만큼 오차 추가
        if (index > 0)
            targetPos += Random.insideUnitCircle.normalized * range / 5f;

        // 타겟 위치로 달걀 이동
        transform.DOMove(targetPos, speed)
        .SetEase(Ease.InSine);
        // 달걀 높이 점프
        eggSprite.transform.DOLocalJump(Vector2.zero, 3f, 1, speed)
        .SetEase(Ease.InSine);

        yield return new WaitForSeconds(speed);

        // 달걀 색 초기화
        eggSprite.color = Color.white;

        // 폭발 프리팹 생성
        LeanPool.Spawn(explosionPrefab, transform.position, Quaternion.identity, SystemManager.Instance.magicPool);

        // 콜라이더 켜기
        coll.enabled = true;
        // 스프라이트 끄기
        eggSprite.enabled = false;
        // 그림자 끄기
        shadow.enabled = false;

        yield return new WaitForSeconds(0.1f);

        // 콜라이더 끄기
        coll.enabled = false;

        // 파티클 사라질때까지 대기
        trailEffect.SmoothDisable();
        yield return new WaitUntil(() => !trailEffect.gameObject.activeSelf);

        // 달걀 디스폰
        LeanPool.Despawn(transform);
    }
}
