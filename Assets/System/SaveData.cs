using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData : System.IDisposable
{
    public int[] unlockMagics; //해금된 마법 리스트

    public SaveData()
    {
        //해금 마법 목록
        unlockMagics = MagicDB.Instance.unlockMagics.ToArray();
        //TODO 해금 캐릭터 목록
        //TODO 소지금(원소젬)
    }

    public void Dispose()
    {
        throw new System.NotImplementedException();
    }
}
