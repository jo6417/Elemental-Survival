using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("Refer")]
    // public GameObject atkEffect; // 타격시 타격지점에서 발생할 이펙트

    [Header("State")]
    public int pierceCount = 0; // 남은 관통 횟수

    [Header("After Effect")]
    public float fixedPower = 0f; // 고정된 데미지
    public float knockbackForce = 0; //넉백 파워

    // 도트 데미지
    public float burnTime = 0; // 화상 지속시간
    public float poisonTime = 0; // 독 지속시간
    public float bleedTime = 0; // 출혈 지속시간

    // 슬로우
    public float slowTime = 0; // 슬로우 지속시간
    public float wetTime = 0; // 젖음 지속시간

    // 행동 불능
    public float shockTime = 0; // 감전 지속시간
    public float freezeTime = 0; // 빙결 지속시간
    public float flatTime = 0f; // 납작해지는 디버프 지속시간
    public float stopTime; // 시간정지 지속시간
}
