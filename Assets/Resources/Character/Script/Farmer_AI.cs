using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;

public class Farmer_AI : MonoBehaviour
{
    [Header("State")]
    private float atkCoolCount;
    public float atkRange = 30f; // 공격 범위
    [SerializeField, ReadOnly] Vector3 targetVelocity; // 현재 이동속도
    [SerializeField] float maxSpeed = 1f; // 최대 이동속도
    [SerializeField] Patten patten = Patten.None;
    enum Patten { PlantSeed, BioGas, SunHeal, Skip, None };

    [Header("Cooltime")]
    [SerializeField] float coolCount;
    [SerializeField] float StabCooltime = 1f;
    [SerializeField] float PlantSeedCooltime = 1f;
    [SerializeField] float BioGasCooltime = 1f;
    [SerializeField] float SunHealCooltime = 1f;

    [Header("Refer")]
    [SerializeField] Character character;
    [SerializeField] TextMeshProUGUI stateText; //! 테스트 현재 상태
    [SerializeField] Transform bodyTransform; // 몸체 오브젝트

    [Header("Walk")]
    [SerializeField, ReadOnly] float targetSearchCount; // 타겟 위치 추적 시간 카운트
    [SerializeField] float targetSearchTime = 3f; // 타겟 위치 추적 시간
    [SerializeField] float targetFollowSpeed = 3f; // 타겟 추적 속도

    [Header("Leg")]
    [SerializeField] float footMoveDistance = 3f; // 해당 거리보다 멀어지면 발 움직임
    [SerializeField] float footMoveSpeed = 0.3f; // 발 움직이는 속도
    [SerializeField] float velocityFactor = 0.3f; // 발 이동 방향 속도로 예측 팩터
    [SerializeField, ReadOnly] int nowMoveFootNum = 0; // 현재 이동 중인 발 개수
    [SerializeField] Transform[] legBones; // 다리 각각 bone 부모 오브젝트
    [SerializeField] Transform[] footEffectors; // 옮겨질 발 오브젝트
    [SerializeField] ParticleSystem[] footTargets; // 발이 옮겨질 목표 오브젝트
    [SerializeField] Vector2[] defaultFootPos = new Vector2[4]; // 발 초기 로컬 포지션
    [SerializeField, ReadOnly] Vector2[] nowMovePos = new Vector2[4]; // 현재 옮겨지고 있는 발의 위치
    [SerializeField, ReadOnly] Vector2[] lastFootPos = new Vector2[4]; // 마지막 발 월드 포지션

    [Header("Stab")]
    [SerializeField] EnemyAtkTrigger stabTrigger;

    [Header("PlantSeed")]
    [SerializeField] Transform seedPrefab; // 씨앗 프리팹
    [SerializeField] Transform plantPrefab; // 식물 프리팹
    [SerializeField] Transform seedHole; // 씨앗 발사할 구멍들

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 모든 발 초기 위치 초기화
        for (int i = 0; i < footTargets.Length; i++)
        {
            // 발의 마지막 월드 위치 초기화
            lastFootPos[i] = footEffectors[i].position;

            // 발 초기 로컬 위치 초기화
            // defaultFootPos[i] = footEffectors[i].position - transform.position;

            // 다리 콜라이더 모두  끄기
            Collider2D[] legColls = legBones[i].GetComponentsInChildren<Collider2D>();
            foreach (Collider2D coll in legColls)
            {
                coll.enabled = false;
            }
        }

