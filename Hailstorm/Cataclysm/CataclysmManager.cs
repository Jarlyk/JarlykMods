using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Navigation;
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

        public static CataclysmBossFightController BossFight { get; private set; }

        public void LoadCataclysm()
        {
            //The technique used here is based on Risk of Ridiculous mod, by x753
            //https://github.com/x753/Risk-of-Ridiculous/blob/master/Ridiculous/Ridiculous.cs
            On.RoR2.Stage.Start += StageOnStart;
            On.RoR2.Stage.GetPlayerSpawnTransform += StageOnGetPlayerSpawnTransform;
            var nextScene = ScriptableObject.CreateInstance<SceneDef>();
            nextScene.SetPropertyValue("sceneName", "testscene");
            Run.instance.AdvanceStage(nextScene);
        }

        private Transform StageOnGetPlayerSpawnTransform(On.RoR2.Stage.orig_GetPlayerSpawnTransform orig, Stage self)
        {
            return new GameObject {transform = {position = new Vector3(-40,3,0)}}.transform;
        }

        private void StageOnStart(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);

            //Destroy random junk that was left in the scene
            UnityEngine.Object.DestroyImmediate(GameObject.Find("EngiTurretMaster"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("EngiTurretMaster (1)"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("EngiTurretMaster (2)"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("EngiTurretMaster (3)"));
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

            //Load the arena
            var arena = Object.Instantiate(HailstormAssets.CataclysmArenaPrefab);

            //Make mobile platforms transport properly
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

            //Configure out-of-bounds box
            var mapBounds = arena.transform.Find("MapBounds").gameObject;
            var mapZone = mapBounds.AddComponent<MapZone>();
            mapZone.triggerType = MapZone.TriggerType.TriggerExit;
            mapZone.zoneType = MapZone.ZoneType.OutOfBounds;

            //Apply skybox material
            RenderSettings.skybox = HailstormAssets.CataclysmSkyboxMaterial;
            RenderSettings.ambientSkyColor = Color.white;
            RenderSettings.ambientGroundColor = Color.white;

            //Replace ground nodes with dynamically computed nodes on the central platform
            BuildGroundNodes(arena);

            //Instantiate laser chargers
            var folP2 = GameObject.Find("FOL_P2");
            var charger1 = Object.Instantiate(HailstormAssets.LaserChargerPrefab, folP2.transform.position, Quaternion.identity);
            var folP4 = GameObject.Find("FOL_P4");
            var charger2 = Object.Instantiate(HailstormAssets.LaserChargerPrefab, folP4.transform.position, Quaternion.identity);

            //Enable the boss fight mechanics
            BossFight = arena.AddComponent<CataclysmBossFightController>();

            //Our hooks are done processing to load this stage, so revert to normal handling
            UnhookStageTransition();
        }

        private void BuildGroundNodes(GameObject arena)
        {
            //Destroy existing nodes
            Object.DestroyImmediate(GameObject.Find("TestSceneGroundNode"));

            var nodeGroupObj = Object.Instantiate(new GameObject("GroundNodes"));
            var nodeGroup = nodeGroupObj.AddComponent<MapNodeGroup>();

            //We're going to distribute ground nodes as a vertex fan around the central ring platform
            //First, we'll generate these positions as local positions
            const int n = 40;
            const float dw = 2*Mathf.PI/n;
            const float r1 = 35f;
            const float r2 = 45f;
            const float y = 0.2f;
            var ringVertices = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var r = (i & 1) == 0 ? r1 : r2;
                var w = i*dw;
                var x = r*Mathf.Cos(w);
                var z = r*Mathf.Sin(w);
                ringVertices[i] = new Vector3(x, y, z);
            }

            //Transform the local vertices into world space around the central arena and use those to construct nodes
            var centralArena = GameObject.Find("Central Arena");
            var nodes = new MapNode[n];
            for (int i = 0; i < n; i++)
            {
                var p = centralArena.transform.TransformPoint(ringVertices[i]);
                var nodeObj = Object.Instantiate(new GameObject("MapNode"), nodeGroupObj.transform);
                nodes[i] = nodeObj.AddComponent<MapNode>();
                nodes[i].flags = NodeFlags.NoCeiling;
                nodes[i].transform.position = p;
            }

            //Construct links based on tri strip topology
            var lineOfSightMasks = new SerializableBitArray[n];
            for (int i = 0; i < n; i++)
            {
                var links = new List<MapNode.Link>(4);
                for (int k = -2; k <= 2; k++)
                {
                    if (k == 0)
                        continue;

                    int relIdx = (i + k) % n;
                    if (relIdx < 0)
                        relIdx += n;
                    var link = new MapNode.Link
                    {
                        nodeB = nodes[relIdx],
                        distanceScore = Vector3.Distance(nodes[i].transform.position, nodes[relIdx].transform.position),
                        minJumpHeight = 100f,
                        hullMask = -1
                    };
                    links.Add(link);
                }
                nodes[i].links = links;

                var losMask = new SerializableBitArray(n);
                for (int j = 0; j < n; j++)
                    losMask[j] = true;
                lineOfSightMasks[i] = losMask;
            }

            nodeGroup.graphType = MapNodeGroup.GraphType.Ground;
            nodeGroup.nodeGraph = new NodeGraph();
            nodeGroup.nodeGraph.SetNodes(new ReadOnlyCollection<MapNode>(nodes),
                                         new ReadOnlyCollection<SerializableBitArray>(lineOfSightMasks));
            nodeGroup.nodeGraph.DebugDrawLinks(HullClassification.Human);
            SceneInfo.instance.SetFieldValue("groundNodes", nodeGroup.nodeGraph);
            SceneInfo.instance.groundNodeGroup = nodeGroup;
        }

        private void CharacterBodyOnHandleConstructTurret(ILContext il)
        {
            int turretMaster = 0;
            var c = new ILCursor(il);
            c.GotoNext(i => i.MatchCallvirt("RoR2.MasterSummon", "Perform"),
                       i => i.MatchStloc(out turretMaster));

            //The ret is the target of multiple branches, but as long as we insert before it, we should be good
            c.GotoNext(i => i.MatchRet());

            c.Emit(OpCodes.Ldloc_S, (byte)turretMaster);
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

        private void UnhookStageTransition()
        {
            On.RoR2.Stage.Start -= StageOnStart;
            On.RoR2.Stage.GetPlayerSpawnTransform -= StageOnGetPlayerSpawnTransform;
        }
    }
}
