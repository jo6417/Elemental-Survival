using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VarManager : MonoBehaviour
{
    #region Singleton
    private static VarManager instance;
    public static VarManager Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<VarManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<VarManager>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public float playerTimeScale = 1f; //플레이어만 사용하는 타임스케일
    public float timeScale = 1f; //전역으로 사용하는 타임스케일

    public void AllTimeScale(float scale)
    {
        playerTimeScale = scale;
        timeScale = scale;
    }
}
