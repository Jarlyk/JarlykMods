using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    public sealed class TornadoLauncher : MonoBehaviour
    {
        private CharacterBody _body;
        private float _lastLaunched;

        public static BuffIndex StormBuff;

        private void Start()
        {
            _lastLaunched = Time.fixedTime - 15;
            _body = GetComponent<CharacterBody>();
            if (_body == null)
            {
                Debug.Log("TornadoLauncher attached to object without a CharacterBody; self-terminating");
                Destroy(this);
            }
        }

        private void FixedUpdate()
        {
            //If owner loses buff, we should die too
            if (!_body.HasBuff(StormBuff))
            {
                Destroy(this);
                return;
            }

            if (Time.fixedTime - _lastLaunched >= 20)
            {
                var info = new FireProjectileInfo
                {
                    projectilePrefab = HailstormAssets.TwisterPrefab,
                    position = transform.position,
                    rotation = Util.QuaternionSafeLookRotation(transform.forward),
                    owner = gameObject
                };
                ProjectileManager.instance.FireProjectile(info);

                _lastLaunched = Time.fixedTime;
            }
        }
    }
}
