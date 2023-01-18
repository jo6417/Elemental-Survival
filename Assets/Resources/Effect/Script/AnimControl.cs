using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class AnimControl : MonoBehaviour
{
    public Animator anim;
    public float animSpeed = 1f;
    [SerializeField] bool stopDespawn = false;
    bool stopTrigger = false;

    private void OnEnable()
    {
        anim = GetComponent<Animator>();
        anim.speed = animSpeed;
    }

    public void Despawn()
    {
        // print(transform.name);
        LeanPool.Despawn(transform);
    }
}
