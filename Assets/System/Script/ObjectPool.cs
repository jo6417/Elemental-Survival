using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    #region Singleton
    private static ObjectPool instance;
    public static ObjectPool Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<ObjectPool>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<ObjectPool>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion
}
