using BepInEx.Configuration;
using UnityEngine;

namespace CombatDirectorTweaks
{
    public static class TweaksConfig
    {
        public static void Init(ConfigFile config)
        {
            MaxEnemyCount = config.Bind(
                "Settings",
                "MaxEnemyCount",
                40,
                "How many total enemies is the CombatDirector allowed to spawn (default: 40)");
        }

        public static ConfigEntry<int> MaxEnemyCount;
    }
}
