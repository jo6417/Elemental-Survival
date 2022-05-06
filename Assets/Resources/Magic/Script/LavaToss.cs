using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class LavaToss : MonoBehaviour
{
    public Transform lavaPool; //적에게 타격입힐 용암 장판
    public MagicHolder magicHolder;
    MagicInfo magic;
    public int lavaNum = 10; //용암 방울 개수

    float range;
    float duration;

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        lavaPool.localScale = Vector2.zero;

        //magic 불러올때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;
        Vector2 targetPos = magicHolder.targetPos;

        range = MagicDB.Instance.MagicRange(magic) / 5f;
        duration = MagicDB.Instance.MagicDuration(magic);

        // magicHolder에서 targetPos 받아와서 해당 위치로 이동
        transform.parent.position = targetPos;

        // duration 만큼 시간 지나면 줄어들어 사라지기
        lavaPool.DOScale(Vector2.zero, duration)
        .SetDelay(2f)
        .OnComplete(() =>
        {
                //디스폰
            StartCoroutine(AutoDespawn());
        });
    }

    private void OnParticleCollision(GameObject other)
    {
        if (range == 0)
            return;

        // 용암 방울 파티클 닿을때마다 장판 커짐
        lavaPool.localScale += 1f / lavaNum * range * new Vector3(0.5f, 0.25f, 0f);
    }

    IEnumerator AutoDespawn(float delay = 0)
    {
        //delay 만큼 지속시간 부여
        float delayCount = delay;
        while (delayCount > 0)
        {
            delayCount -= Time.deltaTime * VarManager.Instance.playerTimeScale;
            yield return null;
        }

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform.parent);
    }
}
