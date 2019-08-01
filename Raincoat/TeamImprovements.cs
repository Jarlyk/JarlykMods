using System.Collections;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JarlykMods.Raincoat
{
    public sealed class TeamImprovements
    {
        private Material _friendlyCrownMaterial;

        public TeamImprovements()
        {
            On.RoR2.Projectile.ProjectileController.Awake += ProjectileControllerAwake;
            On.RoR2.EquipmentSlot.ExecuteIfReady += EquipmentSlotExecuteIfReady;
        }

        private void ProjectileControllerAwake(On.RoR2.Projectile.ProjectileController.orig_Awake orig, ProjectileController self)
        {
            orig(self);
            if (self.name != null && self.name.StartsWith("PoisonStake"))
            {
                self.StartCoroutine(UpdateMalachite(self));
            }
        }

        private IEnumerator UpdateMalachite(ProjectileController self)
        {
            yield return new WaitForEndOfFrame();
            if (self.teamFilter?.teamIndex != TeamIndex.Player)
                yield break;

            var visRoot = self.transform.GetChild(0);
            var crown1 = visRoot.GetChild(0).gameObject;
            var renderer = crown1.GetComponent<MeshRenderer>();
            if (_friendlyCrownMaterial == null)
            {
                _friendlyCrownMaterial = new Material(renderer.material);
                _friendlyCrownMaterial.color = new Color(0.7f, 0.7f, 0.8f, 0.3f);
            }

            renderer.material = _friendlyCrownMaterial;
            var crown2 = visRoot.GetChild(1).gameObject;
            crown2.GetComponent<MeshRenderer>().material = _friendlyCrownMaterial;
        }

        private bool EquipmentSlotExecuteIfReady(On.RoR2.EquipmentSlot.orig_ExecuteIfReady orig, RoR2.EquipmentSlot self)
        {
            if (self.equipmentIndex == EquipmentIndex.Lightning && SceneManager.GetActiveScene().name == "bazaar" && self.characterBody.master.inventory.GetItemCount(ItemIndex.AutoCastEquipment) > 0)
            {
                return false;
            }

            return orig(self);
        }
    }
}
