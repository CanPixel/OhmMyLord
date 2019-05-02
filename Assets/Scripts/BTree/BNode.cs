using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BehaviorTree {
    [System.Serializable]
    public abstract class BNode {
        public enum NodeState {
            FAIL, BUSY, SUCCESS
        }
        public string name = "Node";

        protected NodeState state;

        public Enemy root;

        public BNode() {}
        public abstract NodeState Run();
    }

    public class BAction : BNode {
        public delegate NodeState BEvent();
        public BEvent action;

        public BAction(Enemy root, BEvent act) {
            action = act;
            this.root = root;
            name = "Action Node (" + act.Method.Name + ")";
        }

        public override NodeState Run() {
            if(root.lastNode != this) {
                if(LevelManager.debuggingAI) Debug.Log(name + ": " + state);
                root.lastNode = this;
            }
            state = action();
            return state;
        }
    }

    namespace Decorator {
        public class BTimer : BNode {
            private BNode child;
            protected float time, count = 0;

            public BTimer(BNode child, float time) {
                this.time = time;
                this.child = child;
                name = "Timer Node (" + time + ")";
            }

            public override NodeState Run() {
                count += Time.deltaTime;
                if(count > time) return child.Run();
                else return NodeState.FAIL;
            }
        }

        public class BInverter : BNode {
            private BNode child;

            public BInverter(BNode node) {
                child = node;
                name = "Inverter Node";
            }

            public override NodeState Run() {
                switch(child.Run()) {
                    case NodeState.FAIL:
                        state = NodeState.SUCCESS;
                        return state;
                    default:
                    case NodeState.SUCCESS:
                        state = NodeState.FAIL;
                        return state;
                    case NodeState.BUSY:
                        state = NodeState.BUSY;
                        return state;
                }
            }
        }
    }

    namespace Composite {
        public class BSequence : BNode {
            protected BNode[] children;

            public BSequence(BNode[] nodes) {
                children = nodes;
                name = "Sequence Node";
            }
            
            public override NodeState Run() {
                bool isRunning = false;
                foreach(BNode node in children) {
                    switch(node.Run()) {
                        case NodeState.FAIL:
                            state = NodeState.FAIL;
                            return state;
                        case NodeState.SUCCESS: continue;
                        case NodeState.BUSY:
                            isRunning = true;
                            continue;
                        default:
                            state = NodeState.SUCCESS;
                            return state;
                    }
                }
                state = isRunning ? NodeState.BUSY : NodeState.SUCCESS;
                return state;
            }
        }

        public class BSelector : BNode {
            protected BNode[] children;

            public BSelector(BNode[] nodes) {
                children = nodes;
                name = "Selector Node";
            }

            public override NodeState Run() {
                foreach(BNode node in children) {
                    switch(node.Run()) {
                        case NodeState.FAIL: continue;
                        case NodeState.SUCCESS: 
                            state = NodeState.SUCCESS;
                            return state;
                        case NodeState.BUSY:
                            state = NodeState.BUSY;
                            return state;
                        default: continue;
                    }
                }
                state = NodeState.FAIL;
                return state;
            }
        }
    }
}