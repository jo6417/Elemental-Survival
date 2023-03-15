using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Lean.Pool;
using System.Linq;

public class LaserBeam : MonoBehaviour
{

    [Header("Refer")]
    public MagicHolder magicHolder;
    public MagicHolder subMagicHolder;
    public Transform startObj;
    public Collider2D effectColl; //폭발 콜라이더
    public LineRenderer laserLine; //레이저 이펙트
    public GameObject explosion; //레이저 타격 지점에 폭발 이펙트
    public GameObject scorchEffect; // 그을음 이펙트
    EdgeCollider2D coll;
    Vector2[] collPoints = new Vector2[2];
    [SerializeField] Transform laserParticle;

    [Header("State")]
    public float laserExpandSpeed = 0.2f;
    [SerializeField] Color aimColor;
    [SerializeField] Color laserColor;
    float aimTime = 1f; // 조준 소요 시간
    Vector3 shotPos; // 착탄 지점

    private void Awake()
    {
        laserLine = GetComponent<LineRenderer>();
        coll = GetComponent<EdgeCollider2D>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 레이저 콜라이더 비활성화
        coll.enabled = false;
        // 레이저 콜라이더 위치 초기화
        coll.offset = -transform.position;
        // 레이저 콜라이더 굵기 초기화
        coll.edgeRadius = 0f;

        //레이저 라인 렌더러 끄기
        laserLine.enabled = false;
        // 폭발 이펙트 끄기
        explosion.SetActive(false);

        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);

        // 폭발 이펙트도 마법 정보 및 타겟 넣기        
        subMagicHolder.magic = magicHolder.magic;
        subMagicHolder.targetType = magicHolder.targetType;

        // 목표위치 들어올때까지 대기
        yield return new WaitUntil(() => magicHolder.targetPos != default(Vector3));
        // 발사 주체 입력 될때까지 대기
        yield return new WaitUntil(() => magicHolder.GetTarget() != MagicHolder.TargetType.None);

        // 플레이어가 쓸때
        if (magicHolder.GetTarget() == MagicHolder.TargetType.Enemy)
        {
            // 플레이어가 발사 주체면 스마트폰에서 시작
            startObj = CastMagic.Instance.phone;

            //조준시간 초기화
            aimTime = 1f;
            //조준선 색깔 초기화
            aimColor = Color.red;
            //레이저 색깔 초기화
            laserColor = CustomMethod.HexToRGBA("2DFFFF");
        }
        // 몬스터가 쓸때
        else if (magicHolder.GetTarget() == MagicHolder.TargetType.Player)
        {
            //적이 쏠때는 더 빠르게 조준
            aimTime = 0.5f;
            //조준선 색깔 변경
            aimColor = Color.red;
            //레이저 색깔 변경
            laserColor = CustomMethod.HexToRGBA("FF1919");
        }

