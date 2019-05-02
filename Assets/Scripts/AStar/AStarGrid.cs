using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStar;

namespace AStar {
    public class Node : IHeapItem<Node> {
        public int cost, heuristic;
        public bool accessible;
        public Vector3 position;
        public int gridX, gridY;
        public int movementPenalty;

        public Node parent;
        private int heapIndex;

        public Node(bool access, Vector3 pos, int x, int y, int penalty, float nodeDiam) {
            this.accessible = access;
            this.position = pos;
            gridX = x;
            gridY = y;
            movementPenalty = penalty;

            Collider[] cols = Physics.OverlapBox(position, Vector3.one * nodeDiam);
            if(cols.Length <= 0) accessible = false;
        }

        public int fCost {
            get {return cost + heuristic;}
        }

        public int HeapIndex {
            get {return heapIndex;}
            set {
                heapIndex = value;
            }
        }

        public int CompareTo(Node n) {
            int compare = fCost.CompareTo(n.fCost);
            if(compare == 0) {
                compare = heuristic.CompareTo(n.heuristic);
            }
            return -compare;
        }
    }

    [ExecuteInEditMode]
    public class AStarGrid : MonoBehaviour {
        public LayerMask inaccessibleMask;
        public float nodeRadius;
        public Vector2 worldSize;
        protected Node[,] grid;
        public bool displayGridGizmos;
        
        public GroundType[] walkableRegions;
        private LayerMask walkableMask;
        private Dictionary<int, int> walkableRegionsDict = new Dictionary<int, int>();
        public int proximityPenalty = 10;

        private float nodeDiam;
        private int gridSizeX, gridSizeY;

        private int penaltyMin = int.MaxValue, penaltyMax = int.MinValue;

        public void GenNodes() {
            nodeDiam = nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(worldSize.x / nodeDiam);
            gridSizeY = Mathf.RoundToInt(worldSize.y / nodeDiam);

            foreach(GroundType region in walkableRegions) {
                walkableMask.value |= region.mask.value;
                walkableRegionsDict.Add((int)Mathf.Log(region.mask.value, 2), region.penalty);
            }
            InitGrid();
        }

        public int MaxSize {
            get {
                return gridSizeX * gridSizeY;
            }
        }

        private void InitGrid() {
            grid = new Node[gridSizeX, gridSizeY];
            Vector3 bottomLeft = transform.position - Vector3.right * worldSize.x / 2 - Vector3.forward * worldSize.y / 2;

            for(int x = 0; x < gridSizeX; x++) {
                for(int y = 0; y < gridSizeY; y++) {
                    Vector3 point = bottomLeft + Vector3.right * (x * nodeDiam + nodeRadius) + Vector3.forward * (y * nodeDiam + nodeRadius);
                    bool walkable = !(Physics.CheckSphere(point, nodeRadius, inaccessibleMask));
                    int movementPenalty = 0;
                   
                    Ray ray = new Ray(point + Vector3.up * 50, Vector3.down);
                    RaycastHit hit;
                    if(Physics.Raycast(ray, out hit, 100, walkableMask)) walkableRegionsDict.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                    if(!walkable) movementPenalty += proximityPenalty;

                    grid[x, y] = new Node(walkable, point, x, y, movementPenalty, nodeDiam);
                }
            }
            BlurPenaltyWeights(3);
        }

        public Node NodeFromWorld(Vector3 pos) {
            float xPercent = Mathf.Clamp01((pos.x + worldSize.x / 2) / worldSize.x);
            float yPercent = Mathf.Clamp01((pos.z + worldSize.y / 2) / worldSize.y);
            int x = Mathf.RoundToInt((gridSizeX - 1) * xPercent);
            int y = Mathf.RoundToInt((gridSizeY - 1) * yPercent);
            return grid[x, y];
        }

        protected void BlurPenaltyWeights(int blurSize) {
            int kernelSize = blurSize * 2 + 1;
            int kernelExtents = (kernelSize - 1) / 2;
            int[,] penaltiesHorizontalPass = new int[gridSizeX,gridSizeY];
            int[,] penaltiesVerticalPass = new int[gridSizeX,gridSizeY];

            for (int y = 0; y < gridSizeY; y++) {
                for (int x = -kernelExtents; x <= kernelExtents; x++) {
                    int sampleX = Mathf.Clamp (x, 0, kernelExtents);
                    penaltiesHorizontalPass [0, y] += grid [sampleX, y].movementPenalty;
                }

                for (int x = 1; x < gridSizeX; x++) {
                    int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
                    int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);
                    penaltiesHorizontalPass [x, y] = penaltiesHorizontalPass [x - 1, y] - grid [removeIndex, y].movementPenalty + grid [addIndex, y].movementPenalty;
                }
            }

            for (int x = 0; x < gridSizeX; x++) {
                for (int y = -kernelExtents; y <= kernelExtents; y++) {
                    int sampleY = Mathf.Clamp (y, 0, kernelExtents);
                    penaltiesVerticalPass [x, 0] += penaltiesHorizontalPass [x, sampleY];
                }

                int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, 0] / (kernelSize * kernelSize));
                grid [x, 0].movementPenalty = blurredPenalty;

                for (int y = 1; y < gridSizeY; y++) {
                    int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                    int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY- 1);

                    penaltiesVerticalPass [x, y] = penaltiesVerticalPass [x, y - 1] - penaltiesHorizontalPass [x,removeIndex] + penaltiesHorizontalPass [x, addIndex];
                    blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass [x, y] / (kernelSize * kernelSize));
                    grid [x, y].movementPenalty = blurredPenalty;

                    if (blurredPenalty > penaltyMax) penaltyMax = blurredPenalty;
                    if (blurredPenalty < penaltyMin) penaltyMin = blurredPenalty;
                }
            }
        }

        public List<Node> GetNeighbors(Node n) {
            List<Node> neighbors = new List<Node>();
            for(int x = -1; x <= 1; x++) 
                for(int y = -1; y <= 1; y++) {
                    if(x == 0 && y == 0) continue;
                    int checkX = n.gridX + x;
                    int checkY = n.gridY + y;
                    if(checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) neighbors.Add(grid[checkX, checkY]);
                }
            return neighbors;
        }

        void OnDrawGizmos() {
            Gizmos.DrawWireCube(transform.position,new Vector3(worldSize.x, 1, worldSize.y));

            if (grid != null && displayGridGizmos) {
                foreach (Node n in grid) {
                    Gizmos.color = Color.Lerp (Color.white, Color.black, Mathf.InverseLerp (penaltyMin, penaltyMax, n.movementPenalty));
                    Gizmos.color = (n.accessible)? Gizmos.color : Color.red;
                    Gizmos.DrawCube(n.position, Vector3.one * nodeDiam);
                }
            }
        }
    }

    [System.Serializable]
    public class GroundType {
        public LayerMask mask;
        public int penalty;
    }
}