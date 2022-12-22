using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetLight : MonoBehaviour
{
    [SerializeField] Animator anim;

    [SerializeField] int fixPattern = -1; // 가로등 반짝이는 패턴 종류

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 애니메이터 끄기
        anim.enabled = false;

        // 1초 이내 랜덤 딜레이 대기
        yield return new WaitForSeconds(Random.value);

        // 패턴 고정
        int pattern = fixPattern;

        // 고정 패턴 없으면 랜덤
        if (fixPattern == -1)
            pattern = Random.Range(0, 3);

        anim.SetInteger("Pattern", pattern);

        // 애니메이터 켜기
        anim.enabled = true;
    }
}
