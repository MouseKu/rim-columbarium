using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Columbarium
{
    public class JoyGiver_VisitColumbarium : JoyGiver
    {
        private static readonly string[] ColumbariumDefs = {
            "ColumbariumModularCompactTall", "ColumbariumModularTall",
            "ColumbariumThirtyTwoCompact", "ColumbariumThirtyTwoLarge"
        };

        public override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Map == null) return null;
            List<Thing> candidates = new List<Thing>();
            foreach (string defName in ColumbariumDefs)
            {
                ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (thingDef == null) continue;
                foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(thingDef))
                {
                    Building_Columbarium columbarium = thing as Building_Columbarium;
                    if (columbarium == null || !columbarium.HasStoredMemorials || columbarium.IsForbidden(pawn)) continue;
                    if (pawn.CanReserveAndReach(columbarium, PathEndMode.Touch, Danger.Some))
                        candidates.Add(columbarium);
                }
            }
            if (candidates.Count == 0) return null;
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ColumbariumVisitMemorial"), candidates.RandomElement());
        }
    }
}
