using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Drone_AI : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] Character character;
    [SerializeField] Transform body;
    [SerializeField] SpriteMask eyeMask;
    [SerializeField] SpriteRenderer eye;
    [SerializeField] SpriteRenderer L_floater;
    [SerializeField] SpriteRenderer R_floater;
    [SerializeField] SpriteRenderer[] cableLights = new SpriteRenderer[4];
    [SerializeField] EnemyAtkTrigger atkTrigger;
    [SerializeField] GameObject explosionAttack;
    [SerializeField] SpriteRenderer explosionRange;
    [SerializeField] SpriteRenderer explosionRangeFill;
    [SerializeField] Color redColorMin;
    [SerializeField] Color redColorMax;

    [Header("State")]
    [SerializeField] float attackReadyTime;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // // 사운드 초기화 될때까지 대기
        // yield return new WaitUntil(() => SoundManager.Instance.initFinish);
        // // 시작하면 사운드 재생
        // SoundManager.Instance.PlaySound("MiniDrone_Fly", transform, 0, 0, -1, true);

        // 공격 트리거 발동시 액션 없으면 넣기
        if (atkTrigger.attackAction == null)
            atkTrigger.attackAction += Attack;

        //todo 눈알 위치 초기화
        eye.transform.localPosition = Vector2.zero;

        //todo 눈알 색 초기화
        eye.color = new Color(1f, 10f / 255f, 10f / 255f, 1f);
        //todo 플로터 색 초기화
        L_floater.color = Color.white;
        R_floater.color = Color.white;
        //todo 케이블 불빛 색 초기화
        for (int i = 0; i < cableLights.Length; i++)
        {
            cableLights[i].color = Color.white;
        }

        yield return null;
    }

    void Attack()
    {
        if (character.nowState == Character.State.Idle)
            // 공격 코루틴 실행
            StartCoroutine(SelfExplosion());
    }

    IEnumerator SelfExplosion()
    {
        // 공격 상태로 전환
        character.nowState = Character.State.Attack;

        // 양쪽으로 각도 부르르 떨기
        body.DOPunchRotation(Vector3.forward * 30f, attackReadyTime, 30, 1);

        // 눈알 및 플로터 빨갛게
        eye.DOColor(Color.red, attackReadyTime);

        // 케이블 아래 불빛 반짝이기
        for (int i = 0; i < cableLights.Length; i++)
        {
            // 랜덤 반복 횟수
            int randomNum = Random.Range(1, 10);

            cableLights[i].DOColor(Color.red, attackReadyTime / randomNum)
            .SetLoops(randomNum);
        }

        // 폭발 반경 표시
        explosionRange.enabled = true;
        explosionRangeFill.enabled = true;

        // 폭발 반경 인디케이터 사이즈 초기화
        explosionRangeFill.transform.localScale = Vector3.zero;
        // 폭발 반경 인디케이터 사이즈 키우기
        explosionRangeFill.transform.DOScale(Vector3.one, attackReadyTime)
        .OnComplete(() =>
        {
            explosionRange.enabled = false;
            explosionRangeFill.enabled = false;
        });

        // 자폭 경고 사운드 재생
        SoundManager.Instance.PlaySound("MiniDrone_Warning", transform.position);

        // 공격 준비시간 대기
        yield return new WaitForSeconds(attackReadyTime);

        // 폭발 스폰
        LeanPool.Spawn(explosionAttack, transform.position, Quaternion.identity, ObjectPool.Instance.enemyAtkPool);

        // 딜레이 없이 즉시 사망
        StartCoroutine(character.hitBoxList[0].Dead(0));
    }

    private void Update()
    {
        //todo 이동 방향으로 눈알 돌리기
        Vector3 eyeDir = character.rigid.velocity.normalized;
        //todo 눈알 위치 제한
        eyeDir.x = Mathf.Lerp(-0.4f, 0.4f, eyeDir.x);
        eyeDir.y = Mathf.Lerp(-0.1f, 0.1f, eyeDir.y);

        //todo 좌우 반전 적용

        // 눈알 위치 적용
        eye.transform.localPosition = eyeDir;

        //todo 이동하려는 X 방향 플로터 불 밝히기
        L_floater.color = Color.Lerp(redColorMax, redColorMin, character.rigid.velocity.normalized.magnitude);
        R_floater.color = Color.Lerp(redColorMin, redColorMax, character.rigid.velocity.normalized.magnitude);

        //todo 좌우 반전 적용
    }
}
