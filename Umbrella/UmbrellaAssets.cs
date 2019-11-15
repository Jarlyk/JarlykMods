using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using R2API;
using R2API.AssetPlus;
using UnityEngine;

namespace JarlykMods.Umbrella
{
    public static class UmbrellaAssets
    {
        public static string Prefix = "@JarlykModsUmbrella:";

        public static string PrefabBulletTimer = Prefix + "Assets/Import/bullet_timer/BulletTimer.prefab";
        public static string PrefabJestersDice = Prefix + "Assets/Prefabs/JestersDice.prefab";
        public static string IconBulletTimer = Prefix + "Assets/Import/bullet_timer/BulletTimer.png";
        public static string IconJestersDice = Prefix + "Assets/Icons/JestersDiceIcon.png";

        public static void Init()
        {
            if (Loaded)
                return;

            Loaded = true;
            var execAssembly = Assembly.GetExecutingAssembly();
            using (var stream = execAssembly.GetManifestResourceStream("JarlykMods.Umbrella.umbrella.assets"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                var provider = new AssetBundleResourcesProvider(Prefix.TrimEnd(':'), bundle);
                ResourcesAPI.AddProvider(provider);

                BulletTimerPrefab = bundle.LoadAsset<GameObject>("Assets/Import/bullet_timer/BulletTimer.prefab");
                JestersDicePrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/JestersDice.prefab");
            }

            using (var bankStream = execAssembly.GetManifestResourceStream("JarlykMods.Umbrella.Umbrella.bnk"))
            {
                var bytes = new byte[bankStream.Length];
                bankStream.Read(bytes, 0, bytes.Length);
                SoundBanks.Add(bytes);
            }
        }

        public static bool Loaded { get; private set; }

        public static GameObject BulletTimerPrefab { get; private set; }

        public static GameObject JestersDicePrefab { get; private set; }
    }
}
