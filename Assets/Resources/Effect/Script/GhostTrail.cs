using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class GhostTrail : MonoBehaviour
{
    public SpriteRenderer originSprite;
    public SpriteRenderer copySprite;
    public Color ghostColor;
    Vector3 lastPos; // 마지막 위치
    public float ghostDistance;
    public float fadeTime;
    float delayCount;
    public float delayTime;

    public bool isCopyRotation;
    public bool isCopySprite;

    private void OnEnable()
    {
        // 고스트 컬러 초기화
        copySprite.color = ghostColor;

        // 한단계 낮은 레이어 넣기
        copySprite.sortingOrder = originSprite.sortingOrder - 1;
    }

    private void Update()
    {
        if (originSprite == null)
            return;

        // 원본의 현재 스프라이트 넣어주기
        if (isCopySprite)
            copySprite.sprite = originSprite.sprite;

        // 원본 오브젝트 회전값 복사
        Quaternion rotation = isCopyRotation ? originSprite.transform.rotation : Quaternion.identity;

        // 쿨타임 되면 고스트 생성
        if (delayCount <= 0)
        {
            // 일정 거리 이상 이동했다면
            if (Vector3.Distance(lastPos, transform.position) > ghostDistance)
            {
                // 복사본 스프라이트를 가진 고스트를 복사해서 생성
                GameObject ghost = LeanPool.Spawn(copySprite.gameObject, transform.position, rotation, SystemManager.Instance.effectPool);

                GhostManager ghostManager = ghost.GetComponentInChildren<GhostManager>(true);

                // 증발 시간 넣어주기
                ghostManager.fadeTime = fadeTime;

                // 고스트 증발 시작
                ghostManager.enabled = true;
            }

            // 쿨타임 갱신
            delayCount = delayTime;
            // 마지막 위치 갱신
            lastPos = transform.position;
        }
        else
            // 쿨타임 차감
            delayCount -= Time.deltaTime;
    }
}
