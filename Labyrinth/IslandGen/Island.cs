using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JarlykMods.Labyrinth.IslandGen
{
    public sealed class Island
    {
        public Vector3 Position { get; set; }

        public int SizeX { get; private set; }

        public int SizeZ { get; private set; }

        public Mesh BuildGeometry()
        {
            throw new NotImplementedException();
        }
    }
}
