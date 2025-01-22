using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;

namespace CaravanMyWay
{
    public static class CaravanFormingWaitHandler
    {
        private static bool waitToSend = false;

        public static bool WaitToSend
        {
            get => waitToSend;
            set => waitToSend = value;
        }
    }

    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch("GetGizmos")]
    public static class Pawn_GetGizmos_Patch
    {
        public static void Postfix(ref IEnumerable<Gizmo> __result, Pawn __instance)
        {
            // Only add our gizmo if this pawn is part of a forming caravan
            var lord = CaravanFormingUtility.GetFormAndSendCaravanLord(__instance);
            if (lord == null)
                return;

            var gizmoList = new List<Gizmo>(__result);

            var waitGizmo = new Command_Toggle
            {
                defaultLabel = "Wait to send caravan",
                defaultDesc = "If enabled, the caravan will wait for manual send command instead of departing automatically.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true),
                isActive = () => CaravanFormingWaitHandler.WaitToSend,
                toggleAction = () => CaravanFormingWaitHandler.WaitToSend = !CaravanFormingWaitHandler.WaitToSend
            };

            gizmoList.Add(waitGizmo);
            __result = gizmoList;
        }
    }

    // Patch the transition to exit phase
    [HarmonyPatch(typeof(LordToil_PrepareCaravan_GatherItems))]
    [HarmonyPatch("LordToilTick")]
    public static class LordToil_PrepareCaravan_GatherItems_LordToilTick_Patch
    {
        public static bool Prefix(LordToil_PrepareCaravan_GatherItems __instance)
        {
            if (CaravanFormingWaitHandler.WaitToSend)
            {
                // Keep them in the gathering phase
                return false;
            }
            return true;
        }
    }

    // Patch all transitions in caravan forming
    [HarmonyPatch(typeof(Transition))]
    [HarmonyPatch("ShouldTriggerNow")]
    public static class Transition_ShouldTriggerNow_Patch
    {
        public static bool Prefix(Transition __instance, ref bool __result)
        {
            // Check if this transition is part of a caravan forming lord
            if (__instance.sources[0].lord?.LordJob is LordJob_FormAndSendCaravan && CaravanFormingWaitHandler.WaitToSend)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}