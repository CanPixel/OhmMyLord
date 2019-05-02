using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using AStar;
using System.Threading;

namespace AStar {
    public class AStarRequestManager : MonoBehaviour {
        private Queue<PathResult> results = new Queue<PathResult>();

        private PathRequest current;

        private static AStarRequestManager instance;
        private AStarPath pathfinding;
        private bool isProcessingPath;

        void Awake() {
            instance = this;
            pathfinding = GetComponent<AStarPath>();
        }

        void Update() {
            if(results.Count > 0) {
                int itemsInQueue = results.Count;
                lock(results) {
                    for(int i = 0; i < itemsInQueue; i++) {
                        PathResult result = results.Dequeue();
                        try {
                            result.callback(result.path, result.success);
                        } catch(MissingReferenceException){}
                    }
                }
            }
        }

        public static void RequestPath(PathRequest request) {
            ThreadStart threadStart = delegate {
                instance.pathfinding.FindPath(request, instance.FinishProcessingPath);
            };
            threadStart.Invoke();
        }

        public void FinishProcessingPath(PathResult result) {
            lock(results) results.Enqueue(result);
        }
    }
    public struct PathResult {
        public Vector3[] path;
        public bool success;
        public Action<Vector3[], bool> callback;

        public PathResult(Vector3[] path, bool success, Action<Vector3[], bool> callback) {
            this.path = path;
            this.success = success;
            this.callback = callback;
        }
    }

    public  struct PathRequest {
        public Vector3 start, end;
        public Action<Vector3[], bool> callback;

        public PathRequest(Vector3 start, Vector3 end, Action<Vector3[], bool> callback) {
            this.start = start;
            this.end = end;
            this.callback = callback;
        }
    }
}