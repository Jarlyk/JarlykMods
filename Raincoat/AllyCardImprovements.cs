using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace JarlykMods.Raincoat
{
    public sealed class AllyCardImprovements
    {
        public AllyCardImprovements()
        {
            On.RoR2.UI.AllyCardController.UpdateInfo += AllyCardControllerUpdateInfo;
        }

        private static void AllyCardControllerUpdateInfo(On.RoR2.UI.AllyCardController.orig_UpdateInfo orig, RoR2.UI.AllyCardController self)
        {
            orig(self);
            self.nameLabel.color = Color.white;
            if (self.nameLabel.text.Contains("Engineer Turret"))
            {
                //TODO: Language localization support for turret name
                //var deployable = self.sourceGameObject.GetComponent<Deployable>();
                //var localPlayer = LocalUserManager.GetFirstLocalUser().cachedMasterController;
                //var deployables = localPlayer.master.GetFieldValue<List<DeployableInfo>>("deployablesList");
                //if (deployables != null && deployables.Any(d => d.deployable.gameObject
                //                                                 ?.GetComponent<CharacterMaster>()
                //                                                 ?.GetBodyObject() == deployable.gameObject))
                //{
                    self.nameLabel.color = Color.red;
                //}
            }
        }
    }
}
