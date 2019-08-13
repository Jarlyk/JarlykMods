using System;
using BepInEx;

namespace EliteSpawningOverhaul
{
    [BepInPlugin(PluginGuid, "EliteSpawningOverhaul", "0.1.0")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    public sealed class EsoPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.eso";

        public EsoPlugin()
        {
            EsoLib.Init();
        }
    }
}
