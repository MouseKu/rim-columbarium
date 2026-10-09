using RimWorld;
using Verse;
using Verse.AI;

namespace Columbarium
{
    public partial class Building_Columbarium
    {
        public bool TryFindFrontVisitCell(Pawn pawn, bool allowedOutside, out IntVec3 visitCell)
        {
            visitCell = IntVec3.Invalid;
            if (Map == null || pawn?.Map != Map || !HasStoredMemorials || this.IsForbidden(pawn))
                return false;

            CellRect occupied = GenAdj.OccupiedRect(Position, Rotation, def.size);
            bool horizontalFront = Rotation == Rot4.South || Rotation == Rot4.North;
            int min = horizontalFront ? occupied.minX : occupied.minZ;
            int max = horizontalFront ? occupied.maxX : occupied.maxZ;
            int center = (min + max) / 2;

            // Horizontal buildings are viewed from below. Vertical buildings can be
            // viewed from either side, so a blocked side does not prevent visits.
            for (int distance = 0; distance <= max - min; distance++)
            {
                if (TryFrontCoordinate(center + distance, occupied, horizontalFront, pawn,
                        allowedOutside, oppositeSide: false, out visitCell) ||
                    !horizontalFront && TryFrontCoordinate(center + distance, occupied, false, pawn,
                        allowedOutside, oppositeSide: true, out visitCell))
                    return true;
                if (distance > 0 &&
                    TryFrontCoordinate(center - distance, occupied, horizontalFront, pawn,
                        allowedOutside, oppositeSide: false, out visitCell))
                    return true;
                if (distance > 0 && !horizontalFront &&
                    TryFrontCoordinate(center - distance, occupied, false, pawn,
                        allowedOutside, oppositeSide: true, out visitCell))
                    return true;
            }

            visitCell = IntVec3.Invalid;
            return false;
        }

        private bool TryFrontCoordinate(int coordinate, CellRect occupied, bool horizontalFront,
            Pawn pawn, bool allowedOutside, bool oppositeSide, out IntVec3 visitCell)
        {
            visitCell = IntVec3.Invalid;
            int min = horizontalFront ? occupied.minX : occupied.minZ;
            int max = horizontalFront ? occupied.maxX : occupied.maxZ;
            if (coordinate < min || coordinate > max) return false;

            visitCell = horizontalFront
                ? new IntVec3(coordinate, Position.y, occupied.minZ - 1)
                : new IntVec3(oppositeSide ? occupied.minX - 1 : occupied.maxX + 1,
                    Position.y, coordinate);
            return visitCell.InBounds(Map) && visitCell.Standable(Map) &&
                   (allowedOutside || visitCell.Roofed(Map)) &&
                   !VacuumUtility.VacuumConcernTo(visitCell, pawn) &&
                   pawn.CanReserveAndReach(visitCell, PathEndMode.OnCell, Danger.Some);
        }
    }
}
