using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using AssetPlus;
using UnityEngine;

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
            var stream = execAssembly.GetManifestResourceStream("JarlykMods.Hailstorm.hailstorm");
            var bundle = AssetBundle.LoadFromStream(stream);
            DarknessShader = bundle.LoadAsset<Shader>("Assets/Effects/darkness.shader");

            var affixRed = Resources.Load<Material>("Materials/matAffixRed");
            BlackRim = new Material(affixRed);
            BlackRim.SetFloat("_RimPower", 6);
            BlackRim.SetColor("_RimColor", new Color32(0, 0, 0, 255));
            PureBlack = bundle.LoadAsset<Material>("Assets/Materials/PureBlack.mat");
            PurpleCracks = bundle.LoadAsset<Material>("Assets/Materials/PurpleCracks.mat");

            using (var bankStream = execAssembly.GetManifestResourceStream("JarlykMods.Hailstorm.Hailstorm.bnk"))
            {
                var bytes = new byte[bankStream.Length];
                bankStream.Read(bytes, 0, bytes.Length);
                SoundBanks.Add(bytes);
            }
        }

        public static bool Loaded { get; private set; }

        public static Shader DarknessShader { get; private set; }

        public static Material BlackRim { get; private set; }

        public static Material PureBlack { get; private set; }

        public static Material PurpleCracks { get; private set; }
    }
}
