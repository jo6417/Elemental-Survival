using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Lean.Pool;

public class LaserBeam : MonoBehaviour
{
    public float laserExpandSpeed = 0.2f;
    public MagicHolder magicHolder;
    public MagicHolder subMagicHolder;
    MagicInfo magic;
    public Transform startObj;
    Vector2 targetPos;
    Color aimColor;
    Color laserColor;
    float aimTime = 1f; // 조준 소요 시간
    public Collider2D effectColl; //폭발 콜라이더
    public LineRenderer laserLine; //레이저 이펙트
    public GameObject explosion; //레이저 타격 지점에 폭발 이펙트
    EdgeCollider2D coll;
    List<Vector2> collPoints = new List<Vector2>();

    private void Awake()
    {
        laserLine = GetComponent<LineRenderer>();
        coll = GetComponent<EdgeCollider2D>();
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        //레이저 라인 렌더러 끄기
        laserLine.enabled = false;
        // 폭발 이펙트 끄기
        explosion.SetActive(false);

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 목표위치 초기화
        yield return new WaitUntil(() => magicHolder.targetPos != default(Vector3));
        targetPos = magicHolder.targetPos;

        // 발사 주체 입력 될때까지 대기
        yield return new WaitUntil(() => magicHolder.GetTarget() != MagicHolder.Target.None);

        // 시작 오브젝트 초기화
        if (magicHolder.GetTarget() == MagicHolder.Target.Enemy)
        {
            // 플레이어가 발사 주체면 스마트폰에서 시작
            startObj = CastMagic.Instance.transform;

            // 엎어지기 콜라이더도 타겟 변경
            subMagicHolder.SetTarget(MagicHolder.Target.Enemy);

            //조준시간 초기화
            aimTime = 1f;
            //조준선 색깔 초기화
            aimColor = Color.red;
            //레이저 색깔 초기화
            laserColor = SystemManager.Instance.HexToRGBA("FF7B3B");
        }
        else if (magicHolder.GetTarget() == MagicHolder.Target.Player)
        {
            // 엎어지기 콜라이더도 타겟 변경
            subMagicHolder.SetTarget(MagicHolder.Target.Player);

            //적이 쏠때는 더 빠르게 조준
            aimTime = 0.5f;
            //조준선 색깔 변경
            aimColor = Color.blue;
            //레이저 색깔 변경
            laserColor = Color.cyan;
        }

        //폭발 magicHolder에 magic 넣기
        subMagicHolder.magic = magic;

        //레이저 발사
        StartCoroutine(LaserSeqence());
    }

    IEnumerator LaserSeqence()
    {
        // 레이저 콜라이더 비활성화
        coll.enabled = false;
        // 레이저 콜라이더 위치 초기화
        coll.offset = -transform.position;
        // 레이저 콜라이더 굵기 초기화
        coll.edgeRadius = 0f;
        // 레이저 콜라이더 포인트 2개 생성
        if (collPoints.Count == 0)
        {
            collPoints.Add(startObj.position);
            collPoints.Add(startObj.position);
        }

        // 라인 첫번째 포인트 = 스마트폰 위치
        laserLine.SetPosition(0, startObj.position);

        // 라인 두번째 포인트 시작지점으로 초기화
        laserLine.SetPosition(1, laserLine.GetPosition(0));

        //폭발 이펙트 목표지점으로 이동
        explosion.transform.position = targetPos;

        // 이펙트 콜라이더 크기에 range 반영
        explosion.transform.localScale = Vector2.one * 0.2f * MagicDB.Instance.MagicRange(magic);

        // 레이저 조준선 굵기로 초기화
        laserLine.startWidth = 0.1f;
        laserLine.endWidth = 0.2f;

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
        DOTween.To(() => laserLine.startWidth, x => laserLine.startWidth = x, 0.6f, laserExpandSpeed);
        DOTween.To(() => laserLine.endWidth, x => laserLine.endWidth = x, 1f, laserExpandSpeed);
        yield return new WaitUntil(() => laserLine.endWidth == 1f);

        //폭발 오브젝트 켜기
        StartCoroutine(Explosion());

        // 레이저 콜라이더 굵기 줄이기
        DOTween.To(() => coll.edgeRadius, x => coll.edgeRadius = x, 0f, laserExpandSpeed);

        //레이저 굵기 줄어들어 0에 수렴
        DOTween.To(() => laserLine.startWidth, x => laserLine.startWidth = x, 0f, laserExpandSpeed);
        DOTween.To(() => laserLine.endWidth, x => laserLine.endWidth = x, 0f, laserExpandSpeed);
        yield return new WaitUntil(() => laserLine.endWidth == 0f);

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
            Vector2 pos = Vector2.Lerp(laserLine.GetPosition(1), targetPos, 0.5f);

            //레이저 시작점 스마트폰 따라다니기
            laserLine.SetPosition(0, startObj.position);

            //레이저 목표지점으로 뻗어나가기
            laserLine.SetPosition(1, pos);

            yield return new WaitForSeconds(Time.deltaTime);
        }
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
            Vector2 pos = Vector2.Lerp(laserLine.GetPosition(1), targetPos, 0.3f);

            //레이저 목표지점으로 뻗어나가기
            laserLine.SetPosition(1, pos);

            // 콜라이더 2번째 포인트 갱신 및 콜라이더에 반영
            collPoints[1] = pos;
            coll.SetPoints(collPoints);

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    IEnumerator Explosion()
    {
        // 폭발 이펙트 켜기
        explosion.SetActive(true);

        // 폭발 콜라이더 켜기
        effectColl.enabled = true;

        yield return new WaitForSeconds(0.1f);

        // 폭발 콜라이더 끄기
        effectColl.enabled = false;
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
        magic = null;

        //레이저 오브젝트 디스폰
        LeanPool.Despawn(transform);
    }
}
