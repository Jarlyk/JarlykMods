using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using JarlykMods.Hailstorm.Cataclysm;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using R2API;

namespace JarlykMods.Hailstorm
{
    public static class HailstormAssets
    {
        public static string Prefix = "@JarlykMods.Hailstorm:";

        public static string IconBarrierElite = Prefix + "Assets/Icons/BarrierEliteIcon.png";
        public static string IconDarkElite = Prefix + "Assets/Icons/DarkEliteIcon.png";
        public static string IconStormElite = Prefix + "Assets/Icons/StormEliteIcon.png";

        public static void Init()
        {
            if (Loaded)
                return;

            Loaded = true;
            var execAssembly = Assembly.GetExecutingAssembly();
            using (var stream = execAssembly.GetManifestResourceStream("JarlykMods.Hailstorm.hailstorm.assets"))
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

                CataclysmPlatformPrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/CataclysmPlatform.prefab");
                CataclysmPlatformPrefab.AddComponent<MobilePlatform>();

                CataclysmSkyboxMaterial = bundle.LoadAsset<Material>("Assets/SpaceSkies Free/Skybox_3/Purple_4K_Resolution.mat");
                CataclysmArenaPrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/CataclysmArena.prefab");

                GravBombPrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/GravBomb.prefab");
                GravBombEffect.AugmentPrefab(GravBombPrefab);

                AsteroidProjectilePrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/AsteroidProjectile.prefab");
                AsteroidProjectileController.AugmentPrefab(AsteroidProjectilePrefab);

                LaserChargerPrefab = bundle.LoadAsset<GameObject>("Assets/Prefabs/LaserCharger.prefab");
                LaserChargerInteraction.AugmentPrefab(LaserChargerPrefab);
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

        public static GameObject TwisterVisualPrefab { get; private set; }

        public static GameObject TwisterPrefab { get; private set; }

        public static GameObject CataclysmPlatformPrefab { get; private set; }

        public static Material CataclysmSkyboxMaterial { get; private set; }

        public static GameObject CataclysmArenaPrefab { get; private set; }

        public static GameObject GravBombPrefab { get; private set; }

        public static GameObject AsteroidProjectilePrefab { get; private set; }

        public static GameObject LaserChargerPrefab { get; private set; }

        private static void GameNetworkManager_OnStartClient(On.RoR2.Networking.GameNetworkManager.orig_OnStartClient orig, GameNetworkManager self, NetworkClient newClient)
        {
            orig(self, newClient);
            ClientScene.RegisterPrefab(TwisterPrefab, NetworkHash128.Parse("9725011d8b662d98"));
            ClientScene.RegisterPrefab(GravBombPrefab, NetworkHash128.Parse("6d803141bb60b3f7"));
            ClientScene.RegisterPrefab(AsteroidProjectilePrefab, NetworkHash128.Parse("34eddec13b017082"));

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
