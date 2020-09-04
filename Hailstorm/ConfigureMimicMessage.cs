using System;
using System.Collections.Generic;
using System.Text;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    public sealed class ConfigureMimicMessage : INetMessage
    {
        public NetworkInstanceId mimicMasterId;
        public Quaternion initialRotation;
        public GameObject target;
        public float splatBiasR;
        public float splatBiasG;
        public float splatBiasB;
        public PickupIndex item;

        public void OnReceived()
        {
            ChestMimicSpawner.ConfigureMimic(this);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(mimicMasterId);
            writer.Write(initialRotation);
            writer.Write(target);
            writer.Write(splatBiasR);
            writer.Write(splatBiasG);
            writer.Write(splatBiasB);
            writer.Write(item);
        }

        public void Deserialize(NetworkReader reader)
        {
            mimicMasterId = reader.ReadNetworkId();
            initialRotation = reader.ReadQuaternion();
            target = reader.ReadGameObject();
            splatBiasR = reader.ReadSingle();
            splatBiasG = reader.ReadSingle();
            splatBiasB = reader.ReadSingle();
            item = PickupIndex.ReadFromNetworkReader(reader);
        }
    }
}