        //todo 등장씬
        // 일어 서기전 땅이 갈라지고
        // 땅에 박혀있던 나무가 흔들리며 일어서기
        // 일어설때 거미 위에 있던 흙과 돌들이 떨어짐
        // 중력 이용해서 굴러떨어지는거 녹화 후 애니메이션화 하기
        // 밑에 있던 거미 드론이 등장

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => character.enemy != null);

        //속도 초기화
        character.rigid.velocity = Vector2.zero;
        // 위치 고정 해제
        character.rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Update()
    {
        // 몬스터 정보 없으면 리턴
        if (character.enemy == null)
            return;

        // 타겟 추적 쿨타임 차감
        if (targetSearchCount > 0)
            targetSearchCount -= Time.deltaTime;
        // 쿨타임 됬을때
        else
        {
            // 플레이어 추정 위치 계산
            character.targetPos = character.TargetObj.transform.position + (Vector3)Random.insideUnitCircle;

            // 추적 쿨타임 갱신
            targetSearchCount = targetSearchTime;
        }

        // 추적 위치 벡터를 서서히 이동
        character.movePos = Vector3.Lerp(character.movePos, character.targetPos, Time.deltaTime * targetFollowSpeed);

        // 플레이어 방향
        character.targetDir = character.movePos - transform.position;

        // 패턴 정하기
        ManageAction();

        // 패시브 패턴
        //todo 패시브 스킬로 뿌렸던 씨 근처에 가면 자동으로 물을 줌
        //todo 파란 물 모양 라인렌더러로 구현, 베지어 곡선 포물선
        //todo 일정 시간 이상 물 먹은 나무는 슬라임으로 변함
        //todo 슬라임 스프라이트 알파값 올리고 나무 디스폰, 슬라임 초기화
    }

    void ManageAction()
    {
        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // Idle 아니면 리턴
        if (character.nowState != Character.State.Idle)
            return;

        // 공격 쿨타임 차감
        if (atkCoolCount > 0)
            atkCoolCount -= Time.deltaTime;

        // 플레이어 방향
        Vector2 dir = character.movePos - transform.position;

        // 플레이어와의 거리
        float playerDistance = dir.magnitude;

        // 찌르기 트리거에 플레이어 닿으면 Stab 패턴
        if (stabTrigger.atkTrigger)
        {
            //! 거리 확인용
            stateText.text = "Stab : " + playerDistance;

            // 찌르기 패턴 실행
            StartCoroutine(StabLeg());

            return;
        }

        //! 쿨타임 확인
        stateText.text = "CoolCount : " + atkCoolCount;

        // 범위 내에 있을때
        if (playerDistance <= atkRange)
        {
            // 쿨타임 됬을때
            if (atkCoolCount <= 0)
            {
                //공격 패턴 결정하기
                StartCoroutine(ChooseAttack());

                return;
            }
        }

        // 플레이어 따라가기
        Move();
    }

    IEnumerator ChooseAttack()
    {
        //! 패턴 스킵
        if (patten == Patten.Skip)
            yield break;

        // 공격 상태로 전환
        character.nowState = Character.State.Attack;

        // 가능한 공격 중에서 랜덤 뽑기
        int atkType = Random.Range(0, 4);

        //! 테스트를 위해 패턴 고정
        if (patten != Patten.None)
            atkType = (int)patten;

        // 걷기 멈추기
        yield return StartCoroutine(StopMove());

        // 결정된 공격 패턴 실행
        switch (atkType)
        {
            case 0:
                // 씨뿌리기 패턴
                StartCoroutine(PlantSeed());
                break;

            case 1:
                // 바이오 가스 패턴
                StartCoroutine(BioGas());
                break;

            case 2:
                // 자힐 패턴
                StartCoroutine(SunHeal());
                break;
        }
    }

    void Move()
    {
        character.nowState = Character.State.Walk;

        //! 거리 확인
        stateText.text = "Distance : " + character.targetDir.magnitude;

        // 플레이어까지 거리
        float distance = Vector3.Distance(character.movePos, transform.position);

        // 공격범위 이내 접근 못하게 하는 속도 계수
        float nearSpeed = distance < atkRange
        // 범위 안에 있을때, 거리 비례한 속도
        ? character.targetDir.magnitude - atkRange
        // 범위 밖에 있을때, 최고 속도
        : maxSpeed;

        // 목표 벡터 계산
        targetVelocity =
        character.targetDir.normalized
        * SystemManager.Instance.globalTimeScale
        * nearSpeed;

        // print(targetVelocity
        // + ":" + character.targetDir.normalized
        // + ":" + SystemManager.Instance.globalTimeScale
        // + ":" + nearSpeed);

        // 해당 방향으로 가속
        character.rigid.velocity = Vector3.Lerp(character.rigid.velocity, targetVelocity, Time.deltaTime * targetFollowSpeed);

        character.nowState = Character.State.Idle;

        // 다리 움직이기
        FootCheck(character.rigid.velocity);
    }

    void FootCheck(Vector2 moveVelocity, bool setDefault = false)
    {
        for (int i = 0; i < footTargets.Length; i++)
        {
            // 현재 옮겨질 위치가 없으면, 이동중인 발이 2개 이하일때
            if (
                (nowMovePos[i] == Vector2.zero && nowMoveFootNum <= 2) || setDefault
                )
            {
                // OnComplete 때문에 인덱스 캐싱
                int footNum = i;
                // 발의 현재 위치에서 마지막 위치까지 거리
                float distance = Vector2.Distance((Vector2)transform.position + defaultFootPos[footNum], footTargets[footNum].transform.position);

                // 마지막 위치로부터 footMoveDistance 보다 멀어지면
                if (distance > footMoveDistance * Random.Range(0.8f, 1.2f))
                {
                    // 발 움직이기
                    FootMove(moveVelocity, footNum);
                }
            }
        }
    }

    void FootMove(Vector2 moveVelocity, int footIndex)
    {
        // 이동 방향 벡터 계산
        Vector2 velocity = moveVelocity * velocityFactor;
        // 이동 방향 속도 제한
        velocity = velocity.normalized * Mathf.Clamp(velocity.magnitude, 0, 5f);
        // print("velocity : " + velocity.magnitude);

        // 발이 옮겨질 위치 계산해서 배열에 캐싱
        Vector2 movePos = (Vector2)transform.position + defaultFootPos[footIndex] + velocity;
        nowMovePos[footIndex] = movePos;

        // 발 이동개수 추가
        nowMoveFootNum++;

        //todo 발 그림자 이동

        // 발 점프해서 이동
        footTargets[footIndex].transform.DOJump(movePos, 1f, 1, footMoveSpeed * footMoveDistance)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            // 발 마지막 위치 초기화
            lastFootPos[footIndex] = movePos;

            // 캐싱한 위치 초기화
            nowMovePos[footIndex] = Vector2.zero;

            // 발 이동개수 감소
            nowMoveFootNum--;

            // 발에서 먼지 생성
            footTargets[footIndex].Play();
        });
    }

    IEnumerator StopMove()
    {
        // 속도 멈추기
        DOTween.To(() => character.rigid.velocity, x => character.rigid.velocity = x, Vector2.zero, 0.2f);

        // 멈추는 시간 대기
        yield return new WaitForSeconds(0.2f);

        // 발 모두 원위치
        for (int i = 0; i < 4; i++)
            FootMove(Vector2.zero, i);
    }

    IEnumerator StabLeg()
    {
        // 공격 상태로 변경
        character.nowState = Character.State.Attack;

        // 걷기 멈추기
        yield return StartCoroutine(StopMove());

        // 원위치 시간 대기
        yield return new WaitForSeconds(0.5f);

        // 플레이어 방향 좌,우 판단
        bool isLeft = PlayerManager.Instance.transform.position.x < transform.position.x ? true : false;
        // 공격할 다리 인덱스
        int atkLegIndex = isLeft ? 0 : 3;
        // 공격할 다리 오브젝트
        Transform atkFoot = footTargets[atkLegIndex].transform;
        // 공격할 다리 콜라이더 모두 찾기
        Collider2D[] legColls = legBones[atkLegIndex].GetComponentsInChildren<Collider2D>();
        // 몸체 기울일 방향
        Vector3 bodyRotation = Vector3.forward * (isLeft ? -15f : 15f);

        // 플레이어 반대 방향으로 살짝몸 기울이고
        bodyTransform.transform.DORotate(bodyRotation, 0.5f)
        .SetEase(Ease.OutSine);

        // 공격 준비 로컬 위치
        Vector2 atkReadyPos = isLeft ? new Vector2(-3, 1) : new Vector2(3, 1);
        // 공격 준비 월드 위치
        atkReadyPos += (Vector2)transform.position;

        // 공격할 다리 오므려서 준비
        atkFoot.DOMove(atkReadyPos, 0.5f)
        .SetEase(Ease.OutSine);

        // 몸체 기울이기 및 오므리는 시간 대기
        yield return new WaitForSeconds(1f);

        // 공격 방향으로 몸체 기울이기
        bodyTransform.transform.DORotate(-bodyRotation, 0.2f)
        .SetEase(Ease.OutSine);

        // 공격 다리 콜라이더 모두 활성화
        for (int i = 0; i < legColls.Length; i++)
            legColls[i].enabled = true;

        // 찌를 위치
        Vector2 stabPos = transform.position + (PlayerManager.Instance.transform.position - transform.position).normalized * 20f;

        // 플레이어 위치로 다리 뻗어서 찌르기
        atkFoot.DOMove(stabPos, 0.2f)
                .SetEase(Ease.OutBack);

        //todo 찌른 직후 Solver 끄고 관절 사이 늘리기, 라인 렌더러로 관절사이 이어주기
        // 해당 다리 Solver 끄기
        // 관절 사이 늘리기
        // 늘리는동안 라인 렌더러로 관절사이 이어주기 업데이트

        // 찌르고 대기
        yield return new WaitForSeconds(0.7f);

        // 공격 다리 콜라이더 모두 비활성화
        for (int i = 0; i < legColls.Length; i++)
            legColls[i].enabled = false;

        // 몸체 기울기 초기화
        bodyTransform.transform.DORotate(Vector3.zero, 0.5f)
        .SetEase(Ease.OutBack);

        // 다리 다시 오므리기
        atkFoot.DOMove(atkReadyPos, 0.2f)
        .SetEase(Ease.InBack);

        // 오므리는 시간 대기
        yield return new WaitForSeconds(0.2f);

        // 공격 다리 위치 초기화
        atkFoot.DOMove((Vector2)transform.position + defaultFootPos[atkLegIndex], 0.3f)
        .SetEase(Ease.InBack);

        // 몸체 및 다리 초기화 대기
        yield return new WaitForSeconds(0.5f);

        // 쿨타임 갱신
        coolCount = StabCooltime;

        // 상태 초기화
        character.nowState = Character.State.Idle;
    }

    IEnumerator PlantSeed()
    {
        yield return null;

        for (int i = 0; i < seedHole.childCount; i++)
        {
            // 식물 자라남
            StartCoroutine(GrowPlant(seedHole.GetChild(i).transform.position));

            yield return new WaitForSeconds(0.2f);
        }

        // 후딜레이 대기
        yield return new WaitForSeconds(1f);

        // 쿨타임 갱신
        coolCount = PlantSeedCooltime;

        // 상태 초기화
        character.nowState = Character.State.Idle;
    }

    IEnumerator GrowPlant(Vector2 spawnPos)
    {
        // 씨앗 소환
        Transform seedShadow = LeanPool.Spawn(seedPrefab, spawnPos, Quaternion.identity, SystemManager.Instance.enemyAtkPool);
        // 씨앗 스프라이트
        Transform seed = seedShadow.GetChild(0);

        // 씨앗 랜덤 각도로 초기화
        seed.rotation = Quaternion.Euler(Vector3.forward * Random.Range(0f, 360f));

        // 씨앗 착지 위치
        Vector2 seedPos = seedShadow.position + (seedShadow.position - seedHole.position).normalized * 10f + Random.insideUnitSphere * 3f;

        // 씨앗 이동 시키기
        seedShadow.DOMove(seedPos, 1f)
        .SetEase(Ease.InSine);

        // 씨앗 점프 시키기
        seed.DOLocalJump(Vector2.zero, 5f, 1, 1f)
        .SetEase(Ease.InSine);

        yield return new WaitForSeconds(1f);

        // 씨앗 디스폰
        LeanPool.Despawn(seedShadow);

        // 식물생성
        Transform plant = LeanPool.Spawn(plantPrefab, seedShadow.transform.position, Quaternion.identity, SystemManager.Instance.enemyAtkPool);
    }

    IEnumerator BioGas()
    {
        yield return null;
        //todo 바이오 가스 패턴
        //todo 다리를 아래로 뻗어 높이 올라간 다음
        //todo 다리를 굽히며 아래로 내려가면서
        //todo 구름모양 로컬 파티클이 원형 사방으로 여러번 퍼지며
        //todo 파티클 하나로 shape 스케일 키우기
        //todo 플레이어는 닿으면 독 데미지, 구르기로 회피 가능
        //todo 파티클에 원형 엣지 콜라이더 굵게 적용, 스케일따라 커지도록
        //todo 싹이 튼 씨앗들에 닿으면 Life 슬라임으로 변함
    }

    IEnumerator SunHeal()
    {
        yield return null;
        //todo 자힐 패턴
        //todo 자세를 낮춰 앉은 뒤에
        //todo 머리 위의 나무가 빛나며 광합성
        //todo 노랗고 동그란 태양 입자가 모든 방향에서 생성되어 보스쪽으로 이동
        //todo 흡수가 하나씩 될때마다 보스는 체력 회복
        //todo 플레이어는 다가오는 태양 입자를 파괴 가능
        //todo 태양 입자 플레이어 충돌하면 데미지 주고 통과
    }

    private void OnDrawGizmosSelected()
    {
        // 보스부터 이동 위치까지 직선
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, character.movePos);

        // 이동 위치 기즈모
        Gizmos.DrawIcon(character.movePos, "Circle.png", true, new Color(1, 0, 0, 0.5f));

        // 추적 위치 기즈모
        Gizmos.DrawIcon(character.targetPos, "Circle.png", true, new Color(0, 0, 1, 0.5f));

        // 추적 위치부터 이동 위치까지 직선
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(character.targetPos, character.movePos);
    }
}
