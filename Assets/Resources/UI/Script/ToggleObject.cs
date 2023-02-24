using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleObject : MonoBehaviour
{
    [SerializeField] GameObject targetObj;

    public void ToggleAction(bool isOn)
    {
        // 타겟 오브젝트를 활성화/비활성화        
        if (targetObj != null)
            targetObj.SetActive(!targetObj.activeSelf);
    }
}
