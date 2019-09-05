using System;
using System.Collections.Generic;
using System.Text;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class AsteroidProjectileController : NetworkBehaviour
    {
        private static Xoroshiro128Plus _rng;

        public static float Scale = 5;
        public static float ScaleRange = 2;

        private void Awake()
        {
            if (_rng == null)
            {
                _rng = new Xoroshiro128Plus((ulong)DateTime.Now.Ticks);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            //Disable particles upon collision; we're just a rock now
            var particles = GetComponentInChildren<ParticleSystem>();
            if (particles != null)
                particles.gameObject.SetActive(false);
        }

        private void Start()
        {
            //Randomize total scale
            var scale = Scale + _rng.PlusMinus(ScaleRange);
            transform.localScale = new Vector3(scale, scale, scale);

            //Randomize orientation of mesh
            var meshTransform = transform.GetChild(0).GetChild(0);
            meshTransform.localRotation = Quaternion.Euler(180f*_rng.nextNormalizedFloat,
                                                           180f*_rng.nextNormalizedFloat,
                                                           180f*_rng.nextNormalizedFloat);
        }

        public static void Fire(Vector3 position, Quaternion rotation, float speed)
        {
            var info = new FireProjectileInfo();
            info.position = position;
            info.rotation = rotation;
            info.projectilePrefab = HailstormAssets.AsteroidProjectilePrefab;
            info.force = 10000;
            info.damage = 50;
            info.speedOverride = speed;
            info.useSpeedOverride = true;
            ProjectileManager.instance.FireProjectile(info);
        }

        public static void AugmentPrefab(GameObject prefab)
        {
            var nid = prefab.AddComponent<NetworkIdentity>();

            var apc = prefab.AddComponent<AsteroidProjectileController>();

            var pc = prefab.AddComponent<ProjectileController>();
            pc.allowPrediction = true;

            var pnt = prefab.AddComponent<ProjectileNetworkTransform>();

            var pd = prefab.AddComponent<ProjectileDamage>();

            var ps = prefab.AddComponent<ProjectileSimple>();
            ps.lifetime = 5;
        }
    }
}
