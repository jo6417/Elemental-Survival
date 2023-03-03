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
                instance = FindObjectOfType<ObjectPool>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = "ObjectPool";
                    instance = obj.AddComponent<ObjectPool>();
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("Pool")]
    public Transform enemyPool;
    public Transform itemPool;
    public Transform overlayPool;
    public Transform magicPool;
    public Transform enemyAtkPool;
    public Transform effectPool;
    public Transform objectPool;
    // public Transform soundPool;

    private void Awake()
    {
        // 다른 오브젝트가 이미 있을 때
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        // DontDestroyOnLoad(gameObject);
    }
}
