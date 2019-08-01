namespace JarlykMods.Raincoat
{
    public sealed class PingImprovements
    {
        public PingImprovements()
        {
            On.RoR2.UI.PingIndicator.RebuildPing += PingIndicatorRebuildPing;
        }

        private static void PingIndicatorRebuildPing(On.RoR2.UI.PingIndicator.orig_RebuildPing orig, RoR2.UI.PingIndicator self)
        {
            self.interactablePingDuration = 5 * 60;
            orig(self);
        }
    }
}
