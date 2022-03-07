using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupBtn : MonoBehaviour
{
    public GameObject popupMenu;
    public enum BtnType { itemBtn, scrollBtn, magicBtn };
    public BtnType btnType;
    public int id;

    public void ChooseBtn()
    {
        //아이템 버튼일때
        if (btnType == BtnType.itemBtn)
        {
            ItemInfo item = ItemDB.Instance.GetItemByID(id);

            // 아이템 획득
            PlayerManager.Instance.GainItem(item);
        }
        //마법 구매 버튼일때
        else if (btnType == BtnType.magicBtn)
        {
            MagicInfo magic = MagicDB.Instance.GetMagicByID(id); //마법 찾기

            // 마법 획득 및 언락
            PlayerManager.Instance.GetMagic(magic);
        }

        //팝업 메뉴 닫기
        ClosePopup();
    }

    public void ClosePopup()
    {
        Time.timeScale = 1; //시간 복구

        // 팝업 메뉴 닫기
        popupMenu.SetActive(false);
    }
}
