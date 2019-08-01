using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Raincoat
{
    public static class StarterPack
    {
        public static void GrantStarterItemsToAll()
        {
            if (!NetworkServer.active)
                return;

            foreach (var playerController in PlayerCharacterMasterController.instances)
            {
                var master = playerController.master;
                if (master.inventory.GetTotalItemCountOfTier(ItemTier.Tier1) <= 1)
                {
                    StarterPack.GrantStarterItems(master);
                }
            }
        }

        public static void GrantStarterItems(CharacterMaster master)
        {
            GrantStarterSpeed(master);
            var bodyName = master.bodyPrefab.name;
            Debug.Log("Granting starter items for body " + bodyName);
            switch (bodyName)
            {
                case "EngiBody":
                    GrantStarterEngineer(master);
                    break;
                case "CommandoBody":
                    GrantStarterCommando(master);
                    break;
                case "HuntressBody":
                    GrantStarterHuntress(master);
                    break;
                case "ToolbotBody":
                    GrantStarterMulT(master);
                    break;
                case "MageBody":
                    GrantStarterArtificer(master);
                    break;
                case "MercBody":
                    GrantStarterMerc(master);
                    break;
                case "TreebotBody":
                    GrantStarterRex(master);
                    break;
            }
        }

        public static void GrantStarterEngineer(CharacterMaster master)
        {
            var inv = master.inventory;
            inv.GiveItem(ItemIndex.Mushroom, 4);
            inv.GiveItem(ItemIndex.BarrierOnKill, 1);
            inv.GiveItem(ItemIndex.Bear, 1);
        }

        public static void GrantStarterCommando(CharacterMaster master)
        {
            var inv = master.inventory;
            inv.GiveItem(ItemIndex.Syringe, 1);
            inv.GiveItem(ItemIndex.CritGlasses, 1);
            inv.GiveItem(ItemIndex.HealOnCrit, 1);
            inv.GiveItem(ItemIndex.ChainLightning, 1);
        }

        public static void GrantStarterHuntress(CharacterMaster master)
        {
            var inv = master.inventory;
            inv.GiveItem(ItemIndex.Syringe, 1);
            inv.GiveItem(ItemIndex.CritGlasses, 1);
            inv.GiveItem(ItemIndex.HealOnCrit, 1);
            inv.GiveItem(ItemIndex.SecondarySkillMagazine, 1);
            inv.GiveItem(ItemIndex.BarrierOnKill, 1);
        }

        public static void GrantStarterMulT(CharacterMaster master)
        {
            var inv = master.inventory;
            inv.GiveItem(ItemIndex.Syringe, 3);
            inv.GiveItem(ItemIndex.Seed, 2);
        }

        public static void GrantStarterArtificer(CharacterMaster master)
        {
            var inv = master.inventory;
            inv.GiveItem(ItemIndex.Crowbar, 2);
            inv.GiveItem(ItemIndex.CritGlasses, 1);
            inv.GiveItem(ItemIndex.HealOnCrit, 1);
            inv.GiveItem(ItemIndex.Bear, 1);
        }

        public static void GrantStarterMerc(CharacterMaster master)
        {
            var inv = master.inventory;
            inv.GiveItem(ItemIndex.Syringe, 2);
            inv.GiveItem(ItemIndex.CritGlasses, 1);
            inv.GiveItem(ItemIndex.HealOnCrit, 1);
            inv.GiveItem(ItemIndex.Bear, 1);
        }

        public static void GrantStarterRex(CharacterMaster master)
        {
            var inv = master.inventory;
            inv.GiveItem(ItemIndex.CritGlasses, 1);
            inv.GiveItem(ItemIndex.HealOnCrit, 1);
            inv.GiveItem(ItemIndex.HealWhileSafe, 1);
            inv.GiveItem(ItemIndex.Medkit, 2);
        }
        
        private static void GrantStarterSpeed(CharacterMaster master)
        {
            var inv = master.inventory;
            inv.GiveItem(ItemIndex.Hoof, 1);
            inv.GiveItem(ItemIndex.SprintBonus, 1);
            inv.GiveItem(ItemIndex.SprintOutOfCombat, 1);
            inv.GiveItem(ItemIndex.Feather, 1);
        }
    }
}
