using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapScroll : MonoBehaviour
{
    [SerializeField] private RawImage repeatImage;
    [SerializeField] private Vector2 scrollDirection = new Vector2(0.01f, 0.01f);
    [SerializeField] bool RandomDirection = false; // 랜덤 방향
    [SerializeField] float AddSpeed = 1f; // 추가 스크롤 속도

    private void Awake()
    {
        if (repeatImage == null) repeatImage = GetComponent<RawImage>();
    }

    private void OnEnable()
    {
        // 랜덤 방향 설정
        if (RandomDirection)
            scrollDirection = Random.insideUnitCircle.normalized * 0.01f;
    }

    private void FixedUpdate()
    {
        repeatImage.uvRect = new Rect(
            repeatImage.uvRect.position + scrollDirection * AddSpeed * Time.fixedDeltaTime,
             repeatImage.uvRect.size);
    }
}
