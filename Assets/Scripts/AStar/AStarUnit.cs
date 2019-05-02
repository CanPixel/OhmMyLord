using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStar;

public class AStarUnit : MonoBehaviour {
    const float minPathUpdateDelay = 0.2f;
    const float targetMoveThreshold = 0.5f;

    [HideInInspector]
    private Transform target;
    public float speed = 20f;
    public float turnDst = 5f;
    public float turnSpeed = 3f;
    public float stoppingDistance = 10f;

    private float baseStoppingDistance;

    private bool following = false;
    public bool isFollowing {
        get{return following;}
    }

    [HideInInspector]
    public bool pathSuccess;
    
    protected Path path;

    void Awake() {
        baseStoppingDistance = stoppingDistance;
    }

    public void SetStoppingDistance(float val) {
        stoppingDistance = val;
    }
    public void ResetStoppingDistance() {
        stoppingDistance = baseStoppingDistance;
    }

    public void SetTarget(Transform target) {
        this.target = target;
        Clean();
        Go();
    }

    public Transform GetTarget() {
        return target;
    }

    public int GetDirectionToTarget() {
        if(target == null) return 0;
        return (transform.position.x > target.position.x) ? 1 : -1;
    }

    public void Go() {
        StartCoroutine(UpdatePath());
    }

    public void Clean() {
        StopCoroutine("FollowPath");
        StopCoroutine(UpdatePath());
    }

    public void OnPathFound(Vector3[] wayPoints, bool success) {
        if(gameObject == null || transform == null) return;
        pathSuccess = success;
        if(success) {
            path = new Path(wayPoints, transform.position, turnDst, stoppingDistance);
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }
    }

    IEnumerator UpdatePath() {
        if(Time.timeSinceLevelLoad < 0.3f) yield return new WaitForSeconds(0.3f);
        if(target == null) yield return null;
        try {
            AStarRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));
        } catch(System.Exception){}

        float sqrMoveThreshold = targetMoveThreshold * targetMoveThreshold;
        Vector3 targetPrev = target.position;

        while(true) {
            yield return new WaitForSeconds(minPathUpdateDelay);
            if(target != null && (target.position - targetPrev).sqrMagnitude > sqrMoveThreshold) {
                 AStarRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));
                 targetPrev = target.position;
            }
        }
    }

    private float speedPercent;
    IEnumerator FollowPath() {
        following = true;
        int pathIndex = 0;
        transform.LookAt(path.lookPoints[0]);

        speedPercent = 1;
        while(following) {
            Vector2 pos2D = Util.Vec3ToVec2(transform.position);
            while(path.turnBounds[pathIndex].CrossedLine(pos2D)) {
                if(pathIndex == path.finishIndex) {
                    following = false;
                    break;
                }
                else pathIndex++;
            }

            if(following) {
                if(pathIndex >= path.slowDownIndex && stoppingDistance > 0) {
                    speedPercent = Mathf.Clamp01(path.turnBounds[path.finishIndex].DistanceFromPoint(pos2D) / stoppingDistance);
                    if(speedPercent < 0.01f) following = false;
                }
                Quaternion targetRot = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
                transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);
            }
            yield return null;
        }
    }

    public float GetSpeed() {
        return speedPercent;
    }
}
