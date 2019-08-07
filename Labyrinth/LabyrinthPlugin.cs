using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;

// ReSharper disable UnusedMember.Local

namespace JarlykMods.Labyrinth
{
    [BepInPlugin(PluginGuid, "Labyrinth", "0.0.1")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    public sealed class LabyrinthPlugin
    {
        public const string PluginGuid = "com.jarlyk.labyrinth";

        public LabyrinthPlugin()
        {
        }
    }
}
