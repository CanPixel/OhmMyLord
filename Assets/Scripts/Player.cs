using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using UnityEngine.SceneManagement;

public class Player : IDestroyable {
    private Transform legs, arms;

    public GameObject DMGTextPrefab;

    [Header("Shooting")]
    public SmartCursor cursor;
    public GameObject bolt;
    public float shootDelay = 2, boltSpeed = 1.5f, boltDMG = 10, boltForce = 50;
    private float shootTime = 0;

    [Header("Animations")]
    public float animSpeed = 5;
    public float animLean = 5;

    private float movement;

    [System.Serializable]
    public class Animation {
        public string name = "Animation";
        public float speed = 1;
        public Sprite[] sprites;
        private float time = 0;
        private Sprite current;
        private int index = 0;

        private float baseSpeed;

        public void Move() {
            time += Time.deltaTime;

            if(time > 1 / speed) {
                time = 0;
                index++;
                if(index >= sprites.Length) index = 0;
                current = sprites[index];
            }
        }

        public void Move(float speed) {
            time += Time.deltaTime;

            if(time > 1 / speed) {
                time = 0;
                index++;
                if(index >= sprites.Length) index = 0;
                current = sprites[index];
            }
        }

        public void reset() {
            time = 0;
        }

        public Sprite GetCurrentSprite() {
            return current;
        }
    }

    [System.Serializable]
    public class Animator {
        public SpriteRenderer renderer;
        public Animation[] animations;
        public Dictionary<string, Animation> anims = new Dictionary<string, Animation>();
        private Animation current;
        private Sprite baseSprite;

        [HideInInspector]
        public bool transparantIfNull = false;

        public void Initialize() {
            if(renderer == null) return;
            baseSprite = renderer.sprite;
            foreach(Animation anim in animations) anims.Add(anim.name, anim);
        }

        public void ToggleBlink(bool blink) {
            renderer.gameObject.GetComponent<FloatAnimation>().blink = blink;
        }

        public void Animate(float speed = 0) {
            if(renderer == null) return;
            if(current != null) {
                renderer.enabled = true;
                if(speed >= 0) current.Move();
                else current.Move(speed);
                renderer.sprite = current.GetCurrentSprite();
                if(current.GetCurrentSprite() == null) renderer.sprite = baseSprite;
            }
            else {
                if(transparantIfNull) renderer.enabled = false;
                reset();
            }
        }

        public void reset() {
            if(current != null) current.reset();
            if(renderer != null) renderer.sprite = baseSprite;
        }

        public void SetAnimation(string name) {
            if(renderer == null) return;
            if(name == null) {
                current = null; 
                return;
            }
            current = anims[name];
        }
        public string GetAnimation() {
            return (current != null) ? current.name : "NONE";
        }
    }
    public Animator legsAnimator, armsAnimator, faceAnimator;

    private List<GameObject> opaqueWalls = new List<GameObject>(), fadeWalls = new List<GameObject>();

    private ShaderEffect_CorruptedVram shader;
    private bool beginShade = true;

    protected override void Start() {
        IDestroyable.DMGText_Template = DMGTextPrefab;
        base.Start();
        rb = transform.parent.GetComponent<Rigidbody>();
        shader = Camera.main.GetComponent<ShaderEffect_CorruptedVram>();
        shader.enabled = true;
        shader.shift = 100;

        legs = transform.Find("Legs");
        arms = transform.Find("Arms");
        legsAnimator.Initialize();
        armsAnimator.Initialize();
        faceAnimator.Initialize();

        foreach(SpriteRenderer re in sprites) {
            Sprite sprite = re.sprite;
            Mesh mesh = new Mesh() {
            vertices = Array.ConvertAll(sprite.vertices, i => (Vector3)i), 
            uv = sprite.uv, 
            triangles = Array.ConvertAll(sprite.triangles, i => (int)i)
            };
            GameObject go = new GameObject("Shadow");
            go.transform.SetParent(re.transform);
            MeshFilter fi = go.AddComponent<MeshFilter>();
            fi.mesh = mesh;
            MeshRenderer me = go.AddComponent<MeshRenderer>();
            me.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            me.lightProbeUsage = LightProbeUsage.Off;
            me.receiveShadows = false;
            me.material = re.material;
            me.allowOcclusionWhenDynamic = false;
            me.reflectionProbeUsage = ReflectionProbeUsage.Off;
            go.transform.localPosition = new Vector3(0, 0, 0);
            go.transform.localRotation = Quaternion.identity;
        }
    }

    void FixedUpdate() {
        if(beginShade) {
            shader.shift = Mathf.Lerp(shader.shift, 0, Time.deltaTime * 3);
            if(shader.shift < 0.001f) shader.enabled = beginShade = false;
        }

        DamageFeedback();
        
        movement = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        MoveHorizontal(movement);
        MoveVertical(moveY);

        if(movement != 0) MoveAnimate(movement);
        else if(moveY != 0) MoveAnimate(-moveY, movement);
        else MoveAnimate(0);

        legsAnimator.Animate();
        armsAnimator.Animate();
        faceAnimator.Animate();

        if(shootTime > 0) shootTime -= Time.deltaTime;

        //Walls obstructing the camera
        RaycastWalls();
        FadeWalls();
    }

