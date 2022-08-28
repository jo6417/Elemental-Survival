using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class KeepDistanceMove : MonoBehaviour
{
    Rigidbody2D rigid;
    public Transform followTarget;

    public float minDistance = 3f; // 타겟과 최소 거리
    public float maxDistance = 3f; // 타겟과의 유지거리
    public float addSpeed = 5f; // 추가 속도

    private void Awake()
    {
        rigid = rigid == null ? GetComponent<Rigidbody2D>() : rigid;
    }

    private void FixedUpdate()
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
        float moveSpeed = 0;

        // 타겟과의 거리
        float distance = dir.magnitude;

        // 최소 거리보다 가까울때
        if (distance < minDistance)
        {
            // 거리 차이만큼 속도 계산
            moveSpeed = distance - minDistance;
        }
        // 최대 거리보다 멀때
        else if (distance > maxDistance)
        {
            // 거리 차이만큼 속도 계산
            moveSpeed = distance - maxDistance;
        }

        // 이동시키기
        rigid.velocity = dir.normalized * moveSpeed * addSpeed;
    }
}
