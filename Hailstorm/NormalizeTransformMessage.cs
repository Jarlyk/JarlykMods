using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using R2API.Networking.Interfaces;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    public sealed class NormalizeTransformMessage : INetMessage
    {
        public NetworkInstanceId netId;
        public Vector3 offset;
        public float normalizedScale;

        public void OnReceived()
        {
            if (!NetworkServer.active)
            {
                HailstormPlugin.Instance.StartCoroutine(ConfigureAsync(this));
            }
        }

        private static IEnumerator ConfigureAsync(NormalizeTransformMessage message)
        {
            GameObject gameObj = null;

            yield return new WaitUntil(() =>
            {
                gameObj = ClientScene.FindLocalObject(message.netId);
                return gameObj;
            });

            message.Configure(gameObj);
        }

        public void Configure(GameObject gameObj)
        {
            //Normalize world scale to desired size
            var scaleAdj = normalizedScale;
            gameObj.transform.localScale = new Vector3(1f, 1f, 1f);
            gameObj.transform.localScale = scaleAdj * new Vector3(1.0f / gameObj.transform.lossyScale.x,
                                                                    1.0f / gameObj.transform.lossyScale.y,
                                                                    1.0f / gameObj.transform.lossyScale.z);

            //Normalize to desired position
            gameObj.transform.position += gameObj.transform.rotation*offset;

        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(netId);
            writer.Write(offset);
            writer.Write(normalizedScale);
        }

        public void Deserialize(NetworkReader reader)
        {
            netId = reader.ReadNetworkId();
            offset = reader.ReadVector3();
            normalizedScale = reader.ReadSingle();
        }
    }
}
