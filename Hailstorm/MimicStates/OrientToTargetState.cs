using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using KinematicCharacterController;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;

namespace JarlykMods.Hailstorm.MimicStates
{
    public sealed class OrientToTargetState : BaseState
    {
        private AnimatedFloat _orientT;
        private Quaternion _startRot;
        private Quaternion _interRot;
        private Quaternion _targetRot;
        private Vector3 _startPos;
        private GameObject _target;

        public override void OnEnter()
        {
            base.OnEnter();

            var master = characterBody.master;
            var ai = master.GetComponent<BaseAI>();

            var context = characterBody.GetComponent<MimicContext>();
            if (!context)
                return;
            
            _target = context.target;
            _startRot = context.initialRotation;

            var targetPos = _target.transform.position;
            var myPos = characterBody.transform.position;
            _startPos = myPos;
            Debug.Log($"Mimic pos: {myPos} | target pos: {targetPos}");
            var lookDir = (targetPos - myPos).normalized;
            _targetRot = Util.QuaternionSafeLookRotation(new Vector3(lookDir.x, 0, lookDir.z), Vector3.up);
            if (_startRot == null)
                return;

            Debug.Log($"Mimic rotation {_startRot.eulerAngles} => {_targetRot.eulerAngles}");

            //By default, do simple Slerp, which is likely mostly a y twist
            _interRot = Quaternion.Slerp(_startRot, _targetRot, 0.5f);

            //If twist angle is too large, though, adjust it to be a backflip by rotating around the direction of motion
            if (Mathf.DeltaAngle(_targetRot.eulerAngles.y, _startRot.eulerAngles.y) > 30)
            {
                _interRot = Quaternion.AngleAxis(90, lookDir)*_interRot;
                Debug.Log("Mimic backflip selected");
            }
            Debug.Log($"Mimic interRot: {_interRot.eulerAngles}");

            _orientT = new AnimatedFloat();
            _orientT.Accel = 10.0f;
            _orientT.MaxSpeed = 3.0f;
            _orientT.Setpoint = 1.0f;

            GetModelAnimator().SetLayerWeight(1, 1);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (_orientT == null || !_target)
            {
                outer.SetNextStateToMain();
                return;
            }

            _orientT.Update(Time.fixedDeltaTime);
            var t = _orientT.Position;
            if (t < 0.5f)
            {
                characterBody.transform.rotation = Quaternion.Slerp(_startRot, _interRot, 2*t);
                characterBody.transform.position = _startPos + new Vector3(0, 4.0f*Mathf.Sqrt(2*t), 0);
            }
            else
            {
                //Recompute target to take into account motion
                var targetPos = _target.transform.position;
                var myPos = characterBody.transform.position;
                var lookDir = (targetPos - myPos).normalized;
                _targetRot = Util.QuaternionSafeLookRotation(new Vector3(lookDir.x, 0, lookDir.z), Vector3.up);

                characterBody.transform.rotation = Quaternion.Slerp(_interRot, _targetRot, 2*(t - 0.5f));
                characterBody.transform.position = _startPos + new Vector3(0, 4.0f*Mathf.Sqrt(2*Mathf.Abs(1.0f-t)), 0);
                characterBody.GetComponent<Rigidbody>().detectCollisions = true;
            }

            //Once we're done flipping, reactivate character direction controller and proceed to surprise pounce
            if (t >= 0.99)
            {
                characterBody.transform.rotation = _targetRot;

                var dir = characterBody.GetComponent<CharacterDirection>();
                dir.yaw = characterBody.transform.rotation.eulerAngles.y;
                dir.enabled = true;

                var motor = characterBody.GetComponent<CharacterMotor>();
                motor.Motor.SetRotation(_targetRot);
                motor.enabled = true;

                var kinMotor = characterBody.GetComponent<KinematicCharacterMotor>();
                kinMotor.SetRotation(_targetRot);
                kinMotor.enabled = true;

                outer.SetNextState(Instantiate(typeof(SurprisePounceState)));
            }
        }
    }
}
