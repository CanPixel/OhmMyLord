using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gate : MonoBehaviour {
    private float teleportTime = 0;
    private float teleportDelay = 3f;

    private ShaderEffect_CorruptedVram shader;

    void Start() {
        shader = Camera.main.GetComponent<ShaderEffect_CorruptedVram>();
    }

    void OnTriggerEnter(Collider col) {
        if(col.tag == "Player") {
            teleportTime = 0.1f;
            shader.enabled = true;
        }
    }

    void OnTriggerExit(Collider col) {
        if(col.tag == "Player") {
            teleportTime = 0;
            shader.enabled = false;
        }
    }

    void FixedUpdate() {
        if(teleportTime > 0) {
            teleportTime += Time.deltaTime;
            if(teleportTime > teleportDelay / 2) shader.shift = Mathf.Lerp(shader.shift, teleportTime * 40, Time.deltaTime * 5);
            else shader.shift = Mathf.Sin(teleportTime * 10) * 4;
        }
        if(teleportTime > teleportDelay) Teleport();
    }

    protected void Teleport() {
        LevelManager.GenerateNewWorld();
    }
}
