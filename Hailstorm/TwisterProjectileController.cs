using RoR2;
using RoR2.Projectile;
using System;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    public sealed class TwisterProjectileController : MonoBehaviour
    {
        private int _ffSizeX;
        private int _ffSizeY;
        private int _ffSizeZ;
        private Color[] _vectorField;

        public static BuffIndex ImmunityBuff { get; internal set; }

        public float forceScale = 5000.0f;
        public float damping = 0.8f;

        private void Awake()
        {
            var forceField = GetComponentInChildren<ParticleSystemForceField>().vectorField;
            _ffSizeX = forceField.width;
            _ffSizeY = forceField.height;
            _ffSizeZ = forceField.depth;
            _vectorField = forceField.GetPixels();
        }

        private void FixedUpdate()
        {
            //Check for stuff inside the tornado
            var pos = transform.position;
            var topPos = pos + 3*transform.localScale.y*Vector3.up;
            var colliders = Physics.OverlapCapsule(pos, topPos, transform.localScale.x + 2.0f, LayerIndex.defaultLayer.mask);
            foreach (var collider in colliders)
            {
                //TODO: Ignore Storm elites, as they are immune to their own storms
                //If it has a body and it's not a champion monster type, it can be propelled
                var body = collider.GetComponent<CharacterBody>();
                if (body != null && !body.isChampion)
                {
                    var relPos = transform.InverseTransformPoint(body.corePosition);
                    if (relPos.x >= -1 && relPos.x <= 1 && relPos.y >= -1 && relPos.y <= 1 && relPos.z >= -1 &&
                        relPos.z <= 1)
                    {

                        var rxi = (int) Math.Round(0.5*(relPos.x + 1)*(_ffSizeX - 1));
                        var ryi = (int) Math.Round(0.5*(relPos.y + 1)*(_ffSizeY - 1));
                        var rzi = (int) Math.Round(0.5*(relPos.z + 1)*(_ffSizeZ - 1));
                        var c = _vectorField[rzi*_ffSizeX*_ffSizeY + ryi*_ffSizeX + rxi];
                        var v = forceScale*new Vector3(c.r, c.g, c.b);
                        v.y += Physics.gravity.y*Time.fixedDeltaTime;

                        Vector3 currentV;
                        float mass;
                        var motor = collider.GetComponent<CharacterMotor>();
                        var rigidBody = collider.GetComponent<Rigidbody>();
                        if (motor != null)
                        {
                            currentV = motor.velocity;
                            mass = motor.mass;
                        }
                        else
                        {
                            currentV = rigidBody.velocity;
                            mass = rigidBody.mass;
                        }

                        var force = v - damping*mass*currentV;
                        var hc = collider.GetComponent<HealthComponent>();
                        if (hc != null)
                            hc.TakeDamageForce(force, true, false);
                        else if (rigidBody != null)
                            rigidBody.AddForce(force, ForceMode.VelocityChange);
                    }
                }
            }
        }

        /// <summary>
        /// This function augments the plain unscripted prefab and attaches the necessary scripts
        /// to make it function as a projectile
        /// </summary>
        /// <param name="prefab">The plain Twister prefab</param>
        public static void AugumentPrefab(GameObject prefab)
        {
            var tpc = prefab.AddComponent<TwisterProjectileController>();
            
            var pc = prefab.AddComponent<ProjectileController>();

            var pnt = prefab.AddComponent<ProjectileNetworkTransform>();

            var pcc = prefab.AddComponent<ProjectileCharacterController>();
            pcc.lifetime = 20;
            pcc.velocity = 2;
        }
    }
}
