using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class AsteroidSwarmSettings
    {
        public AsteroidSwarmSettings()
        {
            TotalCount = 200;
            CountPerWave = 10;
            WaveInterval = 0.15f;
            SwarmInterval = 10.0f;
            ProjectileSpeed = 60f;
            ProjectileSpeedRange = 10f;
            StartRadius = 250f;
            StartRadiusRange = 30f;
        }

        public int TotalCount { get; set; }

        public int CountPerWave { get; set; }

        public float WaveInterval { get; set; }

        public float SwarmInterval { get; set; }

        public float ProjectileSpeed { get; set; }

        public float ProjectileSpeedRange { get; set; }

        public float StartRadius { get; set; }

        public float StartRadiusRange { get; set; }
    }
}
