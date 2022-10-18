using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class MetalSword : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] BoxCollider2D coll;
    [SerializeField] SpriteRenderer swordSprite; // 소드 스프라이트
    [SerializeField] List<TrailRenderer> trailList = new List<TrailRenderer>();

    private void Awake()
    {
        // 스프라이트 끄기
        swordSprite.enabled = false;

        // 트레일 모두 끄기
        trailList = GetComponentsInChildren<TrailRenderer>(true).ToList();
        foreach (TrailRenderer trail in trailList)
        {
            trail.Clear();

            trail.enabled = false;
        }

        // 콜라이더 끄기
        coll.enabled = false;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);

        float speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, false);
        float range = MagicDB.Instance.MagicRange(magicHolder.magic);

        // 타겟 방향
        Vector3 targetDir = default;

        // 수동 공격
        if (magicHolder.isManualCast)
        {
            // 타겟 방향
            targetDir = magicHolder.targetPos - PlayerManager.Instance.transform.position;

            // 플레이어 위치에서 마우스 위치로 조금 이동
            transform.position = PlayerManager.Instance.transform.position + targetDir.normalized * 2f;
        }
        // 자동 공격
        else
        {
            // 랜덤 각도를 벡터로 바꾸기
            targetDir = Random.insideUnitCircle;

            // 플레이어 위치에서 마우스 위치로 조금 이동
            transform.position = PlayerManager.Instance.transform.position + targetDir.normalized * 2f;
        }

        float targetAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        // 시작 각도
        Vector3 startDir = Vector3.forward * (targetAngle + 90f);
        // 끝나는 각도
        Vector3 endDir = Vector3.forward * (targetAngle - 90f);

        // range 만큼 스케일 키우기
        transform.localScale = Vector2.one * range;

        // 각도 초기화
        transform.localRotation = Quaternion.Euler(startDir);

        // 트레일 모두 켜기
        foreach (TrailRenderer trail in trailList)
        {
            trail.Clear();

            trail.enabled = true;
        }

        // 스프라이트 컬러 초기화
        swordSprite.enabled = true;
        swordSprite.DOColor(Color.white, 0.2f);
        yield return new WaitForSeconds(0.2f);

        // 콜라이더 켜기
        coll.enabled = true;

        // 일정 각도 회전
        transform.DOLocalRotate(Vector3.forward * -180f, speed, RotateMode.LocalAxisAdd);

        // 트레일 사라질때까지 대기
        yield return new WaitForSeconds(speed);

        // 콜라이더 끄기
        coll.enabled = false;

        // 스프라이트 투명하게
        swordSprite.DOColor(Color.clear, 0.5f);

        yield return new WaitForSeconds(0.5f);

        // 스프라이트 끄기
        swordSprite.enabled = false;

        // 디스폰
        LeanPool.Despawn(transform);
    }
}
