using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CaravanMyWay.Patches
{
    [HarmonyPatch(typeof(FloatMenuMakerMap))]
    [HarmonyPatch("AddHumanlikeOrders")]
    public static class FloatMenuMakerMap_AddHumanlikeOrders_DisableVanillaCaravan_Patch
    {
        public static void Postfix(List<FloatMenuOption> opts)
        {
            if (!CaravanMyWayMod.Settings.enableEnhancedMenus)
                return;

            opts.RemoveAll(opt => 
                opt.Label.StartsWith("Load into caravan") || 
                opt.Label.StartsWith("Load specific amount into caravan") ||
                opt.Label.StartsWith("Load all into caravan") ||
                opt.Label.Contains("into caravan") && opt.Label.StartsWith("Load")
            );
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap))]
    [HarmonyPatch("AddHumanlikeOrders")]
    public static class FloatMenuMakerMap_AddHumanlikeOrders_Patch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (!CaravanMyWayMod.Settings.enableEnhancedMenus)
                return;

            try
            {
                if (!CaravanFormingUtility.IsFormingCaravan(pawn))
                {
                    return;
                }

                IntVec3 c = IntVec3.FromVector3(clickPos);
                Thing thing = c.GetFirstItem(pawn.Map);
                if (thing == null)
                {
                    ModLogger.Debug("No item found at click position");
                    return;
                }

                var lord = CaravanFormingUtility.GetFormAndSendCaravanLord(pawn);
                if (lord == null)
                {
                    ModLogger.Debug("No caravan lord found");
                    return;
                }

                CaravanMenuHandler.AddCaravanOptions(opts, pawn, thing, lord);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error in AddHumanlikeOrders patch: {ex}");
            }
        }
    }
}