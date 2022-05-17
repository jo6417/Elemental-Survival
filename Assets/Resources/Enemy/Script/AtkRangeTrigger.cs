using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtkRangeTrigger : MonoBehaviour
{
    public bool atkTrigger;
    public Collider2D collTrigger;

    private void OnTriggerEnter2D(Collider2D other) {
        //플레이어가 범위 내에 들어왔을때
        if(other.CompareTag("Player"))
        {
            atkTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        //플레이어가 범위 밖으로 나갔을때
        if(other.CompareTag("Player"))
        {
            atkTrigger = false;
        }
    }
}
