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
    [SerializeField] Color lerpColorMin;
    [SerializeField] Color lerpColorMax;

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

        // 눈알 위치 초기화
        eye.transform.localPosition = Vector2.zero;

        // 눈알 색 초기화
        eye.color = new Color(1f, 10f / 255f, 10f / 255f, 1f);
        // 플로터 색 초기화
        L_floater.color = Color.white;
        R_floater.color = Color.white;
        // 케이블 불빛 색 초기화
        for (int i = 0; i < cableLights.Length; i++)
        {
            // 흰색으로 초기화
            cableLights[i].color = Color.white;

            // 랜덤 반복 시간
            float randomTime = Random.Range(0.2f, 0.7f);

            Color randomColor = default;
            switch (Random.Range(0, 3))
            {
                case 0: randomColor = Color.red; break;
                case 1: randomColor = Color.green; break;
                case 2: randomColor = Color.blue; break;
            }

            // 지정색으로 반복 점멸
            cableLights[i].DOColor(randomColor, randomTime)
            .SetLoops(-1);
        }

        // 공격 트리거 발동시 액션 없으면 넣기
        if (atkTrigger.attackAction == null)
            atkTrigger.attackAction += Attack;

        yield return null;
    }

    void Attack()
    {
        // 공격 상태로 전환
        character.nowState = Character.State.Attack;
        // 공격 쿨타임 갱신
        character.atkCoolCount = character.cooltimeNow;

        // 공격 코루틴 실행
        StartCoroutine(SelfExplosion());
    }

    IEnumerator SelfExplosion()
    {
        // 양쪽으로 각도 부르르 떨기
        body.DOPunchRotation(Vector3.forward * 20f, attackReadyTime, 30, 1);

        // 눈알 및 플로터 빨갛게
        eye.DOColor(Color.red, attackReadyTime);

        // 케이블 아래 불빛 반짝이기
        for (int i = 0; i < cableLights.Length; i++)
        {
            // 기존 트윈 종료
            cableLights[i].DOKill();

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
        GameObject explosion = LeanPool.Spawn(explosionAttack, transform.position, Quaternion.identity, ObjectPool.Instance.enemyAtkPool);
        // 몬스터 태그로 변경
        explosion.tag = "Enemy";
        // 몬스터 공격 레이어로 변경
        explosion.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
        // 몬스터 공격력 넣기
        explosion.GetComponent<Attack>().power = character.powerNow;

        // 딜레이 없이 즉시 사망
        StartCoroutine(character.hitBoxList[0].Dead(0));
    }

    private void Update()
    {
        // 이동 방향으로 눈알 돌리기
        Vector3 eyeDir = character.rigid.velocity.normalized;
        // 눈알 위치 제한
        eyeDir.x = Mathf.Lerp(-0.4f, 0.4f, eyeDir.x);
        eyeDir.y = Mathf.Lerp(-0.1f, 0.1f, eyeDir.y);

        // 눈알 위치 적용
        eye.transform.localPosition = eyeDir;

        // 이동하려는 X 방향 플로터 불 밝히기
        Color LColor = Color.Lerp(lerpColorMin, lerpColorMax, 1 - character.rigid.velocity.normalized.x);
        Color RColor = Color.Lerp(lerpColorMin, lerpColorMax, character.rigid.velocity.normalized.x);

        L_floater.color = LColor;
        R_floater.color = RColor;
    }
}
