using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Outlet : MonoBehaviour {
    private float chargeTime = 0;
    
    void FixedUpdate() {
        if(chargeTime > 0) chargeTime -= Time.deltaTime;
    }

    void OnTriggerStay(Collider col) {
        if(col.tag == "Enemy" || col.tag == "Player") {
            if(chargeTime > 0) return;
            IDestroyable dest = col.GetComponent<IDestroyable>();
            if(dest == null) dest = col.GetComponentInChildren<IDestroyable>();
            if(dest == null || (col.tag == "Enemy" && ((Enemy)dest).HasSafeHealthAmount())) return;
            Heal(dest);
        }
    }

    private void Heal(IDestroyable entity) {
        float amount = 1f;
        if(entity.tag == "Player") amount *= 5f;
        entity.Heal(amount);
        chargeTime = 0.5f;
    }
}
