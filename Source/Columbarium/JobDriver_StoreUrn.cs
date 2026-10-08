using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Columbarium
{
    public class JobDriver_StoreUrn : JobDriver
    {
        private Thing Urn => TargetThingA;
        private Building_Columbarium Columbarium => TargetThingB as Building_Columbarium;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Jobs already queued in a save may still have the default count of -1.
            job.count = 1;
            return pawn.Reserve(TargetThingA, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(TargetThingB, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            yield return Toils_General.DoAtomic(() =>
            {
                if (Columbarium == null || Urn == null || !Columbarium.TryAcceptThing(Urn))
                {
                    if (pawn.carryTracker.CarriedThing != null)
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
                }
            });
        }
    }
}
