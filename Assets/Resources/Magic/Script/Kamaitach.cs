using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Kamaitach : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] ParticleManager particleManager;
    [SerializeField] BoxCollider2D coll;
    [SerializeField] TrailRenderer playerTrail; // 플레이어 이동시 남기는 꼬리

    [Header("Spec")]
    float range;
    float duration;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 콜라이더 끄기
        coll.enabled = false;

        //magic 불러올때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        range = MagicDB.Instance.MagicRange(magicHolder.magic);
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);

        // 공격 시작
        StartCoroutine(StartAttack());
    }

    IEnumerator StartAttack()
    {
        // 원래 플레이어 위치기록
        Vector3 nowPlayerPos = PlayerManager.Instance.transform.position;

        // 플레이어 현재 위치부터 마우스 위치까지 방향 벡터
        Vector3 movePos = (Vector3)PlayerManager.Instance.GetMousePos() - nowPlayerPos;
        // 이동할 위치 계산, range 보다 멀리 이동하려고 하면 길이 제한
        movePos = movePos.magnitude > range ? nowPlayerPos + movePos.normalized * range : nowPlayerPos + movePos;

        // 수동 시전 했을때
        if (magicHolder.isManualCast)
        {
            // 시전여부 초기화
            magicHolder.isManualCast = false;

            // 플레이어 위치에 트레일 오브젝트 소환
            TrailRenderer trail = LeanPool.Spawn(playerTrail, nowPlayerPos + Vector3.up, Quaternion.identity, SystemManager.Instance.effectPool);

            // 타겟 위치로 플레이어 이동 시키기
            PlayerManager.Instance.transform.DOMove(movePos, 0.2f);
            // 트레일 오브젝트도 같이 이동 시키기
            trail.transform.DOMove(movePos + Vector3.up, 0.2f);

            // 트레일 줄여서 없에기
            DOTween.To(() => trail.widthMultiplier, x => x = trail.widthMultiplier, 0f, 0.5f)
            .OnComplete(() =>
            {
                // 꼬리 디스폰 시키기
                LeanPool.Despawn(trail.gameObject);
            });
        }

        // 원래 플레이어 위치, 타겟 위치 중간 지점에 오브젝트 위치 이동 시키기
        transform.position = (nowPlayerPos + movePos) / 2f;

        // 타겟 위치 바라보게 회전
        Vector2 rotation = nowPlayerPos - movePos;
        float angle = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(Vector3.forward * angle);

        // 두 지점 거리만큼 공격 범위 늘리기
        float distance = Vector2.Distance(nowPlayerPos, movePos);
        transform.localScale = new Vector3(distance, distance / 2f, 1);

        // 거리에 비례해서 파티클 개수 갱신
        ParticleSystem.EmissionModule emission = particleManager.particle.emission;
        emission.rateOverTime = distance * 5f;

        print(distance);

        // 콜라이더 깜빡여서 공격
        yield return StartCoroutine(FlickerColl());

        // 파티클 끝나면 디스폰
        particleManager.SmoothDespawn();
    }

    IEnumerator FlickerColl()
    {
        // 깜빡일 시간 받기
        float flickCount = duration;
        while (flickCount > 0)
        {
            // 콜라이더 토글
            coll.enabled = !coll.enabled;

            // 잠깐 대기
            flickCount -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }
}
