using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JarlykMods.Labyrinth.IslandGen
{
    public sealed class Pillar
    {
        public Vector3 Position { get; set; }

        public float Diameter { get; set; }

        public float Height { get; set; }

        public Mesh BuildGeometry()
        {
            throw new NotImplementedException();
        }
    }
}
