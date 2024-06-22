using HarmonyLib;
using PSS;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using Wish;
using static DG.Tweening.DOTweenModuleUtils;

namespace CataclysmMining
{
    [HarmonyPatch(typeof(Player))]
    public static class PlayerPatch
    {
        [HarmonyPatch("Update"), HarmonyPostfix]
        public static void Update_Postfix()
        {
            if (!Plugin.modEnabled.Value || !Plugin.cataclysmRunning)
            {
                Plugin.lastInteraction = 0f;
                Plugin.interactives.Clear();
                return;
            }

            Plugin.playPickupSound = true;
            Plugin.lastInteraction += Time.deltaTime;
            if (Plugin.interactCooldown.Value > 0f && Plugin.lastInteraction > Plugin.interactCooldown.Value && Plugin.interactives.Count > 0)
            {
                var interactive = Plugin.interactives.Dequeue();
                if (interactive is IInteractable interactable && interactable.InteractionPoint.canInteract)
                {
                    interactable.Interact(0);
                }
                Plugin.lastInteraction = 0f;
            }
            else if (Plugin.interactCooldown.Value <= 0f)
            {
                while (Plugin.interactives.Count > 0)
                {
                    var interactive = Plugin.interactives.Dequeue();
                    if (interactive is IInteractable interactable && interactable.InteractionPoint.canInteract)
                    {
                        interactable.Interact(0);
                    }
                }
                Plugin.lastInteraction = 0f;
            }
            
            
            Plugin.cooldowns.ForEach(x => x.cooldown -= Time.deltaTime);
            Plugin.cooldowns.RemoveAll(x => x.cooldown <= 0f);

            var transform = Player.Instance.transform;
            if (Plugin.miningEnabled.Value)
            {
                var range = Math.Max(Plugin.miningRange.Value, 1);
                List<Rock> list = Utilities.CircleCast<Rock>(transform.position, (float)range);
                for (int i = 0; i < list.Count; i++)
                {
                    Rock rock = list[i];
                    if (rock == null || !rock.Pickaxeable || Plugin.cooldowns.Any(x => x.decoration == rock))
                    {
                        continue;
                    }
                    bool crit = Player.Instance.GetStat(StatType.Crit) > UnityEngine.Random.value;
                    float damage = Damage(crit);
                    rock.Hit(damage, 100f, crit, true, true, 0.4f, false);
                    Plugin.cooldowns.Add(new Plugin.DecorationHitCooldown
                    {
                        decoration = rock,
                        cooldown = Plugin.hitCooldown.Value
                    });
                    UnityAction<Vector3Int, Vector2, short, bool> onPickaxeHit = Pickaxe.onPickaxeHit;
                    if (onPickaxeHit == null)
                    {
                        return;
                    }
                    onPickaxeHit(rock.Position, new Vector2(rock.Dying ? 10000f : damage, 99f), rock.sceneID, crit);
                }
            }
            if (Plugin.cuttingEnabled.Value)
            {
                var pos = Player.Instance.ExactPosition;
                var subRange = Math.Max(Plugin.cuttingRange.Value,1) * 6;
                var trees = new List<Tree>();
                var woods = new List<Wood>();
                for(var i = -subRange; i <= subRange; i++)
                {
                    for (var j = -subRange; j <= subRange; j++)
                    {
                        var x = pos.x * 6 + i;
                        var y = pos.y * 6 + j;
                        Decoration decoration;
                        if (!SingletonBehaviour<GameManager>.Instance.TryGetObjectSubTile<Decoration>(new Vector3Int((int)x, (int)y, 0), out decoration))
                        {
                            continue;
                        }
                        if (decoration is Tree tree && !trees.Contains(tree))
                        {
                            trees.Add(tree);
                        }
                        if(decoration is Wood wood && !woods.Contains(wood))
                        {
                            woods.Add(wood);
                        }
                    }
                }

                foreach (var tree in trees)
                {
                    if (!tree.Axeable || Plugin.cooldowns.Any(x => x.decoration == tree))
                    {
                        continue;
                    }
                    if (!Plugin.cuttingNotFullyGrown.Value && !Traverse.Create(tree).Property<bool>("FullyGrown").Value)
                    {
                        continue;
                    }
                    bool crit = Player.Instance.GetStat(StatType.Crit) > UnityEngine.Random.value;
                    float damage = Damage(crit);
                    tree.Hit(damage, transform.position, crit, true);
                    Plugin.cooldowns.Add(new Plugin.DecorationHitCooldown
                    {
                        decoration = tree,
                        cooldown = Plugin.hitCooldown.Value
                    });
                    UnityAction<Vector3Int, Vector2, short, bool> onAxeHit = Axe.onAxeHit;
                    if (onAxeHit == null)
                    {
                        return;
                    }
                    onAxeHit(tree.Position, new Vector2(tree.Dying ? 10000f : damage, 99f), tree.sceneID, crit);
                }
                foreach (var wood in woods) {
                    if (!wood.Axeable || Plugin.cooldowns.Any(x => x.decoration == wood))
                    {
                        continue;
                    }
                    if (!Plugin.cuttingFruitTrees.Value && wood is ForageTree)
                    {
                        continue;
                    }
                    if(!Plugin.cuttingNotFullyGrown.Value && wood is ForageTree forage &&  !Traverse.Create(forage).Property<bool>("FullyGrown").Value)
                    {
                        continue;
                    }
                    bool crit = Player.Instance.GetStat(StatType.Crit) > UnityEngine.Random.value;
                    float damage = Damage(crit);
                    wood.Hit(damage, 99f, crit, true, false);
                    Plugin.cooldowns.Add(new Plugin.DecorationHitCooldown
                    {
                        decoration = wood,
                        cooldown = Plugin.hitCooldown.Value
                    });
                    UnityAction<Vector3Int, Vector2, short, bool> onAxeHit = Axe.onAxeHit;
                    if (onAxeHit == null)
                    {
                        return;
                    }
                    onAxeHit(wood.Position, new Vector2(wood ? 10000f : damage, 99f), wood.sceneID, crit);
                }
            }
            if (Plugin.harvestingEnabled.Value)
            {
                var pos = Player.Instance.ExactPosition;
                var subRange = Math.Max(Plugin.harvestingRange.Value, 1) * 6;
                var crops = new List<Crop>();
                var trees = new List<ForageTree>();
                for (var i = -subRange; i <= subRange; i++)
                {
                    for (var j = -subRange; j <= subRange; j++)
                    {
                        var x = pos.x * 6 + i;
                        var y = pos.y * 6 + j;
                        Decoration decoration;
                        if (!SingletonBehaviour<GameManager>.Instance.TryGetObjectSubTile<Decoration>(new Vector3Int((int)x, (int)y, 0), out decoration))
                        {
                            continue;
                        }
                        if (decoration is Crop crop && !crops.Contains(crop))
                        {
                            crops.Add(crop);
                        }
                        if (decoration is ForageTree tree && !trees.Contains(tree))
                        {
                            trees.Add(tree);
                        }
                    }
                }
                foreach (var crop in crops)
                {
                    if  (Plugin.cooldowns.Any(x => x.decoration == crop))
                    {
                        continue;
                    }
                    if (!Plugin.harvestingFlowers.Value && crop.SeedData.isFlower)
                    {
                        continue;
                    }
                    if (crop.data.frozen)
                    {
                        crop.UnFreeze();
                    }
                    if (crop.data.entangled)
                    {
                        crop.UnEntangle();
                    }
                    if (crop.IsOnFire)
                    {
                        crop.PutOutFire();
                    }
                    if (Plugin.harvestingInfuse.Value)
                    {
                        if ((!crop.data.manaInfused && crop.SeedData.manaInfusable) || crop.data.dead)
                        {
                            if (!Plugin.cooldowns.Any(x => x.decoration == crop) && !Plugin.interactives.Contains(crop))
                            {
                                Plugin.cooldowns.Add(new Plugin.DecorationHitCooldown
                                {
                                    decoration = crop,
                                    cooldown = Plugin.hitCooldown.Value
                                });
                                if (crop.InteractionPoint.canInteract)
                                {
                                    Plugin.interactives.Enqueue(crop);
                                }
                            }
                        }
                    }
                    if (!crop.CheckGrowth)
                    {
                        continue;
                    }
                    bool crit = Player.Instance.GetStat(StatType.Crit) > UnityEngine.Random.value;
                    float damage = Damage(crit);
                    crop.ReceiveDamage(new DamageInfo
                    {
                        damage = damage,
                        damageType = DamageType.Player,
                        crit = crit,
                        hitPoint = Vector3.one * 100000f,
                        hitType = HitType.Scythe,
                        knockBack = 0f,
                        sender = transform,
                        trueDamage = true,
                        canReflect = false
                    });
                }
                if (Plugin.harvestingShake.Value)
                {
                    foreach (var tree in trees)
                    {
                        if (Plugin.cooldowns.Any(x => x.decoration == tree) || Plugin.interactives.Contains(tree))
                        {
                            continue;
                        }
                        Plugin.cooldowns.Add(new Plugin.DecorationHitCooldown
                        {
                            decoration = tree,
                            cooldown = Plugin.hitCooldown.Value
                        });
                        if (!tree.InteractionPoint.canInteract)
                        {
                            continue;
                        }
                        Plugin.interactives.Enqueue(tree);
                    }
                }
            }
            if (Plugin.pickupEnabled.Value)
            {
                var pos = Player.Instance.ExactPosition;
                var range = Math.Max(Plugin.harvestingRange.Value, 1);
                var subRange = range * 6;
                var pickups = new List<Decoration>();
                for (var i = -subRange; i <= subRange; i++)
                {
                    for (var j = -subRange; j <= subRange; j++)
                    {
                        var x = pos.x * 6 + i;
                        var y = pos.y * 6 + j;
                        Decoration decoration;
                        if (!SingletonBehaviour<GameManager>.Instance.TryGetObjectSubTile<Decoration>(new Vector3Int((int)x, (int)y, 0), out decoration))
                        {
                            continue;
                        }
                        if (decoration is PickupDecoration pickup && !pickups.Contains(pickup))
                        {
                            pickups.Add(pickup);
                        }
                        if (decoration is Forageable forage && !pickups.Contains(forage))
                        {
                            pickups.Add(forage);
                        }
                    }
                }

                List<GoldenPomegranatePickup> pomegranates = Utilities.CircleCast<GoldenPomegranatePickup>(transform.position, (float)range);
                pickups.AddRange(pomegranates);

                foreach (var pickup in pickups)
                {
                    if (Plugin.cooldowns.Any(x => x.decoration == pickup) || Plugin.interactives.Contains(pickup))
                    {
                        continue;
                    }
                    Plugin.cooldowns.Add(new Plugin.DecorationHitCooldown
                    {
                        decoration = pickup,
                        cooldown = Plugin.hitCooldown.Value
                    });
                    if (!pickup.InteractionPoint.canInteract)
                    {
                        continue;
                    }
                    Plugin.interactives.Enqueue(pickup);
                }
            }
        }

