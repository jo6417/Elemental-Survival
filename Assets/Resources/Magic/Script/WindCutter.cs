using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindCutter : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;

    float duration;

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);

        // 스탯 초기화
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);

        // 출혈 시간에 적용
        magicHolder.bleedTime = duration;

        // // 레이어에 따라 색깔 바꾸기
        // if (magicHolder.gameObject.layer == SystemManager.Instance.layerList.EnemyAttack_Layer)
        //     // 몬스터 공격이면 빨간색
        //     sprite.color = new Color(1, 20f / 255f, 20f / 255f, 1);

        // if (magicHolder.gameObject.layer == SystemManager.Instance.layerList.PlayerAttack_Layer)
        //     // 플레이어 공격이면 흰색
        //     sprite.color = new Color(150f / 255f, 1, 1, 1);

        // 발사할때 사운드 재생
        SoundManager.Instance.PlaySound("WindCutter_Shot", transform.position);

        // 적 충돌시 사운드 - 콜백으로 처리
        if (magicHolder.hitAction == null)
            magicHolder.hitAction += SliceSound;
    }

    void SliceSound()
    {
        print("slice!");
        SoundManager.Instance.PlaySound("WindCutter_Slice", transform.position);
    }
}
