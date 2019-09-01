using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JarlykMods.Hailstorm.Cataclysm
{
    public sealed class CataclysmManager
    {
        public CataclysmManager()
        {
            IL.RoR2.CharacterBody.HandleConstructTurret += CharacterBodyOnHandleConstructTurret;
        }

        private void CharacterBodyOnHandleConstructTurret(ILContext il)
        {
            int turretMaster = 0;
            var c = new ILCursor(il);
            c.GotoNext(i => i.MatchCallvirt("RoR2.MasterSummon", "Perform"),
                       i => i.MatchStloc(out turretMaster));

            //The ret is the target of multiple branches, but as long as we insert before it, we should be good
            c.GotoNext(i => i.MatchRet());

            c.Emit(OpCodes.Ldloc_S, (byte) turretMaster);
            c.EmitDelegate<Action<CharacterMaster>>(OnTurretSpawned);
        }

        private void OnTurretSpawned(CharacterMaster turretMaster)
        {
            var body = turretMaster.GetBody();
            var worldLayer = LayerMask.NameToLayer("World");
            var colliders = Physics.OverlapSphere(body.footPosition, 2.0f, LayerMask.GetMask("Projectile", "World"));

            //TODO: Better way to get closest surface?
            var closest = colliders.FirstOrDefault(c => c.gameObject.layer == worldLayer);

            if (closest != null)
            {
                Debug.Log($"Parenting Engi turret to {closest.gameObject.name}");
                //var parentScale = closest.transform.localScale;
                //var oldScale = body.transform.localScale;
                //var newScale = new Vector3(oldScale.x/parentScale.x, 
                //                           oldScale.y/parentScale.y,
                //                           oldScale.z/parentScale.z);
                body.transform.SetParent(closest.transform, true);
                //body.transform.localScale = newScale;
            }
        }

        public void LoadCataclysm()
        {
            //The technique used here is based on Risk of Ridiculous mod, by x753
            //https://github.com/x753/Risk-of-Ridiculous/blob/master/Ridiculous/Ridiculous.cs
            On.RoR2.Stage.Start += StageOnStart;
            On.RoR2.Stage.GetPlayerSpawnTransform += StageOnGetPlayerSpawnTransform;
            Run.instance.nextStageScene = new SceneField("testscene");
            Run.instance.AdvanceStage(Run.instance.nextStageScene.SceneName);
        }

        private Transform StageOnGetPlayerSpawnTransform(On.RoR2.Stage.orig_GetPlayerSpawnTransform orig, Stage self)
        {
            return new GameObject {transform = {position = new Vector3(-40,3,0)}}.transform;
        }

        private void StageOnStart(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);

            //Destroy random junk that was left in the scene
            UnityEngine.Object.DestroyImmediate(GameObject.Find("EngiTurretBody(Clone)"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("EngiTurretBody(Clone)"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("EngiTurretBody(Clone)"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("EngiTurretBody(Clone)"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("GolemBodyInvincible(Clone)"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Reflection Probe"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Plane"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Plane (1)"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Plane (2)"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Plane (3)"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Teleporter1(Clone)"));

            //Disable combat directors (enemy spawning is going to be different here)
            var director = GameObject.Find("Director");
            foreach (var combatDirector in director.GetComponents<CombatDirector>())
                combatDirector.enabled = false;

            ////Add an exciting flat plane!
            //BuildGround();

            ////Build a floating platform that orbits the center
            //var orbit = BuildOrbit();
            //var platform = Object.Instantiate(HailstormAssets.CataclysmPlatformPrefab, orbit.transform);
            //platform.transform.localPosition = new Vector3(20, 5, 0);
            //platform.transform.localScale = new Vector3(3, 1, 3);

            var arena = Object.Instantiate(HailstormAssets.CataclysmArenaPrefab);
            foreach (var meshRenderer in arena.GetComponentsInChildren<MeshRenderer>())
            {
                var obj = meshRenderer.gameObject;
                if (obj.name.Contains("_P") || obj.name.Contains("P_"))
                {
                    obj.AddComponent<MobilePlatform>();
                    if (obj.transform.childCount > 0)
                    {
                        var child = obj.transform.GetChild(0);
                        child.gameObject.AddComponent<MobilePlatform>();
                    }
                }
            }

            RenderSettings.skybox = HailstormAssets.CataclysmSkyboxMaterial;
            RenderSettings.ambientSkyColor = Color.white;
            RenderSettings.ambientGroundColor = Color.white;

            //Our hooks are done processing to load this stage, so revert to normal handling
            UnHook();
        }

        private void UnHook()
        {
            On.RoR2.Stage.Start -= StageOnStart;
            On.RoR2.Stage.GetPlayerSpawnTransform -= StageOnGetPlayerSpawnTransform;
        }

        private static GameObject BuildOrbit()
        {
            var orbit = Object.Instantiate(new GameObject("Orbit"));
            var rigidBody = orbit.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;
            rigidBody.useGravity = false;
            var orbiter = orbit.AddComponent<Orbiter>();
            orbiter.AngularVelocity = 5f;
            return orbit;
        }

        public void LoadSceneExperimental()
        {
            var activeScene = SceneManager.GetActiveScene();
            var unloadAsync = SceneManager.UnloadSceneAsync(activeScene);
            unloadAsync.completed += UnloadAsyncOnCompleted;
        }

        private void UnloadAsyncOnCompleted(AsyncOperation obj)
        {
            var scene = SceneManager.CreateScene("CataclysmBossScene");
            var roots = BuildSceneRoots();
            foreach (var root in roots)
            {
                SceneManager.MoveGameObjectToScene(root, scene);
            }
        }

        private IReadOnlyList<GameObject> BuildSceneRoots()
        {
            var roots = new List<GameObject>();
            roots.Add(BuildSceneInfo());
            roots.Add(BuildGround());
            roots.Add(BuildSun());
            roots.Add(BuildGameManager());
            roots.Add(BuildDirector());
            return roots;
        }

        private GameObject BuildGround()
        {
            var obj = Object.Instantiate(HailstormAssets.CataclysmPlanePrefab);
            obj.transform.localScale = new Vector3(100, 1, 100);
            obj.transform.localPosition = new Vector3(0, 0, 0);
            obj.GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(0.05f, 0.05f);
            return obj;
        }

        private GameObject BuildGameManager()
        {
            var obj = new GameObject("GameManager");
            var gem = obj.AddComponent<GlobalEventManager>();
            return obj;
        }

        private GameObject BuildSceneInfo()
        {
            var obj = new GameObject("SceneInfo");
            var info = obj.AddComponent<SceneInfo>();
            var csi = obj.AddComponent<ClassicStageInfo>();

            //TODO: Sound system registration, including AkGameObj, possibly AkAmbient and RTPCController

            return obj;
        }

        private GameObject BuildSun()
        {
            var obj = new GameObject("Directional Light (SUN)");
            var light = obj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color32(101, 171, 229, 255);
            light.intensity = 1.43f;
            light.shadows = LightShadows.Hard;
            light.shadowStrength = 0.8f;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
            light.shadowNearPlane = 0.2f;
            return obj;
        }

        private GameObject BuildDirector()
        {
            var obj = new GameObject("Director");
            var nid = obj.AddComponent<NetworkIdentity>();
            var core = obj.AddComponent<DirectorCore>();
            
            return obj;
        }
    }
}
