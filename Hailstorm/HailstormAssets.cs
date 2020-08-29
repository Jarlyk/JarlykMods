using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using R2API;
using RoR2;
using Path = System.IO.Path;

namespace JarlykMods.Hailstorm
{
    public static class HailstormAssets
    {
        public static string Prefix = "@JarlykModsHailstorm:";

        public static string IconBarrierElite = Prefix + "Assets/Icons/BarrierEliteIcon.png";
        public static string IconDarkElite = Prefix + "Assets/Icons/DarkEliteIcon.png";
        public static string IconStormElite = Prefix + "Assets/Icons/StormEliteIcon.png";

        private static Stream OpenAssets(Assembly execAssembly, string prefix, string filename)
        {
            //First check if there's an override file in the same folder as the plugin
            var path = Path.Combine(Path.GetDirectoryName(execAssembly.Location), filename);
            if (File.Exists(path))
                return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            return execAssembly.GetManifestResourceStream(prefix + "." + filename);
        }

        public static void Init()
        {
            if (Loaded)
                return;

            Loaded = true;
            var execAssembly = Assembly.GetExecutingAssembly();
            using (var stream = OpenAssets(execAssembly, "JarlykMods.Hailstorm", "hailstorm.assets"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                var provider = new AssetBundleResourcesProvider(Prefix.TrimEnd(':'), bundle);
                ResourcesAPI.AddProvider(provider);

                DarknessShader = bundle.LoadAsset<Shader>("Assets/Effects/darkness.shader");

                PureBlack = bundle.LoadAsset<Material>("Assets/Materials/PureBlack.mat");
                PurpleCracks = bundle.LoadAsset<Material>("Assets/Materials/PurpleCracks.mat");

                TwisterVisualPrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/TwisterVisual.prefab");
                TwisterPrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/Twister.prefab");
                TwisterProjectileController.AugumentPrefab(TwisterPrefab);

                BarrierMaterial = Resources.Load<GameObject>("Prefabs/TemporaryVisualEffects/barriereffect")
                                           .GetComponentInChildren<MeshRenderer>().material;

                DistortionQuad = bundle.LoadAsset<GameObject>("Assets/Prefabs/DistortionQuad.prefab");
            }

            using (var stream = OpenAssets(execAssembly, "JarlykMods.Hailstorm", "mimic.assets"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                MimicModel = bundle.LoadAsset<GameObject>("mdlMimic");
                MimicMaterial = bundle.LoadAsset<Material>("matMimic");
                MimicBiteEffect = bundle.LoadAsset<GameObject>("MimicBiteEffect");
            }

            using (var bankStream = execAssembly.GetManifestResourceStream("JarlykMods.Hailstorm.Hailstorm.bnk"))
            {
                var bytes = new byte[bankStream.Length];
                bankStream.Read(bytes, 0, bytes.Length);
                SoundAPI.SoundBanks.Add(bytes);
            }

            On.RoR2.Networking.GameNetworkManager.OnStartClient += GameNetworkManager_OnStartClient;
        }

        public static bool Loaded { get; private set; }

        public static Shader DarknessShader { get; private set; }

        public static Material PureBlack { get; private set; }

        public static Material PurpleCracks { get; private set; }

        public static Material BarrierMaterial { get; private set; }

        public static GameObject TwisterVisualPrefab { get; private set; }

        public static GameObject TwisterPrefab { get; private set; }

        public static GameObject DistortionQuad { get; private set; }

        public static GameObject MimicModel { get; private set; }

        public static Material MimicMaterial { get; private set; }

        public static GameObject MimicBiteEffect { get; private set; }

        private static void GameNetworkManager_OnStartClient(On.RoR2.Networking.GameNetworkManager.orig_OnStartClient orig, GameNetworkManager self, NetworkClient newClient)
        {
            orig(self, newClient);
            ClientScene.RegisterPrefab(TwisterPrefab, NetworkHash128.Parse("9725011d8b662d98"));

            //For convenience: pre-generated random IDs that can be used later
            //164497abc3b46e41
            //bfd424a1ac1b07ce
            //7c3cba1b92427f72
            //0ab8e989d1c6c999
            //0e90dedbde0f4b5a
            //a2177e2b92a917c3
            //0327fff31597212d
            //6cd812cea49b73c5
        }
    }
}
