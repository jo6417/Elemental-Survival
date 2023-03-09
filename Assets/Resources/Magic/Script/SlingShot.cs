using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class SlingShot : MonoBehaviour
{

    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] Transform stonePrefab; // 바위 발사체 프리팹
    Transform mergeStone = null; // 합쳐진 돌 투사체

    [Header("State")]
    List<StoneState> shotAble = new List<StoneState>(); // 모든 투사체들의 상태값
    enum StoneState { Charging, Shotable, Shot, Dead };
    List<Transform> stoneList = new List<Transform>(); // 모든 투사체 오브젝트
    int mergeNum; // 바위 합쳐진 개수
    float damage; // 합쳐진 데미지
    float durationCount; // 남은 시간
    int shotNum = 0; // 발사된 개수
    float chargeTime = 0.5f; // 기 모으는 시간

    [Header("Status")]
    float scale;
    float coolTime;
    int atkNum;
    float power;

    private void Awake()
    {
        if (magicHolder.despawnAction == null)
            // 디스폰 콜백 추가
            magicHolder.despawnAction += DespawnCallback;
    }

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 중심점을 주변 랜덤 위치로 옮기기
        transform.position = transform.position + (Vector3)Random.insideUnitCircle.normalized * Random.Range(scale * 2f, scale * 4f);

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        // 스탯 초기화
        scale = MagicDB.Instance.MagicScale(magicHolder.magic);
        atkNum = MagicDB.Instance.MagicAtkNum(magicHolder.magic);
        coolTime = MagicDB.Instance.MagicCoolTime(magicHolder.magic);
        power = MagicDB.Instance.MagicPower(magicHolder.magic);

        // 발사된 개수 초기화
        shotNum = 0;
        // 합쳐진 개수 초기화
        mergeNum = 0;
        // 합체 투사체 초기화
        mergeStone = null;
        // 상태값 리스트 초기화
        shotAble.Clear();
        // 투사체 리스트 초기화
        stoneList.Clear();

        // 공격 개수만큼 바위 생성
        for (int i = 0; i < atkNum; i++)
        {
            // 각 투사체들 상태값 초기화
            shotAble.Add(StoneState.Charging);

            StartCoroutine(ShotStone(i));

            // 생성 사이 딜레이
            yield return new WaitForSeconds(0.1f);
        }

        // 모든 투사체 발사할때까지 대기
        yield return new WaitUntil(() => shotNum >= atkNum);

        // // 쿨다운 시작
        // CastMagic.Instance.Cooldown(magicHolder.magic, magicHolder.isManualCast, coolTime);

        // 모든 투사체 디스폰 될때까지 대기
        for (int i = 0; i < shotAble.Count; i++)
            yield return new WaitUntil(() => shotAble[i] == StoneState.Dead);

        // 디스폰
        LeanPool.Despawn(transform);
    }

    IEnumerator ShotStone(int index)
    {
        // 트윈 시작시간 기록
        float tweenStartTime = Time.time;

        // 생성 위치 계산
        Vector3 spawnPos = transform.position + (Vector3)Random.insideUnitCircle.normalized * Random.Range(scale * 2f, scale * 4f);

        // 바위 생성
        Transform stone = LeanPool.Spawn(stonePrefab, spawnPos, Quaternion.identity, ObjectPool.Instance.magicPool);

        // 투사체 리스트에 등록
        stoneList.Add(stone);

        // 태그 및 레이어 물려주기
        stone.gameObject.tag = gameObject.tag;
        stone.gameObject.layer = gameObject.layer;
        // MagicHolder 물려주기
        MagicHolder stoneMagicHolder = stone.GetComponent<MagicHolder>();
        stoneMagicHolder.magic = magicHolder.magic;
        stoneMagicHolder.targetType = magicHolder.targetType;
        stoneMagicHolder.targetPos = magicHolder.targetPos;

        // 아웃라인 색을 바꿔주기
        SpriteRenderer[] stoneSprites = stone.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < stoneSprites.Length; i++)
            stoneSprites[i].material.SetColor("_OutLineColor", new Color(1f, 0.5f, 0f, 1f));

        // 투사체 컴포넌트 찾기
        MagicProjectile stoneProjectile = stone.GetComponent<MagicProjectile>();
        // 바위 부모 찾기
        Transform scaler = stone.Find("Scaler");
        // 바위 스프라이트 찾기
        SpriteRenderer stoneSprite = scaler.Find("StoneSpin").GetComponent<SpriteRenderer>(); ;
        // 후방 먼지 이펙트 찾기
        ParticleManager backDust = stone.Find("DirtTrail").GetComponent<ParticleManager>();
        // 모래 모으기 이펙트 찾기
        ParticleManager dirtCharge = stone.Find("DirtCharge").GetComponent<ParticleManager>();
        // 콜라이더 찾기
        Collider2D coll = stone.GetComponent<Collider2D>();

        // 콜라이더 켜기
        coll.enabled = true;

        // 뒤쪽 먼지 이펙트 끄기
        backDust.gameObject.SetActive(false);

        // 차지중에는 마우스 바라보기
        StartCoroutine(FollowCursor(stone, index));

        // 타겟 방향으로 회전
        transform.rotation = Quaternion.Euler(magicHolder.targetPos - transform.position);

        // 수동 시전시
        if (magicHolder.isQuickCast)
            // 공격 허용 판단
            StartCoroutine(AllowAttack(index, tweenStartTime));

        // 모래 모으기 이펙트 시작
        dirtCharge.particle.Play();

        // 바위 스케일 초기화
        scaler.localScale = Vector2.zero;
        // 스케일만큼 커지기
        scaler.DOScale(Vector2.one * scale, chargeTime);

        // 먼지 스케일 초기화
        backDust.transform.localScale = Vector2.zero;
        // 스케일만큼 커지기
        backDust.transform.DOScale(Vector2.one * scale, chargeTime)
        .OnComplete(() =>
        {
            // 자동 시전시
            if (!magicHolder.isQuickCast)
                //  사격 가능
                shotAble[index] = StoneState.Shotable;
        });

        // 회전 방향 랜덤
        Vector3 rotation = Random.value > 0.5f ? Vector3.forward : Vector3.back;
        // 회전 속도 랜덤
        float spinTime = Random.Range(0.5f, 1f);

        // 수동 시전시 공격 불능일때
        // 자동 시전시 그냥 회전
        if (!magicHolder.isQuickCast
        || (magicHolder.isQuickCast && shotAble[index] == StoneState.Charging))
            // 바위 회전
            stoneSprite.transform.DOLocalRotate(rotation * 360f, spinTime, RotateMode.WorldAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .OnUpdate(() =>
            {
                // 차징 끝나면
                if (shotAble[index] != StoneState.Charging)
                    // 회전 정지
                    stoneSprite.transform.DOKill();

                // 차징 도중 몬스터 충돌해서 파괴됬을때
                if (!coll.enabled)
                {
                    // 파괴 상태로 변경
                    shotAble[index] = StoneState.Dead;
                    // 발사 개수 증가
                    shotNum++;

                    // 모으기 이펙트 정지
                    dirtCharge.particle.Stop();
                }
            });

        // 모으는 시간 대기
        yield return new WaitForSeconds(chargeTime);

        // 바라보는 반대쪽으로 먼지 이펙트 뿜기 (이동할 방향 표시)
        backDust.gameObject.SetActive(true);

        // 공격 트리거 대기
        yield return new WaitUntil(() => shotAble[index] == StoneState.Shotable);

        // 해당 바위 아직 살아있으면
        if (coll.enabled)
        {
            // 모으기 이펙트 정지
            dirtCharge.particle.Stop();

            // 발사 상태로 변경
            shotAble[index] = StoneState.Shot;
            // 발사된 개수 증가
            shotNum++;

            // 발사
            StartCoroutine(stoneProjectile.ShotMagic());

            // 던지기 사운드 재생
            SoundManager.Instance.PlaySound("SlingShot_Throw", transform.position);
        }
        // 차징 도중 몬스터 충돌해서 파괴됬을때
        else
        {
            // 파괴 상태로 변경
            shotAble[index] = StoneState.Dead;
            // 발사 개수 증가
            shotNum++;

            // 모으기 이펙트 정지
            dirtCharge.particle.Stop();
        }

        // 바위 스프라이트 꺼질때까지 대기
        yield return new WaitUntil(() => !stoneSprite.enabled);

        // 먼지 꼬리 이펙트 끄기
        backDust.SmoothDisable();

        // 디스폰 될때까지 대기
        yield return new WaitUntil(() => !stone.gameObject.activeSelf);

        // 죽은 상태로 변경
        shotAble[index] = StoneState.Dead;
    }

    IEnumerator AllowAttack(int index, float tweenStartTime)
    {
        // 플레이어 속도 느려짐
        Buff buff = PlayerManager.Instance.SetBuff("SlingShot_Slow", nameof(PlayerManager.Instance.characterStat.moveSpeed), true, 0.5f, -1, false);
        // 플레이어 속도 업데이트
        PlayerManager.Instance.Move();

        // 가운데 지점으로 이동
        stoneList[index].DOMove(transform.position, chargeTime)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            // 사이즈 트윈 죽이기
            stoneList[index].Find("Scaler").DOKill();

            // 합쳐진 바위 개수 올리기
            mergeNum++;

            // 합칠 바위가 없으면 해당 바위를 넣기
            if (mergeStone == null)
            {
                mergeStone = stoneList[index];

                // 고정 데미지 넣기
                magicHolder.power = power;
            }
            // 이미 합칠 바위가 있으면 디스폰
            else
            {
                // 발사 상태로 변경
                shotAble[index] = StoneState.Dead;
                // 발사된 개수 증가
                shotNum++;

                // 합쳐진 바위 디스폰
                LeanPool.Despawn(stoneList[index]);

                // 사이즈업
                Vector2 mergeScale = Vector2.one * scale * (1 + magicHolder.magic.scalePerLev * (mergeNum - 1));
                // 합쳐진 개수만큼 데미지 배수
                magicHolder.power = power * (mergeNum - 1);

                // // 남은 duration 계산
                // float remainTime = duration - (Time.time - tweenStartTime);
                // remainTime = Mathf.Clamp(remainTime, 0, duration);

                // 사이즈 커지기
                mergeStone.localScale = Vector2.one * mergeScale;

                //todo 합체 이펙트 재생
                //todo 합체 사운드 재생
            }
        });

        //todo 마우스가 아닌 해당 스킬 키로 변경
        // 마우스를 떼거나, 모두 파괴 될때까지 대기
        yield return new WaitUntil(() => !PlayerManager.Instance.player_Input.Player.Click.inProgress || shotNum >= atkNum);

        // 플레이어 속도 디버프 빼기
        StartCoroutine(PlayerManager.Instance.StopBuff(buff, 0));

        // 바위 디스폰 안됬으면
        if (stoneList[index].gameObject.activeSelf)
        {
            if (shotAble[index] == StoneState.Charging)
                // 공격 허용
                shotAble[index] = StoneState.Shotable;

            // 가운데로 모이는 트윈 끄기
            stoneList[index].DOKill();
        }
    }

    IEnumerator FollowCursor(Transform stone, int index)
    {
        // 프레임 끝나는 시간
        WaitForEndOfFrame delay = new WaitForEndOfFrame();

        // 수동 시전일때, 차지 중일때
        while (magicHolder.isQuickCast && shotAble[index] == StoneState.Charging)
        {
            // 타겟 위치 계속 변경
            magicHolder.targetPos = PlayerManager.Instance.GetMousePos();

            // 마우스 방향 계산
            Vector2 dir = magicHolder.targetPos - stone.transform.position;
            float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion mouseDir = Quaternion.Euler(Vector3.forward * rotation);

            // 마우스 방향으로 회전
            stone.transform.rotation = mouseDir;

            yield return delay;
        }
    }

    void DespawnCallback()
    {
        // 파괴 사운드 재생
        SoundManager.Instance.PlaySound("SlingShot_Destroy", transform.position);
    }
}
