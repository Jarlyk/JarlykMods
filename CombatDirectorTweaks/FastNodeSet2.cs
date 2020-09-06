using System;
using System.Collections.Generic;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace CombatDirectorTweaks
{
    public sealed class FastNodeSet2
    {
        private readonly NodeGraph.Node[] _nodes;
        private readonly int[] _indices;
        private readonly int[] _xStart;
        private readonly float _xMin;

        public FastNodeSet2(IReadOnlyList<NodeGraph.Node> nodes)
        {
            int n = nodes.Count;
            _nodes = new NodeGraph.Node[n];
            _indices = new int[n];
            var x = new float[n];

            for (int i = 0; i < n; i++)
            {
                _indices[i] = i;
                x[i] = nodes[i].position.x;
            }

            //Sort the nodes by their X position
            Array.Sort(x, _indices);
            for (int i = 0; i < n; i++)
                _nodes[i] = nodes[_indices[i]];

            //Now create bins for the various X values
            _xMin = x[0];
            var xRange = (int)Mathf.Floor(x[n - 1] - _xMin);
            _xStart = new int[xRange + 1];
            int k = 0;
            for (int i = 0; i <= xRange; i++)
            {
                while (k < n && x[k] < i)
                    k++;

                _xStart[i] = k;
            }
        }

        public NodeGraph.NodeIndex FindClosestNode(Vector3 position, bool[] openGates, HullClassification hullClassification)
        {
            int n = _indices.Length;
            var x = (int)Math.Floor(position.x - _xMin);
            if (x < 0) x = 0;
            if (x >= _xStart.Length) x = _xStart.Length - 1;
            int i0 = _xStart[x];

            var result = NodeGraph.NodeIndex.invalid;
            var mask = (HullMask)(1 << (int)hullClassification);
            var minDistSqr = float.PositiveInfinity;

            int iDown = i0 - 1;
            int iUp = i0;

            bool downGoing = iDown >= 0;
            bool upGoing = iUp < n;
            while (downGoing || upGoing)
            {
                if (downGoing)
                {
                    var node = _nodes[iDown];
                    var dx = position.x - node.position.x;
                    if (dx * dx >= minDistSqr)
                    {
                        downGoing = false;
                    }
                    else
                    {
                        if ((node.forbiddenHulls & mask) == HullMask.None && (node.gateIndex == 0 || openGates[node.gateIndex]))
                        {
                            float sqrMagnitude = (node.position - position).sqrMagnitude;
                            if (sqrMagnitude < minDistSqr)
                            {
                                minDistSqr = sqrMagnitude;
                                result = new NodeGraph.NodeIndex(_indices[iDown]);
                            }
                        }

                        iDown--;
                        downGoing = iDown >= 0;
                    }
                }

                if (upGoing)
                {
                    var node = _nodes[iUp];
                    var dx = node.position.x - position.x;
                    if (dx * dx >= minDistSqr)
                    {
                        upGoing = false;
                    }
                    else
                    {
                        if ((node.forbiddenHulls & mask) == HullMask.None && (node.gateIndex == 0 || openGates[node.gateIndex]))
                        {
                            float sqrMagnitude = (node.position - position).sqrMagnitude;
                            if (sqrMagnitude < minDistSqr)
                            {
                                minDistSqr = sqrMagnitude;
                                result = new NodeGraph.NodeIndex(_indices[iDown]);
                            }
                        }

                        iUp++;
                        upGoing = iUp < n;
                    }
                }
            }

            return result;
        }
    }
}