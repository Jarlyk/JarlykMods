using RoR2;
using RoR2.Projectile;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm
{
    public sealed class TwisterProjectileController : MonoBehaviour
    {
        private Xoroshiro128Plus _rng;
        private ParticleSystemForceField _forceField;
        private int _ffSizeX;
        private int _ffSizeY;
        private int _ffSizeZ;
        private Color[] _vectorField;
        private uint _soundEvent1;
        private uint _soundEvent2;
        private float _startTime;
        private bool _expiring;

        public static BuffIndex ImmunityBuff { get; internal set; }

        public float forceScale = 4000.0f;
        public float damping = 0.8f;
        public float windLife = 20f;
        public Vector3 initialScale = new Vector3(1, 1, 1);
        public Vector3 finalScale = new Vector3(20, 25, 20);

        public static float totalLife = 25f;

        private void Awake()
        {
            _forceField = GetComponentInChildren<ParticleSystemForceField>();
            var vField = _forceField.vectorField;
            _ffSizeX = vField.width;
            _ffSizeY = vField.height;
            _ffSizeZ = vField.depth;
            _vectorField = vField.GetPixels();
        }

        private void Start()
        {
            _soundEvent1 = AkSoundEngine.PostEvent(SoundEvents.PlayTornado, gameObject);
            var refTop = gameObject.transform.GetChild(gameObject.transform.childCount - 1).gameObject;
            _soundEvent2 = AkSoundEngine.PostEvent(SoundEvents.PlayTornado, refTop);
            _startTime = Time.fixedTime;
            _rng = new Xoroshiro128Plus((ulong)DateTime.Now.Ticks);
        }

        private void OnDestroy()
        {
            //If somehow we died early, stop sounds as well
            if (!_expiring)
            {
                AkSoundEngine.StopPlayingID(_soundEvent1, 1000);
                AkSoundEngine.StopPlayingID(_soundEvent2, 1000);
            }
        }

        private void FixedUpdate()
        {
            var aliveTime = Time.fixedTime - _startTime;
            if (aliveTime > windLife)
            {
                if (_expiring)
                    return;

                //Stop sounds and new particles as soon as we start expiring
                _expiring = true;
                AkSoundEngine.StopPlayingID(_soundEvent1, 3000);
                AkSoundEngine.StopPlayingID(_soundEvent2, 3000);
                var particles = GetComponentInChildren<ParticleSystem>();
                var emission = particles.emission;
                emission.enabled = false;
                return;
            }

            if (NetworkServer.active)
            {
                //Bias toward nearest player, with some randomization
                var playerBodies = PlayerCharacterMasterController.instances.Select(p => p.master?.GetBody());
                var nearestPlayer = playerBodies.Where(b => b != null)
                                                .Select(b => new { Delta = b.corePosition - transform.position, Body = b })
                                                .Where(b => b.Delta.sqrMagnitude > 2f * transform.localScale.x * transform.localScale.x)
                                                .OrderBy(b => b.Delta.sqrMagnitude)
                                                .FirstOrDefault();
                if (nearestPlayer != null)
                {
                    var tApproach = 1.0f + 0.2f * _rng.nextNormalizedFloat;
                    var lookToPlayer = Quaternion.AngleAxis((float)((180 / Math.PI) * Math.Atan2(nearestPlayer.Delta.x, nearestPlayer.Delta.z)), Vector3.up);
                    transform.rotation = Quaternion.LerpUnclamped(transform.rotation, lookToPlayer, tApproach);
                }

                //Scale up over time
                var t = aliveTime / windLife;
                var rt = (float)Math.Sqrt(t);
                var scale = Vector3.Lerp(initialScale, finalScale, rt);
                transform.localScale = scale;
            }

            //Check for stuff inside the tornado
            var pos = transform.position;
            var topPos = pos + 3*transform.localScale.y*Vector3.up;
            var colliders = Physics.OverlapCapsule(pos, topPos, transform.localScale.x + 2.0f, LayerIndex.defaultLayer.mask);
            foreach (var collider in colliders)
            {
                var body = collider.GetComponent<CharacterBody>();
                if (body == null)
                    continue;

                if (body.isPlayerControlled && !body.isLocalPlayer)
                    continue;

                if (!NetworkServer.active && !body.isPlayerControlled)
                    continue;

                //TODO: Allow configurable inclusion of champions
                if (body != null && (body.HasBuff(ImmunityBuff) || body.isChampion))
                    continue;

                var position = body.corePosition;
                var relPos = _forceField.transform.InverseTransformPoint(position);
                if (relPos.x >= -1 && relPos.x <= 1 && relPos.y >= -1 && relPos.y <= 1 && relPos.z >= -1 && relPos.z <= 1)
                {
                    var rxi = (int) Math.Round(0.5*(relPos.x + 1)*(_ffSizeX - 1));
                    var ryi = (int) Math.Round(0.5*(relPos.y + 1)*(_ffSizeY - 1));
                    var rzi = (int) Math.Round(0.5*(relPos.z + 1)*(_ffSizeZ - 1));
                    var c = _vectorField[rzi*_ffSizeX*_ffSizeY + ryi*_ffSizeX + rxi];
                    var v = forceScale*new Vector3(c.r, c.g, c.b);

                    //Apply an inward force to help balance out the tendency to spit out the player at high forces
                    var inward = 0.1f*forceScale*new Vector3(-relPos.x, 0, -relPos.z).normalized;
                    v += inward;
                    v = v.magnitude*_forceField.transform.TransformDirection(v.normalized);

                    //Remove gravity
                    v.y += Physics.gravity.y*Time.fixedDeltaTime;

                    //Get current velocity so that we can dampen it
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

        /// <summary>
        /// This function augments the plain unscripted prefab and attaches the necessary scripts
        /// to make it function as a projectile
        /// </summary>
        /// <param name="prefab">The plain Twister prefab</param>
        public static void AugumentPrefab(GameObject prefab)
        {
            var cc = prefab.GetComponent<CharacterController>();
            cc.detectCollisions = false;
            cc.enableOverlapRecovery = false;

            var nid = prefab.AddComponent<NetworkIdentity>();

            var tpc = prefab.AddComponent<TwisterProjectileController>();
            
            var pc = prefab.AddComponent<ProjectileController>();

            var pnt = prefab.AddComponent<ProjectileNetworkTransform>();

            var pcc = prefab.AddComponent<ProjectileCharacterController>();
            pcc.lifetime = totalLife;
            pcc.velocity = 6;
        }
    }
}
