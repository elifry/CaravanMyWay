using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CaravanMyWay
{
    [StaticConstructorOnStartup]
    public static class CaravanItemAssignmentHandler
    {
        static CaravanItemAssignmentHandler()
        {
            var harmony = new Harmony("LucidBound.CaravanMyWay");
            harmony.PatchAll();
            Log.Message("[CaravanMyWay] Harmony patches loaded!");
        }
    }

    // Patch to disable vanilla caravan loading options
    [HarmonyPatch(typeof(FloatMenuMakerMap))]
    [HarmonyPatch("AddHumanlikeOrders")]
    public static class FloatMenuMakerMap_AddHumanlikeOrders_DisableVanillaCaravan_Patch
    {
        public static void Postfix(List<FloatMenuOption> opts)
        {
            // Remove vanilla caravan loading options - catch all variations
            opts.RemoveAll(opt => 
                opt.Label.StartsWith("Load into caravan") || 
                opt.Label.StartsWith("Load specific amount into caravan") ||
                opt.Label.StartsWith("Load all into caravan") ||
                opt.Label.Contains("into caravan") && opt.Label.StartsWith("Load")
            );
        }
    }

    // Our main patch for custom caravan loading
    [HarmonyPatch(typeof(FloatMenuMakerMap))]
    [HarmonyPatch("AddHumanlikeOrders")]
    public static class FloatMenuMakerMap_AddHumanlikeOrders_Patch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (!CaravanFormingUtility.IsFormingCaravan(pawn)) return;

            // Get the thing under the cursor
            IntVec3 c = IntVec3.FromVector3(clickPos);
            Thing thing = c.GetFirstItem(pawn.Map);
            if (thing == null) return;

            // Get the caravan lord
            var lord = CaravanFormingUtility.GetFormAndSendCaravanLord(pawn);
            if (lord == null) return;

            float itemMass = thing.def.BaseMass;  // Mass per item

            // Add the main menu options
            if (thing.stackCount > 1)
            {
                // Add "Load specific amount..." option
                opts.Add(new FloatMenuOption($"Load specific amount of {thing.Label}...", delegate
                {
                    // First determine the maximum possible amount based on the carrier with the most capacity
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
                                // After amount is chosen, show carrier options
                                List<FloatMenuOption> carrierOptions = new List<FloatMenuOption>();
                                foreach (var carrier in lord.ownedPawns.Where(p => MassUtility.Capacity(p) > 0))
                                {
                                    float currentMass = MassUtility.GearAndInventoryMass(carrier);
                                    float maxMass = MassUtility.Capacity(carrier);
                                    float remainingCapacity = maxMass - currentMass;
                                    string optionLabel = $"{carrier.Label} ({currentMass:F1}/{maxMass:F1} kg)";

                                    if (remainingCapacity >= itemMass * amount)
                                    {
                                        carrierOptions.Add(new FloatMenuOption(optionLabel, delegate
                                        {
                                            CreateLoadJob(pawn, thing, carrier, amount);
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
                            });
                        Find.WindowStack.Add(dialog);
                    }
                }));

                // Add "Load all" option
                opts.Add(new FloatMenuOption($"Load all {thing.Label} into caravan", delegate
                {
                    List<FloatMenuOption> carrierOptions = new List<FloatMenuOption>();
                    foreach (var carrier in lord.ownedPawns.Where(p => MassUtility.Capacity(p) > 0))
                    {
                        float currentMass = MassUtility.GearAndInventoryMass(carrier);
                        float maxMass = MassUtility.Capacity(carrier);
                        float remainingCapacity = maxMass - currentMass;
                        string optionLabel = $"{carrier.Label} ({currentMass:F1}/{maxMass:F1} kg)";

                        if (remainingCapacity >= itemMass * thing.stackCount)
                        {
                            carrierOptions.Add(new FloatMenuOption(optionLabel, delegate
                            {
                                CreateLoadJob(pawn, thing, carrier, thing.stackCount);
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
                }));
            }
            else
            {
                // Single item, just show carrier options
                opts.Add(new FloatMenuOption($"Load {thing.Label} into...", delegate
                {
                    List<FloatMenuOption> carrierOptions = new List<FloatMenuOption>();
                    foreach (var carrier in lord.ownedPawns.Where(p => MassUtility.Capacity(p) > 0))
                    {
                        float currentMass = MassUtility.GearAndInventoryMass(carrier);
                        float maxMass = MassUtility.Capacity(carrier);
                        float remainingCapacity = maxMass - currentMass;
                        string optionLabel = $"{carrier.Label} ({currentMass:F1}/{maxMass:F1} kg)";

                        if (remainingCapacity >= itemMass)
                        {
                            carrierOptions.Add(new FloatMenuOption(optionLabel, delegate
                            {
                                CreateLoadJob(pawn, thing, carrier, 1);
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
                }));
            }
        }

        private static FloatMenuOption CreateLoadOption(Pawn pawn, Thing thing, Pawn carrier, int count, string labelOverride = null)
        {
            string label = labelOverride ?? count.ToString();
            string itemLabel = count == 1 ? thing.def.label : thing.LabelNoCount;  // LabelNoCount often includes the plural form
            return new FloatMenuOption($"Load {label} {itemLabel}", delegate
            {
                CreateLoadJob(pawn, thing, carrier, count);
            });
        }

        private static void CreateLoadJob(Pawn pawn, Thing thing, Pawn carrier, int count)
        {
            if (carrier == pawn)
            {
                Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, thing);
                job.count = count;
                pawn.jobs.TryTakeOrderedJob(job);
                Log.Message($"[CaravanMyWay] Ordered {pawn.Label} to pick up {count} {thing.Label}");
            }
            else if (pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly) && pawn.CanReserve(thing))
            {
                Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer, thing, carrier);
                job.count = count;
                job.haulMode = HaulMode.ToCellStorage;
                pawn.jobs.TryTakeOrderedJob(job);
                Log.Message($"[CaravanMyWay] Ordered {pawn.Label} to load {count} {thing.Label} into {carrier.Label}");
            }
            else
            {
                Messages.Message($"{pawn.Label} cannot reach the items", MessageTypeDefOf.RejectInput);
            }
        }
    }
}