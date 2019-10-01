using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemLib;
using RoR2;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Object = System.Object;

namespace JarlykMods.Umbrella
{
    public sealed class JestersDice
    {
        private readonly Xoroshiro128Plus _rng;
        private Collider[] _colliders;

        public JestersDice()
        {
            _rng = new Xoroshiro128Plus((ulong)DateTime.Now.Ticks);
            EquipIndex = (EquipmentIndex)ItemLib.ItemLib.GetEquipmentId(EquipNames.JestersDice);
        }

        public static EquipmentIndex EquipIndex { get; private set; }

        public void Awake()
        {
            _colliders = new Collider[20];
            BuildTexture();
        }

        public void PerformAction(MonoBehaviour owner, CharacterBody body)
        {
            owner.StartCoroutine(RunDice(body));
        }

        private IEnumerator RunDice(CharacterBody body)
        {
            AkSoundEngine.PostEvent(SoundEvents.PlayJestersDice, body.gameObject);

            //Allow the dice rolling sound to run before actually swapping items
            yield return new WaitForSecondsRealtime(1.2f);

            int collideCount = Physics.OverlapSphereNonAlloc(body.corePosition, 25, _colliders, (int)LayerIndex.fakeActor.mask);
            var pickupControllers = Enumerable.Range(0, collideCount)
                                              .Select(i => _colliders[i].GetComponent<GenericPickupController>())
                                              .Where(c => c != null).ToList();

            int rerollCount = 0;
            foreach (var controller in pickupControllers)
            {
                var newIndex = controller.pickupIndex;

                if (controller.pickupIndex.itemIndex != ItemIndex.None)
                {
                    var tier = ItemCatalog.GetItemDef(controller.pickupIndex.itemIndex).tier;
                    if (tier != ItemTier.NoTier)
                    {
                        newIndex = _rng.NextElementUniform(GetDropList(tier));
                    }
                }
                else if (controller.pickupIndex.equipmentIndex != EquipmentIndex.None)
                {
                    if (controller.pickupIndex.IsLunar())
                    {
                        newIndex = _rng.NextElementUniform(Run.instance.availableLunarDropList);
                    }
                    else
                    {
                        newIndex = _rng.NextElementUniform(Run.instance.availableEquipmentDropList);
                    }
                }

                Debug.Log($"Replacing {controller.pickupIndex} on ground with {newIndex}");
                if (newIndex != controller.pickupIndex)
                {
                    var pos = controller.transform.position;
                    NetworkServer.Destroy(controller.gameObject);
                    PickupDropletController.CreatePickupDroplet(newIndex, pos, Vector3.zero);
                    rerollCount++;
                }
            }

            //Even if no item was rerolled on the ground, still reroll a random item in inventory
            if (rerollCount == 0)
                rerollCount = 1;

            var inv = body.master.inventory;
            for (int i = 0; i < rerollCount; i++)
            {
                //Accumulate list of all items in inventory so we can choose one randomly to reroll
                var allItems = new List<ItemIndex>();
                for (var item = ItemIndex.Syringe; item < (ItemIndex)ItemLib.ItemLib.TotalItemCount; item++)
                {
                    for (int k = 0; k < inv.GetItemCount(item); k++)
                    {
                        //Vvoid rerolling 'hidden' (no tier) items and boss items
                        var def = ItemCatalog.GetItemDef(item);
                        if (def.tier != ItemTier.NoTier && def.tier != ItemTier.Boss)
                        {
                            //Will also avoid rerolling the last Gesture
                            //This is primarily so that if using with Gesture, will create a 'shuffle' run
                            if (item != ItemIndex.AutoCastEquipment || inv.GetItemCount(ItemIndex.AutoCastEquipment) > 1)
                            {
                                allItems.Add(item);
                            }
                        }
                    }
                }

                //If no items to reroll, we're done
                if (allItems.Count == 0)
                    break;

                //Replace item with a random item in the same tier
                var rerollItem = _rng.NextElementUniform(allItems);
                var tier = ItemCatalog.GetItemDef(rerollItem).tier;
                var newPickup = _rng.NextElementUniform(GetDropList(tier));
                inv.RemoveItem(rerollItem, 1);
                inv.GiveItem(newPickup.itemIndex, 1);

                //Display message 
                var lostPickup = new PickupIndex(rerollItem);
                var lostText = Util.GenerateColoredString(Language.GetString(lostPickup.GetPickupNameToken()), lostPickup.GetPickupColor());
                var newText = Util.GenerateColoredString(Language.GetString(newPickup.GetPickupNameToken()), newPickup.GetPickupColor());
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"The die has been cast: {lostText} has been lost and {newText} has been gained"
                });
            }
        }

        private List<PickupIndex> GetDropList(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1:
                    return Run.instance.availableTier1DropList;
                case ItemTier.Tier2:
                    return Run.instance.availableTier2DropList;
                case ItemTier.Tier3:
                    return Run.instance.availableTier3DropList;
                case ItemTier.Lunar:
                    return Run.instance.availableLunarDropList;
                case ItemTier.Boss:
                    return Run.instance.availableTier2DropList;
                default:
                    return new List<PickupIndex>();
            }
        }


        private void BuildTexture()
        {
            Debug.Log("Rendering texture for Jester's Dice");

            //We're going to render items onto the faces of the die, which is a d20 (icosohedron)
            //The UV coordinates are arranged in strips of four triangles each, progressing lower left toward upper right,
            //then continuing to the right
            var dx = (float) 1024/11;
            var dy = (float)(dx*Math.Sqrt(3));

            //Compute enclosed circle radius for triangles
            var r = (dy*dy - dx*dx)/(2*dy);

            //The diameter of this circle is then the diagonal dimension of a contained square
            //Compute the width of the square
            var w = (float) (Math.Sqrt(2)*r);

            var oldActive = RenderTexture.active;
            try
            {
                var dieTex = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
                dieTex.SetPixels32(0, 0, dieTex.width, dieTex.height,
                                   Enumerable.Range(0, dieTex.width*dieTex.height)
                                             .Select(i => new Color32(128, 128, 128, 255)).ToArray());

                var renderTex = RenderTexture.GetTemporary(128, 128, 0, RenderTextureFormat.BGRA32,
                                                           RenderTextureReadWrite.Linear);
                for (var i = 0; i < 20; i++)
                {
                    var itemIndex = (ItemIndex)((int)ItemIndex.Syringe + i);
                    var itemDef = ItemCatalog.GetItemDef(itemIndex);
                    var itemTex = (Texture2D)itemDef.pickupIconTexture;
                    if (itemTex != null)
                    {
                        //Compute center of triangle bounding box
                        var col = Math.DivRem(i, 4, out int row);
                        var xAdj = row/2 + 1;
                        var yAdj = (row + 1) / 2;
                        var boxCenter = new Vector2(0.5f + 2*col*dx + xAdj*dx,
                                                    yAdj*dy + 0.5f*dy);

                        //Based on row, triangle orientation flips
                        //Compute the appropriate scalar and adjust from bounding box center to get circle center
                        var dir = (row & 1) == 1 ? -1 : 1;
                        var circleCenter = boxCenter + new Vector2(0, 0.5f*dy*dir - r*dir);

                        //Now we can Blit the icon texture into the inner square of the circle
                        var offset = circleCenter - new Vector2(0.5f*w, 0.5f*w);
                        Graphics.Blit(itemTex, renderTex);
                        RenderTexture.active = renderTex;
                        var tmpTex = new Texture2D(itemTex.width, itemTex.height, TextureFormat.ARGB32, false);
                        try
                        {
                            tmpTex.ReadPixels(new Rect(0, 0, tmpTex.width, tmpTex.height), 0, 0);
                            for (int y = 0; y < (int)w; y++)
                            {
                                for (int x = 0; x < (int)w; x++)
                                {
                                    var src = tmpTex.GetPixelBilinear(x/w,
                                                                      y/w);
                                    var xr = x - 0.5f*w;
                                    var yr = y - 0.5f*w;
                                    var rr = Math.Sqrt(xr*xr + yr*yr);
                                    var alpha = (float)Math.Sqrt((w - 2*rr)/w);
                                    if (!float.IsNaN(alpha))
                                    {
                                        var blended = Color.Lerp(new Color32(128, 128, 128, 255), src, alpha);
                                        dieTex.SetPixel((int)offset.x + x, (int)offset.y + y, blended);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            UnityEngine.Object.Destroy(tmpTex);
                        }
                    }
                }

                //dieTex.ReadPixels(new Rect(0, 0, dieTex.width, dieTex.height), 0, 0);
                dieTex.Apply();

                var material = UmbrellaAssets.JestersDicePrefab.GetComponent<MeshRenderer>().material;
                material.mainTexture = dieTex;
            }
            finally
            {
                RenderTexture.active = oldActive;
            }

        }

        public static CustomEquipment Build()
        {
            UmbrellaAssets.Init();

            var equipDef = new EquipmentDef
            {
                cooldown = 30f,
                pickupModelPath = "",
                pickupIconPath = "",
                pickupToken = "Jester's Dice",
                nameToken = EquipNames.JestersDice,
                descriptionToken = "Jester's Dice",
                canDrop = true,
                enigmaCompatible = true,
                isLunar = true
            };

            //TODO
            var prefab = UmbrellaAssets.JestersDicePrefab;
            var icon = UmbrellaAssets.JestersDiceIcon;
            var rule = new ItemDisplayRule
            {
                ruleType = ItemDisplayRuleType.ParentedPrefab,
                followerPrefab = prefab,
                childName = "Chest",
                localScale = new Vector3(0.15f, 0.15f, 0.15f),
                localAngles = new Vector3(0f, 180f, 0f),
                localPos = new Vector3(-0.35f, -0.1f, 0f)
            };

            return new CustomEquipment(equipDef, prefab, icon, new[] { rule });
        }

    }
}
