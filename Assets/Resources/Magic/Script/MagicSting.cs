using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class MagicSting : MonoBehaviour
{
    [Header("Refer")]
    MagicHolder magicHolder;
    public string magicName;
    public Collider2D col;
    public Vector2 originScale; //원래 오브젝트 크기
    public Ease stingEase;
    public float addAngle; //스프라이트 방향 보정
    public bool isExpand = false; //커지면서 등장 여부

    [Header("Effect")]
    public GameObject magicEffect;
    public Vector3 effectPos; //이펙트 생성 위치 보정

    private void Awake()
    {
        // 오브젝트 기본 사이즈 저장
        originScale = transform.localScale;

        //시작할때 콜라이더 끄기
        ColliderTrigger(false);
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);

        // 마법 오브젝트 속도, 숫자가 작을수록 빠름
        float speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, false);

        //목표 위치 가져오기
        Vector2 targetPos = magicHolder.targetPos;

        // 크기 초기화
        transform.localScale = Vector2.zero;

        //바라볼 방향 = 목표위치 - 시작점
        Vector2 endDir = targetPos - (Vector2)PlayerManager.Instance.transform.position;

        //시작점 = 플레이어 위치로부터 목표 방향 20% 위치
        Vector2 startPos = (Vector2)PlayerManager.Instance.transform.position + endDir.normalized * magicHolder.range * 0.2f;

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

        //방향 전환을 위한 마지막 위치 벡터
        Vector2 lastPos = startPos;

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
            //콜라이더 발동시키기
            ColliderTrigger(true);
        })
        .Append(
            //목적지를 향해 찌르기
            transform.DOMove(endPos, speed / 2)
            .SetEase(Ease.InBack)
        )
        .Append(
            //시작위치로 돌아오기
            transform.DOMove(startPos, speed / 2)
            .SetEase(Ease.OutBack)
        )
        .AppendCallback(() =>
        {
            // 오브젝트 자동 디스폰하기
            StartCoroutine(AutoDespawn());
        });
    }

    public void ColliderTrigger(bool magicTrigger = true)
    {
        // 마법 트리거 발동 됬을때 (적 데미지 입히기, 마법 효과 발동)
        if (magicTrigger)
        {
            col.enabled = true;

            // 이펙트 오브젝트 생성 (이펙트 있으면)
            if (magicEffect)
                LeanPool.Spawn(magicEffect, transform.position + effectPos, Quaternion.identity);
        }
        else
        {
            col.enabled = false;
        }
    }

    IEnumerator AutoDespawn(float duration = 0f)
    {
        //range 속성만큼 지속시간 부여
        yield return new WaitForSeconds(duration);

        //콜라이더 끄고 종료
        ColliderTrigger(false);

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }
}
