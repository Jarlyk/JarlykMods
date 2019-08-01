using RoR2;

namespace JarlykMods.Raincoat.ItemDropper
{
    public sealed class PickupRecord
    {
        public PickupRecord(float pickupTime, ItemIndex itemIndex)
        {
            PickupTime = pickupTime;
            ItemIndex = itemIndex;
        }

        public float PickupTime { get; }

        public ItemIndex ItemIndex { get; }
    }
}
