using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class Heist : MonoBehaviour
{
    private MagicInfo magic;
    public MagicHolder magicHolder;
    float speed = 0;

    public Color ghostStartColor;
    Color ghostEndColor;
    public float ghostFrequency = 0.001f; //고스트 생성 주기
    public float ghostDuration = 0.1f; //고스트 유지 시간
    float ghostCount = 0;

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());
    }

    // 마법 레벨업 할때 새로 초기화 하기
    IEnumerator Initial()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        //원래 속도 변수가 있으면 버프 빼기
        if (speed != 0)
            PlayerManager.Instance.PlayerStat_Now.moveSpeed = PlayerManager.Instance.PlayerStat_Now.moveSpeed / speed;

        //버프할 스피드 불러오기
        speed = MagicDB.Instance.MagicSpeed(magic, true);
        //플레이어 이동속도 버프하기
        PlayerManager.Instance.PlayerStat_Now.moveSpeed = PlayerManager.Instance.PlayerStat_Now.moveSpeed * speed;

        //속도에 따라 사이즈 변화
        transform.localScale = Vector3.one * speed;

        //플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform);
        transform.localPosition = Vector3.zero;
    }

    private void Update()
    {
        //잔상 남기기
        GhostTrail();
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
        //고스트 오브젝트 소환
        GameObject ghostObj = LeanPool.Spawn(SystemManager.Instance.ghostPrefab, PlayerManager.Instance.transform.position, PlayerManager.Instance.transform.rotation, SystemManager.Instance.effectPool);

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

        LeanPool.Despawn(ghostObj);
    }
}
