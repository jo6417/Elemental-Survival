using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.U2D.IK;

public class Farmer_AI : MonoBehaviour
{
    [Header("State")]
    [SerializeField] float atkRange = 30f; // 공격 범위
    [SerializeField] float followRange = 10f; // 타겟과의 최소 거리
    [SerializeField, ReadOnly] Vector3 targetVelocity; // 현재 이동속도
    [SerializeField] float minSpeed = 1f; // 최소 이동속도
    [SerializeField] float maxSpeed = 1f; // 최대 이동속도
    [SerializeField] Patten patten = Patten.None;
    enum Patten { PlantSeed, BioGas, SunHeal, Skip, None };

    [Header("Sound")]
    [SerializeField] string[] stepSounds = { };
    int step_lastIndex = -1;
    [SerializeField] string[] seedLandSounds = { };

    [Header("Cooltime")]
    [SerializeField] float atkCoolCount;
    [SerializeField] float StabCooltime = 1f;
    [SerializeField] float PlantSeedCooltime = 1f;
    [SerializeField] float BioGasCooltime = 1f;
    [SerializeField] float SunHealCooltime = 1f;

    [Header("Refer")]
    [SerializeField] Character character;
    [SerializeField] IKManager2D ikmanager;
    [SerializeField] TextMeshProUGUI stateText; //! 테스트 현재 상태
    [SerializeField] Transform bodyTransform; // 몸체 오브젝트
    [SerializeField] Transform treeTransform; // 나무 오브젝트
    [SerializeField] Transform treeParticle; // 나무에서 떨어지는 파티클

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
    [SerializeField] Transform[] solvers; // 다리 IK Solver
    [SerializeField] Transform[] footEffectors; // 옮겨질 발 오브젝트
    [SerializeField] ParticleSystem[] footTargets; // 발이 옮겨질 목표 오브젝트
    [SerializeField] Vector2[] defaultFootPos = new Vector2[4]; // 발 초기 로컬 포지션
    [SerializeField, ReadOnly] Vector2[] nowMovePos = new Vector2[4]; // 현재 옮겨지고 있는 발의 위치
    [SerializeField, ReadOnly] Vector2[] lastFootPos = new Vector2[4]; // 마지막 발 월드 포지션

    [Header("Stab")]
    [SerializeField] EnemyAtkTrigger stabTrigger;
    [SerializeField] ParticleSystem L_legTipFlash;
    [SerializeField] ParticleSystem R_legTipFlash;
    [SerializeField] LineRenderer[] legCables;
    [SerializeField] Material legAtkMat; // 공격할때 다리 머터리얼
    [SerializeField] float stretchRange = 10f; // 관절 뻗는 길이의 합
    [SerializeField] float stretchTime = 0.2f;

    [Header("PlantSeed")]
    [SerializeField] Light2D treeLight; // 나무 모양 라이트
    [SerializeField] ParticleSystem seedPulse; // 씨앗 패턴 인디케이터
    [SerializeField] Transform seedHole; // 씨앗 발사할 구멍들
    [SerializeField] Seed_AI seedPrefab; // 씨앗 프리팹
    [SerializeField] List<Seed_AI> seedList; // 씨앗 목록
    [SerializeField] float waterRange = 10f; // 물주기 거리
    [SerializeField] float seedRange = 5f; // 씨앗 발사 거리
    [SerializeField] float waterPos_Y = 5f; // 베지어 곡선 가운데 높이
    [SerializeField] float vertexCount = 12; // 베지어 곡선 포인트 개수

    [Header("BioGas")]
    [SerializeField] Rigidbody2D bioGasPrefab; // 독구름 프리팹
    [SerializeField] ParticleSystem bioPulse; // 가스 패턴 인디케이터
    [SerializeField] int gasLoopNum = 3; // 공격 횟수
    [SerializeField] int gasNum = 36; // 한번에 독구름 생성 개수
    [SerializeField] float gasSpeed = 5f; // 독구름 이동 속도

