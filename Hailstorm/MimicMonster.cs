using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    public sealed class MimicMonster : MonoBehaviour
    {
        private void Awake()
        {
            if (!NetworkServer.active)
            {
                enabled = false;
            }
        }
    }
}
