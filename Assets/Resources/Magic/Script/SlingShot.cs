using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SlingShot : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] MagicProjectile magicProjectile;
    [SerializeField] ParticleManager gatherEffect; // 모래 모으기 이펙트
    [SerializeField] GameObject backDust; // 뒤쪽 먼지 이펙트
    [SerializeField] Transform stone; // 바위 스프라이트
    Tween spinTween;

    [Header("State")]
    float duration;
    float scale;
    bool charging = true; // 충전중 여부
    bool shotAble = false; // 사격 허용 여부

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
        // 뒤쪽 먼지 이펙트 끄기
        backDust.SetActive(false);

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        // 스탯 초기화
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);
        scale = MagicDB.Instance.MagicScale(magicHolder.magic);

        // 수동 시전시
        if (magicHolder.isManualCast)
            // 공격 허용 판단
            StartCoroutine(AllowAttack());
        // 자동 시전시
        else
            // 무조건 사격 가능
            shotAble = true;

        // 주변 랜덤 위치로 이동
        transform.position = transform.position + (Vector3)Random.insideUnitCircle * 3f;

        // 타겟 방향으로 회전
        transform.rotation = Quaternion.Euler(magicHolder.targetPos - transform.position);

        // 차징 시작
        charging = true;

        // 수동 시전일때
        if (magicHolder.isManualCast)
        {
            //todo 플레이어 속도 느려짐
            //todo 소환된 바위끼리 가운데 지점으로 이동

            // 이동 완료하면 합체
            //todo 처음 합쳐진 투사체를 차지 투사체로 등록
            //todo 추가로 합체하면 사이즈업
            //todo 데미지 합산 및 추가 적용
            //todo 합체 이펙트 재생
            //todo 합체 사운드 재생
        }

        // 모래 모으기 이펙트 시작
        gatherEffect.particle.Play();
        // 모으기 이펙트 정지 예약
        gatherEffect.SmoothStop(duration - 0.2f);

        // 바위 점점 커짐
        stone.localScale = Vector2.zero;
        // 스케일만큼 커지기
        stone.DOScale(Vector2.one * scale, duration);

        // 회전 방향 랜덤
        Vector3 rotation = Random.value > 0.5f ? Vector3.forward : Vector3.back;
        // 회전 속도 랜덤
        float spinTime = Random.Range(0.5f, 1f);

        // 수동 시전시 공격 불능일때
        // 자동 시전시 그냥 회전
        if ((magicHolder.isManualCast && !shotAble)
        || !magicHolder.isManualCast)
            // 바위 회전
            spinTween = stone.DOLocalRotate(rotation * 360f, spinTime, RotateMode.LocalAxisAdd)
             .SetEase(Ease.Linear)
             .SetLoops(-1)
             .OnUpdate(() =>
             {
                 // 수동 시전시 공격 가능해지면
                 if (magicHolder.isManualCast && shotAble)
                 {
                     // 회전 정지
                     spinTween.SetLoops(0);
                     spinTween.Pause();
                     spinTween.Kill();
                 }
             });

        // duration 만큼 모으기
        yield return new WaitForSeconds(duration);

        // 바라보는 반대쪽으로 먼지 이펙트 뿜기 (이동할 방향 표시)
        backDust.SetActive(true);

        // 공격 트리거 대기
        yield return new WaitUntil(() => shotAble);

        // 차징 끝
        charging = false;

        // 발사
        StartCoroutine(magicProjectile.ShotMagic());

        //todo 쿨타임 시작

        // 던지기 사운드 재생
        SoundManager.Instance.PlaySound("SlingShot_Throw", transform.position);
    }

    IEnumerator AllowAttack()
    {
        // 공격 비허용
        shotAble = false;

        //todo 마우스가 아닌 해당 스킬 키로 변경
        //todo 마우스 누르지 않을때 혹은 모두 합체할때까지 대기
        yield return new WaitUntil(() => !PlayerManager.Instance.player_Input.Player.Click.inProgress);

        // 공격 허용
        shotAble = true;
    }

    private void Update()
    {
        // 수동 시전일때, 차지 중일때
        if (magicHolder.isManualCast && charging)
        {
            // 타겟 위치 계속 변경
            magicHolder.targetPos = PlayerManager.Instance.GetMousePos();

            // 마우스 방향 계산
            Vector2 dir = magicHolder.targetPos - transform.position;
            float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion mouseDir = Quaternion.Euler(Vector3.forward * rotation);

            // 마우스 방향으로 회전
            transform.rotation = mouseDir;
        }
    }

    void DespawnCallback()
    {
        // 파괴 사운드 재생
        SoundManager.Instance.PlaySound("SlingShot_Destroy", transform.position);
    }
}
