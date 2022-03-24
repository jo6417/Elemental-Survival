using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicTrigger : MonoBehaviour
{
    public Animator anim;
    public float cooltimeCounter = 0;

    private void Update() {
        //쿨타임 카운트 다운
        if(cooltimeCounter > 0)
        cooltimeCounter -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other) {

        // 적 충돌시
        if(other.CompareTag("Enemy") && cooltimeCounter <= 0)
        {
            // Attack 애니메이션 켜기
            anim.SetBool("Attack", true);
        }
    }
}