        //레이저 발사
        StartCoroutine(LaserSeqence());
    }

    IEnumerator LaserSeqence()
    {
        // 라인 첫번째 포인트 = 스마트폰 위치
        laserLine.SetPosition(0, startObj.position);

        // 라인 두번째 포인트 시작지점으로 초기화
        laserLine.SetPosition(1, laserLine.GetPosition(0));

        //폭발 이펙트 목표지점으로 이동
        explosion.transform.position = magicHolder.targetPos;

        // 이펙트 콜라이더 크기에 range 반영
        explosion.transform.localScale = Vector2.one * 0.2f * MagicDB.Instance.MagicRange(magicHolder.magic);

        // 레이저 조준선 굵기로 초기화
        laserLine.startWidth = 0.1f;
        laserLine.endWidth = 0.1f;

        // 라인 렌더러 켜기
        laserLine.enabled = true;

        //조준하기
        StartCoroutine(FollowAim());

        //레이저 조준선 굵기 줄어들어 0에 수렴
        DOTween.To(() => laserLine.startWidth, x => laserLine.startWidth = x, 0f, aimTime);
        DOTween.To(() => laserLine.endWidth, x => laserLine.endWidth = x, 0f, aimTime);
        yield return new WaitUntil(() => laserLine.endWidth == 0f);

        //레이저 발사
        StartCoroutine(ShotLaser());

        // 레이저 콜라이더 굵기 키우기
        DOTween.To(() => coll.edgeRadius, x => coll.edgeRadius = x, 0.5f, laserExpandSpeed);

        //레이저 굵기로 빠르게 키우기
        DOTween.To(() => laserLine.startWidth, x => laserLine.startWidth = x, 1f, laserExpandSpeed);
        DOTween.To(() => laserLine.endWidth, x => laserLine.endWidth = x, 1f, laserExpandSpeed);
        yield return new WaitUntil(() => laserLine.endWidth == 1f);

        //폭발 오브젝트 켜기
        StartCoroutine(Explode());

        // 레이저 콜라이더 굵기 줄이기
        DOTween.To(() => coll.edgeRadius, x => coll.edgeRadius = x, 0f, laserExpandSpeed);

        //레이저 굵기 줄어들어 0에 수렴
        DOTween.To(() => laserLine.startWidth, x => laserLine.startWidth = x, 0f, laserExpandSpeed);
        DOTween.To(() => laserLine.endWidth, x => laserLine.endWidth = x, 0f, laserExpandSpeed);
        yield return new WaitUntil(() => laserLine.endWidth <= 0.01f);

        // 레이저 콜라이더 비활성화
        coll.enabled = false;

        // 폭발 이펙트 대기 후 디스폰
        yield return new WaitForSeconds(2f);

        //디스폰
        Despawn();
    }

    IEnumerator FollowAim()
    {
        //조준선 색으로 바꾸기
        laserLine.startColor = aimColor;
        laserLine.endColor = aimColor;

        while (laserLine.endWidth > 0f)
        {
            shotPos = Vector2.Lerp(laserLine.GetPosition(1), magicHolder.targetPos, 0.5f);

            //레이저 시작점 스마트폰 따라다니기
            laserLine.SetPosition(0, startObj.position);

            //레이저 목표지점으로 뻗어나가기
            laserLine.SetPosition(1, shotPos);

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 레이저 뒤에 파티클 생성
        LaserParticle();
    }

    IEnumerator ShotLaser()
    {
        //레이저 색으로 바꾸기
        laserLine.startColor = laserColor;
        laserLine.endColor = laserColor;

        // 레이저 두번째 포인트 시작지점으로 초기화
        laserLine.SetPosition(1, laserLine.GetPosition(0));

        // 레이저 콜라이더 활성화
        coll.enabled = true;

        // 콜라이더 1번째 포인트 갱신
        collPoints[0] = startObj.position;

        while (laserLine.endWidth < 1f)
        {
            Vector2 pos = Vector2.Lerp(laserLine.GetPosition(1), magicHolder.targetPos, 0.3f);

            //레이저 목표지점으로 뻗어나가기
            laserLine.SetPosition(1, pos);

            // 콜라이더 2번째 포인트 갱신 및 콜라이더에 반영
            collPoints[1] = pos;
            coll.SetPoints(collPoints.ToList());

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    IEnumerator Explode()
    {
        // // 폭발 이펙트 켜기
        // explosion.SetActive(true);

        // // 폭발 콜라이더 켜기
        // effectColl.enabled = true;

        // 그을음 이펙트 남기기
        LeanPool.Spawn(scorchEffect, explosion.transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

        yield return new WaitForSeconds(0.1f);

        // 폭발 콜라이더 끄기
        effectColl.enabled = false;
    }

    void LaserParticle()
    {
        // 레이저 벡터 계산
        Vector3 dir = shotPos - startObj.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        // 레이저 방향 계산
        Quaternion particleRotation = Quaternion.Euler(Vector3.forward * (angle + 90f));

        // 목표 위치에 레이저 파티클 생성
        Transform particle = LeanPool.Spawn(laserParticle, shotPos, particleRotation, ObjectPool.Instance.effectPool);

        // 사이즈 수정
        particle.localScale = Vector2.up * dir.magnitude;
    }

    void Despawn()
    {
        // 폭발 이펙트 끄기
        explosion.SetActive(false);

        // 발사 주체 변수 비우기
        startObj = null;

        //마법 데이터 비우기
        magicHolder.magic = null;
        subMagicHolder.magic = null;

        //레이저 오브젝트 디스폰
        LeanPool.Despawn(transform);
    }
}
