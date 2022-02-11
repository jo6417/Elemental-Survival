using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Magic", menuName ="Magic")]
public class Magic : ScriptableObject
{
    [Header("Info")]
    public string magicID;
    public string magicName = "New Magic";

    [Header("Spec")]
    public float damage = 1; //데미지
    public float speed = 1; //투사체 속도
    public float range = 1; //범위
    public float coolTime = 1; //쿨타임
    public float criticalRate = 0; //크리티컬 확률

    [Header("Effect")] //부가 효과, 0이면 효과 없음
    public float timerTrigger = 0; // 시간 지나면 이펙트 발동
    public float explodeForce = 0; //폭발 세기
    public float explodeRange = 0; //폭발 반경
    public float stunTime = 0; //스턴 시간
    public float KnockBackForce = 0; //넉백 강도
    public float freezeTime = 0; //빙결 시간
}
