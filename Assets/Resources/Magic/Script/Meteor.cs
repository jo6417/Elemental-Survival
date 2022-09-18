using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Meteor : MonoBehaviour
{
    public float angleOffset; //스프라이트 방향 보정
    public Vector2 startOffset; //시작할 위치

    [Header("Refer")]
    public SpriteRenderer rockSprite; //운석 스프라이트
    public ParticleSystem fireTrail;
    // public ParticleSystem mainParticle;
    public MagicInfo magic;
    public MagicHolder magicHolder;
    public GameObject explosionPrefab; // 폭발 파티클
    public GameObject dirtPrefab; // 흙 튀는 파티클
    public GameObject scorchPrefab; // 그을음 프리팹
    public GameObject indicator; // 운석 떨어질 장소 표시
    [SerializeField] CircleCollider2D coll;

    [Header("Magic Stat")]
    float speed;
    float range;

    private void Awake()
    {
        //스프라이트 끄기
        rockSprite.enabled = false;

        //파티클 끄기
        fireTrail.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        //초기화 하기
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 콜라이더 끄기
        magicHolder.coll.enabled = false;

        //마법 떨어뜨리기
        StartCoroutine(FallMagic());
    }

    IEnumerator FallMagic()
    {
        // 마법 오브젝트 속도, 숫자가 작을수록 빠름
        speed = MagicDB.Instance.MagicSpeed(magic, false);
        // 해당 마법 범위 불러오기
        range = MagicDB.Instance.MagicRange(magic);

        //시작 위치
        Vector2 startPos = startOffset + (Vector2)magicHolder.targetPos;

        //떨어질 자리에 인디케이터 표시
        GameObject shadow = LeanPool.Spawn(indicator, magicHolder.targetPos, Quaternion.identity, SystemManager.Instance.effectPool);

        // 인디케이터 색깔 초기화
        SpriteRenderer shadowSprite = shadow.GetComponent<SpriteRenderer>();
        if (magicHolder.targetType == MagicHolder.Target.Player)
            // 플레이어가 타겟이면 빨간색
            shadowSprite.color = new Color(1, 0, 0, 100f / 255f);

        if (magicHolder.targetType == MagicHolder.Target.Enemy)
            // 몬스터가 타겟이면 흰색
            shadowSprite.color = new Color(1, 1, 1, 100f / 255f);

        //인디케이터 사이즈 0,0으로 초기화
        shadow.transform.localScale = Vector3.zero;

        // 끝나는 위치, 타겟 위치 + 반지름만큼 위에
        Vector2 endPos = (Vector2)magicHolder.targetPos + new Vector2(0, coll.radius);

        //시작 위치로 올려보내기
        transform.position = startPos;

        //스프라이트 끄기
        rockSprite.enabled = true;

        //파티클 켜기
        fireTrail.gameObject.SetActive(true);

        //목표 위치 방향으로 회전
        Vector2 dir = endPos - startPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle + angleOffset, Vector3.forward);

        //위치 올라갈때까지 대기
        yield return new WaitUntil(() => (Vector2)transform.position == startPos);

        //인디케이터 사이즈 점점 키우기
        shadow.transform.DOScale(new Vector2(5, 2), speed);

        //목표 위치로 떨어뜨리기
        Tween fallTween = transform.DOMove(endPos, speed)
        .SetEase(Ease.Linear);

        // 땅에 떨어지기 직전(0.1초전)까지 대기
        yield return new WaitForSeconds(speed - 0.1f);

        // 콜라이더 켜기
        magicHolder.coll.enabled = true;
        // 콜라이더 충돌 시간 대기
        yield return new WaitForSeconds(0.1f);
        // 콜라이더 끄기
        magicHolder.coll.enabled = false;

        //인디케이터 디스폰
        LeanPool.Despawn(shadow);

        // 폭발 이펙트 오브젝트 생성
        GameObject explosionEffect = LeanPool.Spawn(explosionPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        if (explosionEffect.TryGetComponent(out MagicHolder explosionHolder))
        {
            // 폭발 이펙트에 마법 정보 입력
            explosionHolder.magic = magic;

            // 폭발 이펙트에 타겟 정보 입력
            explosionHolder.SetTarget(magicHolder.GetTarget());
        }

        // 흙 튀는 파티클 생성
        LeanPool.Spawn(dirtPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        //TODO 일정 레벨 이상이면 용암 장판 남기기?

        // 그을음 남기기
        LeanPool.Spawn(scorchPrefab, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        //스프라이트 끄기
        rockSprite.enabled = false;

        // 카메라 흔들기
        Camera.main.transform.DOShakePosition(0.2f, 0.3f, 50, 90f, false, false);

        // 화염 파티클 중지
        fireTrail.Stop();
        // 남은 화염 파티클 전부 사라질때까지 대기
        yield return new WaitForSeconds(fireTrail.main.duration);

        //파티클 끄기
        fireTrail.gameObject.SetActive(false);

        //디스폰
        LeanPool.Despawn(transform);
    }
}
