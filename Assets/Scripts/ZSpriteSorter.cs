using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZSpriteSorter : MonoBehaviour {
    private Transform player;
    private SpriteRenderer sprite, playerSpr;

    void Awake() {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        sprite = GetComponent<SpriteRenderer>();
        playerSpr = player.GetComponentInChildren<SpriteRenderer>();
    }

    void FixedUpdate() {
        if(playerSpr == null) return;
        if(transform.position.z < player.position.z) sprite.sortingOrder = playerSpr.sortingOrder + 5;
        else sprite.sortingOrder = playerSpr.sortingOrder - 5;
    }
}