    [Header("SunHeal")]
    [SerializeField] int sunNum; // 태양광 소환 횟수
    [SerializeField] float sunRange = 50f; // 태양광 소환 거리
    [SerializeField] float sunSpeed = 5f; // 태양광 이동 속도
    [SerializeField] GameObject landDustPrefab; // 땅에 착지시 먼지 이펙트
    [SerializeField] GameObject sunPrefab; // 태양광 프리팹
    [SerializeField] List<GameObject> sunList = new List<GameObject>();

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        ikmanager.UpdateManager();

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

            // 공격할 다리의 머터리얼 모두 초기화
            SpriteRenderer[] legSprites ={
                legBones[i].GetChild(1).GetComponent<SpriteRenderer>(),
                legBones[i].GetChild(2).GetComponent<SpriteRenderer>(),
                legBones[i].GetChild(3).GetComponent<SpriteRenderer>()
            };
            for (int j = 0; j < legSprites.Length; j++)
                legSprites[j].material = SystemManager.Instance.characterMat;

            // 해당 다리 HDR 불빛 초기화
            SpriteRenderer[] legLights = legBones[i].GetChild(0).GetComponentsInChildren<SpriteRenderer>();
            for (int j = 0; j < legLights.Length; j++)
            {
                Color legColor = legLights[j].color;
                legColor.a = 0f;
                legLights[j].color = legColor;
            }
        }

        // 맞을때마다 Hit 함수 실행
        if (character.hitCallback == null)
            character.hitCallback += Hit;

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

    void Hit()
    {
        //todo 페이즈 변화
        // // 현재 1페이즈일때, 체력이 2/3 이하일때
        // if (nowPhase == 1 && character.hpNow / character.hpMax <= 2f / 3f)
        // {
        //     // 페이즈업 함수 실행 안됬을때
        //     if (nowPhase == nextPhase)
        //     {
        //         // 다음 페이스 숫자 올리기
        //         nextPhase = 2;

        //         // 다음 페이즈 예약
        //         StartCoroutine(PhaseChange());
        //     }
        // }

        // // 현재 2페이즈, 체력이 1/3 이하일때, 3페이즈
        // if (nowPhase == 2 && character.hpNow / character.hpMax <= 1f / 3f)
        // {
        //     // 페이즈업 함수 실행 안됬을때
        //     if (nowPhase == nextPhase)
        //     {
        //         // 다음 페이스 숫자 올리기
        //         nextPhase = 3;

        //         // 다음 페이즈 예약
        //         StartCoroutine(PhaseChange());
        //     }
        // }

        // 체력이 0 이하일때, 죽었을때
        if (character.characterStat.hpNow <= 0)
        {
            // 태양광 모두 없에기
            for (int i = 0; i < sunList.Count; i++)
                // 태양광 디스폰
                LeanPool.Despawn(sunList[i]);

            //todo 보스 전용 죽음 트랜지션 시작
            //todo 넘어지고 폭발 인디케이터 확장
            //todo 거대한 폭발남기며 디스폰
        }
    }

    private void Update()
    {
        // 몬스터 정보 없으면 리턴
        if (character.enemy == null)
            return;

        // 공격 쿨타임 차감
        if (atkCoolCount > 0)
            atkCoolCount -= Time.deltaTime;

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
        character.targetDir = character.movePos - bodyTransform.position;

        //! 쿨타임 및 거리 확인
        stateText.text = "CoolCount : " + atkCoolCount + "\nDistance : " + character.targetDir.magnitude;

        // 패턴 정하기
        ManageAction();

        // 패시브 패턴
        Watering();
    }

    void ManageAction()
    {
        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // Idle 아니면 리턴
        if (character.nowState != CharacterState.Idle)
            return;

        // 찌르기 트리거에 플레이어 닿으면 Stab 패턴
        if (stabTrigger.atkTrigger)
        {
            // 찌르기 패턴 실행
            StartCoroutine(StabLeg());

            return;
        }

        // 범위 내에 있을때
        if (character.targetDir.magnitude <= atkRange)
        {
            // 쿨타임 됬을때, 스킵 아닐때
            if (atkCoolCount <= 0 && patten != Patten.Skip)
            {
                //공격 패턴 결정하기
                StartCoroutine(ChooseAttack());

                return;
            }
        }

        // 플레이어 따라가기
        Move();
    }

    void Watering()
    {
        // 삭제될 인덱스들
        List<Seed_AI> removeIndexes = new List<Seed_AI>();

        // 씨앗 모두 검사
        for (int i = 0; i < seedList.Count; i++)
        {
            // 씨앗이 죽었으면
            if (seedList[i].seedCharacter.isDead)
            {
                // 해당 인덱스 삭제 예약
                removeIndexes.Add(seedList[i]);
                // 물 끄기
                seedList[i].StopWater();
                // 다음으로 넘기기
                continue;
            }

            // 씨앗 초기화 안됬으면
            if (!seedList[i].initStart)
                // 다음으로 넘기기
                continue;

            // 씨앗이 공격 범위 내에 들어오면
            if (Vector3.Distance(seedList[i].transform.position, bodyTransform.position) <= waterRange)
            {
                // 자동으로 물 주기
                WaterFill(seedList[i]);
            }
            // 범위 바깥으로 나가면
            else
            {
                // 물 끄기
                seedList[i].StopWater();
            }
        }

        // 디스폰된 모든 씨앗 리스트에서 삭제
        for (int i = 0; i < removeIndexes.Count; i++)
        {
            seedList.Remove(removeIndexes[i]);
        }
    }

    void WaterFill(Seed_AI seed_AI)
    {
        // 형태 변하는 중이면 리턴
        if (seed_AI.turning)
            return;
        // 죽었으면 리턴
        if (seed_AI.seedCharacter.characterStat.hpNow <= 0)
        {
            // 물 끄기
            seed_AI.StopWater();
            return;
        }

        // 물줄기 끝지점
        Vector3 endPoint = seed_AI.transform.position + Vector3.up * 0.5f;

        // 물줄기 시작지점
        Vector3 startPoint = treeParticle.position;
        // 시작지점을 씨앗 방향으로 조정
        Vector3 seedDir = Vector3.ClampMagnitude(endPoint - startPoint, 1f);
        startPoint += seedDir;

        // 물줄기 중간 지점 베지어 포인트 위치 정하기
        Vector3 middlePoint = (startPoint + endPoint) / 2f + Vector3.up * waterPos_Y;
        var pointList = new List<Vector3>();

        // 베지어 포인트 vertexCount 개수만큼 정하기
        for (float ratio = 0; ratio <= 1; ratio += 1 / vertexCount)
        {
            var tangent1 = Vector3.Lerp(startPoint, middlePoint, ratio);
            var tangent2 = Vector3.Lerp(middlePoint, endPoint, ratio);
            var curve = Vector3.Lerp(tangent1, tangent2, ratio);

            pointList.Add(curve);
        }

        // 라인렌더러에 벡터 리스트 넘겨서 그리기
        seed_AI.waterLine.positionCount = pointList.Count;
        seed_AI.waterLine.SetPositions(pointList.ToArray());

        // 물 채우기
        seed_AI.FillWater();
    }

    IEnumerator ChooseAttack()
    {
        // 공격 상태로 전환
        character.nowState = CharacterState.Attack;

        // 가능한 공격 중에서 랜덤 뽑기
        int atkType = Random.Range(0, 3);

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
        character.nowState = CharacterState.Walk;

        // 플레이어까지 거리
        float distance = Vector3.Distance(character.movePos, bodyTransform.position);

        // 타겟과의 최소 거리 유지
        float nearSpeed = distance < followRange
        // 범위 안에 있을때, 거리 비례한 속도
        ? character.targetDir.magnitude - followRange
        // 범위 밖에 있을때, 최고 속도
        : maxSpeed;

        // 속도 범위 제한
        // nearSpeed = Mathf.Clamp(nearSpeed, minSpeed, maxSpeed);

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

        character.nowState = CharacterState.Idle;

        // 다리 움직이기
        FootCheck(character.rigid.velocity);
    }

    void FootCheck(Vector2 moveVelocity, bool setDefault = false)
    {
        for (int i = 0; i < footTargets.Length; i++)
        {
            // OnComplete 때문에 인덱스 캐싱
            int footIndex = i;
            // 같은 방향의 다른 다리 인덱스
            int sameVerticalIndex = -1;
            int sameHorizonIndex = -1;
            switch (footIndex)
            {
                case 0:
                    sameVerticalIndex = 1;
                    sameHorizonIndex = 3;
                    break;
                case 1:
                    sameVerticalIndex = 0;
                    sameHorizonIndex = 2;
                    break;
                case 2:
                    sameVerticalIndex = 3;
                    sameHorizonIndex = 1;
                    break;
                case 3:
                    sameVerticalIndex = 2;
                    sameHorizonIndex = 0;
                    break;
            }

            // 현재 발이 이동중이지 않고, 같은 가로세로 방향의 발도 이동중이지 않을때
            if ((nowMovePos[footIndex] == Vector2.zero
                && nowMovePos[sameVerticalIndex] == Vector2.zero
                && nowMovePos[sameHorizonIndex] == Vector2.zero
                && nowMoveFootNum <= 2) || setDefault)
            {
                // 발 초기화 위치에서 현재 발 위치까지 거리
                float distance = Vector2.Distance((Vector2)bodyTransform.position + defaultFootPos[footIndex], footTargets[footIndex].transform.position);

                // 마지막 위치로부터 footMoveDistance 보다 멀어지면
                if (distance > footMoveDistance * Random.Range(0.8f, 1.2f))
                {
                    // 발 움직이기
                    FootMove(moveVelocity, footIndex);
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
        Vector2 movePos = (Vector2)bodyTransform.position + defaultFootPos[footIndex] + velocity;
        nowMovePos[footIndex] = movePos;

        // print(footIndex + " : " + movePos + " : " + (Vector2)bodyTransform.position + " : " + defaultFootPos[footIndex] + " : " + velocity);

        // 발 이동개수 추가
        nowMoveFootNum++;

        // 발 점프해서 이동
        footTargets[footIndex].transform.DOJump(movePos, 2f, 1, footMoveSpeed * footMoveDistance)
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

            // 발소리 재생
            StepSound();

            // 해당 다리 HDR 불빛 밝히기
            SpriteRenderer[] legLights = legBones[footIndex].GetChild(0).GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < legLights.Length; i++)
            {
                Color color = legLights[i].color;
                color.a = 1f;
                legLights[i].color = color;
            }

            // 불빛 끄기
            for (int i = 0; i < legLights.Length; i++)
            {
                Color color = legLights[i].color;
                color.a = 0f;
                legLights[i].DOColor(color, 0.5f);
            }
        });
    }

    public void StepSound()
    {
        // 걷기 발소리 재생
        SoundManager.Instance.PlaySoundPool(stepSounds.ToList(), default, step_lastIndex);
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
        character.nowState = CharacterState.Attack;

        // 걷기 멈추기
        yield return StartCoroutine(StopMove());

        // 원위치 시간 대기
        yield return new WaitForSeconds(0.5f);

        // 공격 준비 시간
        float atkReadyTime = 0.2f;

        // 플레이어 방향 좌,우 판단
        bool isLeft = PlayerManager.Instance.transform.position.x < bodyTransform.position.x ? true : false;
        // 공격할 다리 인덱스
        int atkLegIndex = isLeft ? 0 : 3;
        // 공격할 다리 오브젝트
        Transform atkFoot = footTargets[atkLegIndex].transform;
        // 공격할 다리 콜라이더 모두 찾기
        Collider2D[] legColls = legBones[atkLegIndex].GetComponentsInChildren<Collider2D>();
        // 공격할 다리 스프라이트 모두 찾기
        SpriteRenderer[] legSprites ={
            legBones[atkLegIndex].GetChild(1).GetComponent<SpriteRenderer>(),
            legBones[atkLegIndex].GetChild(2).GetComponent<SpriteRenderer>(),
            legBones[atkLegIndex].GetChild(3).GetComponent<SpriteRenderer>()
        };
        // 몸체 기울일 방향
        Vector3 bodyRotation = Vector3.forward * (isLeft ? -15f : 15f);

        // 플레이어 반대 방향으로 살짝몸 기울이고
        bodyTransform.DORotate(bodyRotation, atkReadyTime)
        .SetEase(Ease.OutSine);

        // 공격 준비 로컬 위치
        Vector2 atkReadyPos = isLeft ? new Vector2(-3, 1) : new Vector2(3, 1);
        // 공격 준비 월드 위치
        atkReadyPos += (Vector2)bodyTransform.position;

        // 공격할 다리 오므려서 준비
        atkFoot.DOMove(atkReadyPos, atkReadyTime)
        .SetEase(Ease.OutSine);

        // 공격 준비시간 대기
        yield return new WaitForSeconds(atkReadyTime);

        // 공격 알림 재생
        SoundManager.Instance.PlaySound("Farmer_Leg_Alert");

        // 발 끝에서 반짝이는 인디케이터
        if (isLeft)
            L_legTipFlash.Play();
        else
            R_legTipFlash.Play();

        // 인디케이터 시간 대기
        yield return new WaitForSeconds(0.5f);

        // 공격 방향으로 몸체 기울이기
        bodyTransform.DORotate(-bodyRotation, 0.2f)
        .SetEase(Ease.OutSine);

        // 공격 다리 콜라이더 모두 활성화
        for (int i = 0; i < legColls.Length; i++)
            legColls[i].enabled = true;

        // 공격할 다리의 머터리얼 모두 아웃라인으로 변경
        for (int i = 0; i < legSprites.Length; i++)
            legSprites[i].material = legAtkMat;

        // 찌를 위치
        Vector2 stabPos = legBones[atkLegIndex].position + (PlayerManager.Instance.transform.position - legBones[atkLegIndex].position).normalized * 20f;

        // 플레이어 위치로 다리 뻗어서 찌르기
        atkFoot.DOMove(stabPos, 0.2f)
                .SetEase(Ease.Linear);

        // 찌르는 소리 재생
        SoundManager.Instance.PlaySound("Farmer_Leg_Stab");

        // 찌르는 시간 대기
        yield return new WaitForSeconds(0.2f);

        // 관절 모두 찾기
        Transform[] bones = {
            legBones[atkLegIndex].GetChild(0).GetChild(0),
            legBones[atkLegIndex].GetChild(0).GetChild(0).GetChild(0)
            };
        // 관절 사이 케이블 모두 찾기
        LineRenderer[] cables = {
            bones[0].GetComponent<LineRenderer>(),
            bones[1].GetComponent<LineRenderer>()
            };

        // 해당 다리 Solver 끄기
        solvers[atkLegIndex].gameObject.SetActive(false);

        // 관절 순서대로 늘리기
        for (int i = 0; i < 2; i++)
        {
            // 해당 관절 캐싱
            Transform bone = bones[i];
            // 케이블 캐싱
            LineRenderer cable = legCables[i];

            // 관절 사이 늘리기
            bone.DOLocalMove(bone.localPosition + Vector3.right * stretchRange / 2f, stretchTime)
            .SetEase(Ease.Linear)
            .OnStart(() =>
            {
                // 라인 렌더러 양쪽 포인트 초기화
                cable.SetPosition(0, bone.position);
                cable.SetPosition(1, bone.parent.position);
                // 해당 관절 사이 라인렌더러 켜기
                cable.gameObject.SetActive(true);

                // 케이블 위치 갱신 시작
                StartCoroutine(LegCable(cable, bone));
            });

            // 찌르는 소리 재생
            SoundManager.Instance.PlaySound("Farmer_Leg_Stretch");

            // 뻗는 시간 대기
            yield return new WaitForSeconds(stretchTime);
        }

        // 찌르기 후 대기
        yield return new WaitForSeconds(0.5f);

        // 관절 모두 역순으로 좁히기
        for (int i = 1; i >= 0; i--)
        {
            // 해당 관절 캐싱
            Transform bone = bones[i];
            // 케이블 캐싱
            LineRenderer cable = legCables[i];

            // 관절 사이 좁히기
            bone.DOLocalMove(bone.localPosition + Vector3.left * stretchRange / 2f, stretchTime)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                // 해당 관절 사이 라인렌더러 끄기
                cable.gameObject.SetActive(false);

                // 다리 장착 소리 재생
                SoundManager.Instance.PlaySound("Farmer_Leg_Pull");
            });

            // 복귀 시간 대기
            yield return new WaitForSeconds(stretchTime);
        }

        // 해당 다리 Solver 켜기
        solvers[atkLegIndex].gameObject.SetActive(true);

        // 다리 리셋 위치
        Vector2 legResetPos = (Vector2)bodyTransform.position + defaultFootPos[atkLegIndex];
        // 리셋 직전 들어올리기 위치
        Vector2 legUpPos = legResetPos + Vector2.up * 4f;

        // 플레이어 반대 방향으로 기울이기
        bodyTransform.DORotate(bodyRotation, 0.5f)
        .SetEase(Ease.Linear);

        // 다리 들어올리기
        atkFoot.DOMove(legUpPos, 0.5f)
        .SetEase(Ease.Linear);

        // 오므리는 시간 대기
        yield return new WaitForSeconds(0.5f);

        // 발소리 재생
        StepSound();

        // 공격 다리 위치 초기화
        atkFoot.DOMove(legResetPos, 0.5f)
        .SetEase(Ease.InBack);

        // 몸체 기울기 초기화
        bodyTransform.DORotate(Vector3.zero, 0.5f)
        .SetEase(Ease.InBack);

        // 아웃라인 끄기 대기
        yield return new WaitForSeconds(0.2f);

        // 공격 다리 콜라이더 모두 비활성화
        for (int i = 0; i < legColls.Length; i++)
            legColls[i].enabled = false;

        // 공격할 다리의 머터리얼 모두 초기화
        for (int i = 0; i < legSprites.Length; i++)
            legSprites[i].material = SystemManager.Instance.characterMat;

        // 몸체 및 다리 초기화 대기
        yield return new WaitForSeconds(1f);

        // 쿨타임 갱신
        // atkCoolCount = StabCooltime;

        // 상태 초기화
        character.nowState = CharacterState.Idle;
    }

    IEnumerator LegCable(LineRenderer cable, Transform bone)
    {
        WaitForSeconds wait = new WaitForSeconds(Time.deltaTime);

        // 해당 케이블 켜져있는 동안 반복
        while (cable.gameObject.activeSelf)
        {
            // 늘리는동안 라인 렌더러로 관절사이 이어주기 업데이트
            cable.SetPosition(0, bone.position);
            cable.SetPosition(1, bone.parent.position);

            yield return wait;
        }
    }

    IEnumerator PlantSeed()
    {
        // 씨앗 인디케이터 재생
        seedPulse.Play();
        // 인디케이터 사운드 재생
        SoundManager.Instance.PlaySound("Farmer_Attack_Alert");
        // 인디케이터 딜레이
        yield return new WaitForSeconds(2f);

        for (int i = 0; i < seedHole.childCount; i++)
        {
            // 씨앗 발사
            StartCoroutine(SeedShot(seedHole.GetChild(i).transform.position));

            yield return new WaitForSeconds(0.1f);
        }

        // 후딜레이 대기
        yield return new WaitForSeconds(1f);

        // 쿨타임 갱신
        atkCoolCount = PlantSeedCooltime;

        // 상태 초기화
        character.nowState = CharacterState.Idle;
    }

    IEnumerator SeedShot(Vector2 spawnPos)
    {
        // 씨앗 소환
        Seed_AI seedObj = LeanPool.Spawn(seedPrefab, spawnPos, Quaternion.identity, ObjectPool.Instance.enemyAtkPool);
        // 씨앗 스프라이트
        Transform seedSprite = seedObj.transform.GetChild(0);

        // 씨앗 꺼내는 소리 재생
        SoundManager.Instance.PlaySound("Farmer_Seed_Launch");

        // 씨앗 레이어 변경
        SortingGroup seedSort = seedObj.GetComponentInChildren<SortingGroup>();
        seedSort.sortingOrder = 1;

        // 씨앗 랜덤 각도로 초기화
        seedSprite.rotation = Quaternion.Euler(Vector3.forward * Random.Range(0f, 360f));

        // 플레이어 주변 씨앗 착지 위치
        // Vector2 seedPos = seedObj.transform.position + (seedObj.transform.position - seedHole.position).normalized * seedRange + Random.insideUnitSphere * 0f;
        Vector2 seedPos = character.targetPos + Random.insideUnitSphere * seedRange;

        // 씨앗 이동 시키기
        seedObj.transform.DOMove(seedPos, 1f)
        .SetEase(Ease.InSine);

        // 씨앗 점프 시키기
        seedSprite.DOLocalJump(Vector2.up * 0.5f, 5f, 1, 1f)
        .SetEase(Ease.InSine);

        yield return new WaitForSeconds(1f);

        // 씨앗 착지 소리 재생
        SoundManager.Instance.PlaySoundPool(seedLandSounds.ToList(), seedPos);

        // 씨앗 레이어 초기화
        seedSort.sortingOrder = 0;

        // 씨앗 초기화 시작
        seedObj.initStart = true;

        // 씨앗을 리스트에 저장
        seedList.Add(seedObj);
    }

    IEnumerator BioGas()
    {
        // 애니메이터 끄기
        bodyTransform.GetComponent<Animator>().enabled = false;

        // 공격 횟수만큼 반복
        for (int j = 0; j < gasLoopNum; j++)
        {
            // 다리를 아래로 뻗어 높이 올라간 다음
            bodyTransform.DOLocalMove(new Vector2(0, 5f), 1f)
            .SetEase(Ease.InBack);

            yield return new WaitForSeconds(0.5f);

            // 포이즌 인디케이터 재생
            bioPulse.Play();
            // 인디케이터 사운드 재생
            SoundManager.Instance.PlaySound("Farmer_Attack_Alert");

            yield return new WaitForSeconds(0.5f);

            // 다리를 굽히며 아래로 내려가면서
            bodyTransform.DOLocalMove(new Vector2(0, -1f), 1f)
            .SetEase(Ease.OutCirc);

            yield return new WaitForSeconds(0.5f);

            // 가스 생성 사운드 재생
            SoundManager.Instance.PlaySound("Farmer_BioGas");

            // 독구름 생성 개수 초기화
            int atkNum = gasNum;

            // 독구름이 원형으로 퍼짐
            for (int i = 0; i < atkNum; i++)
            {
                // 독구름 생성
                Rigidbody2D poisonObj = LeanPool.Spawn(bioGasPrefab, bodyTransform.position, Quaternion.identity, ObjectPool.Instance.enemyAtkPool);

                // 목표 각도
                float targetAngle = 360f * i / atkNum;
                // 독구름 목표 방향
                Vector3 targetDir = new Vector3(Mathf.Sin(Mathf.Deg2Rad * targetAngle), Mathf.Cos(Mathf.Deg2Rad * targetAngle), 0);

                poisonObj.velocity = targetDir.normalized * gasSpeed;
            }

            yield return new WaitForSeconds(1f);
        }

        // 일어서기
        bodyTransform.DOLocalMove(Vector2.zero, 2f)
        .SetEase(Ease.OutBack);

        // 일어서기 대기
        yield return new WaitForSeconds(2f);

        // 애니메이터 켜기
        bodyTransform.GetComponent<Animator>().enabled = true;

        // 쿨타임 갱신
        atkCoolCount = BioGasCooltime;

        // 상태 초기화
        character.nowState = CharacterState.Idle;
    }

    IEnumerator SunHeal()
    {
        // 애니메이터 끄기
        bodyTransform.GetComponent<Animator>().enabled = false;
        // 털썩 주저 앉음
        bodyTransform.DOLocalMove(new Vector2(0, -2f), 1f)
        .SetEase(Ease.OutBounce);

        yield return new WaitForSeconds(0.5f);

        // 바닥 충돌 사운드 재생
        SoundManager.Instance.PlaySound("Farmer_Sit");

        // 착지 먼지 이펙트 생성
        LeanPool.Spawn(landDustPrefab, bodyTransform.position, Quaternion.Euler(Vector3.zero), ObjectPool.Instance.effectPool);

        yield return new WaitForSeconds(0.5f);

        // 나무 라이트 반짝임 반복
        treeLight.intensity = 0;
        Tween lightTween = DOTween.To(() => treeLight.intensity, x => treeLight.intensity = x, 2f, 0.5f)
        .SetEase(Ease.OutSine)
        .SetLoops(-1, LoopType.Yoyo);

        // 나무 반짝임 사운드 재생
        AudioSource treeLightSound = SoundManager.Instance.PlaySound("Farmer_Heal_TreeLight", 0, 1, 1000);

        // 카메라 반복 줌아웃
        StartCoroutine(CameraZoomOut(3));

        float sunDelay = 2f;
        WaitForSeconds wait = new WaitForSeconds(sunDelay);

        for (int i = 0; i < sunNum; i++)
        {
            // 태양 입자가 모든 방향에서 생성되어 천천히 보스쪽으로 이동
            StartCoroutine(SunMove());

            yield return wait;
        }

        // 마지막 태양관 들어갈때까지 대기
        yield return new WaitForSeconds(sunRange / sunSpeed);

        // 나무 반짝임 사운드 정지
        SoundManager.Instance.StopSound(treeLightSound, 1f);

        // 나무 반짝임 정지
        lightTween.Kill();
        // 나무 라이트 초기화
        DOTween.To(() => treeLight.intensity, x => treeLight.intensity = x, 0f, 1f);

        // 후딜레이
        yield return new WaitForSeconds(1f);

        // 일어서기
        bodyTransform.DOLocalMove(Vector2.zero, 2f)
        .SetEase(Ease.OutBack);

        // 일어서기 대기
        yield return new WaitForSeconds(2f);

        // 카메라 줌 초기화
        UIManager.Instance.CameraZoom(2f, 0);

        // 애니메이터 켜기
        bodyTransform.GetComponent<Animator>().enabled = true;

        // 쿨타임 갱신
        atkCoolCount = SunHealCooltime;

        // 상태 초기화
        character.nowState = CharacterState.Idle;
    }

    IEnumerator CameraZoomOut(int loopNum)
    {
        // 반짝이며 카메라 천천히 줌아웃
        for (int i = 0; i < loopNum; i++)
        {
            // 입력된 사이즈대로 줌인/줌아웃 트윈 실행
            DOTween.To(() => Camera.main.orthographicSize, x => Camera.main.orthographicSize = x, UIManager.Instance.defaultCamSize + (i + 1) * 3f, 0.5f)
            .SetEase(Ease.OutSine);

            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator SunMove()
    {
        // 태양광 소환 위치
        Vector2 sunPos = (Vector2)bodyTransform.position + Random.insideUnitCircle.normalized * sunRange;

        // 태양광 소환
        GameObject sunObj = LeanPool.Spawn(sunPrefab, sunPos, Quaternion.Euler(Vector3.forward * Random.value * 360f), ObjectPool.Instance.enemyAtkPool);

        // 태양광을 리스트에 저장
        sunList.Add(sunObj);

        // 이동 시간
        float moveTime = sunRange / sunSpeed;

        // 태양광 캐릭터 컴포넌트
        Character sunCharacter = sunObj.GetComponent<Character>();
        // 캐릭터 초기화 까지 대기
        yield return new WaitUntil(() => sunCharacter.initialFinish);

        // 나무 위치로 움직이기
        sunObj.transform.DOMove(treeTransform.position + Vector3.up * 4.5f, moveTime)
        .SetEase(Ease.Linear)
        .OnUpdate(() =>
        {
            // 도중에 태양광 죽으면 멈추기
            if (sunCharacter.isDead)
            {
                sunObj.transform.DOKill();
            }
        })
        .OnComplete(() =>
        {
            // 회복 사운드 재생
            SoundManager.Instance.PlaySound("Farmer_Heal_GetSun");

            // 이동 완료하면 체력 회복
            character.hitBoxList[0].Damage(-1, false);

            // 나무 스프라이트 스케일 바운스
            treeTransform.DOPunchScale(Vector2.one * 0.1f, 0.5f);

            // 디스폰
            LeanPool.Despawn(sunObj);
        });
    }

    private void OnDrawGizmosSelected()
    {
        // 보스부터 이동 위치까지 직선
        Gizmos.color = Color.red;
        Gizmos.DrawLine(bodyTransform.position, character.movePos);

        // 이동 위치 기즈모
        Gizmos.DrawIcon(character.movePos, "Circle.png", true, new Color(1, 0, 0, 0.5f));

        // 추적 위치 기즈모
        Gizmos.DrawIcon(character.targetPos, "Circle.png", true, new Color(0, 0, 1, 0.5f));

        // 추적 위치부터 이동 위치까지 직선
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(character.targetPos, character.movePos);
    }
}
