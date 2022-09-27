using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class GhostTrail : MonoBehaviour
{
    [SerializeField] AfterImage ghostPrefab;
    public SpriteRenderer targetSprite;
    public SpriteRenderer copySprite;
    public Color ghostColor;
    Vector3 lastPos; // 마지막 위치
    public float ghostDistance;
    public float fadeTime;
    float delayCount;
    public float delayTime;
    [SerializeField] bool flipY = false;

    private void OnEnable()
    {
        // 고스트 컬러 초기화
        copySprite.color = ghostColor;

        // 한단계 낮은 레이어 넣기
        copySprite.sortingOrder = targetSprite.sortingOrder - 1;
    }

    private void Update()
    {
        // 쿨타임 되면 고스트 생성
        if (delayCount <= 0)
        {
            // 일정 거리 이상 이동했다면
            if (Vector3.Distance(lastPos, transform.position) > ghostDistance)
            {
                // 스프라이트 복사 시키기
                copySprite.sprite = targetSprite.sprite;

                Quaternion rotation = default;
                Vector3 originRotation = targetSprite.transform.rotation.eulerAngles;
                // Y축 반전이면 180도 돌려서 회전해주기
                if (flipY)
                    originRotation.y -= 180f;
                // 생성 각도 계산
                rotation = Quaternion.Euler(-originRotation);

                // 복사본 스프라이트를 가진 고스트를 복사해서 생성
                SpriteRenderer ghost = LeanPool.Spawn(copySprite, transform.position, rotation, SystemManager.Instance.effectPool);

                // ghost.targetSpriteRenderer = this.targetSprite;
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
