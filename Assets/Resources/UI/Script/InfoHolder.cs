using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoHolder : MonoBehaviour
{
    public GameObject popupMenu;
    public enum HolderType { itemHolder, magicHolder };
    public HolderType holderType;
    public int gemType = -1; //해당 상품의 화폐 종류
    public int id;

    public void ChooseBtn(bool PopupQuit = false)
    {
        //아이템 버튼일때
        if (holderType == HolderType.itemHolder)
        {
            ItemInfo item = ItemDB.Instance.GetItemByID(id);

            // 아이템 획득
            // PlayerManager.Instance.GetItem(item);
        }
        //마법 구매 버튼일때
        else if (holderType == HolderType.magicHolder)
        {
            // 마법 찾아서 인스턴스화
            MagicInfo magic = new MagicInfo(MagicDB.Instance.GetMagicByID(id));

            // 마법 획득 및 언락
            PlayerManager.Instance.GetMagic(magic);
        }

        if (PopupQuit)
            UIManager.Instance.PopupUI(popupMenu);
    }
}
