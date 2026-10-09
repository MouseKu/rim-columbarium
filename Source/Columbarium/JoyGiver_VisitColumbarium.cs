using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Columbarium
{
    public class JoyGiver_VisitColumbarium : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null || def.thingDefs == null) return null;

            bool allowedOutside = JoyUtility.EnjoyableOutsideNow(pawn);
            Building_Columbarium selected = null;
            float totalWeight = 0f;
            foreach (ThingDef thingDef in def.thingDefs)
            {
                foreach (Thing thing in map.listerThings.ThingsOfDef(thingDef))
                {
                    Building_Columbarium columbarium = thing as Building_Columbarium;
                    if (columbarium == null || columbarium.Faction != Faction.OfPlayer ||
                        columbarium.Fogged() ||
                        !FactionUtility.IsPoliticallyProper(columbarium, pawn) ||
                        columbarium.IsForbidden(pawn) ||
                        VacuumUtility.VacuumConcernTo(columbarium, pawn) ||
                        !pawn.CanReserve(columbarium) ||
                        !columbarium.TryFindFrontVisitCell(pawn, allowedOutside, out IntVec3 _))
                        continue;

                    // Match vanilla's preference for nearby graves without allocating a candidate list.
                    float weight = Mathf.Max(150f - (columbarium.Position - pawn.Position).LengthHorizontal, 5f);
                    totalWeight += weight;
                    if (Rand.Chance(weight / totalWeight)) selected = columbarium;
                }
            }
            return selected == null ? null : JobMaker.MakeJob(def.jobDef, selected);
        }
    }
}
