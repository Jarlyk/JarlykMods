﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EliteSpawningOverhaul;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JarlykMods.Hailstorm
{
    public sealed class DarkElitesManager
    {
        public const string EliteName = "Dark";
        public const string BuffName = "Affix_Dark";
        public const string EquipName = "Darkness";

        private readonly Dictionary<string, Material> _darkMats = new Dictionary<string, Material>();

        private readonly Xoroshiro128Plus _rng;
        private readonly EliteIndex _eliteIndex;
        private readonly BuffIndex _buffIndex;
        private readonly EquipmentIndex _equipIndex;
        private readonly AnimatedFloat _walkerU;
        private readonly AnimatedFloat _walkerV;
        private DarknessEffect _darknessEffect;
        private float _lastCheckTime;
        private bool _darknessSeen;

        public DarkElitesManager()
        {
            _rng = new Xoroshiro128Plus((ulong) DateTime.Now.Ticks);

            var eliteDef = new EliteDef
            {
                name = EliteName,
                modifierToken = EliteName,
                color = new Color32(0, 0, 0, 255),
            };
            var equipDef = new EquipmentDef
            {
                name = EquipName,
                cooldown = 10f,
                pickupModelPath = "",
                pickupIconPath = HailstormAssets.IconDarkElite,
                nameToken = EquipName,
                pickupToken = "Darkness",
                descriptionToken = "Night-bringer",
                canDrop = false,
                enigmaCompatible = false
            };
            var buffDef = new BuffDef
            {
                name = BuffName,
                buffColor = new Color32(255, 255, 255, 255),
                iconPath = HailstormAssets.IconDarkElite,
                canStack = false
            };

            var equip = new CustomEquipment(equipDef, new ItemDisplayRule[0]);
            var buff = new CustomBuff(BuffName, buffDef);
            var elite = new CustomElite(EliteName, eliteDef, equip, buff, 1);

            _eliteIndex = (EliteIndex)ItemAPI.AddCustomElite(elite);
            _buffIndex = (BuffIndex) ItemAPI.AddCustomBuff(buff);
            _equipIndex = (EquipmentIndex) ItemAPI.AddCustomEquipment(equip);
            eliteDef.eliteEquipmentIndex = _equipIndex;
            equipDef.passiveBuff = _buffIndex;
            buffDef.eliteIndex = _eliteIndex;

            //When the camera starts up, hook in our darkness effect
            On.RoR2.CameraRigController.Start += CameraRigControllerOnStart;

            //Update elite materials
            On.RoR2.CharacterModel.InstanceUpdate += CharacterModelOnInstanceUpdate;

            //Dark elites spawn much less frequently, but are only slightly stronger/costlier than tier 1s
            var card = new EliteAffixCard
            {
                spawnWeight = 0.3f,
                costMultiplier = 10.0f,
                damageBoostCoeff = 2.0f,
                healthBoostCoeff = 6.0f,
                eliteType = _eliteIndex
            };
            
            //Register the card for spawning if ESO is enabled
            EsoLib.Cards.Add(card);
            Card = card;

            //Create random walk trackers for dark elite material texture animation
            _walkerU = new AnimatedFloat
            {
                Accel = 0.3f,
                MaxSpeed = 0.6f
            };
            _walkerV = new AnimatedFloat
            {
                Accel = 0.3f,
                MaxSpeed = 0.6f
            };

            //Whenever scene changes, stop the breathing sound
            SceneManager.sceneUnloaded += s => AkSoundEngine.PostEvent(SoundEvents.StopLargeBreathing, null);
        }

        public EliteAffixCard Card { get; }

        private void CharacterModelOnInstanceUpdate(On.RoR2.CharacterModel.orig_InstanceUpdate orig, CharacterModel self)
        {
            orig(self);
            if (self.GetFieldValue<EliteIndex>("myEliteIndex") == _eliteIndex)
            {
                int replaced = 0;
                for (var i=0; i < self.baseRendererInfos.Length; i++)
                {
                    var mat = self.baseRendererInfos[i].defaultMaterial;
                    if (!_darkMats.TryGetValue(mat.name, out var darkMat))
                    {
                        //We have a special case for Wisp-related textures
                        //This will also impact any other cloud-based textures, which should be okay
                        const string remapTexName = "_RemapTex";
                        if (mat.GetTexturePropertyNames().Contains(remapTexName))
                        {
                            darkMat = new Material(mat);
                            darkMat.SetColor("_TintColor", new Color(6f, 0.1f, 7f, 1.3f));
                            darkMat.SetColor("_EmissionColor", new Color(0.12f, 0f, 0.1f, 0.1f));
                            var cloudTex = darkMat.GetTexture(remapTexName) as Texture2D;
                            if (cloudTex != null)
                            {
                                //Make clouds purple
                                var darkCloudTex = ReplaceWithRamp(cloudTex, new Vector3(0.3f, 0, 0.51f), 0.5f);
                                darkMat.SetTexture(remapTexName, darkCloudTex);
                            }
                        }
                        else
                        {
                            ////darkMat.color = new Color(0.1f, 0.1f, 0.1f);
                            //var texture = darkMat.mainTexture as Texture2D;
                            //if (texture != null)
                            //{
                            //    //Make base textures just darker overall
                            //    var darkTex = DarkifyTexture(texture, 0.05f, 0.05f, 0.05f);
                            //    darkMat.mainTexture = darkTex;
                            //}
                            if (self.name.ToLower().Contains("wisp"))
                            {
                                darkMat = HailstormAssets.PureBlack;
                            }
                            else
                            {
                                darkMat = HailstormAssets.PurpleCracks;
                            }
                        }
                        _darkMats[mat.name] = darkMat;
                        replaced++;
                    }
                    self.baseRendererInfos[i].defaultMaterial = darkMat;
                }

                if (replaced > 0)
                    Debug.Log($"Dark Elite: {replaced} materials replaced");
            }
        }

        private static Texture2D DarkifyTexture(Texture2D texture, float sr, float sg, float sb)
        {
            Texture2D darkTex;
            var tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0,
                                                 RenderTextureFormat.ARGB32,
                                                 RenderTextureReadWrite.Linear);
            try
            {
                Graphics.Blit(texture, tmp);
                var previous = RenderTexture.active;
                RenderTexture.active = tmp;
                darkTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                darkTex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);

                var pixels = darkTex.GetPixels();
                for (var k = 0; k < pixels.Length; k++)
                {
                    pixels[k].r *= sr;
                    pixels[k].g *= sg;
                    pixels[k].b *= sb;
                }

                darkTex.SetPixels(pixels);
                darkTex.Apply();
                RenderTexture.active = previous;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(tmp);
            }

            return darkTex;
        }

        private static Texture2D ReplaceWithRamp(Texture2D origTex, Vector3 vec, float startGrad)
        {
            Texture2D tex = new Texture2D(origTex.width, origTex.height, TextureFormat.RGBA32, false);

            int start = Mathf.CeilToInt(startGrad * 255);
            int gradLength = tex.width - start;
            Color32 back = new Color32(0, 0, 0, 0);
            Color32 temp = new Color32(0, 0, 0, 0);
            for (int x = 0; x < tex.width; x++)
            {
                if (x > start)
                {
                    float frac = ((float)x - (float)start) / (float)gradLength;
                    temp.r = (byte)Mathf.RoundToInt(255 * frac * vec.x);
                    temp.g = (byte)Mathf.RoundToInt(255 * frac * vec.y);
                    temp.b = (byte)Mathf.RoundToInt(255 * frac * vec.z);
                    temp.a = (byte) Mathf.RoundToInt(128*frac);
                }
                else
                {
                    temp = back;
                }

                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, temp);
                }
            }

            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();

            return tex;
        }

        public void Awake()
        {
            _lastCheckTime = Time.time;
        }

        public void Update()
        {
            //Animate the darkness texture
            _walkerU.Update(Time.deltaTime);
            _walkerV.Update(Time.deltaTime);
            HailstormAssets.PurpleCracks.SetTextureOffset("_MainTex", new Vector2(_walkerU.Position, _walkerV.Position));

            if (Time.time - _lastCheckTime > 0.5f)
            {
                //Give darkness texture a new target setpoint and update darkness effect in general
                _walkerU.Setpoint = _rng.nextNormalizedFloat - 0.5f;
                _walkerV.Setpoint = _rng.nextNormalizedFloat - 0.5f;
                CheckUpdate();
                _lastCheckTime = Time.time;
            }
        }

        private void CheckUpdate()
        {
            var rigs = CameraRigController.readOnlyInstancesList;
            if (rigs.Count == 0)
                return;

            var camera = rigs[0]?.sceneCam;
            var bodies = CharacterBody.readOnlyInstancesList;
            if (camera == null || _darknessEffect == null || bodies == null)
                return;

            bool canSeeDarkElite = false;
            int darkEliteCount = 0;
            var minX = 0.5f;
            foreach (var body in bodies)
            {
                if (body.isPlayerControlled || !body.HasBuff(_buffIndex) || body.teamComponent?.teamIndex != TeamIndex.Monster)
                    continue;

                darkEliteCount++;
                var posView = camera.WorldToViewportPoint(body.corePosition);
                if (posView.x > 0 && posView.x < 1 && posView.y > 0 && posView.y < 1 && posView.z > 0)
                {
                    canSeeDarkElite = true;
                    minX = Math.Min(minX, Math.Abs(posView.x - 0.5f));
                    if (!_darknessSeen)
                    {
                        AkSoundEngine.PostEvent(SoundEvents.PlayLargeBreathing, camera.gameObject);
                        _darknessEffect.SyncBreathingStart();
                        _darknessSeen = true;
                    }

                    AkSoundEngine.PostEvent(SoundEvents.PlayHorrorAmbiance, body.gameObject);
                    break;
                }
            }

            if (canSeeDarkElite)
            {
                _darknessEffect.SetDarkTarget(5.0f + 20f*minX);
            }
            else if (darkEliteCount > 0)
            {
                _darknessEffect.SetDarkTarget(80f);
            }
            else if (_darknessEffect.enabled)
            {
                _darknessEffect.Banish();
                _darknessSeen = false;
                AkSoundEngine.PostEvent(SoundEvents.StopLargeBreathing, null);
            }
        }

        private void CameraRigControllerOnStart(On.RoR2.CameraRigController.orig_Start orig, CameraRigController self)
        {
            orig(self);
            var camera = self.sceneCam;
            if (camera != null)
            {
                if (_darknessEffect != null)
                {
                    Object.Destroy(_darknessEffect);
                    _darknessEffect = null;
                }

                //Only create the darkness on actual stages
                //This should help alleviate an issue that some players encounter with the shader causing problems on bazaar
                if (SceneInfo.instance?.countsAsStage == true)
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
}
