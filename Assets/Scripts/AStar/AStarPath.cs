using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStar;
using System.Diagnostics;
using System;
/**
    Made side-by-side with the A* pathfinding tutorial by Sebastian Lague
    https://www.youtube.com/watch?v=dn1XRIaROM4

    I did write all this code by myself whilst following the tutorial, I did not simply copy paste(!!)
    Modified & optimized for my own needs
    ~ Can Ur
 */

namespace AStar {
    public class AStarPath : MonoBehaviour {
        private AStarGrid grid;

        void Awake() {
            grid = GetComponent<AStarGrid>();
        }

        public void FindPath(PathRequest request, Action<PathResult> callback) {
            Vector3[] wayPoints = new Vector3[0];
            bool pathSuccess = false;

            Node startNode = grid.NodeFromWorld(request.start);
            Node targetNode = grid.NodeFromWorld(request.end);
            if(startNode.accessible && targetNode.accessible) {
                Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
                HashSet<Node> closedSet = new HashSet<Node>();
                openSet.Add(startNode);

                while(openSet.Count > 0) {
                    Node current = openSet.RemoveFirst();
                    closedSet.Add(current);

                    if(current == targetNode) {
                        pathSuccess = true;
                        break;
                    }

                    foreach(Node neighbor in grid.GetNeighbors(current)) {
                        if(!neighbor.accessible || closedSet.Contains(neighbor)) continue;

                        int newMoveCost = current.cost + GetDistance(current, neighbor) + neighbor.movementPenalty;
                        if(newMoveCost < neighbor.cost || !openSet.Contains(neighbor)) {
                            neighbor.cost = newMoveCost;
                            neighbor.heuristic = GetDistance(neighbor, targetNode);
                            neighbor.parent = current;
                            if(!openSet.Contains(neighbor)) openSet.Add(neighbor);
                            else openSet.Update(neighbor);
                        }
                    }
                }
            }
            if(pathSuccess) {
                wayPoints = Retrace(startNode, targetNode);
                pathSuccess = wayPoints.Length > 0;
            }
            callback(new PathResult(wayPoints, pathSuccess, request.callback));
        }

        protected Vector3[] Retrace(Node start, Node end) {
            List<Node> path = new List<Node>();
            Node current = end;
            while (current != start) {
                path.Add(current);
                current = current.parent;
            }
            Vector3[] wayPoints = SimplifyPath(path);
            Array.Reverse(wayPoints);
            return wayPoints;
        }

        protected Vector3[] SimplifyPath(List<Node> path) {
            List<Vector3> wayPoints = new List<Vector3>();
            Vector2 directionOld = Vector2.zero;
            for(int i = 1; i < path.Count; i++) {
                Vector2 directionNew = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
                if(directionNew != directionOld) wayPoints.Add(path[i].position);
                directionOld = directionNew;
            }
            return wayPoints.ToArray();
        }

        public int GetDistance(Node nodeA, Node nodeB) {
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
            if(dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
            else return 14 * dstX + 10 * (dstY - dstX);
        }
    }

    public struct Line {
        const float verticalLineGradient = 1e5f;

        float gradient;
        float yIntercept;
        Vector2 pointOnLine1, pointOnLine2;

        float gradientPerpendic;
        bool approachSide;

        public Line(Vector2 point, Vector2 pointPerpendic) {
            float dx = point.x - pointPerpendic.x;
            float dy = point.y - pointPerpendic.y;

            if(dx == 0) gradientPerpendic = verticalLineGradient;
            else gradientPerpendic = dy / dx;

            if(gradientPerpendic == 0) gradient = verticalLineGradient;
            else gradient = -1 / gradientPerpendic;
        
            yIntercept = point.y - gradient * point.x;
            pointOnLine1 = point;
            pointOnLine2 = point + new Vector2(1, gradient);
            
            approachSide = false;
            approachSide = GetSide(pointPerpendic);
        }

        private bool GetSide(Vector2 p) {
            return (p.x - pointOnLine1.x) * (pointOnLine2.y - pointOnLine1.y) > (p.y - pointOnLine1.y) * (pointOnLine2.x - pointOnLine1.x);
        }

        public bool CrossedLine(Vector2 p) {
            return GetSide(p) != approachSide;
        }

        public float DistanceFromPoint(Vector2 p) {
            float yInterceptPerpend = p.y - gradientPerpendic * p.x;
            float intersectX = (yInterceptPerpend - yIntercept) / (gradient - gradientPerpendic);
            float intersectY = gradient * intersectX + yIntercept;
            return Vector2.Distance(p, new Vector2(intersectX, intersectY));
        }

        public void DrawWithGizmos(float length) {
            Vector3 lineDir = new Vector3(1, 0, gradient).normalized;
            Vector3 lineCenter = new Vector3(pointOnLine1.x, 0, pointOnLine1.y) + Vector3.up;
            Gizmos.DrawLine(lineCenter - lineDir * length / 2f, lineCenter + lineDir * length / 2f);
        }
    }

    public class Path {
        public readonly Vector3[] lookPoints;
        public readonly Line[] turnBounds;
        public readonly int finishIndex;
        public readonly int slowDownIndex;

        public Path(Vector3[] wayPoints, Vector3 start, float turn, float stoppingDst) {
            lookPoints = wayPoints;
            turnBounds = new Line[lookPoints.Length];
            finishIndex = turnBounds.Length - 1;
        
            Vector2 prev = Util.Vec3ToVec2(start);
            for(int i = 0; i < lookPoints.Length; i++) {
                Vector2 current = Util.Vec3ToVec2(lookPoints[i]);
                Vector2 dir = (current - prev).normalized;
                Vector2 bound = (i == finishIndex)? current : current - dir * turn;
                turnBounds[i] = new Line(bound, prev - dir * turn);
                prev = bound;
            }

            float dstFromEndPoint = 0;
            for(int i = lookPoints.Length - 1; i > 0; i--) {
                dstFromEndPoint += Vector3.Distance(lookPoints[i], lookPoints[i - 1]);
                if(dstFromEndPoint > stoppingDst) {
                    slowDownIndex = i;
                    break;
                }
            }
        }

        public void DrawWithGizmos() {
            Gizmos.color = Color.black;
            foreach(Vector3 p in lookPoints) Gizmos.DrawCube(p + Vector3.up, Vector3.one);
        }
    }
}