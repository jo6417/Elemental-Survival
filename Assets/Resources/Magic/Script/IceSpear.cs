using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class IceSpear : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] Collider2D coll;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Vector2 originScale; //원래 오브젝트 크기
    [SerializeField] float addAngle; //스프라이트 방향 보정
    [SerializeField] ParticleSystem stingEffect; // 찌르기 이펙트
    [SerializeField] ParticleManager snowEffect; // 눈꽃 이펙트

    private void Awake()
    {
        // 오브젝트 기본 사이즈 저장
        originScale = transform.localScale;

        // //시작할때 콜라이더 끄기
        // ColliderTrigger(false);
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 크기 초기화
        transform.localScale = Vector2.zero;

        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);
        // 마법 오브젝트 속도, 숫자가 작을수록 빠름
        float speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, false);
        // 냉동 시간 갱신
        magicHolder.freezeTime = magicHolder.duration;

        // 콜라이더 켜기
        coll.enabled = true;
        // 스프라이트 켜기
        sprite.enabled = true;

        //바라볼 방향 = 목표위치 - 시작점
        Vector2 endDir = (Vector2)magicHolder.targetPos - (Vector2)PlayerManager.Instance.transform.position;

        //시작점 = 플레이어 위치로부터 목표 방향 20% 위치
        Vector2 startPos = (Vector2)PlayerManager.Instance.transform.position + endDir.normalized;

        //목적지 = 플레이어 위치 + (목표 방향 * 범위)
        Vector2 endPos = (Vector2)PlayerManager.Instance.transform.position + endDir.normalized * magicHolder.range;

        //방향 각도 구하기
        float endRotation = Mathf.Atan2(endDir.y, endDir.x) * Mathf.Rad2Deg;
        endRotation += addAngle;
        // float startRotation = endRotation + 181f;

        //목표 위치로 회전
        transform.rotation = Quaternion.Euler(Vector3.forward * endRotation);

        //시작 위치로 이동
        transform.position = startPos;

        //목표 위치로 찌르기
        Sequence stingSeq = DOTween.Sequence();
        stingSeq
        .Append(
            // 0,0에서 점점 커지면서 오브젝트 나타내기
            transform.DOScale(originScale, speed)
            .SetEase(Ease.OutBack)
        )
        .Join(
            //한바퀴 돌리기
            transform.DORotate(Vector3.forward * 360f, speed, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutBack)
        )
        .AppendCallback(() =>
        {
            // 찌르기 이펙트 시작
            stingEffect.Play();
        })
        .Append(
            //목적지를 향해 찌르기
            transform.DOMove(endPos, speed)
            .SetEase(Ease.InBack)
        )
        .AppendCallback(() =>
        {
            // 찌르기 이펙트 정지
            stingEffect.Stop();
        })
        .Join(
            //시작위치로 돌아오기
            transform.DOMove(startPos, speed)
            .SetEase(Ease.OutBack)
        )
        .AppendCallback(() =>
        {
            // 콜라이더 끄기
            coll.enabled = false;
            // 스프라이트 끄기
            sprite.enabled = false;
        });

        // 콜라이더 꺼질때까지 대기
        yield return new WaitUntil(() => !coll.enabled);

        // 눈꽃 이펙트 정지
        snowEffect.SmoothStop();
        // 눈꽃 이펙트 정지 끝날때 까지 대기
        yield return new WaitForSeconds(snowEffect.particle.main.startLifetime.constantMax);

        // 디스폰
        StartCoroutine(AutoDespawn());
    }

    IEnumerator AutoDespawn(float delay = 0f)
    {
        //range 속성만큼 지속시간 부여
        yield return new WaitForSeconds(delay);

        // //콜라이더 끄고 종료
        // ColliderTrigger(false);

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }
}
