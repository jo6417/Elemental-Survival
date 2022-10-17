using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interacter : MonoBehaviour
{
    public InteractTriggerCallback interactTriggerCallback; // 상호작용 트리거 콜백
    public delegate void InteractTriggerCallback(bool able);

    public InteractSubmitCallback interactSubmitCallback; // 상호작용 확인 콜백
    public delegate void InteractSubmitCallback(bool isPress = true);

}
