using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupBtn : MonoBehaviour
{
    public GameObject popupMenu;
    public enum BtnType { itemBtn, scrollBtn, magicBtn };
    public BtnType btnType;
    public int id;

    public void ChooseMagic()
    {
        //아이템 버튼일때
        if (btnType == BtnType.itemBtn)
        {
            ItemInfo item = ItemDB.Instance.GetItemByID(id);

            // 아이템 획득
            PlayerManager.Instance.GainItem(item);
        }
        //스크롤 합성 버튼일때
        else if (btnType == BtnType.scrollBtn)
        {
            MagicInfo magic = MagicDB.Instance.GetMagicByID(id); //마법 찾기

            // 마법 획득
            PlayerManager.Instance.GetMagic(magic);

            //TODO 합성 재료 마법 2가지 없에기
            PlayerManager.Instance.hasMagics.Remove(MagicDB.Instance.GetMagicByName(magic.element_A));
            PlayerManager.Instance.hasMagics.Remove(MagicDB.Instance.GetMagicByName(magic.element_B));
        }
        //마법 구매 버튼일때
        else if (btnType == BtnType.magicBtn)
        {
            MagicInfo magic = MagicDB.Instance.GetMagicByID(id); //마법 찾기

            // 마법 획득
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
