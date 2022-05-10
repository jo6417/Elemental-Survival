using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MagicCooltime : MonoBehaviour
{
    ToolTipTrigger toolTipTrigger;
    MagicInfo magic;
    Image cooltimeImg;

    private void Awake() {
        toolTipTrigger = GetComponent<ToolTipTrigger>();
        cooltimeImg = transform.Find("Cooltime").GetComponent<Image>();
    }

    private void OnEnable() {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => toolTipTrigger.magic != null);
        magic = toolTipTrigger.magic;
    }

    private void Update() {
        if(magic == null)
        return;

        // print(magic.magicName + " : " + magic.coolCount);

        //쿨타임 표시하기
        UpdateCool();
    }

    void UpdateCool()
    {
        //쿨타임 계산해서 불러오기
        float coolMax = MagicDB.Instance.MagicCoolTime(magic);

        //현재 남은 쿨타임 이미지에 반영해서 채우기
        cooltimeImg.fillAmount = magic.coolCount / coolMax;
    }
}
