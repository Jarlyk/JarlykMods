using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using AssetPlus;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    public static class HailstormAssets
    {
        public static void Init()
        {
            if (Loaded)
                return;

            Loaded = true;
            var execAssembly = Assembly.GetExecutingAssembly();
            using (var stream = execAssembly.GetManifestResourceStream("JarlykMods.Hailstorm.hailstorm"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                DarknessShader = bundle.LoadAsset<Shader>("Assets/Effects/darkness.shader");

                PureBlack = bundle.LoadAsset<Material>("Assets/Materials/PureBlack.mat");
                PurpleCracks = bundle.LoadAsset<Material>("Assets/Materials/PurpleCracks.mat");
                IconBarrierElite = bundle.LoadAsset<Sprite>("Assets/Icons/BarrierEliteIcon.png");
                IconDarkElite = bundle.LoadAsset<Sprite>("Assets/Icons/DarkEliteIcon.png");

                TwisterVisualPrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/TwisterVisual.prefab");
                TwisterPrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/Twister.prefab");
                TwisterProjectileController.AugumentPrefab(TwisterPrefab);

                BarrierMaterial = Resources.Load<GameObject>("Prefabs/TemporaryVisualEffects/barriereffect")
                                           .GetComponentInChildren<MeshRenderer>().material;
            }

            using (var bankStream = execAssembly.GetManifestResourceStream("JarlykMods.Hailstorm.Hailstorm.bnk"))
            {
                var bytes = new byte[bankStream.Length];
                bankStream.Read(bytes, 0, bytes.Length);
                SoundBanks.Add(bytes);
            }

            On.RoR2.Networking.GameNetworkManager.OnStartClient += GameNetworkManager_OnStartClient;
        }

        public static bool Loaded { get; private set; }

        public static Shader DarknessShader { get; private set; }

        public static Material PureBlack { get; private set; }

        public static Material PurpleCracks { get; private set; }

        public static Material BarrierMaterial { get; private set; }

        public static Sprite IconBarrierElite { get; private set; }

        public static Sprite IconDarkElite { get; private set; }

        public static GameObject TwisterVisualPrefab { get; private set; }

        public static GameObject TwisterPrefab { get; private set; }

        private static void GameNetworkManager_OnStartClient(On.RoR2.Networking.GameNetworkManager.orig_OnStartClient orig, GameNetworkManager self, NetworkClient newClient)
        {
            orig(self, newClient);
            var h = new NetworkHash128();
            ClientScene.RegisterPrefab(TwisterPrefab, NetworkHash128.Parse("9725011d8b662d98"));
        }
    }
}
