using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;
using SpawnPoint = On.RoR2.SpawnPoint;

namespace JarlykMods.Hailstorm
{
    public sealed class BillboardEffectPresenter : MonoBehaviour
    {
        private Vector3 _startPos;

        public void Start()
        {
            _startPos = transform.position;
        }

        public void Update()
        {
            var camera = Camera.main;
            if (!camera)
                return;

            gameObject.transform.rotation = Quaternion.LookRotation(-1*camera.transform.forward, camera.transform.up);
            gameObject.transform.position = 0.4f*_startPos + 0.6f*camera.transform.position;
        }
    }
}
