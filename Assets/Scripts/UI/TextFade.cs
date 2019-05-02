using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextFade : MonoBehaviour {
    private float time;
    public float lifeTime = 2;
    public float duration = 1;

    private TextMesh mesh;

    void Start() {
        mesh = GetComponent<TextMesh>();
    }

    void FixedUpdate() {
        time += Time.deltaTime;
        if(time > lifeTime) {
            mesh.color =  Color.Lerp(mesh.color, new Color(mesh.color.r, mesh.color.g, mesh.color.b, -1), Time.deltaTime * 3 * (1 / duration));
            if(time > lifeTime + duration + 1) Destroy(gameObject);
        }
    }
}
