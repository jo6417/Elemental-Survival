using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SpriteCopy : MonoBehaviour
{
    public SpriteRenderer originSprite; // 복제할 스프라이트 원본
    [SerializeField] SpriteRenderer copySprite; // 복제된 스프라이트

    private void OnEnable()
    {
        if (copySprite == null)
            copySprite = GetComponent<SpriteRenderer>();

        // 색깔 투명하게 초기화
        copySprite.color = new Color(1, 1, 1, 0);
    }


    private void FixedUpdate()
    {
        // CopySprite();
    }

    private void Update()
    {
        CopySprite();
    }

    void CopySprite()
    {
        // 복사할 스프라이트 없으면 리턴
        // if (originSprite == null)
        //     return;

        // 프레임마다 스프라이트 복제
        copySprite.sprite = originSprite.sprite;

        // 월드 회전값 복사
        copySprite.transform.rotation = originSprite.transform.rotation;
        // 로컬 위치 복사
        copySprite.transform.localPosition = originSprite.transform.localPosition;
    }

    public void ChangeColor(Color color, float changeTime)
    {
        // 기존 트윈 끄기
        copySprite.DOKill();

        // 입력된 컬러 적용
        copySprite.color = color;

        // 원래 색에서 알파값 낮추기
        Color originColor = copySprite.color;
        originColor.a = 0;

        // 알파값 낮추기
        copySprite.DOColor(originColor, changeTime);
    }
}