        private static float Damage(bool crit)
        {
            Player player = Player.Instance;
            float multiplier = Plugin.damageMultiplier;
            float damage = UnityEngine.Random.Range(Mathf.Lerp(Plugin.damageRange.x, Plugin.damageRange.y, player.GetStat(StatType.Accuracy)), Plugin.damageRange.y);
            damage += player.GetStat(StatType.SpellDamage) * Plugin.damageRatio;
            multiplier *= 1f + player.GetStat(StatType.Power);
            multiplier *= 1f + player.GetStat(StatType.SpellPower);
            return (damage * multiplier + player.GetStat(StatType.FlatDamage)) * (crit ? (1.5f + player.GetStat(StatType.CritDamage)) : 1f);
        }

        [HarmonyPatch("Pickup"), HarmonyPrefix]
        public static bool Pickup_Prefix(object __instance, int item, int amount, bool rollForExtra)
        {
            if (!Plugin.modEnabled.Value || !Plugin.cataclysmRunning)
            {
                return true;
            }
            MyPickup(__instance,item,amount,rollForExtra);
            return false;
        }

        public static void MyPickup(object instance, int item, int amount, bool rollForExtra)
        {
            var player = instance as Player;
            if (player == null)
            {
                return;
            }
            Database.GetData<ItemData>(item, delegate (ItemData data)
            {
                if (Plugin.playPickupSound)
                {
                    AudioManager.Instance.PlayAudio(SingletonBehaviour<Prefabs>.Instance.pickupSound, 0.4f, 0f);
                    Plugin.playPickupSound = false;
                }
                if (rollForExtra)
                {
                    if (GameSave.Exploration.GetNode("Exploration1c", true) && Utilities.Chance((float)GameSave.Exploration.GetNodeAmount("Exploration1c", 3, true) * 0.1f + 0.1f))
                    {
                        amount++;
                    }
                    if (Utilities.Chance(player.GetStat(StatType.ExtraForageableChance)))
                    {
                        amount++;
                    }
                }
                player.Inventory.AddItem(data.GenerateItem(), amount, 0, true, true, true);
            }, null);
            player.lastPickupTime = Time.time;
        }

        [HarmonyPatch("AddPauseObject"), HarmonyPrefix]
        public static bool AddPauseObject_Prefix(string id)
        {
            if (!Plugin.modEnabled.Value || !Plugin.cataclysmRunning)
            {
                return true;
            }
            if( id == "forage")
            {
                return false;
            }
            return true;
        }
    }
}
