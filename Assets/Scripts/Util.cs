using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util {
    public static bool InRange(float val, float target, float range) {
        float a = target + range;
        float b = target - range;
        return (val < a) & (val > b);
    }

    public static Vector2 Vec3ToVec2(Vector3 v3) {
        return new Vector2(v3.x, v3.z);
    }

    public static Vector3 ClampVec(Vector3 vec, int min, int max) {
        return new Vector3(Mathf.Clamp(vec.x, min, max), Mathf.Clamp(vec.y, min, max), Mathf.Clamp(vec.z, min, max));
    }

    public static Vector3 Abs(Vector3 vec) {
        return new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
    }

    public static bool ContainsVec2(List<Vector2Int> list, Vector2Int i) {
        foreach(Vector2Int vec in list) if(vec.x == i.x && vec.y == i.y) return true;
        return false;
    }

    public static bool ContainsVec3(List<Vector3> list, Vector3 i) {
        foreach(Vector3 vec in list) if(vec.x == i.x && vec.y == i.y && vec.z == i.z) return true;
        return false;
    }

    public static bool ContainsVec2(List<Vector2> list, Vector2 i) {
        foreach(Vector2 vec in list) if(vec.x == i.x && vec.y == i.y) return true;
        return false;
    }
}
