using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace JarlykMods.Durability
{
    public sealed class UpdateDurabilityMessage : MessageBase
    {
        public float durability;
        public float durabilityAlt;

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(durability);
            writer.Write(durabilityAlt);
        }

        public override void Deserialize(NetworkReader reader)
        {
            durability = reader.ReadSingle();
            durabilityAlt = reader.ReadSingle();
        }

        public override string ToString()
        {
            return $"UpdateDurabilityMessage({durability},{durabilityAlt})";
        }
    }
}
