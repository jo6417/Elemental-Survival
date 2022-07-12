using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class KeepDistanceMove : MonoBehaviour
{
    Rigidbody2D rigid;
    public Transform followTarget;

    public float keepDistance = 5f; // 타겟과의 거리

    private void Awake()
    {
        rigid = rigid == null ? GetComponent<Rigidbody2D>() : rigid;
    }

    void Update()
    {
        FollowMove();
    }

    void FollowMove()
    {
        // 타겟이 없으면 리턴
        if (!followTarget)
            return;

        // 현재 위치부터 타겟 방향으로 방향 벡터
        Vector2 dir = followTarget.position - transform.position;

        // 벡터 길이 계산
        float moveSpeed = dir.magnitude - keepDistance;

        // 이동시키기
        rigid.velocity = dir.normalized * moveSpeed * 5f;
    }
}
