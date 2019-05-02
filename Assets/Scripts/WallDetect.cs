using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WallDetect : MonoBehaviour {
    private Vector3 pos;
    private bool place;
    private Collider box;

    public string compareTag;

    public bool Detect() {
        place = true;
        if(box == null) box = GetComponent<Collider>();
        foreach(Collider col in Physics.OverlapBox(box.bounds.center, box.bounds.size / 2)) {
            if(col.tag == compareTag && col.tag == box.tag && col.gameObject != gameObject) {
                place = false;
                break;
            }
        }
        return place;
    }

    public bool ShouldBePlaced() {
        return place;
    }

    public void CleanDuplicateDetectors() {
        box = GetComponent<Collider>();
        pos = box.bounds.center;
        foreach(Collider col in Physics.OverlapBox(box.bounds.center, box.bounds.size / 2)) {
            if(col.tag == gameObject.tag && col.transform.parent.gameObject != transform.parent.gameObject) Destroy(col.gameObject);
        }
    }

    public void RemoveIndoorWalls() {
        if(gameObject.tag != "Wall") return;
        box = GetComponent<Collider>();
        pos = box.bounds.center;
        foreach(Collider col in Physics.OverlapBox(box.bounds.center, box.bounds.size / 2)) {
            if(col.gameObject != gameObject && col.tag == "WallDetector") {
                WallDetect detect = col.gameObject.GetComponent<WallDetect>();
                if(!detect.ShouldBePlaced()) Destroy(gameObject);
            }
        }
    }

    void OnDrawGizmos() {
        if(place) Gizmos.color = Color.green;
        else Gizmos.color = Color.red;
        if(box != null && gameObject.tag != "Wall") Gizmos.DrawCube(pos, box.bounds.size);
    }
}
