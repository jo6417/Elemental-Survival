using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformControl : MonoBehaviour
{
    [SerializeField] Shuffle shuffle; // 뒤집기, 회전 여부
    enum Shuffle { MirrorX, MirrorY, Rotate, None };
    [SerializeField, Range(0f, 1f)] float flipRate = 0.5f; // 뒤집기 확률
    [SerializeField, Range(0f, 360f)] float rotateMin = 0f; // 회전 최소값
    [SerializeField, Range(0f, 360f)] float rotateMax = 360f; // 회전 최대값

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return null;

        switch (shuffle)
        {
            case Shuffle.None:
                // 기본 각도로 초기화
                transform.rotation = Quaternion.Euler(Vector3.zero);
                break;
            case Shuffle.MirrorX:
                // 확률에 따라 좌우반전
                transform.rotation = Random.value > flipRate ? Quaternion.Euler(Vector3.zero) : Quaternion.Euler(Vector3.up * 180f);
                // print(transform.rotation);
                break;
            case Shuffle.MirrorY:
                // 확률에 따라 상하반전
                transform.rotation = Random.value > flipRate ? Quaternion.Euler(Vector3.zero) : Quaternion.Euler(Vector3.right * 180f);
                break;
            case Shuffle.Rotate:
                // 랜덤 각도로 회전
                transform.rotation = Quaternion.Euler(Vector3.forward * Random.Range(rotateMin, rotateMax));
                break;
        }
    }
}
