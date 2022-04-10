using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class MagicSting : MonoBehaviour
{
    [Header("Refer")]
    private MagicInfo magic;
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

        //초기화 하기
        StartCoroutine(Initial());
    }

    private void OnEnable()
    {
        StartCoroutine(StingMagicObj());
    }

    IEnumerator Initial()
    {
        //시작할때 콜라이더 끄기
        ColliderTrigger(false);

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => GetComponentInChildren<MagicHolder>() != null);
        magicHolder = GetComponentInChildren<MagicHolder>();
        magic = magicHolder.magic;

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (magic == null)
        {
            magic = MagicDB.Instance.GetMagicByName(transform.name.Split('_')[0]);
            magicName = magic.magicName;
        }

        //magic 못찾으면 코루틴 종료
        if (magic == null)
            yield break;
    }

    IEnumerator StingMagicObj()
    {
        //초기화
        StartCoroutine(Initial());

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magic != null);

        // 마법 오브젝트 속도, 숫자가 작을수록 빠름
        float speed = MagicDB.Instance.MagicSpeed(magic, false);

        //범위 계산
        float range = MagicDB.Instance.MagicRange(magic);

        //목표 위치 가져오기
        Vector2 targetPos = magicHolder.targetPos;

        // 0,0에서 점점 커지면서 오브젝트 나타내기
        transform.localScale = Vector2.zero;
        transform.DOScale(originScale, 0.5f);

        //시작 위치
        Vector2 startPos = (Vector2)PlayerManager.Instance.transform.position;

        //목적지 = 시작점 + (목적지 방향 벡터) * 범위
        Vector2 endDir = targetPos - (Vector2)PlayerManager.Instance.transform.position;
        Vector2 endPos = startPos + endDir.normalized * range;

        //방향 각도 구하기
        float endRotation = Mathf.Atan2(endDir.y, endDir.x) * Mathf.Rad2Deg;
        endRotation += addAngle;
        float startRotation = endRotation + 180f;

        //목표 위치로 회전
        transform.rotation = Quaternion.Euler(Vector3.forward * startRotation);

        //플레이어 위치에서 시작
        transform.position = startPos;

        //방향 전환을 위한 마지막 위치 벡터
        Vector2 lastPos = startPos;

        //목표 위치로 찌르기
        Sequence stingSeq = DOTween.Sequence();
        stingSeq
        .Append(
            //진행할 방향 바라보기
            transform.DORotate(Vector3.forward * endRotation, 0.5f)
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
            // 마법 오브젝트 속도
            float duration = MagicDB.Instance.MagicDuration(magic);

            // 오브젝트 자동 디스폰하기
            StartCoroutine(AutoDespawn(duration));
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

    IEnumerator AutoDespawn(float duration)
    {
        //range 속성만큼 지속시간 부여
        yield return new WaitForSeconds(duration);

        //콜라이더 끄고 종료
        ColliderTrigger(false);

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }
}
