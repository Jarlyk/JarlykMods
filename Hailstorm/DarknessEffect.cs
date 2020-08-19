using System;
using RoR2;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    public sealed class DarknessEffect : MonoBehaviour
    {
        private Material _material;
        private float _darkStartBase;
        private float _breathTimeRef;

        public AnimatedFloat Distance { get; private set; }

        public AnimatedFloat Intensity { get; private set; }

        public void SetDarkTarget(float dist, float intensity)
        {
            enabled = true;
            _darkStartBase = dist;
            Distance.MaxSpeed = dist > Distance.Position ? 20 : 50;
            Intensity.Setpoint = intensity;
        }

        public void Banish()
        {
            _darkStartBase = 120;
            Distance.MaxSpeed = 50;
            Intensity.Setpoint = 0;
        }

        public void SyncBreathingStart()
        {
            _breathTimeRef = Time.time;
        }

        private void Awake()
        {
            _material = new Material(HailstormAssets.DarknessShader);

            Distance = new AnimatedFloat();
            Distance.Accel = 20;
            Distance.MaxSpeed = 50;
            Distance.Setpoint = 80;
            Distance.Position = 80;

            Intensity = new AnimatedFloat();
            Intensity.Accel = 2f;
            Intensity.MaxSpeed = 0.3f;
            Intensity.Setpoint = 0f;
            Intensity.Position = 0f;

            _breathTimeRef = Time.time;
        }
        
        private void OnRenderImage(RenderTexture source, RenderTexture dest)
        {
            Graphics.Blit(source, dest, _material);
        }
        
        private void Update()
        {
            if (!enabled) return;

            const double period = 5.0;
            var t = (2*Math.PI)*((Time.time-_breathTimeRef)%period)/period;
            var x = (float)Math.Cos(t);
            Distance.Setpoint = _darkStartBase + 0.7f*x;
            Distance.Update(Time.deltaTime);
            Intensity.Update(Time.deltaTime);

            _material.SetFloat("_DarkStart", Distance.Position);
            _material.SetFloat("_DarkEnd", Distance.Position+20);
            _material.SetFloat("_Intensity", Intensity.Position);

            //If the darkness has been banished, stop the effect
            if (Distance.Position > 119 && Distance.Setpoint > 119 && Intensity.Position < 0.05f)
            {
                enabled = false;
            }
        }
    }
}
