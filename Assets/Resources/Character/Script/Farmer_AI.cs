using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class Farmer_AI : MonoBehaviour
{
    [Header("State")]
    [SerializeField] float atkRange = 30f; // 공격 범위
    [SerializeField] float followRange = 10f; // 타겟과의 거리
    [SerializeField, ReadOnly] Vector3 targetVelocity; // 현재 이동속도
    [SerializeField] float maxSpeed = 1f; // 최대 이동속도
    [SerializeField] Patten patten = Patten.None;
    enum Patten { PlantSeed, BioGas, SunHeal, Skip, None };

    [Header("Cooltime")]
    [SerializeField] float atkCoolCount;
    [SerializeField] float StabCooltime = 1f;
    [SerializeField] float PlantSeedCooltime = 1f;
    [SerializeField] float BioGasCooltime = 1f;
    [SerializeField] float SunHealCooltime = 1f;

    [Header("Refer")]
    [SerializeField] Character character;
    [SerializeField] TextMeshProUGUI stateText; //! 테스트 현재 상태
    [SerializeField] Transform bodyTransform; // 몸체 오브젝트
    [SerializeField] Transform treeTransform; // 나무 오브젝트

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
    [SerializeField] Light2D treeLight; // 나무 모양 라이트
    [SerializeField] Transform seedPrefab; // 씨앗 프리팹
    [SerializeField] Transform plantPrefab; // 식물 프리팹
    [SerializeField] Transform seedHole; // 씨앗 발사할 구멍들
    [SerializeField] List<Transform> seedList; // 씨앗 목록

    [Header("BioGas")]
    [SerializeField] Rigidbody2D poisonPrefab; // 독구름 프리팹
    [SerializeField] int poisonLoopNum; // 공격 횟수
    [SerializeField] int poisonNum; // 한번에 독구름 생성 개수
    [SerializeField] float poisonSpeed; // 독구름 이동 속도

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
        if (character.hpNow <= 0)
        {
            //todo 태양광 모두 없에기
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
        character.targetDir = character.movePos - transform.position;

        //! 쿨타임 및 거리 확인
        stateText.text = "CoolCount : " + atkCoolCount + "\nDistance : " + character.targetDir.magnitude;

        // 패턴 정하기
        ManageAction();

        // 패시브 패턴
        Passive();
    }

    void ManageAction()
    {
        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // Idle 아니면 리턴
        if (character.nowState != Character.State.Idle)
            return;

        // 범위 내에 있을때
        if (character.targetDir.magnitude <= atkRange)
        {
            // 쿨타임 됬을때
            if (atkCoolCount <= 0)
            {
                //공격 패턴 결정하기
                StartCoroutine(ChooseAttack());

                return;
            }
        }

        // 찌르기 트리거에 플레이어 닿으면 Stab 패턴
        if (stabTrigger.atkTrigger)
        {
            // 찌르기 패턴 실행
            StartCoroutine(StabLeg());

            return;
        }

        // 플레이어 따라가기
        Move();
    }

    void Passive()
    {
        // 삭제될 인덱스들
        List<int> removeIndexes = new List<int>();

        // 씨앗 모두 검사
        for (int i = 0; i < seedList.Count; i++)
        {
            //todo 씨앗이 디스폰 됬으면
            if (!seedList[i].gameObject.activeInHierarchy)
            {
                //todo 해당 인덱스 삭제 예약
                removeIndexes.Add(i);
                // 다음으로 넘기기
                continue;
            }

            //todo 씨앗이 공격 범위 내에 들어오면
            if (Vector3.Distance(seedList[i].position, transform.position) <= atkRange)
            {
                //todo 패시브 스킬로 뿌렸던 씨 근처에 가면 자동으로 물을 줌
                //todo 파란 물 모양 라인렌더러로 구현, 베지어 곡선 포물선
                //todo 일정 시간 이상 물 먹은 나무는 슬라임으로 변함
                //todo 슬라임 스프라이트 알파값 올리고 나무 디스폰, 슬라임 초기화
            }
        }

        // 디스폰된 모든 씨앗 리스트에서 삭제
        for (int i = 0; i < removeIndexes.Count; i++)
        {
            seedList.RemoveAt(i);
        }
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

        // 플레이어까지 거리
        float distance = Vector3.Distance(character.movePos, bodyTransform.position);

        // 공격범위 이내 접근 못하게 하는 속도 계수
        float nearSpeed = distance < followRange
        // 범위 안에 있을때, 거리 비례한 속도
        ? character.targetDir.magnitude - followRange
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
                float distance = Vector2.Distance((Vector2)bodyTransform.position + defaultFootPos[footNum], footTargets[footNum].transform.position);

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
        Vector2 movePos = (Vector2)bodyTransform.position + defaultFootPos[footIndex] + velocity;
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
        bool isLeft = PlayerManager.Instance.transform.position.x < bodyTransform.position.x ? true : false;
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
        atkReadyPos += (Vector2)bodyTransform.position;

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
        Vector2 stabPos = legBones[atkLegIndex].position + (PlayerManager.Instance.transform.position - legBones[atkLegIndex].position).normalized * 20f;

        // 플레이어 위치로 다리 뻗어서 찌르기
        atkFoot.DOMove(stabPos, 0.2f)
                .SetEase(Ease.OutBack);

        //todo 찌른 직후 Solver 끄고 관절 사이 늘리기, 라인 렌더러로 관절사이 이어주기
        // 해당 다리 Solver 끄기
        // 관절 사이 늘리기
        // 늘리는동안 라인 렌더러로 관절사이 이어주기 업데이트

        // 찌르는 시간 대기
        yield return new WaitForSeconds(0.2f);

        // 공격 다리 콜라이더 모두 비활성화
        for (int i = 0; i < legColls.Length; i++)
            legColls[i].enabled = false;

        // 찌르기 후딜
        yield return new WaitForSeconds(0.5f);

        // 몸체 기울기 초기화
        bodyTransform.transform.DORotate(Vector3.zero, 0.5f)
        .SetEase(Ease.OutBack);

        // 다리 다시 오므리기
        atkFoot.DOMove(atkReadyPos, 0.2f)
        .SetEase(Ease.InBack);

        // 오므리는 시간 대기
        yield return new WaitForSeconds(0.2f);

        // 공격 다리 위치 초기화
        atkFoot.DOMove((Vector2)bodyTransform.position + defaultFootPos[atkLegIndex], 0.3f)
        .SetEase(Ease.InBack);

        // 몸체 및 다리 초기화 대기
        yield return new WaitForSeconds(0.5f);

        // 쿨타임 갱신
        // atkCoolCount = StabCooltime;

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
        atkCoolCount = PlantSeedCooltime;

        // 상태 초기화
        character.nowState = Character.State.Idle;
    }

    IEnumerator GrowPlant(Vector2 spawnPos)
    {
        // 씨앗 소환
        Transform seedShadow = LeanPool.Spawn(seedPrefab, spawnPos, Quaternion.identity, SystemManager.Instance.enemyAtkPool);
        // 씨앗 스프라이트
        Transform seed = seedShadow.GetChild(0);

        // 씨앗을 리스트에 저장
        seedList.Add(seedShadow);

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

        // 애니메이터 끄기
        bodyTransform.GetComponent<Animator>().enabled = false;

        // 공격 횟수만큼 반복
        for (int j = 0; j < poisonLoopNum; j++)
        {
            // 다리를 아래로 뻗어 높이 올라간 다음
            bodyTransform.DOLocalMove(new Vector2(0, 5f), 1f)
            .SetEase(Ease.InBack);

            yield return new WaitForSeconds(1f);

            // 다리를 굽히며 아래로 내려가면서
            bodyTransform.DOLocalMove(new Vector2(0, -1f), 1f)
            .SetEase(Ease.OutCirc);

            yield return new WaitForSeconds(0.5f);

            // 독구름 생성 개수 초기화
            int atkNum = poisonNum;

            // 독구름이 원형으로 퍼짐
            for (int i = 0; i < atkNum; i++)
            {
                // 독구름 생성
                Rigidbody2D poisonObj = LeanPool.Spawn(poisonPrefab, bodyTransform.position, Quaternion.identity, SystemManager.Instance.enemyAtkPool);

                // 목표 각도
                float targetAngle = 360f * i / atkNum;
                // 독구름 목표 방향
                Vector3 targetDir = new Vector3(Mathf.Sin(Mathf.Deg2Rad * targetAngle), Mathf.Cos(Mathf.Deg2Rad * targetAngle), 0);

                poisonObj.velocity = targetDir.normalized * poisonSpeed;
            }

            yield return new WaitForSeconds(1f);
        }

        //todo 싹이 튼 씨앗들에 닿으면 Life 슬라임으로 변함

        // 일어서기
        bodyTransform.DOLocalMove(new Vector2(0, 1.5f), 2f)
        .SetEase(Ease.OutBack);

        // 일어서기 대기
        yield return new WaitForSeconds(2f);

        // 애니메이터 켜기
        bodyTransform.GetComponent<Animator>().enabled = true;

        // 쿨타임 갱신
        atkCoolCount = BioGasCooltime;

        // 상태 초기화
        character.nowState = Character.State.Idle;
    }

    IEnumerator SunHeal()
    {
        // 애니메이터 끄기
        bodyTransform.GetComponent<Animator>().enabled = false;
        // 털썩 주저 앉음
        bodyTransform.DOLocalMove(new Vector2(0, -1f), 1f)
        .SetEase(Ease.OutBounce);

        yield return new WaitForSeconds(0.5f);

        // 착지 먼지 이펙트 생성
        LeanPool.Spawn(landDustPrefab, bodyTransform.position, Quaternion.Euler(Vector3.zero), SystemManager.Instance.effectPool);

        yield return new WaitForSeconds(0.5f);

        // 나무 라이트 반짝임 반복
        treeLight.intensity = 0;
        Tween lightTween = DOTween.To(() => treeLight.intensity, x => treeLight.intensity = x, 2f, 0.5f)
        .SetEase(Ease.OutSine)
        .SetLoops(-1, LoopType.Yoyo);

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

        // 나무 반짝임 정지
        lightTween.Kill();
        // 나무 라이트 초기화
        DOTween.To(() => treeLight.intensity, x => treeLight.intensity = x, 0f, 1f);

        // 후딜레이
        yield return new WaitForSeconds(1f);

        // 일어서기
        bodyTransform.DOLocalMove(new Vector2(0, 1.5f), 2f)
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
        character.nowState = Character.State.Idle;
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
        Vector2 sunPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * sunRange;

        // 태양광 소환
        GameObject sunObj = LeanPool.Spawn(sunPrefab, sunPos, Quaternion.Euler(Vector3.forward * Random.value * 360f), SystemManager.Instance.enemyAtkPool);

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
