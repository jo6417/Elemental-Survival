using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicBtn : MonoBehaviour
{
    public GameObject popupMenu;
    public int magicID;

    public void chooseMagic(){
        Time.timeScale = 1; //시간 복구

        // 팝업 메뉴 닫기
        popupMenu.SetActive(false);

        // 플레이어 보유 마법에 해당 magicID 추가하기
        PlayerManager.Instance.hasMagics.Add(magicID);

        // 마법DB에서 가진 마법 true로 변경
        MagicDB.Instance.GetMagicByID(magicID).hasMagic = true;
    }
}
