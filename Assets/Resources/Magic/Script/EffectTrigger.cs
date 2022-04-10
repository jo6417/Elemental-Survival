using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class EffectTrigger : MonoBehaviour
{
    void EffectDespawn()
    {
        LeanPool.Despawn(transform);
    }
}
