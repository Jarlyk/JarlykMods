using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx;
using EliteSpawningOverhaul;
using JarlykMods.Hailstorm.Cataclysm;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    [BepInPlugin(PluginGuid, "Cataclysm", "0.4.1")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(EsoPlugin.PluginGuid)]
    [R2APISubmoduleDependency(nameof(CommandHelper))]
    public sealed class CataclysmPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.cataclysm";

        private readonly Xoroshiro128Plus _rng;

        public CataclysmPlugin()
        {
            CatclysmConfig.Init(Config);
            CataclysmAssets.Init();

            _rng = new Xoroshiro128Plus((ulong) DateTime.Now.Ticks);

            CommandHelper.AddToConsoleWhenReady();
        }

        [ConCommand(commandName = "ca_cataclysm", flags = ConVarFlags.ExecuteOnServer,
            helpText = "Test the Cataclysm")]
        private static void TestCataclysm(ConCommandArgs args)
        {
            var cataclysm = new CataclysmManager();
            cataclysm.LoadCataclysm();
        }

        [ConCommand(commandName = "ca_gravbomb", flags = ConVarFlags.ExecuteOnServer,
            helpText = "Spawn a grav bomb where you're standing")]
        private static void GravBomb(ConCommandArgs args)
        {
            var user = LocalUserManager.GetFirstLocalUser();
            var body = user.cachedBody;
            if (body?.master == null)
            {
                Debug.LogError("Cannot find local user body!");
                return;
            }

            GravBombEffect.Spawn(body.corePosition + 5f*Vector3.up, 6);
        }

        [ConCommand(commandName = "ca_bossphase", flags = ConVarFlags.ExecuteOnServer,
            helpText = "Set Boss Phase for Cataclysm fight")]
        private static void SetBossPhase(ConCommandArgs args)
        {
            var bossFight = CataclysmManager.BossFight;
            if (bossFight == null)
            {
                Debug.LogWarning("Boss fight is not currently active");
                return;
            }

            if (args.Count < 1)
            {
                Debug.LogWarning("Must specify phase as argument; enum name or integer value are both acceptable");
                return;
            }

            if (!Enum.TryParse<BossPhase>(args.userArgs[0], out var phase))
            {
                Debug.LogWarning($"{args.userArgs[0]} is not a valid boss phase");
                return;
            }

            bossFight.SetPhase(phase);
        }

        [ConCommand(commandName = "ca_gearup", flags = ConVarFlags.ExecuteOnServer, 
            helpText = "Provide random late-game gear to help test Cataclysm boss fight; run again to reroll.  May optionally specify number of [white] [green] [red] items as space-delimited arguments")]
        private static void GearUp(ConCommandArgs args)
        {
            int whites = 80;
            int greens = 20;
            int reds = 5;
            if (args.Count == 3)
            {
                bool success = int.TryParse(args[0], out whites);
                success &= int.TryParse(args[1], out greens);
                success &= int.TryParse(args[2], out reds);

                if (!success)
                {
                    Debug.LogWarning("Invalid parameters: specify space delivered number of items for [white] [green] [red].");
                    return;
                }
            }

            var rng = new Xoroshiro128Plus((ulong) DateTime.Now.Ticks);

            //Gearing applies to all players
            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                //Get inventory, accommodating case where body doesn't exist yet
                var inv = controller.master.GetBody()?.inventory;
                if (inv != null)
                {
                    //Start by clearing existing inventory
                    for (var item = ItemIndex.Syringe; item < (ItemIndex)ItemCatalog.itemCount; item++)
                    {
                        int count = inv.GetItemCount(item);
                        if (count > 0)
                            inv.RemoveItem(item, count);
                    }

                    //Grant items of each rarity
                    for (int i=0; i < whites; i++)
                        inv.GiveItem(PickupCatalog.GetPickupDef(rng.NextElementUniform(Run.instance.availableTier1DropList)).itemIndex);
                    for (int i = 0; i < greens; i++)
                        inv.GiveItem(PickupCatalog.GetPickupDef(rng.NextElementUniform(Run.instance.availableTier2DropList)).itemIndex);
                    for (int i = 0; i < reds; i++)
                        inv.GiveItem(PickupCatalog.GetPickupDef(rng.NextElementUniform(Run.instance.availableTier3DropList)).itemIndex);
                }
            }
        }
    }
}
