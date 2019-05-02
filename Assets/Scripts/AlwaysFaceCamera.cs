using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlwaysFaceCamera : MonoBehaviour {
    public bool lockX = false, lockY = false, lockZ = false;
    public Vector3 offset;

    void FixedUpdate() {
        transform.LookAt(Camera.main.transform);

        transform.rotation = Quaternion.Euler(transform.eulerAngles + offset);
        transform.rotation = Quaternion.Euler(lockX ? 0 : transform.eulerAngles.x, lockY ? 0 : transform.eulerAngles.y, lockZ ? 0 : transform.eulerAngles.z);
    }
}
