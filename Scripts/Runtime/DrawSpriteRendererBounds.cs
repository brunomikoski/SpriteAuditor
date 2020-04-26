using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawSpriteRendererBounds : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;


    private void Reset()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(spriteRenderer.bounds.center, spriteRenderer.bounds.size);
    }
}
