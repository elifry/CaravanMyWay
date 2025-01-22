using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace CaravanMyWay
{
    [StaticConstructorOnStartup]
    public static class CaravanHandler
    {
        private static bool waitToSend = false;
        public static bool WaitToSend
        {
            get => waitToSend;
            set => waitToSend = value;
        }

        public static float GetPendingMass(Pawn carrier)
        {
            float pendingMass = 0f;
            
            if (carrier.jobs?.jobQueue != null)
            {
                foreach (var jobQueueEntry in carrier.jobs.jobQueue)
                {
                    pendingMass += GetJobMass(jobQueueEntry.job);
                }
            }

            if (carrier.CurJob != null)
            {
                pendingMass += GetJobMass(carrier.CurJob);
            }

            return pendingMass;
        }

        private static float GetJobMass(Job job)
        {
            if ((job.def == JobDefOf.TakeInventory || job.def == JobDefOf.HaulToContainer) && 
                job.targetA.Thing != null)
            {
                Thing thing = job.targetA.Thing;
                return thing.def.BaseMass * job.count;
            }
            return 0f;
        }

        public static float GetAvailableCapacity(Pawn carrier)
        {
            float currentMass = MassUtility.GearAndInventoryMass(carrier);
            float pendingMass = GetPendingMass(carrier);
            float maxMass = MassUtility.Capacity(carrier);
            return maxMass - (currentMass + pendingMass);
        }

        public static void CreateLoadJob(Pawn pawn, Thing thing, Pawn carrier, int count)
        {
            if (carrier == pawn)
            {
                Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, thing);
                job.count = count;
                pawn.jobs.TryTakeOrderedJob(job);
                ModLogger.Message($"Ordered {pawn.Label} to pick up {count} {thing.Label}");
            }
            else if (pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly) && pawn.CanReserve(thing))
            {
                Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer, thing, carrier);
                job.count = count;
                job.haulMode = HaulMode.ToCellStorage;
                pawn.jobs.TryTakeOrderedJob(job);
                ModLogger.Message($"Ordered {pawn.Label} to load {count} {thing.Label} into {carrier.Label}");
            }
            else
            {
                Messages.Message($"{pawn.Label} cannot reach the items", MessageTypeDefOf.RejectInput);
            }
        }
    }
}