using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinObject : MonoBehaviour
{
    public float spinSpeed = 1;
    void Update()
    {
        transform.Rotate(Vector3.back * Time.unscaledDeltaTime * spinSpeed, Space.Self);
    }
}
