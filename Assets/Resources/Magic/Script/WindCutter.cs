using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindCutter : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    public SpriteRenderer sprite;
    public Animator anim;
    public MagicInfo magic;
    float duration;

    private void Awake()
    {
        anim = anim == null ? GetComponent<Animator>() : anim;
        sprite = sprite == null ? GetComponent<SpriteRenderer>() : sprite;
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 레이어에 따라 색깔 바꾸기
        if (magicHolder.gameObject.layer == SystemManager.Instance.layerList.EnemyAttack_Layer)
            // 몬스터 공격이면 빨간색
            sprite.color = new Color(1, 20f / 255f, 20f / 255f, 1);

        if (magicHolder.gameObject.layer == SystemManager.Instance.layerList.PlayerAttack_Layer)
            // 플레이어 공격이면 흰색
            sprite.color = new Color(150f / 255f, 1, 1, 1);

        // 애니메이션 재생
        anim.speed = 3f;
    }
}
