using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AssetPlus;
using UnityEngine;

namespace JarlykMods.Umbrella
{
    public static class UmbrellaAssets
    {
        public static void Init()
        {
            if (Loaded)
                return;

            Loaded = true;
            var execAssembly = Assembly.GetExecutingAssembly();
            using (var stream = execAssembly.GetManifestResourceStream("JarlykMods.Umbrella.umbrella"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                BulletTimerPrefab = bundle.LoadAsset<GameObject>("Assets/Import/bullet_timer/BulletTimer.prefab");
                BulletTimerIcon = bundle.LoadAsset<UnityEngine.Object>("Assets/Import/bullet_timer/BulletTimer.png");

                JestersDicePrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/JestersDice.prefab");
                JestersDiceIcon = bundle.LoadAsset<UnityEngine.Object>("Assets/Icons/JestersDiceIcon.png");
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

        public static UnityEngine.Object BulletTimerIcon { get; private set; }

        public static GameObject JestersDicePrefab { get; private set; }

        public static UnityEngine.Object JestersDiceIcon { get; private set; }
    }
}
