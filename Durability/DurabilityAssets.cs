using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace JarlykMods.Durability
{
    public static class DurabilityAssets
    {
        public static void Init()
        {
            if (Loaded)
                return;

            Loaded = true;
            var execAssembly = Assembly.GetExecutingAssembly();
            using (var stream = execAssembly.GetManifestResourceStream("JarlykMods.Durability.durability.assets"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                DurabilityBarPrefab = bundle.LoadAsset<GameObject>("Assets/UI/DurabilityBar.prefab");
            }
        }

        public static bool Loaded { get; private set; }

        public static GameObject DurabilityBarPrefab { get; private set; }
    }
}
