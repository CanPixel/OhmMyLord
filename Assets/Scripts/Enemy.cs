using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStar;
using BehaviorTree;
using UnityEditor;

[RequireComponent(typeof(AStarUnit))]
public class Enemy : IDestroyable {
    [HideInInspector]
    public GameObject player;
    public GameObject sparks;

    //Local healing point in Level
    private GameObject outlet;
    protected Collider npcCollider;
    protected float lifeTick = 0;

    public int powerValue;
    public float maxSynergyDistance = 7;
    [HideInInspector]
    public bool synergy = false;
    protected Enemy partner;
    protected SynergySystem.Recipe targetSynergy;
    protected float synergyTick; 
    public float synergyDuration = 8;

    [Header("Ranged Attack")]
    public float nearToPlayerRange = 2;
    public GameObject projectile;
    public float chargeDelay = 2;
    public BoltProperties boltProperties;

    [Header("Melee Attack")]
    public GameObject rotationPart;
    public float playerHurtDistance = 2, meleeDamage = 10, knockback = 10;

    [Header("Animation")]
    public float animSpeed = 7;
    protected float animRunSpeed;
    public Player.Animator legsAnimator;
    public Player.Animator armsAnimator, faceAnimator, notifierAnimator;
    
    [Header("Notifiers")]
    public Dictionary<string, Sprite> notifierDict = new Dictionary<string, Sprite>();

    [System.Serializable]
    public class BoltProperties {
        public float speed = 2;
        public float DMG = 10;
        public float force = 20;
    }

    [System.Serializable]
    public enum Behavior {
        NULL, LED, POT, EMG
    }
    [Space(10)]
    public Behavior behavior;
    private float chargeTime = 0, scaleFact = 1;

    protected BNode behaviorTree;
    [HideInInspector]
    public BNode lastNode;

    protected Transform preHealingTarget;
    protected AStarUnit pathfinding;
    private int enemyCount = 0;
    private Light LEDlight;

    protected List<Enemy> allies = new List<Enemy>();

    public bool RunAIOnStart = false;

    void Awake() {
        npcCollider = GetComponent<Collider>();
        if(RunAIOnStart) {
            player = GameObject.FindGameObjectWithTag("Player");
            StartAI(null);
        }
    }

    public void StartAI(GameObject outlet) {
        this.outlet = outlet;
        rb = GetComponent<Rigidbody>();
        pathfinding = GetComponent<AStarUnit>();
        if(behavior == Behavior.LED) LEDlight = transform.GetChild(0).GetComponentInChildren<Light>();
        behaviorTree = null;
        animRunSpeed = animSpeed;

        switch(behavior) {
            default:
            case Behavior.NULL:
                break;
            case Behavior.LED:
                behaviorTree = new BehaviorTree.Composite.BSequence(new BNode[]{
                new BAction(this, CheckHealth), 
                new BAction(this, CheckForSynergies),
                new BAction(this, MoveInRangeOfPlayer), 
                new BehaviorTree.Decorator.BTimer(new BehaviorTree.Composite.BSequence(new BNode[] {
                    new BAction(this, RangedCharge), new BAction(this, RangedAttack)
                }), 1)
                });
                pathfinding.SetTarget(player.transform);
                break;
            case Behavior.POT:
                behaviorTree = new BehaviorTree.Composite.BSequence(new BNode[]{
                new BAction(this, CheckHealth), 
                new BAction(this, CheckForSynergies), 
                new BAction(this, MoveInRangeOfPlayer), 
                new BehaviorTree.Decorator.BTimer(new BehaviorTree.Composite.BSequence(new BNode[] {
                    new BAction(this, MeleeCharge), new BAction(this, MeleeAttack)
                }), 1)
                });
                pathfinding.SetTarget(player.transform);
                break; 
            case Behavior.EMG:
                behaviorTree = new BehaviorTree.Composite.BSequence(new BNode[]{
                new BAction(this, CheckHealth), new BAction(this, CheckForSynergies), new BAction(this, MoveInRangeOfPlayer), 
                new BehaviorTree.Decorator.BTimer(new BehaviorTree.Composite.BSequence(new BNode[] {
                    new BAction(this, SwirlCharge), new BAction(this, SwirlAttack)
                }), 0.25f)
                });
                pathfinding.SetTarget(player.transform);
                break; 
        }
        legsAnimator.Initialize();
        if(armsAnimator != null) armsAnimator.Initialize();
        if(faceAnimator != null) faceAnimator.Initialize();
        if(notifierAnimator != null) {
            notifierAnimator.Initialize();
            notifierAnimator.transparantIfNull = true;
        }
    }

