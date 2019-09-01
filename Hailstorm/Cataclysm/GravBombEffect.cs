using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class GravBombEffect : MonoBehaviour
    {
        private float _startTime;

        public float forceMagnitude = -5000;

        private void Start()
        {
            _startTime = Time.fixedTime;
        }

        private void FixedUpdate()
        {
            var t = Time.fixedTime - _startTime;
            if (t > 3f)
            {
                var colliders = Physics.OverlapSphere(transform.position, transform.localScale.x, LayerIndex.defaultLayer.mask);
                foreach (var collider in colliders)
                {
                    var hc = collider.GetComponent<HealthComponent>();
                    if (hc != null)
                    {
                        var damage = new DamageInfo
                        {
                            damage = 0.4f*hc.fullCombinedHealth,
                            damageColorIndex = DamageColorIndex.Default,
                            damageType = DamageType.Generic,
                            position = transform.position
                        };
                        hc.TakeDamage(damage);
                    }
                }
                Destroy(gameObject);
            }
            else if (t > 2.5f)
            {
                var radius = transform.localScale.x;
                var colliders = Physics.OverlapSphere(transform.position, radius, LayerIndex.defaultLayer.mask);
                foreach (var collider in colliders)
                {
                    var hc = collider.GetComponent<HealthComponent>();
                    if (hc == null)
                        continue;

                    Vector3 a = collider.transform.position - transform.position;
                    a = a.normalized * forceMagnitude;

                    Vector3 velocity;
                    float mass;
                    var motor = collider.GetComponent<CharacterMotor>();
                    if (motor != null)
                    {
                        velocity = motor.velocity;
                        mass = motor.mass;
                    }
                    else
                    {
                        Rigidbody rigidBody = collider.GetComponent<Rigidbody>();
                        velocity = rigidBody.velocity;
                        mass = rigidBody.mass;
                    }

                    velocity.y += Physics.gravity.y * Time.fixedDeltaTime;
                    hc.TakeDamageForce(a - velocity * (0.8f * mass), true, false);
                }
            }
        }

        public static void AugmentPrefab(GameObject prefab)
        {
            var nid = prefab.AddComponent<NetworkIdentity>();
            var effect = prefab.AddComponent<GravBombEffect>();
        }
    }
}
