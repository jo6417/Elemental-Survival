using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CurveTest : MonoBehaviour
{
    // Rigidbody2D rigidArrow;
    [SerializeField] Collider2D coll;
    [SerializeField] float orbitAngle; // 현재 공전 각도
    [SerializeField] float orbitSpeed = 10f;
    [SerializeField] float weight = 3f; // 베지어 가중치

    public Transform playerTransform; // 플레이어 위치
    public float duration = 1f; // 이동 시간
    public float speed = 5f;
    public float cooltime = 1f; // 대기 시간
    public Transform[] waypoints; // 곡선을 이룰 3개의 지점

    [SerializeField] int currentWaypoint = 0; // 현재 목표 지점
    [SerializeField] float elapsedTime = 0f; // 경과 시간

    // 화살의 상태
    private enum ArrowState
    {
        Idle, // 대기 상태
        Attack // 이동 상태
    }

    private ArrowState currentState = ArrowState.Idle; // 현재 상태

    private float coolCount = 0f; // 대기 시간 카운트

    void Update()
    {
        switch (currentState)
        {
            case ArrowState.Idle:
                Idle();
                break;

            case ArrowState.Attack:
                Attack();
                break;
        }
    }

    // 대기 상태일 때의 처리
    private void Idle()
    {
        // 콜라이더 끄기
        coll.enabled = false;

        // 공전
        transform.position = OrbitAround(PlayerManager.Instance.transform, 3f, speed);
        // 공전 각도 증가
        orbitAngle += speed * orbitSpeed * Time.deltaTime;

        // 대기 상태일 때는 CoolCount 감소
        coolCount -= Time.deltaTime;

        if (coolCount <= 0f)
        {
            // CoolCount가 0이 되면 Attack 상태로 전환
            currentState = ArrowState.Attack;
            currentWaypoint = 0;
            elapsedTime = 0f;
            coolCount = cooltime;
        }
    }

    private Vector2 OrbitAround(Transform center, float radius, float orbitSpeed)
    {
        //오브젝트를 공전시킬 위치
        Vector3 orbitPosition = center.position + (Quaternion.Euler(0f, 0f, orbitAngle) * Vector3.right * radius);

        // 공전 위치로 회전
        transform.rotation = Quaternion.Euler(0f, 0f, orbitAngle);

        // 공전 위치를 리턴
        return orbitPosition;
    }

    private void Attack()
    {
        // 이동 상태일 때는 경과 시간에 따라 이동 처리
        elapsedTime += Time.deltaTime;
        float time = elapsedTime / duration;

        if (time >= 1f)
        {
            // 이동이 완료되면 Idle 상태로 전환
            currentState = ArrowState.Idle;
            elapsedTime = 0f;
            currentWaypoint = 0; // currentWaypoint를 0으로 초기화합니다.
        }
        else
        {
            // 곡선을 따라 이동 처리
            Vector3[] points = new Vector3[waypoints.Length + 2];
            points[0] = playerTransform.position;
            for (int i = 0; i < waypoints.Length; i++)
            {
                points[i + 1] = waypoints[i].transform.position;
            }
            points[points.Length - 1] = playerTransform.position;

            Vector3 pos = GetBezierCurvePoint(time, points);

            // 화살 회전 처리
            if (currentWaypoint + 1 < waypoints.Length)
            {
                Vector3 dir = (waypoints[currentWaypoint + 1].position - waypoints[currentWaypoint].position).normalized;
                transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);
            }

            transform.position = pos;

            // 경로상 다음 위치에 도달했는지 검사하고, 다음 위치로 이동합니다.
            if (Vector3.Distance(transform.position, waypoints[currentWaypoint].position) < 0.1f)
            {
                currentWaypoint++;
            }
        }
    }

    private Vector3 GetBezierCurvePoint(float t, Vector3[] points)
    {
        // 베지에 곡선에서 사용할 보간을 구합니다.
        Vector3[] interpolations = new Vector3[points.Length - 1];
        for (int i = 0; i < interpolations.Length; i++)
        {
            interpolations[i] = Vector3.Lerp(points[i], points[i + 1], t * weight);
        }

        // 재귀적으로 베지에 곡선의 값을 구합니다.
        while (interpolations.Length > 1)
        {
            Vector3[] newInterpolations = new Vector3[interpolations.Length - 1];
            for (int i = 0; i < newInterpolations.Length; i++)
            {
                newInterpolations[i] = Vector3.Lerp(interpolations[i], interpolations[i + 1], t * weight);
            }
            interpolations = newInterpolations;
        }

        return interpolations[0];
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        // 베지어 곡선을 구성하는 각 포인트들을 기즈모로 표시합니다.
        for (int i = 0; i < waypoints.Length; i++)
        {
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
        }

        // 베지어 곡선을 이루는 포인트들을 잇는 곡선을 기즈모로 표시합니다.
        for (float t = 0f; t <= 1f; t += 0.05f)
        {
            Vector3[] points = new Vector3[waypoints.Length + 2];
            points[0] = playerTransform.position;
            for (int i = 0; i < waypoints.Length; i++)
            {
                points[i + 1] = waypoints[i].transform.position;
            }
            points[points.Length - 1] = playerTransform.position;

            Gizmos.DrawLine(GetBezierCurvePoint(t, points), GetBezierCurvePoint(t + 0.05f, points));
        }
    }
}
