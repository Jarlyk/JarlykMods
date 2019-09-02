using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JarlykMods.Hailstorm.Cataclysm
{
    /// <summary>
    /// This is a duplicate of an internal interface inside the RoR2 scripts
    /// </summary>
    public interface ICustomHologramContentProvider
    {
        // Token: 0x06000B48 RID: 2888
        bool ShouldDisplayHologram(GameObject viewer);

        // Token: 0x06000B49 RID: 2889
        GameObject GetHologramContentPrefab();

        // Token: 0x06000B4A RID: 2890
        void UpdateHologramContent(GameObject hologramContentObject);
    }
}
