using RimWorld;
using RimWorld.Planet;
using Verse;
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

    // Patch to prevent auto-departure when waiting is enabled
    [HarmonyPatch(typeof(LordToil_PrepareCaravan_GatherItems))]
    [HarmonyPatch("Notify_ReachedDutyLocation")]
    public static class LordToil_PrepareCaravan_GatherItems_Notify_ReachedDutyLocation_Patch
    {
        public static bool Prefix()
        {
            // If waiting is enabled, prevent the caravan from proceeding to exit
            return !CaravanFormingWaitHandler.WaitToSend;
        }
    }

    // Add a patch for the manual send command
    [HarmonyPatch(typeof(LordToil_PrepareCaravan_GatherItems))]
    [HarmonyPatch("ShouldBeCalledOff")]
    public static class LordToil_PrepareCaravan_GatherItems_ShouldBeCalledOff_Patch
    {
        public static void Postfix(ref bool __result)
        {
            if (CaravanFormingWaitHandler.WaitToSend)
            {
                __result = false;
            }
        }
    }
}