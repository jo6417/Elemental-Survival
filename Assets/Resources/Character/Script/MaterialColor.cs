using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialColor : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Material mat;
    public Color color;

    private void Start() {
        mat = sprite.material;
    }
    
    void Update()
    {
        mat.color = color;
    }
}
