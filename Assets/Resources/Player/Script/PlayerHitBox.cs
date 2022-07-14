using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitBox : MonoBehaviour
{
    private void OnCollisionStay2D(Collision2D other)
    {
        if (PlayerManager.Instance.hitCoolCount > 0 || PlayerManager.Instance.isDash)
            return;

        //무언가 충돌되면 움직이는 방향 수정
        PlayerManager.Instance.Move();

        //적에게 콜라이더 충돌
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Magic"))
        {
            StartCoroutine(PlayerManager.Instance.Hit(other.transform));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // print("OnTriggerEnter2D : " + other.name);

        if (PlayerManager.Instance.hitCoolCount > 0 || PlayerManager.Instance.isDash)
            return;

        // 적에게 트리거 충돌
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Magic"))
        {
            StartCoroutine(PlayerManager.Instance.Hit(other.transform));
        }
    }

    // private void OnTriggerStay2D(Collider2D other)
    // {
    //     // print("OnTriggerStay2D : " + other.name);

    //     // 적에게 트리거 충돌
    //     if (other.gameObject.CompareTag("Enemy") && hitCoolCount <= 0 && !isDash)
    //     {
    //         Hit(other.transform);
    //     }
    // }

    // private void OnTriggerExit2D(Collider2D other)
    // {
    //     print("OnTriggerExit2D : " + other.name);

    // }
}
