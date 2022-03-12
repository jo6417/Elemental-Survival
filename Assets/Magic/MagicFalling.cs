using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class MagicFalling : MonoBehaviour
{
    public float upDistance = 2f;
    public MagicInfo magic;
    public BoxCollider2D col;
    public Vector2 originColliderSize;
    public Vector2 originScale;
    public Ease fallEase;
    public float magicSpeed = 1f;
    public bool isAutoDespawn = true;
    public float magicDuration = 0.5f;    

    [Header("Effect")]
    public GameObject magicEffect;
    public Vector3 effectPos;
    
    private void Awake() {
        // 콜라이더 사이즈 저장
        originColliderSize = col.size;
        // 오브젝트 기본 사이즈 저장
        originScale = transform.localScale;

        //시작할때 콜라이더 끄기
        MagicTrigger(false);
    }

    private void OnEnable() {
        StartCoroutine(FallingMagicObj());
    }

    IEnumerator FallingMagicObj()
    {
        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magic != null);

        // 속도 버프 계수
        float speedBuff = magicSpeed * ( (magic.speed - 1f) + (PlayerManager.Instance.rateFire - 1f) );
        // 마법 오브젝트 속도
        float speed = magicSpeed - speedBuff;

        // 속도 버프 계수
        float durationBuff = magicDuration * (PlayerManager.Instance.duration - 1f);
        // 마법 오브젝트 속도
        float duration = magicDuration - durationBuff;

        // 팝업창 0,0에서 점점 커지면서 나타내기
        transform.localScale = Vector2.zero;
        transform.DOScale(originScale, speed)
        .SetUpdate(true)
        .SetEase(Ease.OutBack);

        Vector2 startPos = (Vector2)transform.position + Vector2.up * upDistance;
        Vector2 endPos = transform.position;

        //시작 위치로 올려보내기
        transform.position = startPos;
        //목표 위치로 떨어뜨리기        
        transform.DOMove(endPos, speed)
        .SetEase(fallEase)
        .OnComplete(() => {
            // 이펙트 오브젝트 생성
            LeanPool.Spawn(magicEffect, transform.position + effectPos, Quaternion.identity);

            //콜라이더 발동시키기
            MagicTrigger(true);

            // 오브젝트 자동 디스폰하기
            if(isAutoDespawn)
            StartCoroutine(AutoDespawn(duration));
        });
    }

    IEnumerator AutoDespawn(float duration)
    {
        //range 속성만큼 지속시간 부여
        yield return new WaitForSeconds(duration);

        //콜라이더 끄고 종료
        MagicTrigger(false);

        if(isAutoDespawn)
        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }

    void MagicTrigger(bool magicTrigger)
    {
        // magicInfo 데이터로 히트박스 크기 적용
        // if(magic != null)
        // col.size = originColliderSize * magic.range;

        // 마법 트리거 발동 됬을때 (적 데미지 입히기, 마법 효과 발동)
        if (magicTrigger)
        {
            col.enabled = true;
        }
        else
        {
            col.enabled = false;
        }
    }
}
