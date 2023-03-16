using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using Lean.Pool;

public class AstralForm : MonoBehaviour
{
    public MagicHolder magicHolder;
    public Canvas fogCanvas;
    public GameObject ghostPrefab; // 잔상 효과 프리팹

    [Header("Ghost")]
    float ghostCount = 0;
    public float ghostFrequency = 0.005f; //잔상 생성 주기
    public float ghostDuration = 0.15f; //잔상 유지 시간
    bool ghostSwitch = true;
    public Color ghostStartColor;
    Color ghostEndColor;
    List<GameObject> ghosts = new List<GameObject>(); // 고스트 오브젝트 리스트

    [Header("Stat")]
    // float duration;
    float speed;

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
        fogCanvas = GetComponentInChildren<Canvas>();

        // ghostEndColor 초기화
        ghostEndColor = ghostStartColor;
        ghostEndColor.a = 0f;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);

        speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, false); // 스피드만큼 시간 느려지고 플레이어 빨라짐

        // 플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform, false);
        // 위치 초기화
        transform.localPosition = Vector2.zero;

        //잔상 생성 시작
        ghostSwitch = true;

        // 안개 사이즈 줄이기
        transform.localScale = Vector2.zero;
        // 안개 사이즈 키우기
        transform.DOScale(Vector2.one * 30f, 1f);

        // 영체로 변신 시작
        ToggleAstralForm(true);

        // 플레이어 머터리얼 바꾸기
        PlayerManager.Instance.playerSprite.material = SystemManager.Instance.ghostHDRMat;

        // 머터리얼 색깔 초기화
        PlayerManager.Instance.playerSprite.material.color = Color.clear;
        // 머터리얼 색깔 변경
        PlayerManager.Instance.playerSprite.material.DOColor(new Color(1, 0, 1, 0) * 5f, 1f);

        // 지속시간 - 1초 만큼 대기
        yield return new WaitForSeconds(magicHolder.duration - 1f);

        // 머터리얼 색깔 초기화
        PlayerManager.Instance.playerSprite.material.DOColor(Color.clear, 1f);

        // 안개 사이즈 줄이기
        transform.DOScale(Vector2.zero, 1f);

        // 1초 대기
        yield return new WaitForSeconds(1f);

        // 잔상 생성 멈추기
        ghostSwitch = false;

        // 모든 고스트 디스폰
        for (int i = 0; i < ghosts.Count; i++)
            // 고스트 활성화 되어있으면
            if (ghosts[i] != null && ghosts[i].activeSelf)
                // 고스트 디스폰
                LeanPool.Despawn(ghosts[i]);

        // 플레이어 머터리얼 초기화
        PlayerManager.Instance.playerSprite.material = SystemManager.Instance.spriteUnLitMat;

        // 영체로 변신 해제
        ToggleAstralForm(false);

        // 오브젝트 디스폰
        LeanPool.Despawn(transform);
    }

    void ToggleAstralForm(bool isStop)
    {
        // 플레이어 물리 충돌 토글
        PlayerManager.Instance.physicsColl.enabled = !isStop;

        // 씬 타임 스케일 토글
        float timeScale = isStop ? 1f / speed : 1f;
        SystemManager.Instance.TimeScaleChange(timeScale);

        Buff buff = null;
        if (isStop)
            // 플레이어 속도 상승 버프
            buff = PlayerManager.Instance.SetBuff("AstralForm_Fast", nameof(PlayerManager.Instance.characterStat.moveSpeed), true, speed * speed, magicHolder.duration, false);
        else
        {
            if (buff != null)
                // 플레이어 속도 버프 제거
                StartCoroutine(PlayerManager.Instance.StopBuff(buff, 0));
        }

        //플레이어 이동속도 갱신
        PlayerManager.Instance.Move();
    }

    private void Update()
    {
        //잔상 남기기
        GhostTrail();
    }

    void GhostTrail()
    {
        if (ghostCount <= 0 && ghostSwitch)
        {
            //쿨타임 갱신
            ghostCount = ghostFrequency * PlayerManager.Instance.characterStat.moveSpeed;

            StartCoroutine(MakeGhost());
        }
        else
        {
            ghostCount -= Time.deltaTime;
        }
    }

    IEnumerator MakeGhost()
    {
        //고스트 오브젝트 소환
        GameObject ghostObj = LeanPool.Spawn(ghostPrefab, PlayerManager.Instance.transform.position, PlayerManager.Instance.playerSprite.transform.rotation, ObjectPool.Instance.effectPool);

        //스프라이트 렌더러 찾기
        SpriteRenderer ghostSprite = ghostObj.GetComponent<SpriteRenderer>();

        // 고스트 리스트에 담기
        ghosts.Add(ghostObj);

        //플레이어 현재 스프라이트 넣기
        ghostSprite.sprite = PlayerManager.Instance.playerSprite.sprite;

        // 플레이어 레이어 넣기
        ghostSprite.sortingLayerID = PlayerManager.Instance.playerSprite.sortingLayerID;
        // 플레이어보다 한단계 낮게
        ghostSprite.sortingOrder = PlayerManager.Instance.playerSprite.sortingOrder - 1;

        //고스트 색 초기화
        ghostSprite.color = new Color(1, 1, 1, 150f / 255f);

        yield return new WaitForSeconds(ghostDuration);

        //고스트 색깔로 변경, 알파값 유지
        ghostSprite.DOColor(ghostStartColor, ghostDuration);

        yield return new WaitForSeconds(ghostDuration);

        //알파값 최저로 낮춰 없에기
        ghostSprite.DOColor(ghostEndColor, ghostDuration);

        yield return new WaitForSeconds(ghostDuration);

        // 리스트에서 고스트 지우기
        ghosts.Remove(ghostObj);

        // 고스트 활성화 되어있으면
        if (ghostObj.activeSelf)
            // 고스트 디스폰
            LeanPool.Despawn(ghostObj);
    }
}
