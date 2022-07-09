using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class KeepDistanceMove : MonoBehaviour
{
    Rigidbody2D rigid;
    public Transform followTarget;

    private void Awake()
    {
        rigid = rigid == null ? GetComponent<Rigidbody2D>() : rigid;
    }

    void Update()
    {
        FollowMove(followTarget);
    }

    void FollowMove(Transform Getter)
    {
        // 타겟이 없으면 리턴
        if (!followTarget)
            return;

        // 아이템 위치부터 플레이어 쪽으로 방향 벡터
        Vector2 dir = Getter.position - transform.position;

        // 플레이어 반대 방향으로 날아가기
        rigid.DOMove((Vector2)Getter.position - dir.normalized * 5f, 0.3f);
    }
}
