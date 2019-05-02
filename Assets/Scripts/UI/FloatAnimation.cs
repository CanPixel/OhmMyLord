using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatAnimation : MonoBehaviour {
    private float baseY;

    public float speed = 2, amplitude = 1;

    public bool blink = false;

    private SpriteRenderer spr;

    void Awake() {
        baseY = transform.position.y;
        spr = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate() {
        transform.position = new Vector3(transform.position.x, baseY + Mathf.Sin(Time.time * speed) * amplitude / 10, transform.position.z);
    
        if(blink && spr != null) spr.color = new Color(1, 1, 1, Mathf.Clamp01(Mathf.Sin(Time.time * speed)));
    }
}
