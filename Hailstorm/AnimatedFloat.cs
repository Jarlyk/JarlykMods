using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEngine.Windows.Speech;

namespace JarlykMods.Hailstorm
{
    public sealed class AnimatedFloat
    {
        public AnimatedFloat()
        {
            PositionDeadband = 1E-4f;
            VelocityDeadband = 1E-4f;
        }

        public float Position { get; set; }

        public float Velocity { get; set; }

        public float Setpoint { get; set; }

        public float MaxSpeed { get; set; }

        public float Accel { get; set; }

        public float PositionDeadband { get; set; }

        public float VelocityDeadband { get; set; }

        public void Update(float dt)
        {
            //If we're close enough, no animation
            var dx = Setpoint - Position;
            if (Math.Abs(dx) <= PositionDeadband && Math.Abs(Velocity) <= VelocityDeadband)
                return;

            //For simplicity, we assume time steps are small, so we don't deal with transitions that occur within a step
            //This lets us consider only whether we're accelerating; we'll assign 'a' accordingly
            //NOTE: If dt or Accel are large, this might oscillate unless Deadband is set reasonably loose
            var dir = Math.Sign(dx);
            float a = 0;

            //If we're moving the wrong direction, fix that
            if (Math.Sign(Velocity) != dir)
            {
                a = Accel*dir;
            }
            else
            {
                //We're moving the right direction, so take absolute values to simplify signs during calculation
                dx = Math.Abs(dx);
                var v = Math.Abs(Velocity);

                //First, let's check if we need to decelerate
                var tDecel = v/Accel;
                var xDecel = v*tDecel - 0.5f*Accel*tDecel*tDecel;
                if (xDecel >= dx || v > MaxSpeed)
                {
                    //Decelerate
                    a = -Accel*dir;
                }
                else if (v < MaxSpeed)
                {
                    //Accelerate, relying on the checks in next frame to cap out at appropriate velocity
                    a = Accel*dir;
                }
            }

            //Apply final motion
            Position += Velocity*dt + 0.5f*a*dt*dt;
            Velocity += a*dt;
        }
    }
}
