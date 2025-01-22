using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CaravanMyWay
{
    public static class CaravanMenuHandler
    {
        public static void AddCaravanOptions(List<FloatMenuOption> opts, Pawn pawn, Thing thing, Lord lord)
        {
            float itemMass = thing.def.BaseMass;

            if (thing.stackCount > 1)
            {
                AddStackableItemOptions(opts, pawn, thing, lord, itemMass);
            }
            else
            {
                AddSingleItemOption(opts, pawn, thing, lord);
            }
        }

        private static void AddStackableItemOptions(List<FloatMenuOption> opts, Pawn pawn, Thing thing, Lord lord, float itemMass)
        {
            // Add "Load specific amount..." option
            opts.Add(new FloatMenuOption($"Load {thing.def.label} into caravan...", delegate
            {
                float maxCapacity = lord.ownedPawns
                    .Where(p => MassUtility.Capacity(p) > 0)
                    .Max(p => MassUtility.Capacity(p) - MassUtility.GearAndInventoryMass(p));
                
                int maxPossible = (int)(maxCapacity / itemMass);
                maxPossible = Math.Min(maxPossible, thing.stackCount);

                if (maxPossible > 0)
                {
                    Dialog_Slider dialog = new Dialog_Slider(
                        "Choose amount", 1, maxPossible,
                        delegate(int amount)
                        {
                            ShowCarrierOptions(pawn, thing, lord, amount, itemMass);
                        });
                    Find.WindowStack.Add(dialog);
                }
            }));

            // Add "Load all" option
            opts.Add(new FloatMenuOption($"Load {thing.Label} x{thing.stackCount} (all) into caravan", delegate
            {
                ShowCarrierOptions(pawn, thing, lord, thing.stackCount, itemMass);
            }));
        }

        private static void AddSingleItemOption(List<FloatMenuOption> opts, Pawn pawn, Thing thing, Lord lord)
        {
            opts.Add(new FloatMenuOption($"Load {thing.Label} into caravan", delegate
            {
                ShowCarrierOptions(pawn, thing, lord, 1, thing.def.BaseMass);
            }));
        }

        private static void ShowCarrierOptions(Pawn pawn, Thing thing, Lord lord, int amount, float itemMass)
        {
            List<FloatMenuOption> carrierOptions = new List<FloatMenuOption>();
            foreach (var carrier in lord.ownedPawns.Where(p => MassUtility.Capacity(p) > 0))
            {
                float availableCapacity = CaravanHandler.GetAvailableCapacity(carrier);
                float currentMass = MassUtility.GearAndInventoryMass(carrier);
                float maxMass = MassUtility.Capacity(carrier);
                string optionLabel = $"{carrier.Label} ({currentMass:F1}/{maxMass:F1} kg)";

                if (availableCapacity >= itemMass * amount)
                {
                    carrierOptions.Add(new FloatMenuOption(optionLabel, delegate
                    {
                        CaravanHandler.CreateLoadJob(pawn, thing, carrier, amount);
                    }));
                }
                else
                {
                    carrierOptions.Add(new FloatMenuOption(optionLabel + " - Too heavy", null)
                    {
                        Disabled = true
                    });
                }
            }

            Find.WindowStack.Add(new FloatMenu(carrierOptions));
        }
    }
}