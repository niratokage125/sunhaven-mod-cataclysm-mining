using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Wish;

namespace CataclysmMining
{
    [HarmonyPatch(typeof(Pickup))]
    public static class PickupPatch
    {
        [HarmonyPatch("Update"), HarmonyPrefix]
        public static bool Update_Prefix(object __instance)
        {
            if (!Plugin.modEnabled.Value || !Plugin.cataclysmRunning)
            {
                return true;
            }
            MyUpdate(__instance);
            return false;
        }

        [HarmonyPatch("Update"), HarmonyReversePatch]
        public static void MyUpdate(object instance)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                {
                    var tmp = new List<CodeInstruction>();
                    for (int i = 0; i < code.Count; i++)
                    {
                        tmp.Add(code[i]);
                        if (code[i].OperandIs(AccessTools.Field(typeof(Pickup), "pickupTime")))
                        {
                            var c = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PickupPatch), nameof(MyPickupTimeMultiplier)));
                            tmp.Add(c);
                        }
                        if (code[i].OperandIs(AccessTools.Field(typeof(Pickup), "radius")))
                        {
                            var c = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PickupPatch), nameof(MyPickupRangeMultiplier)));
                            tmp.Add(c);
                        }
                    }
                    code = tmp;
                }

                return code;
            }
            _ = Transpiler(null);
        }

        public static Single MyPickupTimeMultiplier(Single value)
        {
            return Plugin.dropItemPickupTime.Value * value;
        }
        public static Single MyPickupRangeMultiplier(Single value)
        {
            return Plugin.dropItemPickupRange.Value * value;
        }
    }

}
