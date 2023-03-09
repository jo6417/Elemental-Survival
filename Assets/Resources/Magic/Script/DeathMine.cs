using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class DeathMine : MonoBehaviour
{
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] SpriteRenderer bombSprite; //폭탄 이미지
    [SerializeField] GameObject scorchPrefab; // 그을음 프리팹
    [SerializeField] GameObject explosionPrefab;
    [SerializeField] SpriteRenderer bombLight;
    [SerializeField] ParticleSystem timerTextParticle;
    [SerializeField] ParticleSystem landEffect;
    [SerializeField] Collider2D coll;
    [SerializeField] Transform shadow;
    [SerializeField] ParticleSystem sparkEffect; // 폭발 직전 불꽃 파티클
    [SerializeField] SpriteRenderer rangeSprite;
    [SerializeField] SpriteRenderer rangeFillSprite;
    bool isExplode = false;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 폭발 여부 초기화
        isExplode = false;

        // 콜라이더 끄기
        coll.enabled = false;

        // 착지 이펙트 끄기
        landEffect.gameObject.SetActive(false);

        // 폭발 콜라이더 끄기
        rangeSprite.GetComponent<Collider2D>().enabled = false;

        // 폭발 범위 스케일 초기화
        rangeSprite.transform.localScale = Vector2.zero;
        rangeFillSprite.transform.localScale = Vector2.zero;
        // 폭발 범위 끄기
        rangeSprite.enabled = true;
        rangeFillSprite.enabled = true;

        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder.initDone);

        // 라이트 색 초기화
        bombLight.color = Color.cyan;

        // 타이머 파티클 색 초기화
        ParticleSystem.MainModule timerMain = timerTextParticle.main;
        timerMain.startColor = Color.cyan;

        // 타이머 파티클 시간 초기화
        timerMain.startLifetime = magicHolder.duration;

        // 폭탄 스프라이트 켜기
        bombSprite.enabled = true;
        // 그림자 켜기
        shadow.gameObject.SetActive(true);

        // targetPos 들어올때까지 대기
        yield return new WaitUntil(() => magicHolder.targetPos != null);

        // 타겟 위치로 이동
        transform.position = magicHolder.targetPos;

        // 위로 폭탄 이미지 올리기
        bombSprite.transform.localPosition = Vector3.up * 2f;

        // 아래로 이동
        bombSprite.transform.DOLocalMove(Vector3.zero, 1f)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
            // 착지 이펙트 켜기
            landEffect.gameObject.SetActive(true);
        });

        yield return new WaitForSeconds(1.2f);

        // 타이머 파티클 켜기
        timerTextParticle.Play();

        // 콜라이더 켜기
        coll.enabled = true;

        // duration 동안 대기
        yield return new WaitForSeconds(magicHolder.duration);

        // 아직 안터졌으면 폭파
        if (!isExplode)
            Explode();
    }

    private void FixedUpdate()
    {
        // 그림자 회전값 고정
        shadow.rotation = Quaternion.Euler(Vector3.zero);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // 플레이어 충돌시
        if (other.gameObject.CompareTag(TagNameList.Player.ToString()))
        {
            // 콜라이더 끄기, 중복 충돌 방지
            coll.enabled = false;

            // 폭발하기
            Explode();
        }

        // 몬스터 충돌시
        if (other.gameObject.CompareTag(TagNameList.Enemy.ToString()))
        {
            // 콜라이더 끄기, 중복 충돌 방지
            coll.enabled = false;

            // 폭발하기
            Explode();
        }
    }

    public void Explode()
    {
        // 폭발하기
        StartCoroutine(Explosion());
    }

    IEnumerator Explosion()
    {
        // 폭발 여부 true
        isExplode = true;

        // 폭발 범위 스케일 초기화
        rangeSprite.transform.DOScale(Vector2.one * magicHolder.range, 0.1f);
        rangeFillSprite.transform.DOScale(Vector2.one, 1f);

        // 타이머 파티클 빨갛게
        ParticleSystem.MainModule timerMain = timerTextParticle.main;
        timerMain.startColor = Color.red;

        // 폭탄 라이트 빨간색으로 깜빡이기
        bombLight.DOColor(Color.red, 0.5f)
        .SetEase(Ease.Flash, 10f, 1)
        .SetLoops(2, LoopType.Yoyo);

        // 불꽃 파티클 켜기
        sparkEffect.gameObject.SetActive(true);

        // 깜빡이며 대기
        yield return new WaitForSeconds(1f);

        // 타이머 파티클 끄기
        timerTextParticle.Stop();
        // 폭탄 스프라이트 끄기
        bombSprite.enabled = false;
        // 그림자 끄기
        shadow.gameObject.SetActive(false);

        // 폭발 콜라이더 켜기
        rangeSprite.GetComponent<Collider2D>().enabled = true;

        // 폭발 범위 끄기
        rangeSprite.enabled = false;
        rangeFillSprite.enabled = false;

        // 폭발 이펙트 스폰
        GameObject explosionHit = LeanPool.Spawn(explosionPrefab, transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

        // 폭발 사운드 재생
        SoundManager.Instance.PlaySound("Explosion_Tiny", transform.position);

        // 그을음 남기기
        LeanPool.Spawn(scorchPrefab, transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

        yield return new WaitForEndOfFrame();

        // 폭발 콜라이더 끄기
        rangeSprite.GetComponent<Collider2D>().enabled = false;

        // 지뢰 디스폰
        LeanPool.Despawn(transform);
    }
}
