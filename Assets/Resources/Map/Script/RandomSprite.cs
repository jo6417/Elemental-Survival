using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSprite : MonoBehaviour
{
    [SerializeField] List<Sprite> spritePool;
    [SerializeField] SpriteRenderer sprite;

    private void OnEnable()
    {
        // 스프라이트 풀에서 랜덤 스프라이트 찾아 넣기
        if (spritePool.Count > 0)
            sprite.sprite = spritePool[Random.Range(0, spritePool.Count)];
    }
}
