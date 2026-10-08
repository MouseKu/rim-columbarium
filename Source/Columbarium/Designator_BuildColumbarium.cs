using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Columbarium
{
    public abstract class Designator_BuildColumbarium : Designator_Build
    {
        protected Designator_BuildColumbarium(string defName)
            : base(DefDatabase<ThingDef>.GetNamed(defName))
        {
        }

        private void KeepTwoAxes()
        {
            if (placingRot == Rot4.North) placingRot = Rot4.South;
            else if (placingRot == Rot4.West) placingRot = Rot4.East;
        }

        public override void SelectedUpdate()
        {
            base.SelectedUpdate();
            KeepTwoAxes();
        }

        public override void DoExtraGuiControls(float leftX, float bottomY)
        {
            base.DoExtraGuiControls(leftX, bottomY);
            KeepTwoAxes();
        }

        public override void SelectedProcessInput(Event ev)
        {
            KeepTwoAxes();
            base.SelectedProcessInput(ev);
            KeepTwoAxes();
        }

        public override void ProcessInput(Event ev)
        {
            KeepTwoAxes();
            base.ProcessInput(ev);
            KeepTwoAxes();
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 cell)
        {
            KeepTwoAxes();
            return base.CanDesignateCell(cell);
        }

        public override void DesignateSingleCell(IntVec3 cell)
        {
            KeepTwoAxes();
            base.DesignateSingleCell(cell);
        }

        public override void RenderHighlight(List<IntVec3> dragCells)
        {
            KeepTwoAxes();
            base.RenderHighlight(dragCells);
        }
    }

    public sealed class Designator_BuildEightCompact : Designator_BuildColumbarium
    {
        public Designator_BuildEightCompact() : base("ColumbariumModularCompactTall") { }
    }

    public sealed class Designator_BuildEight : Designator_BuildColumbarium
    {
        public Designator_BuildEight() : base("ColumbariumModularTall") { }
    }

    public sealed class Designator_BuildThirtyTwoCompact : Designator_BuildColumbarium
    {
        public Designator_BuildThirtyTwoCompact() : base("ColumbariumThirtyTwoCompact") { }
    }

    public sealed class Designator_BuildThirtyTwoLarge : Designator_BuildColumbarium
    {
        public Designator_BuildThirtyTwoLarge() : base("ColumbariumThirtyTwoLarge") { }
    }
}
