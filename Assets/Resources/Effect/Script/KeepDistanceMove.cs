using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class KeepDistanceMove : MonoBehaviour
{
    [Header("Refer")]
    public Rigidbody2D rigid;
    public Transform followTarget;

    [Header("State")]
    public float minDistance = 3f; // 타겟과 최소 거리
    public float maxDistance = 3f; // 타겟과의 유지거리
    public float addSpeed = 5f; // 추가 속도

    [Header("State")]
    [SerializeField] Transform jumpObj; // 점프시킬 오브젝트
    public AnimationCurve jumpCurve;
    public float jumpDelay;

    private void Awake()
    {
        rigid = rigid == null ? GetComponent<Rigidbody2D>() : rigid;
    }

    private void OnEnable()
    {
        // if (jumpObj != null)
        // {
        //     // 점프 오브젝트 위치 초기화
        //     jumpObj.position = Vector2.zero;
        // }
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

        // // 점프 오브젝트 있을때만
        // if (jumpObj != null)
        //     // 점프중 아닐때, 이동중일때
        //     if (jumpObj.localPosition == Vector3.zero && Mathf.Sqrt(moveSpeed) > 0.1f)
        //     {
        //         jumpObj.DOLocalJump(Vector2.zero, 1f, 1, 0.5f)
        //         .SetDelay(jumpDelay)
        //         .SetEase(jumpCurve);
        //     }
    }
}
