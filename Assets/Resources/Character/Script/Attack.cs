using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("Refer")]
    // public GameObject atkEffect; // 타격시 타격지점에서 발생할 이펙트
    [SerializeField] string playSoundName; // 타격시 발생할 사운드

    [Header("State")]
    public int pierceCount = 0; // 남은 관통 횟수
    public enum TargetType { None, Enemy, Player, Both };
    public TargetType targetType; //마법의 목표 타겟

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
    public float stunTime; // 스턴 지속시간

    private void OnEnable()
    {
        if (playSoundName != "")
            StartCoroutine(AttackSound());
    }

    IEnumerator AttackSound()
    {
        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => SoundManager.Instance.init);

        // 공격 시작시 사운드 재생
        SoundManager.Instance.PlaySound(playSoundName, transform.position);
    }

    public void SetTarget(TargetType changeTarget)
    {
        //입력된 타겟에 따라 오브젝트 태그 및 레이어 변경
        switch (changeTarget)
        {
            case TargetType.Enemy:
                gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
                break;

            case TargetType.Player:
                gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
                break;

            case TargetType.Both:
                gameObject.layer = SystemManager.Instance.layerList.AllAttack_Layer;
                break;
        }

        //해당 마법의 타겟 변경
        targetType = changeTarget;

        // StartCoroutine(MagicTarget(changeTarget));
    }
}
