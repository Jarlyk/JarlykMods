using System;
using System.Collections.Generic;
using System.Text;
using JarlykMods.Hailstorm.Cataclysm.BossPhases;
using UnityEngine;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class CataclysmBossFightController : MonoBehaviour
    {
        private readonly Dictionary<BossPhase, PhaseBase> _phases = new Dictionary<BossPhase, PhaseBase>();
        private Xoroshiro128Plus _rng;
        private float _lastAutoGravBombs;

        public BossPhase ActivePhase { get; private set; }

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
            var now = Time.fixedTime;
            if (now - _lastAutoGravBombs > interval)
            {
                SpawnGravBombs(count, scale);
                _lastAutoGravBombs = now;
            }
        }

        public void SpawnGravBombs(int count, float scale)
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

        private void Awake()
        {
            _rng = new Xoroshiro128Plus((ulong) DateTime.Now.Ticks);
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
