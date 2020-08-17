using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using R2API;
using R2API.AssetPlus;

namespace JarlykMods.Raincoat
{
    public static class RaincoatAssets
    {
        //public static string Prefix = "@JarlykModsRaincoat:";

        public static void Init()
        {
            if (Loaded)
                return;

            Loaded = true;
            var execAssembly = Assembly.GetExecutingAssembly();
            using (var stream = execAssembly.GetManifestResourceStream("JarlykMods.Raincoat.raincoat.assets"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                //var provider = new AssetBundleResourcesProvider(Prefix.TrimEnd(':'), bundle);
                //ResourcesAPI.AddProvider(provider);

                ArtifactMerchActiveIcon = bundle.LoadAsset<Sprite>("Assets/Icons/ArtifactMerchActiveIcon.png");
                ArtifactMerchInactiveIcon = bundle.LoadAsset<Sprite>("Assets/Icons/ArtifactMerchInactiveIcon.png");
            }
        }

        public static bool Loaded { get; private set; }

        public static Sprite ArtifactMerchActiveIcon { get; private set; }

        public static Sprite ArtifactMerchInactiveIcon { get; private set; }
    }
}