    protected BAction.NodeState CheckHealth() {
        if(!HasSafeHealthAmount()) {
            if(synergy) {
                partner.ResetSynergy();
                ResetSynergy();
            }
            synergyTick = 0;
            notifierAnimator.ToggleBlink(true);
            faceAnimator.SetAnimation("Worry");
            notifierAnimator.SetAnimation("Broken");
            if(preHealingTarget == null) preHealingTarget = pathfinding.GetTarget();
            if(pathfinding.GetTarget() != outlet.transform)  pathfinding.SetTarget(outlet.transform);
            float bump = Mathf.Sin(Time.time * 4 * (1f - (maxHealth / Health) * 2)) / 7 + 1.25f;
            sprites[0].transform.localScale = new Vector3(bump, bump, bump);
        }
        else {
            faceAnimator.SetAnimation(null);
            notifierAnimator.SetAnimation(null);
            if(outlet != null && pathfinding.GetTarget() == outlet.transform) {
                pathfinding.SetTarget(preHealingTarget);
                preHealingTarget = null;
            }
        }
        return BAction.NodeState.SUCCESS;
    }

    protected BAction.NodeState CheckForSynergies() {
        //Synergies only happen on-screen
        if(!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), npcCollider.bounds)) return BAction.NodeState.FAIL;
        else if(synergy) synergyTick += Time.deltaTime;

        if(synergy && (partner == null || !partner.synergy || Vector3.Distance(partner.transform.position, transform.position) > 5)) ResetSynergy();

        //Health == priority
        if(!HasSafeHealthAmount()) return BAction.NodeState.FAIL;
        if(synergy) {
            notifierAnimator.SetAnimation("Combine");
            notifierAnimator.ToggleBlink(false);
            if(synergyTick > synergyDuration) Synergize();
            try {
                float bump = (Mathf.Sin(chargeTime * synergyTick * 4 + gameObject.GetInstanceID() * 10) + 2f) / 4 + 0.75f;
                sprites[0].transform.localScale = new Vector3(bump, bump, bump);
            } catch(System.Exception) {}
            chargeTime += Time.deltaTime;
            return BAction.NodeState.SUCCESS;
        }

        //Viscinity check for NPCs
        allies.Clear();
        Collider[] enemies = Physics.OverlapSphere(transform.position, maxSynergyDistance);
        for(int i = 0; i < enemies.Length; i++) if(enemies[i].tag == "Enemy") allies.Add(enemies[i].GetComponent<Enemy>());
        enemyCount = allies.Count;

        targetSynergy = SynergySystem.GetAvailableRecipe();
        partner = SynergySystem.GetMatchingComponent(this, allies.ToArray(), targetSynergy);
        if(partner == null) return BAction.NodeState.SUCCESS;
       
        //Apply Synergy attempt
        if(!synergy && SynergySystem.SynergyPossible() && lifeTick > 5 && Random.Range(0, 100) < 20) {
            SynergySystem.AddSynergy(this, partner);
            pathfinding.SetTarget(partner.transform);
            partner.pathfinding.SetTarget(transform);
            
            //FIX
            pathfinding.SetStoppingDistance(25);
            partner.pathfinding.SetStoppingDistance(25);
            chargeTime = partner.chargeTime = 0;
        }
        return BAction.NodeState.SUCCESS;
    }

    protected void Synergize() {
        LevelManager.SpawnSynergy(targetSynergy, transform.position);
        LevelManager.RemoveEnemyFromList(this);
        LevelManager.RemoveEnemyFromList(partner);
        SynergySystem.RemoveSynergy(this, partner);

        for(int i = 0; i < 2; i++) Instantiate(sparks, transform.position + Vector3.up, Quaternion.identity);

        DestroyImmediate(partner.gameObject);
        DestroyImmediate(gameObject);
    }

    protected void ResetSynergy() {
        if(partner != null && pathfinding.GetTarget() == partner.transform) {
            sprites[0].transform.localScale = partner.sprites[0].transform.localScale = Vector3.one;
            pathfinding.SetTarget(player.transform);
            partner.pathfinding.SetTarget(player.transform);
        }
        SynergySystem.DelayNextSynergy();
        pathfinding.ResetStoppingDistance();
        partner.pathfinding.ResetStoppingDistance();
        synergyTick = 0;
        chargeTime = partner.chargeTime = 0;
        SynergySystem.RemoveSynergy(this, partner);
    }

    protected BAction.NodeState RangedCharge() {
        if(synergy) return BAction.NodeState.SUCCESS;
        Transform part = sprites[0].transform;
        part.localPosition = new Vector3(Mathf.Sin(Time.time * (20 + chargeTime)) / 6, part.localPosition.y, part.localPosition.z);
        
        float bump = Mathf.Lerp(scaleFact, 1, chargeTime * 3);
        part.localScale = new Vector3(bump, bump, bump);
        chargeTime += Time.deltaTime;
        if(chargeTime > chargeDelay) {
            chargeTime = 0;
            return BAction.NodeState.SUCCESS;
        }
        return BAction.NodeState.FAIL;
    }
    protected BAction.NodeState RangedAttack() {
        if(synergy) return BAction.NodeState.SUCCESS;
        sprites[0].transform.localPosition = new Vector3(0, sprites[0].transform.localPosition.y, sprites[0].transform.localPosition.z);
        chargeTime = 0;
        scaleFact = 1.4f;
        ShootBolt();
        return BAction.NodeState.SUCCESS;
    }

    protected BAction.NodeState SwirlCharge() {
        if(synergy) return BAction.NodeState.SUCCESS;
        Transform part = sprites[0].transform;
        part.localPosition = new Vector3(Mathf.Sin(Time.time * (10 + chargeTime)) / 6, part.localPosition.y, part.localPosition.z);
        
        float bump = Mathf.Lerp(scaleFact, 1, chargeTime * 3);
        part.localScale = new Vector3(bump, bump, bump);
        chargeTime += Time.deltaTime;
        if(chargeTime > chargeDelay) {
            chargeTime = 0;
            return BAction.NodeState.SUCCESS;
        }
        return BAction.NodeState.FAIL;
    }
    protected BAction.NodeState SwirlAttack() {
        if(synergy) return BAction.NodeState.SUCCESS;
        sprites[0].transform.localPosition = new Vector3(0, sprites[0].transform.localPosition.y, sprites[0].transform.localPosition.z);
        chargeTime = 0;
        scaleFact = 1.4f;
        float speed = Time.time * 5f;
        ShootBolt(transform.position + new Vector3(Mathf.Cos(speed), 0, Mathf.Sin(speed)));
        return BAction.NodeState.SUCCESS;
    }

    protected BAction.NodeState MeleeCharge() {
        if(synergy) return BAction.NodeState.SUCCESS;
         if(rotationPart == null) return BAction.NodeState.FAIL;
        sprites[0].enabled = false;
        Transform part = rotationPart.transform;
        part.localRotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(part.localEulerAngles.z, 90 * pathfinding.GetDirectionToTarget(), Time.deltaTime * 1.5f));
        float bump = Mathf.Lerp(scaleFact, 1, chargeTime * 3);
        part.localScale = new Vector3(bump, bump, bump);
        chargeTime += Time.deltaTime;
        if(chargeTime > chargeDelay) {
            chargeTime = 0;
            return BAction.NodeState.SUCCESS;
        }
        return BAction.NodeState.FAIL;
    }
    protected BAction.NodeState MeleeAttack() {
        if(synergy) return BAction.NodeState.SUCCESS;
        if(rotationPart == null) return BAction.NodeState.FAIL;
        rotationPart.transform.localRotation = Quaternion.Euler(0, 0, -90 * pathfinding.GetDirectionToTarget());
        MeleeHurt(pathfinding.GetTarget());
       return BAction.NodeState.SUCCESS;
    }

    protected BAction.NodeState MoveInRangeOfPlayer() {
        if(sprites[0] != null) sprites[0].enabled = true;
        if(rotationPart != null) rotationPart.transform.localRotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(rotationPart.transform.localEulerAngles.z, 0, Time.deltaTime * 5));
        UpdateDirection();
        if(!InRangeOfTarget(pathfinding.stoppingDistance / 1.5f)) legsAnimator.SetAnimation("Walk");
        else legsAnimator.SetAnimation(null);
        if(!InRangeOfTarget()) return BAction.NodeState.FAIL;  
        else return BAction.NodeState.SUCCESS;
    }

    public bool HasSafeHealthAmount() {
        return Health >= maxHealth / 2;
    }

    public void Anger(Transform tar) {
        if(synergy) return;
        pathfinding.SetTarget(tar);
    }
    public override void Damage(float dmg, Transform src) {
        base.Damage(dmg, src);
        synergyTick = Mathf.Clamp01(synergyTick - 0.1f);
        if(src != null) Anger(src);
    }

    protected void ShootBolt(Vector3 target) {
        if(this.projectile == null) return;
        GameObject bolt = Instantiate(this.projectile, transform.position + Vector3.up, Quaternion.identity);
        Vector3 hitPoint = (target - transform.position).normalized;
        Vector3 velocity = new Vector3(Mathf.RoundToInt(rb.velocity.normalized.x), Mathf.RoundToInt(rb.velocity.normalized.y), Mathf.RoundToInt(rb.velocity.normalized.z));
        Vector3 direction = hitPoint + new Vector3((Mathf.RoundToInt(hitPoint.x) == Mathf.RoundToInt(velocity.x)) ? velocity.x : 0, 0, ((int)hitPoint.z == (int)velocity.z) ? velocity.z : 0);
        Vector3 bulletForce = direction * 300 * boltProperties.speed;
        bolt.GetComponent<Rigidbody>().AddForce(bulletForce);
        bolt.transform.LookAt(player.transform.position);

        Bolt boltSrc = bolt.GetComponent<Bolt>();
        boltSrc.force = boltProperties.force;
        boltSrc.boltDMG = boltProperties.DMG;
        boltSrc.ignore = gameObject;
        boltSrc.source = gameObject;
    }
    protected void ShootBolt() {
        ShootBolt(pathfinding.GetTarget().position);
    }
   //
    protected void MeleeHurt(Transform target) {
        if(target.gameObject == transform.gameObject) return;
        Collider[] col = Physics.OverlapSphere(transform.position, playerHurtDistance);
        GameObject targ = target.gameObject;
        foreach(Collider colli in col) if(colli.gameObject == pathfinding.GetTarget().gameObject) {
            targ = colli.gameObject;
            break;
        }
        IDestroyable tar =  targ.GetComponent<IDestroyable>();
        if(tar == null) tar = targ.GetComponentInChildren<IDestroyable>();
        if(tar == null) return;
        tar.Damage(meleeDamage * (IsEnemy(tar)? 0.5f : 1), transform);
        tar.Recoil(transform.forward, knockback);
    }
    
    public static bool IsEnemy(IDestroyable tar) {
        return tar.GetType() == typeof(Enemy);
    }
    public bool InRangeOfTarget() {
        return InRangeOfTarget(nearToPlayerRange);
    }
    public bool InRangeOfTarget(float range) {
        if(pathfinding.GetTarget() == null) return false;
        return Vector3.Distance(transform.position, pathfinding.GetTarget().position) < range;
    }

    protected void UpdateDirection() {
        FlipDirection(pathfinding.GetDirectionToTarget() < 0);
    }
    protected void FlipDirection(bool i) {
        if(sprites[0] == null) return;
        foreach(SpriteRenderer sprite in sprites) sprite.flipX = i;
    }

    public Transform GetCurrentTarget() {
        return pathfinding.GetTarget();
    }

    public void CalculateLEDBrightness() {
        if(LEDlight == null) return;
        LEDlight.intensity = (Health / maxHealth) * 4;
    }

    protected override void Die() {
        if(synergy) SynergySystem.RemoveSynergy(this, partner);
        LevelManager.RemoveEnemyFromList(this);
        base.Die();
    }

    void FixedUpdate() {
        lifeTick += Time.deltaTime;
        if(behaviorTree != null) behaviorTree.Run();
        DamageFeedback();
        CalculateLEDBrightness();

        if(pathfinding != null) animRunSpeed = animSpeed * Mathf.Clamp01(pathfinding.GetSpeed() - 0.1f);
        legsAnimator.Animate(animRunSpeed);
        armsAnimator.Animate();
        faceAnimator.Animate();
        notifierAnimator.Animate();

        if(pathfinding != null && pathfinding.GetTarget() == null) pathfinding.SetTarget(player.transform);
    }

    /* 
    void OnDrawGizmos() {
        GUIStyle style = new GUIStyle();
        style.fontSize = 32;
        if(synergy) Handles.Label(transform.position + Vector3.up * 3, partner.name, style);
    }*/
}