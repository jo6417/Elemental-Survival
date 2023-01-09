using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformControl : MonoBehaviour
{
    [SerializeField] Shuffle shuffle; // 뒤집기, 회전 여부
    [SerializeField] bool autoShuffle = false; // 자동 초기화 여부
    public enum Shuffle { MirrorX, MirrorY, Rotate, None };
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

        // 자동일때
        if (autoShuffle)
            ShuffleTransform();
    }

    public Shuffle ShuffleTransform()
    {
        switch (shuffle)
        {
            case Shuffle.None:
                // 기본 각도로 초기화
                transform.rotation = Quaternion.Euler(Vector3.zero);
                return Shuffle.None;
            case Shuffle.MirrorX:
                // 확률에 따라 좌우반전
                if (Random.value > flipRate)
                {
                    transform.rotation = Quaternion.Euler(Vector3.zero);
                    return Shuffle.None;
                }
                else
                {
                    transform.rotation = Quaternion.Euler(Vector3.up * 180f);
                    return Shuffle.MirrorX;
                }
            case Shuffle.MirrorY:
                // 확률에 따라 상하반전
                if (Random.value > flipRate)
                {
                    transform.rotation = Quaternion.Euler(Vector3.zero);
                    return Shuffle.None;
                }
                else
                {
                    transform.rotation = Quaternion.Euler(Vector3.right * 180f);
                    return Shuffle.MirrorY;
                }
            case Shuffle.Rotate:
                // 랜덤 각도로 회전
                transform.rotation = Quaternion.Euler(Vector3.forward * Random.Range(rotateMin, rotateMax));
                return Shuffle.Rotate;

            default: return Shuffle.None;
        }
    }
}
