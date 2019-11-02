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
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    [BepInPlugin(PluginGuid, "Hailstorm", "0.3.2")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(EsoPlugin.PluginGuid)]
    public sealed class HailstormPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.hailstorm";

        private readonly DarkElitesManager _darkElites;
        private readonly BarrierElitesManager _barrierElites;
        private readonly StormElitesManager _stormElites;
        private readonly Mimics _mimics;
        private readonly Xoroshiro128Plus _rng;

        public HailstormPlugin()
        {
            HailstormConfig.Init(Config);
            HailstormAssets.Init();

            if (HailstormConfig.EnableDarkElites.Value)
                _darkElites = new DarkElitesManager();

            if (HailstormConfig.EnableBarrierElites.Value)
                _barrierElites = new BarrierElitesManager();

            if (HailstormConfig.EnableStormElites.Value)
                _stormElites = new StormElitesManager();

            if (HailstormConfig.EnableMimics.Value)
                _mimics = new Mimics();

            _rng = new Xoroshiro128Plus((ulong) DateTime.Now.Ticks);

            R2API.Utils.CommandHelper.AddToConsoleWhenReady();

            IL.RoR2.EliteCatalog.cctor += EliteCatalogTypeInitializer;
        }

        private void EliteCatalogTypeInitializer(ILContext il)
        {
            var c = new ILCursor(il);
            c.Index = 2;

            //If modHelper is not null, skip creating it
            c.Emit(OpCodes.Ldsfld,
                   typeof(EliteCatalog).GetField("modHelper", BindingFlags.Static | BindingFlags.Public));
            var skipCreate = c.DefineLabel();
            c.Emit(OpCodes.Brtrue, skipCreate);

            c.GotoNext(MoveType.After,
                       i => i.MatchStsfld("RoR2.EliteCatalog", "modHelper"));
            c.MarkLabel(skipCreate);
        }

        private void Awake()
        {
            _darkElites?.Awake();
            _barrierElites?.Awake();
        }

        public void Start()
        {
            typeof(BuffCatalog).SetPropertyValue("buffCount", typeof(BuffCatalog).GetFieldValue<BuffDef[]>("buffDefs").Length);
            typeof(EliteCatalog).TypeInitializer.Invoke(null, null);
            _barrierElites?.Start();
        }

        private void Update()
        {
            _darkElites?.Update();
            _barrierElites?.Update();
        }

        [ConCommand(commandName = "hs_cataclysm", flags = ConVarFlags.ExecuteOnServer,
            helpText = "Test the Cataclysm")]
        private static void TestCataclysm(ConCommandArgs args)
        {
            var cataclysm = new CataclysmManager();
            cataclysm.LoadCataclysm();
        }

        [ConCommand(commandName = "hs_gravbomb", flags = ConVarFlags.ExecuteOnServer,
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

        [ConCommand(commandName = "hs_bossphase", flags = ConVarFlags.ExecuteOnServer,
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

        [ConCommand(commandName = "hs_gearup", flags = ConVarFlags.ExecuteOnServer, 
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
                        inv.RemoveItem(item, int.MaxValue);
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
