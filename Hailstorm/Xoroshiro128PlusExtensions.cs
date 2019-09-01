using System;
using System.Collections.Generic;
using System.Text;

namespace JarlykMods.Hailstorm
{
    public static class Xoroshiro128PlusExtensions
    {
        public static float PlusMinus(this Xoroshiro128Plus rng, float range)
        {
            return 2*(rng.nextNormalizedFloat - 0.5f)*range;
        }
    }
}
