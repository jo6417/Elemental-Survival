using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class RockHand : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] ParticleManager dirtPileParticle;
    [SerializeField] ParticleManager dirtBustParticle;
    [SerializeField] BoxCollider2D coll;
    [SerializeField] SpriteRenderer handSprite;
    [SerializeField] List<Sprite> handImgs = new List<Sprite>();

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 스프라이트 끄기
        handSprite.enabled = false;
        // 콜라이더 끄기
        coll.enabled = false;

        // 나오고 들어가는 시간
        float startTime = 0.2f;
        float endTime = 0.5f;

        // 마법 스탯 초기화
        yield return new WaitUntil(() => magicHolder.magic != null);

        float speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, false);
        float range = MagicDB.Instance.MagicRange(magicHolder.magic);
        float duration = MagicDB.Instance.MagicDuration(magicHolder.magic);

        // 스턴 시간 초기화
        magicHolder.stunTime = duration;

        // 스케일 초기화
        transform.localScale = Vector3.one * range / 10f;

        // 타겟 위치로 이동
        transform.position = magicHolder.targetPos;

        // 파티클 켜기
        dirtPileParticle.gameObject.SetActive(true);

        // 스프라이트 리스트중 하나로 이미지 변경
        handSprite.sprite = handImgs[Random.Range(0, handImgs.Count)];

        // 스프라이트 켜기
        handSprite.enabled = true;

        // 콜라이더 위치 초기화
        coll.offset = Vector2.zero;
        // 콜라이더 사이즈 초기화
        coll.size = new Vector2(3, 0);
        // 콜라이더 켜기
        coll.enabled = true;

        // 주먹 위로 튀어나오기
        handSprite.transform.DOLocalMove(new Vector2(0, 4f), startTime)
        .SetEase(Ease.Linear);

        // 콜라이더 위치 조정
        DOTween.To(() => coll.offset, x => coll.offset = x, new Vector2(0f, -2f), startTime)
        .SetEase(Ease.Linear);
        // 콜라이더 사이즈 키우기
        DOTween.To(() => coll.size, x => coll.size = x, new Vector2(3f, 4f), startTime)
        .SetEase(Ease.Linear);

        // 튀어나올때 흙 튀기기
        dirtBustParticle.gameObject.SetActive(true);

        yield return new WaitForSeconds(startTime);

        // 콜라이더 끄기
        coll.enabled = false;

        // speed 만큼 대기
        yield return new WaitForSeconds(speed);

        // 다시 밑으로 들어가기
        handSprite.transform.DOLocalMove(Vector2.zero, endTime)
        .SetEase(Ease.Linear);

        yield return new WaitForSeconds(endTime);

        // 파티클 끄기
        dirtPileParticle.SmoothDisable();
        yield return new WaitUntil(() => !dirtPileParticle.gameObject.activeSelf);

        // 디스폰
        LeanPool.Despawn(transform);
    }
}
