using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class KingSlimeAI : MonoBehaviour
{
    EnemyManager enemyManager;
    EnemyAI enemyAI;
    float coolCount;
    bool isInside;

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        enemyAI = GetComponent<EnemyAI>();
    }

    private void Update()
    {
        //보스안에 플레이어 들어왔을때
        if (isInside)
        {
            // 플레이어 이동속도 디버프
            PlayerManager.Instance.speedDebuff = 0.1f;
        }
        else
        {
            // 플레이어 이동속도 디버프 해제
            PlayerManager.Instance.speedDebuff = 1f;
        }

        // enemyAI에 stopMove 걸고 Idle 됬을때 공격 패턴 선택
        if (enemyAI.state == EnemyAI.State.Idle && enemyAI.stopMove)
        {
            //공격 상태로 전환
            enemyAI.state = EnemyAI.State.Attack;

            //공격 패턴 선택
            ChooseAttack();
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        //플레이어가 충돌하고, 플레이어 대쉬 아닐때, 보스 점프 상태일때
        if (other.gameObject.CompareTag("Player") && !PlayerManager.Instance.isDash && enemyAI.state == EnemyAI.State.Move)
        {
            //공격 상태로 전환
            enemyAI.state = EnemyAI.State.Attack;

            // 이동 멈추기
            enemyAI.stopMove = true;

            // 애니메이터 활성화 (보잉거리는 애니메이션)
            enemyAI.anim.enabled = true;
            enemyAI.anim.speed = 1f;

            // 슬라임 콜라이더 trigger로 전환
            enemyAI.coll.isTrigger = true;

            // 플레이어를 슬라임 가운데로 이동 시키기
            PlayerManager.Instance.transform.DOMove(enemyAI.jumpLandPos, 0.5f);

            //플레이어가 슬라임 안에 있으면 true
            isInside = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        //플레이어 충돌
        if (other.gameObject.CompareTag("Player"))
        {
            // idle 상태일때, 쿨타임 됬을때
            if (enemyAI.state == EnemyAI.State.Idle && Time.time - coolCount > 1)
            {
                // 고정 데미지에 확률 계산
                float damage = Random.Range(enemyAI.enemy.power * 0.8f, enemyAI.enemy.power * 1.2f);

                // 플레이어 체력 깎기
                PlayerManager.Instance.Damage(damage);

                // 플레이어가 입은 데미지만큼 보스 회복
                enemyManager.Damage(-damage, false);

                // 쿨타임 갱신
                coolCount = Time.time;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        //플레이어가 나가고, idle 상태일때
        if (other.gameObject.CompareTag("Player") && enemyAI.state == EnemyAI.State.Idle)
        {
            //플레이어 안에 있지 않음
            isInside = false;

            // 애니메이터 비활성화
            enemyAI.anim.enabled = false;

            // 스케일 복구
            transform.localScale = Vector2.one;

            // 콜라이더 trigger 비활성화
            enemyAI.coll.isTrigger = false;

            // 이동 재개
            enemyAI.stopMove = false;
        }
    }

    void ChooseAttack()
    {

    }
}
