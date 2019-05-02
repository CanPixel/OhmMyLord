using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IHealth {
    float Health {
        get;set;
    }
    void Damage(float dmg, Transform src);
}

public class IDestroyable : MonoBehaviour, IHealth {
    public float Health {
       get {return health;} 
       set {health = value;}
    }
    private float health;
    public UnityEvent OnDeath;
    public float maxHealth;

    public static GameObject DMGText_Template;

    protected SpriteRenderer[] sprites;
    private float damageTick = 0;

    protected Rigidbody rb;

    protected virtual void Start() {
        health = maxHealth;
        sprites = transform.GetComponentsInChildren<SpriteRenderer>();
        OnDeath.AddListener(Die);
    }

    protected virtual void Die() {
        AStarUnit a = GetComponent<AStarUnit>();
        if(a != null) a.Clean();
        Destroy(gameObject);
    }

    protected void DamageFeedback() {
        if(sprites == null) return;
        if(damageTick > 0) damageTick -= Time.deltaTime;

        float effect = 1f - Mathf.Sin(damageTick * 10);
        if(sprites[0] == null) return;
        foreach(SpriteRenderer spr in sprites) {spr.color = new Color(effect, effect, effect);}
    }

    protected void Notify(float amount, bool rand = false) {
        GameObject go = Instantiate(DMGText_Template, transform.position + new Vector3(1, 2, 0) + (rand ? new Vector3(Random.Range(-1, 3), Random.Range(-0.5f, 2), 0) : Vector3.zero), Quaternion.identity);
        TextMesh text = go.GetComponent<TextMesh>();
        if(amount > 0) {
            text.color = new Color(0.35f, 1f, 0.35f);
            text.text = "+";
        }
        else {
            text.color = new Color(1, 0.25f, 0.25f);
            text.text = amount.ToString();
        }
    }

    public void Recoil(Vector3 forward, float force) {
        Vector3 knockback = forward;
        knockback.y = 0;
        rb.AddForce(knockback.normalized * force * 100);
    }

     public virtual void Heal(float heal) {
        if(damageTick > 0) return;
        Notify(heal, true);
        health += heal;
        damageTick = heal / 20f;
    }

    public virtual void Damage(float dmg, Transform src) {
        if(damageTick > 0) return;
        Notify(-dmg);
        health -= dmg;
        damageTick = dmg / 20f;
        if(health <= 0) OnDeath.Invoke();
    }
}
