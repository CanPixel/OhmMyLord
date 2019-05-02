using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SmartCursor : MonoBehaviour {
    private Image image;
    public GameObject shieldOBJ, eyeOBJ, player;
    public float speed = 12;
    public bool main = false;
    public Player playerSrc;

    public string[] ignoreTags;

    public enum CursorPartType {
        TARGET, EYE, SHIELD
    }
    public class CursorPart {
        private Color tar;
        private Image root;
        private float speed, bump = 0, baseScale;

        public CursorPart(Image root) {
            this.root = root;
            tar = root.color;
            baseScale = root.transform.localScale.x;
        }

        public void Update() {
            root.color = new Color(Mathf.Lerp(root.color.r, tar.r, Time.deltaTime * speed), Mathf.Lerp(root.color.g, tar.g, Time.deltaTime * speed), Mathf.Lerp(root.color.b, tar.b, Time.deltaTime * speed));
           
            bump = Mathf.Lerp(bump, 0, Time.deltaTime * speed / 2);
            root.transform.localScale = new Vector2(baseScale + bump, baseScale + bump);
        }

        public void SetLerpColor(Color col, float speed) {
            if(this.tar != col) bump = 0.5f;
            tar = col;
            this.speed = speed;
        }
    }
    public CursorPart shield, target, eye;

    private float bump = 0;
    private GameObject scan, oldScan;
    private float distToScan = 0;
    private float autoScale = 0;

    void Awake() {
        image = GetComponent<Image>();
        Cursor.visible = false;
        if(!main) return;
        shield = new CursorPart(shieldOBJ.GetComponent<Image>());
        target = new CursorPart(image);
        eye = new CursorPart(eyeOBJ.GetComponent<Image>());
    }

    void FixedUpdate() {
        //Track mouseposition
        transform.position = new Vector2(Mathf.Lerp(transform.position.x, Input.mousePosition.x, Time.deltaTime * speed), Mathf.Lerp(transform.position.y, Input.mousePosition.y, Time.deltaTime * speed));
        if(bump > 0) bump = Mathf.Lerp(bump, 0, Time.deltaTime * 3);

        //Updating each part of the cursor (for animations / visual feedback)
        if(main) {
            if(shield != null && eye != null && target != null) {
                shield.Update();
                eye.Update();
                target.Update();

                float targetScale = eyeOBJ.transform.localScale.x / 3 + bump / 8 + distToScan;
                if(autoScale > 0) targetScale = Mathf.Lerp(transform.localScale.x, targetScale, Time.deltaTime * 2);
                transform.localScale = new Vector2(targetScale, targetScale);
                shieldOBJ.transform.localScale = new Vector2(1 + bump, 1 + bump);
            }

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out hit)) {
                scan = hit.transform.gameObject;
                switch(scan.tag) {
                    default:    SetColor(CursorPartType.EYE, Color.white); break;
                    case "Wall": SetColor(CursorPartType.EYE, Color.black, 12); SetColor(CursorPartType.SHIELD, new Color(0.15f, 0.15f, 0.15f)); SetColor(CursorPartType.TARGET, Color.white); autoScale = 0; break;
                    case "Floor": SetColor(CursorPartType.EYE, Color.white); SetColor(CursorPartType.SHIELD, Color.black); SetColor(CursorPartType.TARGET, Color.white); autoScale = 0; break;
                    case "Enemy": SetColor(CursorPartType.EYE, Color.red, 7); SetColor(CursorPartType.TARGET, Color.red, 6); SetColor(CursorPartType.SHIELD, Color.black, 6); if(oldScan != scan) { bump = 0.8f; autoScale = 0;} break;
                    case "Player": SetColor(CursorPartType.TARGET, Color.gray); SetColor(CursorPartType.EYE, Color.gray); bump = 0.1f; autoScale = 0.1f; break;
                    case "ChargingDock": SetColor(CursorPartType.EYE, Color.green, 7); SetColor(CursorPartType.TARGET, Color.green, 6); SetColor(CursorPartType.SHIELD, Color.black, 6); if(oldScan != scan) { bump = 0.8f; autoScale = 0;} break;
                    case "Portal": SetColor(CursorPartType.EYE, new Color(0.502f, 0, 0.502f), 7); SetColor(CursorPartType.TARGET, new Color(0.502f, 0, 0.502f), 6); SetColor(CursorPartType.SHIELD, Color.white, 6); if(oldScan != scan) { bump = 0.8f; autoScale = 0;} break;
                }
                oldScan = scan;
                if(Input.GetMouseButtonDown(0)) playerSrc.ShootBolt(hit);
            }
            else SetColor(CursorPartType.EYE, Color.white);
        
            if(scan != null) {
                if(!MatchesIgnoreTags(scan.tag)) distToScan = Mathf.Clamp(1 / Mathf.Clamp(Vector3.Distance(player.transform.position, scan.transform.position)*3, 0, 50) * 3, 0, 0.6f);
                else distToScan = Mathf.Lerp(distToScan, 0, Time.deltaTime * 4);
            }
        }
    }

    public void Bump(float i) {
        bump = Mathf.Abs(i);
    }

    public bool MatchesIgnoreTags(string tag) {
        for(int i = 0; i < ignoreTags.Length; i++) if(ignoreTags[i] == tag) return true;
        return false;
    }

    public void SetColor(CursorPartType part, Color col, float speed = 8) {
        if(!main) return;
        switch(part) {
            default:
            case CursorPartType.TARGET:
                target.SetLerpColor(col, speed);
                break;  
            case CursorPartType.EYE:
                eye.SetLerpColor(col, speed);
                break;
            case CursorPartType.SHIELD:
                shield.SetLerpColor(col, speed);
                break;
        }
    }
}
