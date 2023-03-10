using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class LavaToss : MonoBehaviour
{
    public Transform lavaPool; //적에게 타격입힐 용암 장판
    public MagicHolder magicHolder;
    public int lavaNum = 10; //용암 방울 개수

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        lavaPool.localScale = Vector2.zero;

        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder.initDone);

        // magicHolder에서 targetPos 받아와서 해당 위치로 이동
        transform.parent.position = magicHolder.targetPos;

        // duration 만큼 시간 지나면 줄어들어 사라지기
        lavaPool.DOScale(Vector2.zero, magicHolder.duration)
        .SetDelay(2f)
        .OnComplete(() =>
        {
            //디스폰
            StartCoroutine(AutoDespawn());
        });
    }

    private void OnParticleCollision(GameObject other)
    {
        if (!magicHolder.initDone) return;

        // 용암 방울 파티클 닿을때마다 장판 커짐
        lavaPool.localScale += 1f / lavaNum * magicHolder.range * new Vector3(0.5f, 0.25f, 0f);
    }

    IEnumerator AutoDespawn(float delay = 0)
    {
        //delay 만큼 지속시간 부여
        float delayCount = delay;
        while (delayCount > 0)
        {
            delayCount -= Time.deltaTime;
            yield return null;
        }

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform.parent);
    }
}
