using System;
using UnityEngine;

namespace JarlykMods.Hailstorm
{
    public sealed class DarknessEffect : MonoBehaviour
    {
        private Material _material;
        private float _darkStartBase;
        private float _breathTimeRef;

        public AnimatedFloat Distance { get; private set; }

        public void Darken()
        {
            enabled = true;
            _darkStartBase = 3.5f;
            Distance.MaxSpeed = 50;
        }

        public void Undarken()
        {
            _darkStartBase = 80;
            Distance.MaxSpeed = 5;
        }

        public void Banish()
        {
            _darkStartBase = 120;
            Distance.MaxSpeed = 50;
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

            _material.SetFloat("_DarkStart", 0.5f*Distance.Position);
            _material.SetFloat("_DarkEnd", 4*Distance.Position);

            //If the darkness has been banished, stop the effect
            if (Distance.Position > 119 && Distance.Setpoint > 119)
            {
                enabled = false;
            }
        }
    }
}
