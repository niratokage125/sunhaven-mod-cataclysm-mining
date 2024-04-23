using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wish;

namespace CataclysmMining
{
    [HarmonyPatch(typeof(Cataclysm))]
    public static class CataclysmPatch
    {
        [HarmonyPatch("StartCostume"), HarmonyPostfix]
        public static void StartCostume_Postfix(bool localPlayer, GameObject ___rockSpinPrefab)
        {
            if (!Plugin.modEnabled.Value || !localPlayer)
            {
                return;
            }
            DamageSource source = ___rockSpinPrefab.GetComponent<DamageSource>();
            if (source == null)
            {
                return;
            }
            Plugin.damageRange = source._damageRange;
            Plugin.damageRatio = source.damageRatio;
            Plugin.damageMultiplier = source.damageMultiplier;
            Plugin.cataclysmRunning = true;
            if (Plugin.pickupEnabled.Value)
            {
                Traverse.Create(Player.Instance).Field<FloatRef>("pickupFloatRef").Value.value = 1f;
            }
        }
        [HarmonyPatch("EndCostume"), HarmonyPostfix]
        public static void EndCostume_Postfix(bool localPlayer)
        {
            if (!localPlayer)
            {
                return;
            }
            Plugin.cataclysmRunning = false;
            Plugin.cooldowns.Clear();
            Traverse.Create(Player.Instance).Field<FloatRef>("pickupFloatRef").Value.value = 0f;
        }
        [HarmonyPatch(nameof(Cataclysm.Cooldown), MethodType.Getter), HarmonyPostfix]
        public static void Cooldown_Postfix(ref float __result)
        {
            if (!Plugin.modEnabled.Value)
            {
                return;
            }
            __result *= Plugin.castCooldown.Value;
        }
    }
}
