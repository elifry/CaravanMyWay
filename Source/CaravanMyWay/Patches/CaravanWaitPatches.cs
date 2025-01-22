using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;

namespace CaravanMyWay.Patches
{
    // Wait handler patches
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch("GetGizmos")]
    public static class Pawn_GetGizmos_Patch
    {
        public static void Postfix(ref IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (!CaravanMyWayMod.Settings.enableWaitToSend)
                return;

            var lord = CaravanFormingUtility.GetFormAndSendCaravanLord(__instance);
            if (lord == null)
                return;

            var gizmoList = new List<Gizmo>(__result);

            var waitGizmo = new Command_Toggle
            {
                defaultLabel = "Wait to send caravan",
                defaultDesc = "If enabled, the caravan will wait for manual send command instead of departing automatically.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true),
                isActive = () => CaravanHandler.WaitToSend,
                toggleAction = () => CaravanHandler.WaitToSend = !CaravanHandler.WaitToSend
            };

            gizmoList.Add(waitGizmo);
            __result = gizmoList;
        }
    }

    [HarmonyPatch(typeof(LordToil_PrepareCaravan_GatherItems))]
    [HarmonyPatch("ShouldBeCalledOff")]
    public static class LordToil_PrepareCaravan_GatherItems_ShouldBeCalledOff_Patch
    {
        public static void Postfix(ref bool __result)
        {
            if (!CaravanMyWayMod.Settings.enableWaitToSend)
                return;

            if (CaravanHandler.WaitToSend)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Transition))]
    [HarmonyPatch("ShouldTriggerNow")]
    public static class Transition_ShouldTriggerNow_Patch
    {
        public static void Postfix(Transition __instance, ref bool __result)
        {
            if (!CaravanMyWayMod.Settings.enableWaitToSend)
                return;
            
            if (__instance?.sources != null && 
                __instance.sources.Count > 0 && 
                __instance.sources[0]?.lord?.LordJob is LordJob_FormAndSendCaravan && 
                CaravanHandler.WaitToSend)
            {
                __result = false;
            }
        }
    }
}