    protected void FadeWalls() {
        foreach(GameObject wall in opaqueWalls.ToArray()) {
            Material mat = wall.GetComponent<Renderer>().material;
            Color baseColor = mat.GetColor("_Color");
            mat.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(baseColor.a, 0.3f, Time.deltaTime * 2)));

            if(Vector2.Distance(wall.transform.position, transform.position) > 3) {
                fadeWalls.Add(wall);
                opaqueWalls.Remove(wall);
            } else {
                mat.SetFloat("_ZWrite", 0);
                mat.SetFloat("_Cutoff", 0);
            }
        }
        foreach(GameObject wall in fadeWalls.ToArray()) {
            Material mat = wall.GetComponent<Renderer>().material;
            Color baseColor = mat.GetColor("_Color");
            mat.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(baseColor.a, 1, Time.deltaTime)));
            
            if(baseColor.a > 0.9f) {
                mat.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, 1));
                mat.SetFloat("_ZWrite", 1);
                mat.SetFloat("_Cutoff", 0);
                fadeWalls.Remove(wall);
            }
        }
    }
    protected void RaycastWalls() {
        Ray ray = Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit)) {
            if(hit.collider.tag == "Wall") {
                if(!opaqueWalls.Contains(hit.collider.gameObject)) opaqueWalls.Add(hit.collider.gameObject);
                return;
            }
        }
    }

    protected new void Notify(float amount, bool rand = false) {
        GameObject go = Instantiate(DMGText_Template, transform.position + new Vector3(0, 2, 0), Quaternion.identity);
        TextMesh text = go.GetComponent<TextMesh>();
        text.text = amount.ToString();
        if(amount < 0) text.color = Color.red;
        else text.color = Color.green;
    }

    private Vector3 tar;
    public void ShootBolt(RaycastHit hit) {
        if(shootTime > 0) return;
        if(hit.collider.gameObject.tag == "Player") return;
        tar = hit.point;
        tar.y = 0;
        shootTime = shootDelay / 10f;
        
        GameObject bolt = Instantiate(this.bolt, transform.position + Vector3.up, Quaternion.identity);
        Vector3 hitPoint = (hit.point - transform.position).normalized;
        Vector3 velocity = new Vector3(Mathf.RoundToInt(rb.velocity.normalized.x), Mathf.RoundToInt(rb.velocity.normalized.y), Mathf.RoundToInt(rb.velocity.normalized.z));
        Vector3 direction = hitPoint + new Vector3((Mathf.RoundToInt(hitPoint.x) == Mathf.RoundToInt(velocity.x)) ? velocity.x : 0, 0, ((int)hitPoint.z == (int)velocity.z) ? velocity.z : 0);
        Vector3 bulletForce = direction * 500 * boltSpeed;
        bolt.GetComponent<Rigidbody>().AddForce(bulletForce);
        bolt.transform.LookAt(hit.point);

        Bolt boltSrc = bolt.GetComponent<Bolt>();
        boltSrc.force = boltForce;
        boltSrc.boltDMG = boltDMG;
        boltSrc.source = gameObject;
        
        //Cursor visual feedback
        cursor.Bump((shootDelay + 0.6f) * (boltDMG / 50));
    }

    protected void MoveHorizontal(float i) {
        rb.AddForce(i * animSpeed * 1010 * Time.deltaTime, 0, 0);
        if(i < 0) FlipDirection(true);
        else if(i > 0) FlipDirection(false);
  }
    protected void MoveVertical(float i) {
        rb.AddForce(0, 0, i * animSpeed * 1000 * Time.deltaTime);
    }

    protected void MoveAnimate(float i, float dir = 1) {
        //Frame by frame animation
        if(i != 0) legsAnimator.SetAnimation("Walk");
        else legsAnimator.SetAnimation(null);

        //Dynamic animation
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, Mathf.LerpAngle(transform.eulerAngles.z, animLean * i * dir, Time.deltaTime * 2 * animSpeed));
        transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Lerp(transform.localPosition.y, (Mathf.Sin(Time.time * 4.5f * animSpeed) * i / 5), Time.deltaTime * 2 * animSpeed), transform.localPosition.z);
        transform.localScale = new Vector3(transform.localScale.x, Mathf.Lerp(transform.localScale.y, (Mathf.Sin(Time.time *3 * animSpeed) * i / 8) + 1, Time.deltaTime * 2 * animSpeed), transform.localScale.z);
        legs.localScale = new Vector3(legs.localScale.x, Mathf.Lerp(legs.localScale.y, (Mathf.Cos(Time.time * 4 * animSpeed) * i / 6) + 1, Time.deltaTime * 2 * animSpeed), legs.localScale.z);
        arms.localPosition = new Vector3(arms.localPosition.x, Mathf.Sin(Time.time * 10 * i) / 20, arms.localPosition.z);

        //Bumpy idle anims
        if(i == 0) {
            arms.localPosition = new Vector3(arms.localPosition.x, Mathf.Sin(Time.time * 7) / 20, arms.localPosition.z);
            transform.localPosition = new Vector3(transform.localPosition.x, (Mathf.Cos(Time.time * 7) / 20) - 0.1f, transform.localPosition.z);
            legs.position = new Vector3(legs.position.x, transform.parent.position.y, legs.position.z);
        }
    }

    protected void FlipDirection(bool i) {
        foreach(SpriteRenderer sprite in sprites) sprite.flipX = i;
    }
}
