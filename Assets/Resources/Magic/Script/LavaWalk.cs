using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class LavaWalk : MonoBehaviour
{
    MagicHolder magicHolder;

    public GameObject footprint; //발자국 오브젝트
    private Vector2 lastFootPos; //마지막 발자국 위치
    // private float cooltimeCounter;
    private bool isRightFoot; //좌, 우 어느 발인지 여부

    [SerializeField] SpriteRenderer footSprite;
    float distance;

    private void Awake()
    {
        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    // 마법 레벨업 할때 새로 초기화 하기
    IEnumerator Init()
    {
        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);

        //플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform);
        transform.localPosition = Vector3.zero;

        //스프라이트 사이즈 얻기위해 렌더러 참조
        footSprite = footprint.GetComponent<SpriteRenderer>();

        //프리팹 스케일 미리 설정해놓기
        footprint.GetComponent<Transform>().localScale = Vector2.one * magicHolder.range;

        //마지막 발자국 위치 초기화
        lastFootPos = PlayerManager.Instance.transform.position;
    }

    private void Update()
    {
        if (!magicHolder.initDone) return;

        //일정 거리마다 발자국 생성
        if (Vector2.Distance(lastFootPos, PlayerManager.Instance.transform.position) > distance)
            MakeFootprint();
    }

    void MakeFootprint()
    {
        //발자국 사이 거리 갱신
        distance = footSprite.bounds.size.x;

        //발자국 좌우 바꾸기
        isRightFoot = !isRightFoot;

        //플레이어 위치
        Vector2 playerPos = PlayerManager.Instance.transform.position;
        //플레이어 방향, 방향 없을때는 vector2.right 넣기
        Vector3 playerDir = PlayerManager.Instance.lastDir;

        //발자국이 바라볼 각도 계산
        float rotation = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;

        //발자국 좌우 여부 계산
        playerDir.x = isRightFoot ? 0 : 180;

        //플레이어 방향으로 돌리기
        playerDir.z = isRightFoot ? rotation : -rotation;

        //마법 오브젝트 생성
        GameObject magicObj = LeanPool.Spawn(footprint, playerPos, Quaternion.Euler(playerDir), ObjectPool.Instance.magicPool);

        MagicHolder _magicHolder = magicObj.GetComponent<MagicHolder>();

        //마법 정보 넣기
        _magicHolder.magic = magicHolder.magic;

        //마법 타겟 지정
        _magicHolder.SetTarget(magicHolder.targetType);

        //마지막 발자국 위치 갱신
        lastFootPos = magicObj.transform.position;
    }
}
