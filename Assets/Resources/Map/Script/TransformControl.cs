using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformControl : MonoBehaviour
{
    [SerializeField] Shuffle shuffle; // 뒤집기, 회전 여부
    public enum Shuffle { None, Origin, FlipX, FlipY, Rotate };
    [SerializeField] bool autoStart = false; // 자동 초기화 여부

    [Header("Flip")]
    [SerializeField, Range(0f, 1f)] float flipRate = 0.5f; // 뒤집기 확률

    [Header("Rotate")]
    [SerializeField, Range(0f, 360f)] float rotateMin = 0f; // 회전 최소값
    [SerializeField, Range(0f, 360f)] float rotateMax = 360f; // 회전 최대값

    [Header("Move")]
    [SerializeField] private Vector2 moveDirection;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return null;

        // 자동일때
        if (autoStart)
            ShuffleTransform();
    }

    public Shuffle ShuffleTransform()
    {
        switch (shuffle)
        {
            case Shuffle.Origin:
                // 기본 각도로 초기화
                transform.rotation = Quaternion.Euler(Vector3.zero);
                break;

            case Shuffle.FlipX:
                // 확률에 따라 좌우반전
                if (Random.value > flipRate)
                    transform.rotation = Quaternion.Euler(Vector3.zero);
                else
                    transform.rotation = Quaternion.Euler(Vector3.up * 180f);

                break;

            case Shuffle.FlipY:
                // 확률에 따라 상하반전
                if (Random.value > flipRate)
                    transform.rotation = Quaternion.Euler(Vector3.zero);
                else
                    transform.rotation = Quaternion.Euler(Vector3.right * 180f);

                break;

            case Shuffle.Rotate:
                // 랜덤 각도로 회전
                transform.rotation = Quaternion.Euler(Vector3.forward * Random.Range(rotateMin, rotateMax));
                break;
        }

        return shuffle;
    }

    private void FixedUpdate()
    {
        // 시간마다 조금씩 이동
        if (moveDirection != Vector2.zero)
            transform.position += (Vector3)moveDirection * Time.fixedDeltaTime;
    }
}
