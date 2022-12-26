using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    #region Singleton
    private static InputManager instance = null;
    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<InputManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<InputManager>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    private void Awake()
    {
        // 최초 생성 됬을때
        if (instance == null)
        {
            // 이 오브젝트로 갱신
            instance = this;

            // 파괴되지 않게 설정
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            print("InputManager Destroy");

            // 해당 오브젝트가 아니라면
            if (instance != this)
                // 해당 오브젝트 파괴
                Destroy(gameObject);
        }
    }
}
