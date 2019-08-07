using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JarlykMods.Hailstorm.hailstorm");
            var bundle = AssetBundle.LoadFromStream(stream);
            DarknessShader = bundle.LoadAsset<Shader>("Assets/Effects/darkness.shader");

            var affixRed = Resources.Load<Material>("Materials/matAffixRed");
            BlackRim = new Material(affixRed);
            BlackRim.SetFloat("_RimPower", 6);
            BlackRim.SetColor("_RimColor", new Color32(0, 0, 0, 255));
            //PureBlack = bundle.LoadAsset<Material>("Assets/Materials/PureBlack.mat");
        }

        public static bool Loaded { get; private set; }

        public static Shader DarknessShader { get; private set; }

        public static Material BlackRim { get; private set; }
    }
}
