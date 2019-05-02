using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bolt : MonoBehaviour {
    public float force;
    public float boltDMG;

    public GameObject ignore, source;

    private Transform[] children;

    void OnTriggerEnter(Collider col) {
        if(col.gameObject != ignore) {
            if(col.tag == "Enemy" || (col.tag == "Player" && gameObject.tag == "EnemyBolt")) {
                if(col.gameObject == null) return;
                IDestroyable dest = col.gameObject.GetComponentInChildren<IDestroyable>();
                if(dest == null) return;

                try {
                    dest.Damage(boltDMG * (Enemy.IsEnemy(dest)? 0.5f : 1), source.transform);
                    dest.Recoil(transform.forward, force);
                } catch(System.Exception){}
            }
            foreach(Transform child in transform) if(child.GetComponent<Light>() != null) Destroy(child.gameObject);
            transform.DetachChildren();
            Destroy(gameObject);
        }
    }
}