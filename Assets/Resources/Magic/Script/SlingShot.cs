using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlingShot : MonoBehaviour
{
    [SerializeField] MagicHolder magicHolder;

    private void Awake()
    {
        if (magicHolder.despawnAction == null)
            // 디스폰 콜백 추가
            magicHolder.despawnAction += DespawnCallback;
    }

    void DespawnCallback()
    {
        // 파괴 사운드 재생
        SoundManager.Instance.PlaySound("SlingShot_Destroy", transform.position);
    }

    private void OnEnable()
    {
        // 던지기 사운드 재생
        SoundManager.Instance.PlaySound("SlingShot_Throw", transform.position);
    }
}
