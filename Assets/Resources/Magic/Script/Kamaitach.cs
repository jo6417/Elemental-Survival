using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Kamaitach : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] ParticleManager particleManager;
    [SerializeField] BoxCollider2D coll;
    [SerializeField] AfterImage ghostPrefab; // 잔상 효과 프리팹
    [SerializeField] Sprite dashSprite; // 잔상 스프라이트

    Vector3 nowPlayerPos;
    Vector3 movePos;
    float distance;

    [Header("Spec")]
    [SerializeField] float ghostTime = 1f; // 잔상 소멸 시간
    [SerializeField] float ghostDelay = 1f; // 잔상 간격
    float range;
    float duration;

    private void Awake()
    {
        // ParticleManager에 의해 자동 디스폰 되지 않게 비활성화
        ghostPrefab.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 콜라이더 끄기
        coll.enabled = false;

        //magic 불러올때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        range = MagicDB.Instance.MagicRange(magicHolder.magic);
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);

        // 공격 시작
        StartCoroutine(StartAttack());
    }

    IEnumerator StartAttack()
    {
        // 원래 플레이어 위치기록
        nowPlayerPos = PlayerManager.Instance.transform.position;

        // 플레이어 현재 위치부터 마우스 위치까지 방향 벡터
        movePos = (Vector3)PlayerManager.Instance.GetMousePos() - nowPlayerPos;
        // 이동할 위치 계산, range 보다 멀리 이동하려고 하면 길이 제한
        movePos = movePos.magnitude > range ? nowPlayerPos + movePos.normalized * range : nowPlayerPos + movePos;

        // 원래 플레이어 위치, 타겟 위치 중간 지점에 오브젝트 위치 이동 시키기
        transform.position = (nowPlayerPos + movePos) / 2f;

        // 타겟 위치 바라보게 회전
        Vector2 rotation = nowPlayerPos - movePos;
        float angle = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(Vector3.forward * angle);

        // 두 지점 거리만큼 공격 범위 늘리기
        distance = Vector2.Distance(nowPlayerPos, movePos);
        transform.localScale = new Vector3(distance, distance / 2f, 1);

        // 수동 시전 했을때
        if (magicHolder.isManualCast)
        {
            // 시간 멈추기
            SystemManager.Instance.TimeScaleChange(0.1f);

            // 플레이어 키입력 막기
            PlayerManager.Instance.player_Input.Disable();

            // 플레이어 애니메이터 끄기
            PlayerManager.Instance.anim.enabled = false;
            // 플레이어 스프라이트 바꾸기
            PlayerManager.Instance.sprite.sprite = dashSprite;

            // 시전여부 초기화
            magicHolder.isManualCast = false;

            // 타겟 위치로 플레이어 이동 시키기
            PlayerManager.Instance.transform.DOMove(movePos, 0.2f)
            .SetUpdate(true);

            // 잔상 회전값
            Vector3 ghostRotation = (movePos - nowPlayerPos).x > 0 ? Vector3.zero : Vector3.up * -180f;

            // 해당 방향으로 플레이어 회전 시키기
            PlayerManager.Instance.transform.rotation = Quaternion.Euler(ghostRotation);

            // 고스트 오브젝트 소환, 자동 디스폰
            StartCoroutine(MakeGhost());

            // 딜레이 초기화
            float timeCount = 0.2f;
            // 딜레이 시간동안 대기
            // yield return new WaitForSecondsRealtime(0.2f);
            while (timeCount > 0)
            {
                // 시간 멈추지 않았으면 카운트 차감
                if (Time.timeScale > 0)
                    timeCount -= Time.unscaledDeltaTime;
            }

            // 시간 정상화
            SystemManager.Instance.TimeScaleChange(1f);

            // 플레이어 애니메이터 켜기
            PlayerManager.Instance.anim.enabled = true;

            // 플레이어 키입력 풀기
            PlayerManager.Instance.player_Input.Enable();

            // 이동 끝나면 파티클 실행
            particleManager.particle.Play();
        }
        else
            // 이동하지 않으면 바로 파티클 실행
            particleManager.particle.Play();

        // 거리에 비례해서 파티클 개수 갱신
        ParticleSystem.EmissionModule emission = particleManager.particle.emission;
        emission.rateOverTime = distance * 5f;

        // 콜라이더 깜빡여서 공격
        yield return StartCoroutine(FlickerColl());

        // 파티클 끝나면 디스폰
        particleManager.SmoothDespawn();
    }

    IEnumerator FlickerColl()
    {
        // 깜빡일 시간 받기
        float flickCount = duration;
        while (flickCount > 0)
        {
            // 콜라이더 토글
            coll.enabled = !coll.enabled;

            // 잠깐 대기
            flickCount -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    IEnumerator MakeGhost()
    {
        // 거리에 비례해서 소환 개수 정하기
        int ghostNum = (int)(distance / ghostDelay);

        for (int i = 0; i < ghostNum; i++)
        {
            // 움직일 방향
            Vector3 moveDir = movePos - nowPlayerPos;
            // 잔상 소환 위치
            Vector3 ghostPos = nowPlayerPos + moveDir.normalized * i * ghostDelay;
            // 잔상 회전값
            Vector3 ghostRotation = moveDir.x > 0 ? Vector3.zero : Vector3.up * -180f;

            // 잔상 단일 소환
            AfterImage ghostObj = LeanPool.Spawn(ghostPrefab, ghostPos, Quaternion.Euler(ghostRotation), ObjectPool.Instance.effectPool);

            // 대쉬 스프라이트 넣기
            ghostObj.targetSprite = dashSprite;

            // 잔상 소멸시간 초기화
            ghostObj.ghostTime = ghostTime;
            // 사이즈 초기화
            ghostObj.transform.localScale = Vector2.one;

            yield return new WaitForSecondsRealtime(0.01f);
        }
    }
}
