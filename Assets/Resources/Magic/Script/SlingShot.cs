using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlingShot : MonoBehaviour
{
    [SerializeField] MagicProjectile magicProjectile;

    private void Awake()
    {
        if (magicProjectile.despawnCallback == null)
            // 디스폰 콜백 추가
            magicProjectile.despawnCallback += DespawnCallback;
    }

    void DespawnCallback()
    {
        // 파괴 사운드 재생
        SoundManager.Instance.SoundPlay("SlingShot_Destroy", transform);
    }

    private void OnEnable()
    {
        // 던지기 사운드 재생
        SoundManager.Instance.SoundPlay("SlingShot_Throw", transform);
    }
}
