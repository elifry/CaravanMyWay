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
    public static class CaravanItemAssignmentHandler
    {
        // Static helper methods moved back into containing class
        private static float GetPendingMass(Pawn carrier)
        {
            float pendingMass = 0f;
            
            // Check all jobs in the queue
            if (carrier.jobs?.jobQueue != null)
            {
                foreach (var jobQueueEntry in carrier.jobs.jobQueue)
                {
                    var job = jobQueueEntry.job;
                    // Check for inventory loading jobs
                    if ((job.def == JobDefOf.TakeInventory || job.def == JobDefOf.HaulToContainer) && job.targetA.Thing != null)
                    {
                        Thing thing = job.targetA.Thing;
                        pendingMass += thing.def.BaseMass * job.count;
                    }
                }
            }

            // Check current job if it's a loading job
            if (carrier.CurJob != null)
            {
                var job = carrier.CurJob;
                if ((job.def == JobDefOf.TakeInventory || job.def == JobDefOf.HaulToContainer) && job.targetA.Thing != null)
                {
                    Thing thing = job.targetA.Thing;
                    pendingMass += thing.def.BaseMass * job.count;
                }
            }

            return pendingMass;
        }

        private static float GetAvailableCapacity(Pawn carrier)
        {
            float currentMass = MassUtility.GearAndInventoryMass(carrier);
            float pendingMass = GetPendingMass(carrier);
            float maxMass = MassUtility.Capacity(carrier);
            return maxMass - (currentMass + pendingMass);
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

        [HarmonyPatch(typeof(FloatMenuMakerMap))]
        [HarmonyPatch("AddHumanlikeOrders")]
        public static class FloatMenuMakerMap_AddHumanlikeOrders_Patch
        {
            public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
            {
                // First remove vanilla options
                opts.RemoveAll(opt => 
                    opt.Label.StartsWith("Load into caravan") || 
                    opt.Label.StartsWith("Load specific amount into caravan") ||
                    opt.Label.StartsWith("Load all into caravan") ||
                    opt.Label.Contains("into caravan") && opt.Label.StartsWith("Load")
                );

                // Then add our custom options
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
                    opts.Add(new FloatMenuOption($"Load {thing.def.label} into caravan...", delegate
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
                                        float availableCapacity = GetAvailableCapacity(carrier);
                                        float currentMass = MassUtility.GearAndInventoryMass(carrier);
                                        float maxMass = MassUtility.Capacity(carrier);
                                        string optionLabel = $"{carrier.Label} ({currentMass:F1}/{maxMass:F1} kg)";

                                        if (availableCapacity >= itemMass * amount)
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
                    opts.Add(new FloatMenuOption($"Load {thing.Label} x{thing.stackCount} (all) into caravan", delegate
                    {
                        List<FloatMenuOption> carrierOptions = new List<FloatMenuOption>();
                        foreach (var carrier in lord.ownedPawns.Where(p => MassUtility.Capacity(p) > 0))
                        {
                            float availableCapacity = GetAvailableCapacity(carrier);
                            float currentMass = MassUtility.GearAndInventoryMass(carrier);
                            float maxMass = MassUtility.Capacity(carrier);
                            string optionLabel = $"{carrier.Label} ({currentMass:F1}/{maxMass:F1} kg)";

                            if (availableCapacity >= itemMass * thing.stackCount)
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
                    opts.Add(new FloatMenuOption($"Load {thing.Label} into caravan", delegate
                    {
                        List<FloatMenuOption> carrierOptions = new List<FloatMenuOption>();
                        foreach (var carrier in lord.ownedPawns.Where(p => MassUtility.Capacity(p) > 0))
                        {
                            float availableCapacity = GetAvailableCapacity(carrier);
                            float currentMass = MassUtility.GearAndInventoryMass(carrier);
                            float maxMass = MassUtility.Capacity(carrier);
                            string optionLabel = $"{carrier.Label} ({currentMass:F1}/{maxMass:F1} kg)";

                            if (availableCapacity >= itemMass)
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
        }
    }
}