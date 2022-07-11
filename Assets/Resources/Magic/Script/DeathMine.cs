using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class DeathMine : MonoBehaviour
{
    MagicHolder magicHolder;
    MagicInfo magic;
    bool atkAble = false;
    [SerializeField]
    GameObject explosionPrefab;
    [SerializeField]
    SpriteRenderer bombLight;
    [SerializeField]
    ParticleSystem runeLaser;

    private void Awake()
    {
        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        // 공격 불가능
        atkAble = false;

        // magic 정보 들어올때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 라이트 색 초기화
        bombLight.color = Color.cyan;

        // 룬 레이저 끄기
        runeLaser.gameObject.SetActive(false);

        // 룬 레이저 색 초기화
        ParticleSystem.ColorOverLifetimeModule particleColor = runeLaser.colorOverLifetime;
        particleColor.enabled = false;

        // 마법 range 만큼 감지 및 폭발 범위 적용
        explosionPrefab.transform.localScale = Vector2.one * MagicDB.Instance.MagicRange(magic);

        // targetPos 들어올때까지 대기
        yield return new WaitUntil(() => magicHolder.targetPos != null);

        // 타겟 위치보다 위로 이동
        // transform.position = magicHolder.targetPos + Vector3.up;

        //todo 아래로 이동
        transform.parent.DOMove(magicHolder.targetPos, 1f)
        .SetEase(Ease.InBack);
        yield return new WaitForSeconds(1.2f);

        // 룬 레이저 켜기
        runeLaser.gameObject.SetActive(true);

        // 공격 가능
        atkAble = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 공격 가능할때, 플레이어가 접근하면
        if (atkAble && other.CompareTag("Player"))
        {
            atkAble = false;

            // 폭발하기
            StartCoroutine(Explosion());
        }
    }

    IEnumerator Explosion()
    {
        // 룬 문자 파티클 깜빡이 켜기
        ParticleSystem.ColorOverLifetimeModule particleColor = runeLaser.colorOverLifetime;
        particleColor.enabled = true;

        // 라이트 색 바꾸며 깜빡이기
        bombLight.DOColor(Color.red, 1f)
        .SetLoops(2, LoopType.Yoyo);

        // 깜빡이는 2초간 대기
        yield return new WaitForSeconds(2f);

        // 폭발 이펙트 스폰
        GameObject effect = LeanPool.Spawn(explosionPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        // 일단 비활성화
        effect.SetActive(false);

        //폭발에 마법 정보 넣기
        MagicHolder effectHolder = effect.GetComponent<MagicHolder>();
        effectHolder.magic = magic;
        effectHolder.targetType = MagicHolder.Target.Enemy;

        // 폭발 활성화
        effect.SetActive(true);

        // 지뢰 디스폰
        LeanPool.Despawn(transform.parent);
    }
}
