using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MagicCooltime : MonoBehaviour
{
    public MagicInfo magic;
    [SerializeField, ReadOnly] Image cooltimeImg;
    [SerializeField, ReadOnly] float coolCount;

    private void Awake()
    {
        cooltimeImg = transform.Find("Cooltime").GetComponent<Image>();
    }

    private void Update()
    {
        if (magic == null)
            return;

        // print(magic.magicName + " : " + magic.coolCount);

        //쿨타임 표시하기
        UpdateCool();
    }

    void UpdateCool()
    {
        // 쿨타임 시간 계산해서 불러오기
        float coolMax = MagicDB.Instance.MagicCoolTime(magic);

        // 마법의 잔여 쿨타임
        coolCount = magic.coolCount;

        // 현재 남은 쿨타임 이미지에 반영해서 채우기
        cooltimeImg.fillAmount = coolCount / coolMax;
    }
}
