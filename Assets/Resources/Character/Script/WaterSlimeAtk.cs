using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using DG.Tweening;

public class WaterSlimeAtk : MonoBehaviour
{
    [Header("State")]
    bool attackReady; //공격 준비중

    [Header("Refer")]
    [SerializeField] EnemyAtkTrigger atkTrigger; // 공격 트리거
    public Character character;
    public string enemyName;
    public GameObject bubblePrefab; //거품 프리팹
    IEnumerator atkCoroutine; // 거품 공격 코루틴

    private void Awake()
    {
        character = character == null ? GetComponentInChildren<Character>() : character;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => character.enemy != null);

        // 공격 범위 초기화
        atkTrigger.GetComponent<CircleCollider2D>().radius = character.enemy.range;

        // 적 정보 들어오면 이름 표시
        enemyName = character.enemy.name;

        // 공격 오브젝트 있으면 끄기
        if (bubblePrefab != null)
            bubblePrefab.SetActive(false);

        // 콜백에 공격 함수 넣기
        if (atkTrigger.attackAction == null)
            atkTrigger.attackAction += Attack;
    }

    private void Update()
    {
        // 몬스터 정보 없으면 리턴
        if (character == null || character.enemy == null)
            return;

        // 죽었으면 공격 멈추기
        if (character.isDead && atkCoroutine != null)
            StopCoroutine(atkCoroutine);

        // 상태 이상 있으면 리턴
        if (!character.ManageState())
            return;

        // 이미 공격중이면 리턴
        if (character.nowState == CharacterState.Attack)
        {
            //속도 멈추기
            character.rigid.velocity = Vector3.zero;
            return;
        }

        // 공격 준비중이면 리턴
        if (attackReady)
            return;

        // 타겟 없거나 비활성화면 리턴
        if (!character.TargetObj || !character.TargetObj.activeSelf)
            return;

        // 타겟 방향 계산
        if (character.TargetObj != null)
            character.targetDir = character.TargetObj.transform.position - transform.position;
    }

    void Attack()
    {
        StartCoroutine(ReserveAttack());
    }

    IEnumerator ReserveAttack()
    {
        // 이미 공격 예약 상태면 리턴 (중복 예약 방지)
        if (attackReady)
            yield break;

        //움직일 방향에따라 회전
        if (character.targetDir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        // 이동 멈추기
        character.rigid.velocity = Vector3.zero;

        // 점프중이라면
        if (character.enemyAI.jumpCoolCount > 0)
        {
            // 공격 예약
            attackReady = true;

            // Idle 상태 될때까지 대기
            yield return new WaitUntil(() => character.nowState == CharacterState.Idle);
        }

        // 거품 공격 실행
        atkCoroutine = BubbleAttack();
        StartCoroutine(atkCoroutine);
    }

    public IEnumerator BubbleAttack()
    {
        // 공격 액션으로 전환
        character.nowState = CharacterState.Attack;
        // 공격 쿨타임 갱신
        character.atkCoolCount = character.cooltimeNow;

        //애니메이터 끄기
        character.animList[0].enabled = false;

        //스프라이트 길쭉해지기
        transform.DOScale(new Vector2(0.8f, 1.2f), 0.5f)
        .SetEase(Ease.InBack);
        yield return new WaitForSeconds(0.5f);

        //스프라이트 납작해지기
        transform.DOScale(new Vector2(1.2f, 0.8f), 0.5f)
        .SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.5f);

        // 부들거리며 떨기
        transform.DOShakeScale(1f, 0.1f, 20, 90, false)
        .OnComplete(() =>
        {
            // 스케일 초기화
            transform.DOScale(Vector3.one, 0.5f);
        });

        // 공격 오브젝트 생성
        GameObject bubbleAtk = LeanPool.Spawn(bubblePrefab, transform.position, Quaternion.identity, ObjectPool.Instance.magicPool);

        // 마법 정보 찾기
        MagicHolder bubbleMagic = bubbleAtk.GetComponent<MagicHolder>();

        //타겟 정보 넣기
        if (character.TargetObj != null)
        {
            bubbleMagic.targetObj = character.TargetObj;
            bubbleMagic.targetPos = character.TargetObj.transform.position;
        }

        // 타겟 설정
        if (character.IsGhost)
            bubbleMagic.SetTarget(MagicHolder.TargetType.Enemy);
        else
            bubbleMagic.SetTarget(MagicHolder.TargetType.Player);

        // 공격 시간 대기
        yield return new WaitForSeconds(1.5f);

        //애니메이터 켜기
        character.animList[0].enabled = true;
        // Idle로 전환
        character.nowState = CharacterState.Idle;

        // 코루틴 비우기
        atkCoroutine = null;

        //공격 준비 끝
        attackReady = false;
    }
}
