using UnityEngine.Networking;

namespace JarlykMods.Raincoat.ItemDropper
{
    public class DropRecentItemMessage : MessageBase
    {
        public int Test;

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(Test);
        }

        public override void Deserialize(NetworkReader reader)
        {
            Test = reader.ReadInt32();
        }

        public override string ToString() => $"DropRecentItemMessage";
    }
}
