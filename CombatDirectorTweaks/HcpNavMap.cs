using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = System.Object;

namespace CombatDirectorTweaks
{
    public sealed class HcpNavMap
    {
        private readonly bool[] _occupied;
        private readonly int _rowSize;
        private readonly int _planeSize;
        private readonly Bounds _bounds;
        private readonly float _resolution;
        private readonly float _resR2;
        private readonly float _resR3;

        public HcpNavMap(Bounds bounds, float resolution)
        {
            _bounds = bounds;
            _resolution = resolution;
            _resR2 = resolution/Mathf.Sqrt(2);
            _resR3 = resolution/Mathf.Sqrt(3);
            _rowSize = Mathf.FloorToInt(bounds.size.x/resolution);
            _planeSize = _rowSize*Mathf.FloorToInt(bounds.size.z/resolution);

            var n = _planeSize*Mathf.FloorToInt(bounds.size.y/resolution);
            _occupied = new bool[n];
        }

        public enum NodeIndex : int { }
    }
}
