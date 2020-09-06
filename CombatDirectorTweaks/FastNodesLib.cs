using System;
using System.Collections.Generic;
using System.Text;
using R2API.Utils;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace CombatDirectorTweaks
{
    public static class FastNodesLib
    {
        private static FastGraph _ground;
        private static FastGraph _air;
        private static PerfStats _groundStatsOld;
        private static PerfStats _groundStatsNew;
        private static PerfStats _airStatsOld;
        private static PerfStats _airStatsNew;
        private static bool _perfToggle;

        public static void InitHooks()
        {
            On.RoR2.SceneInfo.Awake += SceneInfo_Awake;
            On.RoR2.Navigation.NodeGraph.FindClosestNode += NodeGraph_FindClosestNode;
        }

        private static void SceneInfo_Awake(On.RoR2.SceneInfo.orig_Awake orig, SceneInfo self)
        {
            orig(self);

            Debug.Log("Preparing fast node sets for scene " + self.sceneDef?.nameToken);
            _ground = null;
            _air = null;
            if (self.groundNodes)
                _ground = new FastGraph(self.groundNodes);
            if (self.airNodes)
                _air = new FastGraph(self.airNodes);

            if (_ground == null)
                Debug.Log("No ground nodes loaded");
            if (_air == null)
                Debug.Log("No air nodes loaded");

            _groundStatsOld = new PerfStats("Ground FindClosestNode (Old)");
            _groundStatsNew = new PerfStats("Ground FindClosestNode (New)");
            _airStatsOld = new PerfStats("Air FindClosestNode (Old)");
            _airStatsNew = new PerfStats("Air FindClosestNode (New)");
        }

        private static NodeGraph.NodeIndex NodeGraph_FindClosestNode(On.RoR2.Navigation.NodeGraph.orig_FindClosestNode orig, NodeGraph self, Vector3 position, HullClassification hullclassification)
        {
            if (self == _ground?.Graph)
            {
                NodeGraph.NodeIndex result;
                if (_perfToggle)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    result = _ground.FindClosestNode2(position, hullclassification);
                    _groundStatsNew.AddEvent(stopwatch.ElapsedTicks);
                }
                else
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    result = orig(self, position, hullclassification);
                    _groundStatsOld.AddEvent(stopwatch.ElapsedTicks);
                }

                _perfToggle = !_perfToggle;
                return result;
            }
            
            if (self == _air?.Graph)
            {
                NodeGraph.NodeIndex result;
                if (_perfToggle)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    result = _air.FindClosestNode2(position, hullclassification);
                    _airStatsNew.AddEvent(stopwatch.ElapsedTicks);
                }
                else
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    result = orig(self, position, hullclassification);
                    _airStatsOld.AddEvent(stopwatch.ElapsedTicks);
                }

                _perfToggle = !_perfToggle;
                return result;
            }

            return orig(self, position, hullclassification);
        }

        private sealed class FastGraph
        {
            public FastGraph(NodeGraph graph)
            {
                Graph = graph;
                var graphNodes = graph.GetFieldValue<NodeGraph.Node[]>("nodes");
                Nodes = new FastNodeSet(graphNodes);
                Nodes2 = new FastNodeSet2(graphNodes);
                OpenGates = graph.GetFieldValue<bool[]>("openGates");
            }

            public NodeGraph Graph { get; }

            public FastNodeSet Nodes { get; }

            public FastNodeSet2 Nodes2 { get; }

            public bool[] OpenGates { get; }

            public NodeGraph.NodeIndex FindClosestNode(Vector3 position, HullClassification hullClassification)
            {
                var result = NodeGraph.NodeIndex.invalid;
                var mask = (HullMask) (1 << (int)hullClassification);
                var minDistSqr = float.PositiveInfinity;
                foreach (var nodeInfo in Nodes.GetNodeInfoNearCentered(position))
                {
                    if (nodeInfo.DeltaR*nodeInfo.DeltaR > minDistSqr)
                        continue;

                    var node = nodeInfo.Node;
                    if ((node.forbiddenHulls & mask) == HullMask.None && (node.gateIndex == 0 || OpenGates[node.gateIndex]))
                    {
                        float sqrMagnitude = (node.position - position).sqrMagnitude;
                        if (sqrMagnitude < minDistSqr)
                        {
                            minDistSqr = sqrMagnitude;
                            result = new NodeGraph.NodeIndex(nodeInfo.Index);
                        }
                    }
                }

                return result;
            }

            public NodeGraph.NodeIndex FindClosestNode2(Vector3 position, HullClassification hullClassification)
            {
                return Nodes2.FindClosestNode(position, OpenGates, hullClassification);
            }
        }

        private sealed class PerfStats
        {
            private const int LogFrequency = 200;

            private long _accTime;
            private int _accCount;
            private string _name;

            public PerfStats(string name)
            {
                _name = name;
            }

            public void AddEvent(long ticks)
            {
                _accTime += ticks;
                _accCount++;

                if (_accCount == LogFrequency)
                {
                    var ms = TimeSpan.FromTicks(_accTime).TotalMilliseconds;
                    Debug.Log($"{_name}: {LogFrequency} runs took {ms:0.0} ms");
                    _accTime = 0;
                    _accCount = 0;
                }
            }
        }
    }
}
