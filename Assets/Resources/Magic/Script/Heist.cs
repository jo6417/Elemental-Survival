using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class Heist : MonoBehaviour
{
    [Header("Refer")]
    private MagicInfo magic;
    public MagicHolder magicHolder;
    public GameObject electroTrail;
    public GameObject ghostPrefab; // 잔상 효과 프리팹
    public List<GameObject> ghostList = new List<GameObject>(); // 소환된 잔상 리스트

    int magicLevel = 0;
    float speed = 0;
    public Color ghostStartColor;
    Color ghostEndColor;
    public float ghostFrequency = 0.001f; //고스트 생성 주기
    public float ghostDuration = 0.1f; //고스트 유지 시간
    float ghostCount = 0;
    private Vector2 lastEffectPos; //마지막 이펙트 남긴 위치
    public float effectDistance = 1f; // 이펙트 간의 거리

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    // 마법 레벨업 할때 새로 초기화 하기
    IEnumerator Init()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 처음 마법 레벨 저장
        magicLevel = magic.magicLevel;

        // 레벨 갱신되면 스피드 스탯 새로 계산
        speed = MagicDB.Instance.MagicSpeed(magic, true, magicHolder.targetType);

        // 플레이어 이동속도 버프하기
        PlayerManager.Instance.PlayerStat_Now.moveSpeed = PlayerManager.Instance.PlayerStat_Now.moveSpeed * speed;

        //플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform);
        transform.localPosition = Vector3.zero;

        //마지막 발자국 위치 초기화
        lastEffectPos = PlayerManager.Instance.transform.position;
    }

    private void OnDisable()
    {
        // 기존 스피드 버프 계수 빼기
        PlayerManager.Instance.PlayerStat_Now.moveSpeed = PlayerManager.Instance.PlayerStat_Now.moveSpeed / speed;

        // 소환된 잔상 모두 삭제
        foreach (GameObject ghost in ghostList)
        {
            // 고스트 살아있으면
            if (ghost && ghost != null && ghost.activeSelf)
                LeanPool.Despawn(ghost);
        }
    }

    private void Update()
    {
        //잔상 남기기
        GhostTrail();

        //대쉬 할때 전기 이펙트 남기기
        if (PlayerManager.Instance.isDash && Vector2.Distance(lastEffectPos, PlayerManager.Instance.transform.position) > effectDistance)
            ShockTrail();
    }

    void GhostTrail()
    {
        // 전역 시간 멈췄으면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        if (ghostCount <= 0)
        {
            //쿨타임 갱신
            ghostCount = ghostFrequency * PlayerManager.Instance.PlayerStat_Now.moveSpeed;

            StartCoroutine(GhostTransition());
        }
        else
        {
            ghostCount -= Time.deltaTime;
        }
    }

    IEnumerator GhostTransition()
    {
        //잔상 오브젝트 소환
        GameObject ghostObj = LeanPool.Spawn(ghostPrefab, PlayerManager.Instance.transform.position, PlayerManager.Instance.transform.rotation, SystemManager.Instance.effectPool);

        //잔상 리스트에 오브젝트 저장
        ghostList.Add(ghostObj);

        //스프라이트 렌더러 찾기
        SpriteRenderer ghostSprite = ghostObj.GetComponent<SpriteRenderer>();

        //플레이어 현재 스프라이트 넣기
        ghostSprite.sprite = PlayerManager.Instance.sprite.sprite;

        // 플레이어 레이어 넣기
        ghostSprite.sortingLayerID = PlayerManager.Instance.sprite.sortingLayerID;
        // 플레이어보다 한단계 낮게
        ghostSprite.sortingOrder = PlayerManager.Instance.sprite.sortingOrder - 1;

        //고스트 색 초기화
        ghostSprite.color = new Color(1, 1, 1, 150f / 255f);

        yield return new WaitForSeconds(ghostDuration / 3f);

        //고스트 색깔로 변경, 알파값 유지
        ghostSprite.DOColor(ghostStartColor, ghostDuration / 3f);

        yield return new WaitForSeconds(ghostDuration / 3f);

        //알파값 최저로 낮춰 없에기
        ghostSprite.DOColor(ghostEndColor, ghostDuration / 3f);

        yield return new WaitForSeconds(ghostDuration / 3f);

        //잔상 리스트에서 오브젝트 삭제
        ghostList.Remove(ghostObj);

        LeanPool.Despawn(ghostObj);
    }

    void ShockTrail()
    {
        //플레이어 위치
        Vector2 playerPos = PlayerManager.Instance.transform.position;

        //마법 오브젝트 생성
        GameObject magicObj = LeanPool.Spawn(electroTrail, playerPos, Quaternion.identity, SystemManager.Instance.effectPool);

        // 오브젝트 사이즈에 범위 반영
        magicObj.transform.localScale = Vector3.one * MagicDB.Instance.MagicRange(magic);

        MagicHolder _magicHolder = magicObj.GetComponent<MagicHolder>();

        //마법 정보 넣기
        _magicHolder.magic = magic;

        // 감전 시간 반영하기
        _magicHolder.shockTime = MagicDB.Instance.MagicDuration(magic);

        //마법 타겟 지정
        _magicHolder.SetTarget(MagicHolder.Target.Enemy);

        //마지막 이펙트 위치 갱신
        lastEffectPos = magicObj.transform.position;
    }
}
