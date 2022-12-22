using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoPush : MonoBehaviour
{
    [SerializeField] Rigidbody2D rigid; // 밀어낼 오브젝트
    [SerializeField, ReadOnly] float delayCount; // 남은 딜레이 시간 기록
    [SerializeField] float delay_Min; // 밀어내기까지 최소 딜레이 시간
    [SerializeField] float delay_Max; // 밀어내기까지 최대 딜레이 시간
    [SerializeField] float power_Min; // 밀어내는 힘 최소
    [SerializeField] float power_Max; // 밀어내는 힘 최대
    [SerializeField] List<Vector3> direction = new List<Vector3>(); // 타격 방향 고정

    private void Update()
    {
        // 딜레이 시간 이후에
        if (Time.time >= delayCount)
        {
            // 랜덤 방향 선정
            Vector3 dir = Random.insideUnitCircle.normalized;

            // 임의 방향 있을때
            if (direction.Count > 0)
            {
                // 방향 중 랜덤으로 하나 선정
                dir = direction[Random.Range(0, direction.Count)];
            }

            //todo 밀어내기
            rigid.velocity = dir.normalized * Random.Range(power_Min, power_Max);

            // 시간 기록 갱신
            delayCount = Time.time + Random.Range(delay_Min, delay_Max);
        }
    }
}
