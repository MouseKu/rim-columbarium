using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Columbarium
{
    public class JobDriver_VisitColumbarium : JobDriver_VisitJoyThing
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Building_Columbarium columbarium = job.GetTarget(TargetIndex.A).Thing as Building_Columbarium;
            if (columbarium == null || !columbarium.TryFindFrontVisitCell(pawn,
                    JoyUtility.EnjoyableOutsideNow(pawn), out IntVec3 visitCell))
                return false;

            job.SetTarget(TargetIndex.B, visitCell);
            return base.TryMakePreToilReservations(errorOnFailed) &&
                   pawn.Reserve(visitCell, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            if (!job.GetTarget(TargetIndex.B).IsValid)
            {
                EndJobWith(JobCondition.Incompletable);
                yield break;
            }

            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            Toil wait = Toils_General.Wait(job.def.joyDuration, TargetIndex.A);
            wait.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            wait.tickIntervalAction = WaitTickAction;
            wait.AddFinishAction(() => JoyUtility.TryGainRecRoomThought(pawn));
            yield return wait;
        }

        protected override void WaitTickAction(int delta)
        {
            Building_Columbarium columbarium = job.GetTarget(TargetIndex.A).Thing as Building_Columbarium;
            if (columbarium == null || !columbarium.HasStoredMemorials)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            bool horizontal = columbarium.Rotation == Rot4.South || columbarium.Rotation == Rot4.North;
            pawn.Rotation = horizontal ? Rot4.North :
                pawn.Position.x < columbarium.Position.x ? Rot4.East : Rot4.West;

            float joyGainFactor = 1f;
            Room room = pawn.GetRoom(RegionType.Set_All);
            if (room != null)
                joyGainFactor *= room.GetStat(RoomStatDefOf.GraveVisitingJoyGainFactor);

            pawn.GainComfortFromCellIfPossible(delta);
            JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.EndJob,
                joyGainFactor, columbarium);
        }
    }
}
