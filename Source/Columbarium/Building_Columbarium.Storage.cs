using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Columbarium
{
    // Keeps the save container, player commands, and hauling order together.
    public partial class Building_Columbarium
    {
        public bool HasStoredMemorials => urns != null && urns.Count > 0;

        public ThingOwner GetDirectlyHeldThings() => urns;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, urns);
        }

        public bool Accepts(Thing thing)
        {
            return urns.Count < Capacity && IsMemorialUrn(thing) && urns.CanAcceptAnyOf(thing);
        }

        public bool TryAcceptThing(Thing thing)
        {
            if (!Accepts(thing)) return false;
            if (thing.holdingOwner != null)
                return thing.holdingOwner.TryTransferToContainer(thing, urns, 1) == 1;
            if (thing.Spawned) thing.DeSpawn();
            return urns.TryAdd(thing);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref urns, "urns", this);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            // Return excess urns if a save contains more than this building can hold.
            while (urns.Count > Capacity)
            {
                Thing excess = urns[urns.Count - 1];
                if (!urns.TryDrop(excess, Position, map, ThingPlaceMode.Near, out Thing _)) break;
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            IntVec3 pos = Position;
            if (map != null && urns.Count > 0) urns.TryDropAll(pos, map, ThingPlaceMode.Near);
            base.Destroy(mode);
            urns.ClearAndDestroyContents();
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            return text + (text.NullOrEmpty() ? "" : "\n") + "Columbarium_UrnsStored".Translate(urns.Count, Capacity);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
            if (urns.Count < Capacity)
                yield return new Command_Action
                {
                    defaultLabel = "Columbarium_StoreUrn".Translate(),
                    defaultDesc = "Columbarium_StoreUrnDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("Things/UrnWriting"),
                    action = ChooseUrn
                };
            if (urns.Count > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Columbarium_MemorialRecords".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Memorial"),
                    action = ShowMemorialRecords
                };
                yield return new Command_Action
                {
                    defaultLabel = "Columbarium_RemoveUrn".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Open"),
                    action = ShowRemoveUrn
                };
            }
        }

        private void ChooseUrn()
        {
            var options = new List<FloatMenuOption>();
            var candidates = new List<Thing>();
            foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing))
            {
                if (IsMemorialUrn(thing)) candidates.Add(thing);
            }
            foreach (Thing thing in candidates)
            {
                if (!Accepts(thing)) continue;
                Thing selected = thing;
                options.Add(new FloatMenuOption(MemorialRecordOf(thing)?.PersonName ?? "Unknown".Translate(), () => OrderHaul(selected)));
            }
            if (options.Count == 0) options.Add(new FloatMenuOption("Columbarium_NoAvailableUrns".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ShowMemorialRecords()
        {
            var options = urns.InnerListForReading.Select(u => new FloatMenuOption(
                MemorialRecordOf(u)?.PersonName ?? "Unknown".Translate(),
                () => Find.WindowStack.Add(new Dialog_Memorial(MemorialRecordOf(u))))).ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ShowRemoveUrn()
        {
            var options = urns.InnerListForReading.Select(u => new FloatMenuOption(
                MemorialRecordOf(u)?.PersonName ?? "Unknown".Translate(),
                () => RemoveUrnNear(u))).ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void RemoveUrnNear(Thing urn)
        {
            CellRect occupied = GenAdj.OccupiedRect(Position, Rotation, def.size);
            int southZ = occupied.minZ - 1;
            int centerX = (occupied.minX + occupied.maxX) / 2;
            IntVec3 dropCell = new IntVec3(centerX, Position.y, southZ);
            if (!dropCell.InBounds(Map)) dropCell = Position;
            if (urns.TryDrop(urn, dropCell, Map, ThingPlaceMode.Near, out Thing _)) return;

            Messages.Message("Columbarium_NoSpaceNearBuilding".Translate(),
                MessageTypeDefOf.RejectInput, false);
        }

        private void OrderHaul(Thing urn)
        {
            foreach (Pawn hauler in Map.mapPawns.FreeColonistsSpawned)
            {
                if (!hauler.CanReserveAndReach(urn, PathEndMode.ClosestTouch, Danger.Some) ||
                    !hauler.CanReserveAndReach(this, PathEndMode.Touch, Danger.Some)) continue;
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ColumbariumStoreUrn"), urn, this);
                job.count = 1;
                if (hauler.jobs.TryTakeOrderedJob(job)) return;
            }
            Messages.Message("Columbarium_NoColonistCanHaulUrn".Translate(), MessageTypeDefOf.RejectInput, false);
        }

        private static bool IsMemorialUrn(Thing thing)
        {
            return thing is MinifiedThing minified && minified.InnerThing is Building_MemorialUrnDisplay;
        }

        private static IMemorialRecord MemorialRecordOf(Thing thing)
        {
            if (thing is MinifiedThing minified) thing = minified.InnerThing;
            return thing as IMemorialRecord;
        }

    }
}
