using JarlykMods.Hailstorm.Cataclysm.BossPhases;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class CataclysmBossFightController : MonoBehaviour
    {
        private readonly Dictionary<BossPhase, PhaseBase> _phases = new Dictionary<BossPhase, PhaseBase>();
        private Xoroshiro128Plus _rng;
        private float _lastAutoGravBombs;
        private float _lastAutoAsteroidSwarm;

        public BossPhase ActivePhase { get; private set; }

        public AsteroidSwarmSettings AsteroidSwarm { get; private set; }

        public void SetPhase(BossPhase bossPhase)
        {
            if (ActivePhase == bossPhase)
                return;

            _phases[ActivePhase]?.OnExit();
            ActivePhase = bossPhase;
            _phases[ActivePhase]?.OnEnter();
        }

        public void AutoSpawnGravBombs(int count, float scale, float interval)
        {
            //Attacks are marshaled to the client, so only host needs to spawn them
            if (!NetworkServer.active)
                return;

            var now = Time.fixedTime;
            if (now - _lastAutoGravBombs > interval)
            {
                SpawnGravBombs(count, scale);
                _lastAutoGravBombs = now;
            }
        }

        public void AutoAsteroidSwarm()
        {
            //Attacks are marshaled to the client, so only host needs to spawn them
            if (!NetworkServer.active)
                return;

            var now = Time.fixedTime;
            if (now - _lastAutoAsteroidSwarm > AsteroidSwarm.SwarmInterval)
            {
                BeginAsteroidSwarm();
                _lastAutoAsteroidSwarm = now;
            }
        }

        private void SpawnGravBombs(int count, float scale)
        {
            //We want the bombs to be roughly evenly distributed, so we'll use radial coordinates and distribute at multiple angles
            //We'll then alternate toward smaller and larger radial biases

            const float innerRadius = 40;
            const float innerRadiusScatter = 15;
            const float outerRadius = 90;
            const float outerRadiusScatter = 30;

            const float innerAngleScatter = 30*(Mathf.PI/180);
            const float outerAngleScatter = 60*(Mathf.PI/180);

            float yNominal = 10 + scale/4;
            float yScatter = 12 + scale/2;

            var w0 = _rng.nextNormalizedFloat*(2*Mathf.PI);
            var dw = (2*Mathf.PI)/count;
            bool innerOdd = _rng.nextBool;
            for (int i = 0; i < count; i++)
            {
                var inner = (i & 1) == 1 ? innerOdd : !innerOdd;
                var radiusNominal = inner ? innerRadius : outerRadius;
                var radiusScatter = inner ? innerRadiusScatter : outerRadiusScatter;
                var angleScatter = inner ? innerAngleScatter : outerAngleScatter;
                angleScatter /= count;

                var r = radiusNominal + _rng.PlusMinus(radiusScatter);
                var w = w0 + i*dw + _rng.PlusMinus(angleScatter);

                var x = r*Mathf.Cos(w);
                var z = r*Mathf.Sin(w);
                var y = yNominal + _rng.PlusMinus(yScatter);
                GravBombEffect.Spawn(new Vector3(x, y, z), scale);
            }
        }

        private void BeginAsteroidSwarm()
        {
            StartCoroutine(RunAsteroidSwarm());
        }

        private IEnumerator RunAsteroidSwarm()
        {
            int count = 0;
            while (count < AsteroidSwarm.TotalCount)
            {
                int spawnCount = Math.Min(AsteroidSwarm.CountPerWave, AsteroidSwarm.TotalCount - count);
                for (int i = 0; i < spawnCount; i++)
                {
                    var w = _rng.nextNormalizedFloat*2*Mathf.PI;
                    var phi = (0.07f + _rng.PlusMinus(0.15f))*Mathf.PI;
                    var r = AsteroidSwarm.StartRadius + _rng.PlusMinus(AsteroidSwarm.StartRadiusRange);

                    var pos = new Vector3(r*Mathf.Cos(w),
                                          r*Mathf.Sin(phi),
                                          r*Mathf.Sin(w));
                    var rot = Quaternion.FromToRotation(Vector3.forward, -pos.normalized);
                    var speed = AsteroidSwarm.ProjectileSpeed + _rng.PlusMinus(AsteroidSwarm.ProjectileSpeedRange);
                    AsteroidProjectileController.Fire(pos, rot, speed);
                }

                count += spawnCount;
                yield return new WaitForSeconds(AsteroidSwarm.WaveInterval);
            }
        }

        private void Awake()
        {
            _rng = new Xoroshiro128Plus((ulong) DateTime.Now.Ticks);
            AsteroidSwarm = new AsteroidSwarmSettings();
            _phases.Add(BossPhase.Inactive, null);
            _phases.Add(BossPhase.Introduction, new IntroductionPhase(this));
            _phases.Add(BossPhase.ChargeLaser, new ChargeLaserPhase(this));
            _phases.Add(BossPhase.RunLaser, new RunLaserPhase(this));
            _phases.Add(BossPhase.TheHatching, new TheHatchingPhase(this));
            _phases.Add(BossPhase.Voidspawn, new VoidspawnPhase(this));
            _phases.Add(BossPhase.Finale, new FinalePhase(this));
            _phases.Add(BossPhase.Reward, new RewardPhase(this));
        }

        private void Start()
        {
            //TODO: Automatically start the Introduction phase when not debugging
            //SetPhase(BossPhase.Introduction);
            _lastAutoGravBombs = Time.fixedTime;
            _lastAutoAsteroidSwarm = Time.fixedTime;
        }

        private void FixedUpdate()
        {
            var phase = _phases[ActivePhase];
            if (phase != null)
            {
                var nextPhase = phase.FixedUpdate();
                SetPhase(nextPhase);
            }
        }
    }
}
