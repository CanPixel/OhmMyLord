using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour {
    public Player player;

    private Image image;

    void Start() {
        image = transform.Find("Bar").GetComponent<Image>();
    }

    void FixedUpdate() {
        float percentage = player.Health / player.maxHealth * 100;
        image.fillAmount = percentage / 100f;
    }
}
