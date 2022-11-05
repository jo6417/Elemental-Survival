using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniDrone_AI : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 사운드 초기화 될때까지 대기
        yield return new WaitUntil(() => SoundManager.Instance.init);

        // 시작하면 사운드 재생
        SoundManager.Instance.PlaySound("MiniDrone_Fly", transform, 0, 0, -1, true);
    }
}
