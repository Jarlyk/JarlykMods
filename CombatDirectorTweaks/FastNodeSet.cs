using System;
using System.Collections.Generic;
using System.Text;
using RoR2.Navigation;
using Unity.Jobs;
using UnityEngine;

namespace CombatDirectorTweaks
{
    public sealed class FastNodeSet
    {
        private readonly Vector3 _origin;
        private readonly NodeGraph.Node[] _nodes;
        private readonly int[] _indices;
        private readonly float[] _radii;

        public FastNodeSet(IReadOnlyList<NodeGraph.Node> nodes)
        {
            int n = nodes.Count;
            _nodes = new NodeGraph.Node[n];
            _indices = new int[n];
            _radii = new float[n];

            //Compute all node positions relative to the mean origin
            _origin = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                _origin += nodes[i].position;
            }

            _origin = (1.0f / n) * _origin;
            for (int i = 0; i < n; i++)
            {
                _indices[i] = i;
                _radii[i] = (nodes[i].position - _origin).magnitude;
            }

            //Sort the nodes by their distance from the origin
            Array.Sort(_radii, _indices);
            for (int i = 0; i < n; i++)
                _nodes[i] = nodes[_indices[i]];
        }

        public IEnumerable<NodeInfo> GetNodeInfoNearCentered(Vector3 position)
        {
            var r0 = (position - _origin).magnitude;

            //Determine nearest circle
            var i0 = FindIndexBelow(r0);

            //Now yield nodes starting from the center of the range and working outward
            int n = Mathf.Max(i0, _radii.Length - i0);
            for (int k = 1; k <= n; k++)
            {
                var iDown = i0 - k;
                if (iDown >= 0)
                    yield return new NodeInfo(_nodes[iDown], _indices[iDown], r0 - _radii[iDown]);

                var iUp = i0 + k - 1;
                if (iUp < _radii.Length)
                    yield return new NodeInfo(_nodes[iUp], _indices[iUp], _radii[iUp] - r0);
            }
        }

        public IEnumerable<NodeGraph.Node> GetNodesNearCentered(Vector3 position)
        {
            var r0 = (position - _origin).magnitude;

            //Determine nearest circle
            var i0 = FindIndexBelow(r0);

            //If this is beyond all nodes, nothing to return
            if (i0 == _radii.Length)
                yield break;

            //Now yield nodes starting from the center of the range and working outward
            int n = Mathf.Max(i0, _radii.Length - i0);
            for (int k = 1; k <= n; k++)
            {
                var iDown = i0 - k;
                if (iDown >= 0)
                    yield return _nodes[iDown];

                var iUp = i0 + k - 1;
                if (iUp < _radii.Length)
                    yield return _nodes[iUp];
            }
        }

        public IEnumerable<NodeGraph.Node> GetNodesNearCentered(Vector3 position, float maxRadius)
        {
            var r0 = (position - _origin).magnitude;

            //Determine inner circle
            var iMin = FindIndexBelow(r0 - maxRadius);

            //If this is beyond all nodes, nothing to return
            if (iMin == _radii.Length)
                yield break;

            //Determine outer circle
            var iMax = FindIndexAbove(r0 + maxRadius);

            //Now yield nodes starting from the center of the range and working outward
            var i0 = (iMin + iMax) / 2;
            for (int k = 1; k < (iMax - iMin + 1) / 2; k++)
            {
                var iDown = i0 - k;
                if (iDown >= iMin)
                    yield return _nodes[iDown];

                var iUp = i0 + k - 1;
                if (iUp < iMax)
                    yield return _nodes[iUp];
            }
        }

        public IEnumerable<NodeGraph.Node> GetNodesNear(Vector3 position, float maxRadius)
        {
            var r0 = (position - _origin).magnitude;

            //Determine inner circle
            var iMin = FindIndexBelow(r0 - maxRadius);

            //If this is beyond all nodes, nothing to return
            if (iMin == _radii.Length)
                yield break;

            //Determine outer circle
            var iMax = FindIndexAbove(r0 + maxRadius);

            //Yield nodes directly in original sorted order
            for (int i = iMin; i < iMax; i++)
                yield return _nodes[i];
        }

        private int FindIndexBelow(float r)
        {
            int index = Array.BinarySearch(_radii, r);
            if (index < 0)
            {
                index = ~index + 1;
            }

            return index;
        }

        private int FindIndexAbove(float r)
        {
            int index = Array.BinarySearch(_radii, r + 0.1f);
            if (index < 0)
            {
                index = ~index + 1;
            }

            return Mathf.Min(index, _radii.Length - 1);
        }

        public readonly struct NodeInfo
        {
            public NodeInfo(NodeGraph.Node node, int index, float dr)
            {
                Node = node;
                Index = index;
                DeltaR = dr;
            }

            public NodeGraph.Node Node { get; }

            public int Index { get; }

            public float DeltaR { get; }
        }

    }
}
