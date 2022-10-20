using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SlingShot : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    public ParticleManager hitEffect;
    public Rigidbody2D rigid;
    [SerializeField] Collider2D coll;

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    private void OnDisable()
    {
        //타겟 위치 초기화
        magicHolder.targetPos = Vector2.zero;
    }

    IEnumerator Init()
    {
        //콜라이더 끄기
        coll.enabled = false;

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);

        // 마법 스피드 계산 + 추가 스피드 곱하기
        float speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, false);

        //콜라이더 켜기
        coll.enabled = true;

        // 타겟 위치로 날아가기
        transform.DOMove(magicHolder.targetPos, speed)
        .SetEase(Ease.InBack);

        // 투사체 날릴 방향
        Vector2 dir = magicHolder.targetPos;

        // 날아가는 방향따라 회전 시키기
        rigid.angularVelocity = dir.x > 0 ? -dir.magnitude * 30f : dir.magnitude * 30f;
    }
}
