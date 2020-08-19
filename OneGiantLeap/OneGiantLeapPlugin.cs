using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using KinematicCharacterController;
using RoR2;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// ReSharper disable UnusedMember.Local

namespace JarlykMods.OneGiantLeap
{
    [BepInPlugin(PluginGuid, "OneGiantLeap", "0.2.2")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    public class OneGiantLeapPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.onegiantleap";

        private Collider _lastCollider;

        public OneGiantLeapPlugin()
        {
            On.RoR2.SceneDirector.PopulateScene += OnSceneDirectorPopulateScene;
            On.RoR2.JumpVolume.OnTriggerStay += OnJumpVolumeTriggerStay;
        }

        private void OnJumpVolumeTriggerStay(On.RoR2.JumpVolume.orig_OnTriggerStay orig, JumpVolume self, Collider other)
        {
            _lastCollider = other;
            orig(self, other);
        }

        private void OnSceneDirectorPopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self)
        {
            orig(self);

            if (SceneCatalog.GetSceneDefForCurrentScene().name == "moon")
            {
                var origin = GameObject.Find("PlayerSpawnOrigin");
                var geyser2 = GameObject.Find("MoonGeyser (2)");

                var newGeyser = Instantiate(geyser2, origin.transform.position + new Vector3(-30f, 1.5f, 0), Quaternion.Euler(-90, 0, 270));
                newGeyser.SetActive(true);

                var jumpVolume = newGeyser.transform.GetChild(0).GetComponent<JumpVolume>();
                jumpVolume.targetElevationTransform = geyser2.transform.GetChild(0).GetChild(2).transform;
                
                //Compute a jump velocity that reaches the target via a parabolic arc
                var p0 = newGeyser.transform.position;
                var y0 = p0.y;
                var p2 = jumpVolume.targetElevationTransform.position;
                var y2 = p2.y;
                var y1 = Mathf.Max(y0,y2) + 80f;
                var t = Mathf.Sqrt(2*(y2 - y1)/Physics.gravity.y);
                var vy = (2*y1 - y2 - y0)/t;
                var dx = (p2.x - p0.x);
                var dz = (p2.z - p0.z);
                var d = Mathf.Sqrt(dx*dx + dz*dz);
                var vd = d/t;
                var vx = vd*(dz/d);
                var vz = vd*(dx/d);
                jumpVolume.jumpVelocity = new Vector3(vx, vy, vz);
                Debug.Log($"Configured jump with velocity {vx:0.000},{vy:0.000},{vz:0.000}");

                //We need to disable clipping, then reenable it before impact
                jumpVolume.onJump.AddListener(() =>
                {
                    if (!_lastCollider) return;

                    var body = _lastCollider.GetComponent<CharacterBody>();
                    if (!body) return;

                    body.rigidbody.detectCollisions = false;
                    StartCoroutine(DelayedDisableNoClip(body, 8).GetEnumerator());
                });

                Debug.Log("Created moon geyser clone");
            }
        }

        private IEnumerable DelayedDisableNoClip(CharacterBody body, int delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            body.rigidbody.detectCollisions = true;
        }
    }
}