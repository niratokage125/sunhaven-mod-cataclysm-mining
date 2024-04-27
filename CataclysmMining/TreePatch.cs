using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Wish;

namespace CataclysmMining
{
    [HarmonyPatch(typeof(Tree))]
    public static class TreePatch
    {
        [HarmonyPatch("Die"), HarmonyPrefix]
        public static void Die_Prefix(ref float duration, bool hitFromLocalPlayer)
        {
            if (!Plugin.modEnabled.Value || !Plugin.cuttingEnabled.Value || !hitFromLocalPlayer || !Plugin.cataclysmRunning)
            {
                return;
            }
            duration = 0.2f;
        }
    }
}
