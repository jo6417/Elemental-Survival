using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoHolder : MonoBehaviour
{
    public GameObject popupMenu;
    public enum HolderType { itemHolder, magicHolder };
    public HolderType holderType;
    public int id;

    public void ChooseBtn()
    {
        //아이템 버튼일때
        if (holderType == HolderType.itemHolder)
        {
            ItemInfo item = ItemDB.Instance.GetItemByID(id);

            // 아이템 획득
            PlayerManager.Instance.GainItem(item);
        }
        //마법 구매 버튼일때
        else if (holderType == HolderType.magicHolder)
        {
            MagicInfo magic = MagicDB.Instance.GetMagicByID(id); //마법 찾기

            // 마법 획득 및 언락
            PlayerManager.Instance.GetMagic(magic);
        }
    }
}
