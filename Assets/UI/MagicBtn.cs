using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicBtn : MonoBehaviour
{
    public GameObject popupMenu;
    public enum BtnType { itemBtn, magicBtn };
    public BtnType btnType;
    public int ID;

    public void ChooseMagic()
    {
        //팝업 메뉴 닫기
        ClosePopup();

        if (btnType == BtnType.itemBtn)
        {
            // 플레이어 보유 아이템에 해당 magicID 추가하기
            PlayerManager.Instance.hasItems.Add(ID);
            // 아이템 DB에서 가진 아이템 true로 변경
            ItemDB.Instance.GetItemByID(ID).hasItem = true;

            //TODO 플레이어 스탯 반영하기
        }
        else if (btnType == BtnType.magicBtn)
        {
            MagicInfo magic = MagicDB.Instance.GetMagicByID(ID); //마법 찾기

            // 플레이어 보유 마법에 해당 magicID 추가하기
            PlayerManager.Instance.hasMagics.Add(ID);
            // 마법DB에서 가진 마법 true로 변경
            magic.hasMagic = true;

            //TODO 합성 재료 마법 2가지 없에기
            PlayerManager.Instance.hasMagics.Remove(MagicDB.Instance.GetMagicByName(magic.element_A).id);
            PlayerManager.Instance.hasMagics.Remove(MagicDB.Instance.GetMagicByName(magic.element_B).id);
        }
    }

    public void ClosePopup()
    {
        Time.timeScale = 1; //시간 복구

        // 팝업 메뉴 닫기
        popupMenu.SetActive(false);
    }
}
