using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Columbarium
{
    public class Building_MemorialUrnDisplay : Building, IMemorialRecord
    {
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
        public string personName;
        public int birthYear = int.MinValue;
        public int birthQuadrum = -1;
        public int birthDayOfSeasonZeroBased = -1;
        public int deathYear = int.MinValue;
        public int deathQuadrum = -1;
        public int deathDayOfSeasonZeroBased = -1;
        private long legacyBirthAbsTicks = -1L;
        private long legacyDeathAbsTicks = -1L;
        public string causeOfDeath;
        public string memorialNote = "";
        public int flowerIndex = FlowerCatalog.DefaultIndex;
        public float baseUrnMarketValueAtCreation = -1f;
        private Color urnColor = Color.white;
        private bool needsLegacyColorMigration = true;

        string IMemorialRecord.PersonName { get => personName; set => personName = value; }
        int IMemorialRecord.BirthYear { get => birthYear; set => birthYear = value; }
        int IMemorialRecord.BirthQuadrum { get => birthQuadrum; set => birthQuadrum = value; }
        int IMemorialRecord.BirthDayOfSeasonZeroBased { get => birthDayOfSeasonZeroBased; set => birthDayOfSeasonZeroBased = value; }
        int IMemorialRecord.DeathYear { get => deathYear; set => deathYear = value; }
        int IMemorialRecord.DeathQuadrum { get => deathQuadrum; set => deathQuadrum = value; }
        int IMemorialRecord.DeathDayOfSeasonZeroBased { get => deathDayOfSeasonZeroBased; set => deathDayOfSeasonZeroBased = value; }
        string IMemorialRecord.CauseOfDeath { get => causeOfDeath; set => causeOfDeath = value; }
        string IMemorialRecord.MemorialNote { get => memorialNote; set => memorialNote = value; }
        int IMemorialRecord.FlowerIndex { get => flowerIndex; set => flowerIndex = value; }
        float IMemorialRecord.BaseUrnMarketValueAtCreation { get => baseUrnMarketValueAtCreation; set => baseUrnMarketValueAtCreation = value; }

        public void Record(Corpse corpse)
        {
            MemorialRecordUtility.Record(this, corpse);
        }

        public void SetUrnColor(Color color)
        {
            GetComp<CompColorable>()?.SetColor(color);
            urnColor = color;
            needsLegacyColorMigration = false;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            // Debug spawning skips the cremation recipe, so provide a usable memorial record.
            if (!Prefs.DevMode || respawningAfterLoad || !personName.NullOrEmpty()) return;

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
            {
                if (!(thing is Corpse corpse) || corpse.InnerPawn?.ageTracker == null) continue;
                Record(corpse);
                return;
            }

            personName = "Test colonist " + Rand.Range(1000, 10000);
            causeOfDeath = "Debug spawn";
            flowerIndex = Rand.Range(0, 12);
            IMemorialRecord record = this;
            MemorialRecordUtility.SetDateFromAbsoluteTicks(record,
                GenDate.TickGameToAbs(Find.TickManager.TicksGame), birth: false);
            birthYear = deathYear - 30;
            birthQuadrum = deathQuadrum;
            birthDayOfSeasonZeroBased = deathDayOfSeasonZeroBased;
        }

        public override void Notify_RecipeProduced(Pawn worker)
        {
            base.Notify_RecipeProduced(worker);
            if (worker?.CurJob?.RecipeDef?.Worker is RecipeWorker_FillUrn recipeWorker)
                recipeWorker.RegisterProduct(this);
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            return text + (text.NullOrEmpty() ? "" : "\n") + "Columbarium_RemainsOf".Translate(personName.NullOrEmpty() ? "Unknown".Translate().ToString() : personName)
                + "\n" + "Columbarium_MemorialFlower".Translate(FlowerCatalog.FlowerName(flowerIndex), FlowerCatalog.SkillName(flowerIndex));
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!Materials.TryGetValue(DrawColor, out Material material))
            {
                material = GraphicDatabase.Get<Graphic_Single>("Things/UrnWriting", ShaderDatabase.CutoutComplex,
                    DrawSize, DrawColor).MatSingle;
                Materials[DrawColor] = material;
            }
            Graphics.DrawMesh(MeshPool.plane10,
                Matrix4x4.TRS(drawLoc + new Vector3(0f, .01f, 0f), Quaternion.identity,
                    new Vector3(DrawSize.x, 1f, DrawSize.y)), material, 0);
            FlowerCatalog.DrawAt(drawLoc, DrawSize.x, flowerIndex);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
            yield return new Command_Action
            {
                defaultLabel = "Columbarium_MemorialRecord".Translate(),
                defaultDesc = "Columbarium_MemorialRecordDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Memorial"),
                action = () => Find.WindowStack.Add(new Dialog_Memorial(this))
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref personName, "personName");
            Scribe_Values.Look(ref birthYear, "birthYear", int.MinValue);
            Scribe_Values.Look(ref birthQuadrum, "birthQuadrum", -1);
            Scribe_Values.Look(ref birthDayOfSeasonZeroBased, "birthDayOfSeasonZeroBased", -1);
            Scribe_Values.Look(ref deathYear, "deathYear", int.MinValue);
            Scribe_Values.Look(ref deathQuadrum, "deathQuadrum", -1);
            Scribe_Values.Look(ref deathDayOfSeasonZeroBased, "deathDayOfSeasonZeroBased", -1);
            Scribe_Values.Look(ref legacyBirthAbsTicks, "birthAbsTicks", -1L);
            Scribe_Values.Look(ref legacyDeathAbsTicks, "deathAbsTicks", -1L);
            Scribe_Values.Look(ref causeOfDeath, "causeOfDeath");
            Scribe_Values.Look(ref memorialNote, "memorialNote", "");
            Scribe_Values.Look(ref flowerIndex, "flowerIndex", FlowerCatalog.DefaultIndex);
            Scribe_Values.Look(ref baseUrnMarketValueAtCreation, "materialMarketValueAtUrnCreation", -1f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                IMemorialRecord record = this;
                if (!MemorialRecordUtility.ValidDate(birthYear, birthQuadrum, birthDayOfSeasonZeroBased) && legacyBirthAbsTicks != -1L)
                    MemorialRecordUtility.SetDateFromAbsoluteTicks(record, legacyBirthAbsTicks, birth: true);
                if (!MemorialRecordUtility.ValidDate(deathYear, deathQuadrum, deathDayOfSeasonZeroBased) && legacyDeathAbsTicks != -1L)
                    MemorialRecordUtility.SetDateFromAbsoluteTicks(record, legacyDeathAbsTicks, birth: false);
                legacyBirthAbsTicks = -1L;
                legacyDeathAbsTicks = -1L;
            }
            Scribe_Values.Look(ref urnColor, "urnColor", Color.white);
            Scribe_Values.Look(ref needsLegacyColorMigration, "needsLegacyColorMigration", true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && needsLegacyColorMigration)
            {
                GetComp<CompColorable>()?.SetColor(urnColor);
                needsLegacyColorMigration = false;
            }
        }
    }
}
