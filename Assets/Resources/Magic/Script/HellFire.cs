using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class HellFire : MonoBehaviour
{
    public float angleOffset; //스프라이트 방향 보정
    public Vector2 startOffset; //시작할 위치

    [Header("Refer")]
    public ParticleSystem mainParticle;
    public Collider2D coll;
    public MagicInfo magic;
    public MagicHolder magicHolder;
    public GameObject despawnEffectPrefab;
    public GameObject indicator;

    [Header("Magic Stat")]
    float speed;

    private void OnEnable()
    {
        //초기화 하기
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        //스케일 초기화
        transform.localScale = Vector3.one;

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        //마법 떨어뜨리기
        StartCoroutine(FallMagic());
    }

    IEnumerator FallMagic()
    {
        // 마법 오브젝트 속도, 숫자가 작을수록 빠름
        speed = MagicDB.Instance.MagicSpeed(magic, false);

        //시작 위치
        Vector2 startPos = startOffset + (Vector2)magicHolder.targetPos;

        //떨어질 자리에 인디케이터 표시
        GameObject shadow = LeanPool.Spawn(indicator, magicHolder.targetPos, Quaternion.identity, SystemManager.Instance.effectPool);

        //인디케이터 사이즈 0,0으로 초기화
        shadow.transform.localScale = Vector3.zero;

        //끝나는 위치
        Vector2 endPos = magicHolder.targetPos;

        //시작 위치로 올려보내기
        transform.position = startPos;

        //목표 위치 방향으로 회전
        Vector2 dir = endPos - startPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle + angleOffset, Vector3.forward);

        //위치 올라갈때까지 대기
        yield return new WaitUntil(() => (Vector2)transform.position == startPos);

        //인디케이터 사이즈 점점 키우기
        shadow.transform.DOScale(new Vector2(5, 2), speed);

        //목표 위치로 떨어뜨리기
        Tween fallTween = transform.DOMove(endPos, speed)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            //인디케이터 디스폰
            LeanPool.Despawn(shadow);

            // 이펙트 오브젝트 생성
            LeanPool.Spawn(despawnEffectPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            // 스케일 줄어들어 사라지기
            transform.DOScale(Vector3.zero, 0.5f)
            .OnComplete(() =>
            {
                //디스폰
                LeanPool.Despawn(transform);
            });
        });
    }
}
