using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class HotBlade : MonoBehaviour
{
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] GhostTrail ghostSpawner;
    [SerializeField] Collider2D coll;
    [SerializeField] Rigidbody2D rigid;
    [SerializeField] ParticleManager particle;
    WaitForSeconds waitSecond;
    IEnumerator collToggleCoroutine;

    private void OnEnable()
    {
        waitSecond = new WaitForSeconds(Time.deltaTime);

        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 콜라이더 끄기
        coll.enabled = false;

        // 스케일 제로
        transform.localScale = Vector2.zero;

        // 초기화
        yield return new WaitUntil(() => magicHolder.magic != null);

        // 스탯 초기화
        float range = MagicDB.Instance.MagicRange(magicHolder.magic);
        float speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, false);
        float duration = MagicDB.Instance.MagicDuration(magicHolder.magic);

        // 톱날 회전
        transform.DOLocalRotate(Vector3.forward * 360f, 0.2f, RotateMode.LocalAxisAdd)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Incremental);

        // 스케일 키우기
        transform.DOScale(Vector2.one * range * 0.1f, 0.5f);

        // 명중률 낮추기
        // targetPos += Random.insideUnitCircle.normalized;

        // 스프라이트 켜기
        sprite.enabled = true;

        // 잔상 켜기
        ghostSpawner.gameObject.SetActive(true);

        // 파티클 켜기
        particle.gameObject.SetActive(true);

        // 콜라이더 지속 깜빡이기
        collToggleCoroutine = ColliderToggle();
        StartCoroutine(collToggleCoroutine);

        // 타겟 위치로 날아가기
        transform.DOMove(magicHolder.targetPos, speed)
        .SetEase(Ease.OutExpo);

        float durationCount = duration;

        // 지속시간 만큼 대기
        yield return new WaitForSeconds(duration);

        // 플레이어 방향 벡터 계산
        Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 일정 거리 이하 가까워질 때까지 반복
        while (playerDir.magnitude > 1f)
        {
            // 플레이어 방향 벡터 갱신
            playerDir = PlayerManager.Instance.transform.position - transform.position;

            // 플레이어 방향으로 날아가기
            rigid.velocity = playerDir.normalized * 50f;

            yield return waitSecond;
        }

        // 콜라이더 깜빡이기 그만
        StopCoroutine(collToggleCoroutine);
        // 콜라이더 끄기
        coll.enabled = false;

        // 스프라이트 끄기
        sprite.enabled = false;

        // 잔상 끄기
        ghostSpawner.gameObject.SetActive(false);

        // 파티클 끄기
        particle.SmoothDisable();

        // 파티클 꺼질때까지 대기
        yield return new WaitUntil(() => !particle.gameObject.activeSelf);

        // 디스폰
        LeanPool.Despawn(transform);
    }

    IEnumerator ColliderToggle()
    {
        // 마법 살아있을때 계속
        while (gameObject.activeSelf)
        {
            // 콜라이더 토글
            coll.enabled = !coll.enabled;

            yield return waitSecond;
        }
    }
}
