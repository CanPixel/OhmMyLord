using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour {
    public Text deathTitle;

    private RawImage image;

    private float scale, baseAlpha;

    void Awake() {
        image = GetComponent<RawImage>();
        scale = deathTitle.transform.localScale.x;
        baseAlpha = image.color.a;
    }

    public void Invoke() {
        gameObject.SetActive(true);
        image.color = new Color(0, 0, 0, 0);
        deathTitle.transform.localScale = Vector3.zero;
    }

    void FixedUpdate() {
        float val =  Mathf.Lerp(deathTitle.transform.localScale.x, scale, Time.deltaTime * 2);
        deathTitle.transform.localScale = new Vector3(val, val, val);

        image.color = Color.Lerp(image.color, new Color(0, 0, 0, baseAlpha), Time.deltaTime);

        if(Input.GetKeyDown(KeyCode.F7)) LevelManager.GenerateNewWorld();
    }
}
