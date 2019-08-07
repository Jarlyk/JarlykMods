using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using UnityEngine;
using Refl = R2API.Utils.Reflection;

namespace JarlykMods.Hailstorm
{
    public sealed class DarkElitesManager
    {
        public const string EliteName = "Dark";
        public const string BuffName = "Affix_Dark";
        public const string EquipName = "Darkness";

        private readonly Dictionary<Material, Material> _particleMats = new Dictionary<Material, Material>();

        private DarknessEffect _darknessEffect;
        private EliteIndex _eliteIndex;
        private BuffIndex _buffIndex;
        private EquipmentIndex _equipIndex;
        private float _lastCheckTime;

        public DarkElitesManager()
        {
            //Custom items should be registered now, so grab their indices
            _eliteIndex = (EliteIndex)ItemLib.ItemLib.GetEliteId(EliteName);
            _buffIndex = (BuffIndex)ItemLib.ItemLib.GetBuffId(BuffName);
            _equipIndex = (EquipmentIndex)ItemLib.ItemLib.GetEquipmentId(EquipName);

            //When the camera starts up, hook in our darkness effect
            On.RoR2.CameraRigController.Start += CameraRigControllerOnStart;

            //Update elite materials
            On.RoR2.CharacterModel.InstanceUpdate += CharacterModelOnInstanceUpdate;
            IL.RoR2.CharacterModel.UpdateOverlays += CharacterModelOnUpdateOverlays;
        }

        private void CharacterModelOnUpdateOverlays(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(i => i.MatchLdarg(0),
                       i => i.MatchLdfld("RoR2.CharacterModel", "wasPreviouslyClayGooed"));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Material>>(() => HailstormAssets.BlackRim);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(CharacterModel).GetField("myEliteIndex", BindingFlags.Instance | BindingFlags.NonPublic));
            c.Emit(OpCodes.Ldc_I4, (int)_eliteIndex);
            c.Emit(OpCodes.Ceq);
            c.Emit(OpCodes.Call, typeof(CharacterModel).GetMethod("<UpdateOverlays>g__AddOverlay|94_0", BindingFlags.Instance | BindingFlags.NonPublic));

            //TODO: Overlays is something that we might want to make extensible?
        }

        private void CharacterModelOnInstanceUpdate(On.RoR2.CharacterModel.orig_InstanceUpdate orig, CharacterModel self)
        {
            orig(self);
            if (Refl.GetFieldValue<EliteIndex>(self, "myEliteIndex") == _eliteIndex)
            {
                var poisonMat = Refl.GetFieldValue<Material>(self, "elitePoisonParticleReplacementMaterial");
                if (poisonMat != null)
                {
                    if (!_particleMats.TryGetValue(poisonMat, out var newMat))
                    {
                        newMat = new Material(poisonMat);
                        newMat.color = Color.black;
                        _particleMats.Add(poisonMat, newMat);
                    }
                    Refl.SetFieldValue(self, "particleMaterialOverride", newMat);
                }
            }
        }

        public void Awake()
        {
            _lastCheckTime = Time.time;
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                _darknessEffect.enabled = !_darknessEffect.enabled;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                _darknessEffect.Darken();
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                _darknessEffect.Undarken();
            }

            if (Time.time - _lastCheckTime > 0.5f)
            {
                CheckUpdate();
                _lastCheckTime = Time.time;
            }
        }

        private void CheckUpdate()
        {
            var camera = CameraRigController.readOnlyInstancesList[0]?.sceneCam;
            if (camera == null)
                return;

            bool canSeeDarkElite = false;
            int darkEliteCount = 0;
            foreach (var body in CharacterBody.readOnlyInstancesList)
            {
                if (!body.HasBuff(_buffIndex))
                    continue;

                darkEliteCount++;
                var posView = camera.WorldToViewportPoint(body.corePosition);
                if (posView.x > 0 && posView.x < 1 && posView.y > 0 && posView.y < 1 && posView.z > 0)
                {
                    canSeeDarkElite = true;
                    break;
                }
            }

            if (canSeeDarkElite)
            {
                _darknessEffect.Darken();
            }
            else
            {
                _darknessEffect.Undarken();
            }

            if (darkEliteCount == 0)
                _darknessEffect.Banish();
        }

        private void CameraRigControllerOnStart(On.RoR2.CameraRigController.orig_Start orig, CameraRigController self)
        {
            orig(self);
            var camera = self.sceneCam;
            if (camera != null)
            {
                _darknessEffect = camera.gameObject.AddComponent<DarknessEffect>();
                _darknessEffect.enabled = false;
                if (camera.depthTextureMode == DepthTextureMode.None)
                {
                    camera.depthTextureMode = DepthTextureMode.Depth;
                    Debug.Log("Camera did not have depth texture enabled; enabling for use with darkness");
                }
            }
        }
    }
}
