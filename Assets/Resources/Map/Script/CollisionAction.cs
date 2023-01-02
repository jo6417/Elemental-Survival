using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CollisionAction : MonoBehaviour
{
    [SerializeField] bool collisionBounce = true; // 충돌시 바운스 여부
    [SerializeField] List<string> collisionSound; // 충돌시 사운드 재생

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌 했을때
        if (other.gameObject.CompareTag(SystemManager.TagNameList.Player.ToString()))
        {
            // 충돌시 바운스 모션
            if (collisionBounce)
            {
                transform.localScale = Vector3.one;
                transform.DOKill();
                transform.DOPunchScale(new Vector3(-0.1f, 0.1f, 0), 0.5f)
                .SetEase(Ease.OutQuint);
            }

            // 충돌 사운드 재생
            if (collisionSound.Count > 0)
            {
                SoundManager.Instance.PlaySound(collisionSound[Random.Range(0, collisionSound.Count)], transform.position);
            }
        }
    }
}
