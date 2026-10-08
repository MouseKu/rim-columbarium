using RimWorld;
using Verse;
using Verse.AI;

namespace Columbarium
{
    public class JobDriver_VisitColumbarium : JobDriver_VisitJoyThing
    {
        protected override void WaitTickAction(int delta)
        {
            Building_Columbarium columbarium = job.GetTarget(TargetIndex.A).Thing as Building_Columbarium;
            if (columbarium == null || !columbarium.HasStoredMemorials)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

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
