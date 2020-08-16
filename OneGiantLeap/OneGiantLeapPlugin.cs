using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable UnusedMember.Local

namespace JarlykMods.OneGiantLeap
{
    [BepInPlugin(PluginGuid, "OneGiantLeap", "0.2.2")]
    [BepInDependency(R2API.R2API.PluginGUID)]
    public class OneGiantLeapPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jarlyk.onegiantleap";

        public OneGiantLeapPlugin()
        {
            On.RoR2.SceneDirector.PopulateScene += OnSceneDirectorPopulateScene;
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
                jumpVolume.jumpVelocity = new Vector3(-100, 82, -3);

                Debug.Log("Created moon geyser clone");
            }
        }

        public void Update()
        {
        }
    }
}