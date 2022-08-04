using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Quad_AI : MonoBehaviour
{
    [Header("State")]
    [SerializeField]
    Patten patten = Patten.None;
    enum Patten { PushSide, PushCircle, BladeShot, FanSmash, None };
    private float coolCount;
    public float atkRange = 30f; // 공격 범위
    Vector3 playerPos; // 플레이어 추정 위치
    public float fanSensitive = 0.1f; //프로펠러 기울기 감도 조절

    [Header("Refer")]
    public TextMeshProUGUI stateText; //! 테스트 현재 상태
    public EnemyManager enemyManager;
    public Transform body;
    public Transform eye;
    public Transform[] fans = new Transform[4]; // 프로펠러들

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        //애니메이션 스피드 초기화
        if (enemyManager.animList != null)
        {
            foreach (Animator anim in enemyManager.animList)
            {
                anim.speed = 1f;
            }
        }

        //속도 초기화
        enemyManager.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        enemyManager.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Update()
    {
        // 몬스터 정보 없으면 리턴
        if (enemyManager.enemy == null)
            return;

        // 상태 이상 있으면 리턴
        if (!enemyManager.ManageState())
            return;

        //행동 관리
        ManageAction();
    }

    void ManageAction()
    {
        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // Idle 아니면 리턴
        if (enemyManager.nowAction != EnemyManager.Action.Idle)
            return;

        // 플레이어 추정 위치 계산
        playerPos = PlayerManager.Instance.transform.position + (Vector3)Random.insideUnitCircle * 3f;

        // 플레이어 방향
        Vector2 dir = playerPos - transform.position;

        // 플레이어와의 거리
        float distance = dir.magnitude;

        // 쿨타임 차감
        coolCount -= Time.deltaTime;

        //! 쿨타임 확인
        stateText.text = "CoolCount : " + coolCount;

        // 쿨타임 됬을때, 범위 내에 있을때
        if (distance <= atkRange)
        {
            // if (coolCount <= 0)
            // {
            //     // 속도 초기화
            //     enemyManager.rigid.velocity = Vector3.zero;

            //     //공격 패턴 결정하기
            //     ChooseAttack();

            //     return;
            // }
        }
        else
            // 플레이어 따라가기
            Walk();
    }

    void ChooseAttack()
    {
        // 현재 액션 변경
        enemyManager.nowAction = EnemyManager.Action.Attack;

        // 랜덤 패턴 결정
        int randomNum = Random.Range(0, 5);

        // print("randomNum : " + randomNum);

        //! 테스트를 위해 패턴 고정
        if (patten != Patten.None)
            randomNum = (int)patten;

        // switch (randomNum)
        // {
        //     case 0:
        //         // 주먹 내려찍기 패턴
        //         StartCoroutine(FistDrop());
        //         //쿨타임 갱신
        //         coolCount = fistDropCoolTime;
        //         break;
        // }
    }

    void Walk()
    {
        enemyManager.nowAction = EnemyManager.Action.Walk;

        //애니메이터 켜기
        enemyManager.animList[0].enabled = true;
        // Idle 애니메이션으로 전환
        // enemyManager.animList[0].SetBool("UseFist", false);
        // enemyManager.animList[0].SetBool("UseDrill", false);

        // 플레이어 방향
        Vector3 playerDir = PlayerManager.Instance.transform.position - body.position;

        // 플레이어 방향 각도
        float playerAngle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;

        // 플레이어 방향으로 눈 이동
        eye.position = body.position + playerDir.normalized * 1f;

        // 플레이어 방향으로 회전
        eye.rotation = Quaternion.Euler(0, 0, playerAngle + 135f);

        // 기울임 각도 계산
        float angleZ = -Mathf.Clamp(playerDir.x, -20f, 20f);
        Quaternion rotation = Quaternion.Lerp(fans[0].rotation, Quaternion.Euler(0, 0, angleZ), fanSensitive);

        // 보스 몸체 기울이기
        body.rotation = rotation;

        // 프로펠러 기울이기
        // for (int i = 0; i < fans.Length; i++)
        // {
        //     fans[i].rotation = rotation;
        // }

        //해당 방향으로 가속
        enemyManager.rigid.velocity = playerDir.normalized * enemyManager.speed * SystemManager.Instance.globalTimeScale;

        enemyManager.nowAction = EnemyManager.Action.Idle;
    }
}
