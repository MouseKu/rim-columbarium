using RimWorld;
using UnityEngine;
using Verse;

namespace Columbarium
{
    public class Dialog_Memorial : Window
    {
        private readonly IMemorialRecord record;
        private bool editing;
        private string draft;

        public override Vector2 InitialSize => new Vector2(460f, 470f);

        public Dialog_Memorial(IMemorialRecord record)
        {
            this.record = record;
            draft = record.MemorialNote ?? "";
            doCloseButton = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 35), "Columbarium_MemorialRecordTitle".Translate());
            Text.Font = GameFont.Small;
            Widgets.DrawLineHorizontal(0, 38, inRect.width);

            Text.Anchor = TextAnchor.MiddleCenter;
            string name = record.PersonName.NullOrEmpty() ? "Unknown".Translate().ToString() : record.PersonName;
            if (record.AgeBiologicalYears >= 0)
                name += " (" + record.AgeBiologicalYears + ")";
            Widgets.Label(new Rect(10, 43, inRect.width - 20, 30),
                name);
            Widgets.Label(new Rect(10, 73, inRect.width - 20, 24), DateText(record, birth: true));
            Widgets.Label(new Rect(10, 97, inRect.width - 20, 24), DateText(record, birth: false));
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.DrawLineHorizontal(0, 126, inRect.width);

            if (editing)
            {
                draft = Widgets.TextArea(new Rect(10, 135, inRect.width - 20, 145), draft);
                float buttonWidth = (inRect.width - 30f) * .5f;
                if (Widgets.ButtonText(new Rect(10, 290, buttonWidth, 36), "Columbarium_Finish".Translate()) && !draft.NullOrEmpty())
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "Columbarium_EpitaphCannotChange".Translate(),
                        () =>
                        {
                            record.MemorialNote = draft;
                            editing = false;
                        }, false, "Columbarium_EngraveEpitaph".Translate()));
                }
                if (Widgets.ButtonText(new Rect(20 + buttonWidth, 290, buttonWidth, 36), "Columbarium_Cancel".Translate()))
                {
                    draft = record.MemorialNote ?? "";
                    editing = false;
                }
            }
            else
            {
                string epitaph = record.MemorialNote.NullOrEmpty() ? "" : record.MemorialNote;
                Widgets.Label(new Rect(10, 135, inRect.width - 20, 145), epitaph);
                if (record.MemorialNote.NullOrEmpty() &&
                    Widgets.ButtonText(new Rect(10, 290, inRect.width - 20, 36), "Columbarium_EngraveEpitaph".Translate()))
                {
                    draft = "";
                    editing = true;
                }
            }

            Widgets.DrawLineHorizontal(0, 340, inRect.width);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(10, 350, inRect.width - 20, 28),
                "Columbarium_MemorialFlower".Translate(FlowerCatalog.FlowerName(record.FlowerIndex),
                    FlowerCatalog.SkillName(record.FlowerIndex)));
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static string DateText(IMemorialRecord record, bool birth)
        {
            int year = birth ? record.BirthYear : record.DeathYear;
            int quadrumValue = birth ? record.BirthQuadrum : record.DeathQuadrum;
            int dayZeroBased = birth ? record.BirthDayOfSeasonZeroBased : record.DeathDayOfSeasonZeroBased;
            string date = "Columbarium_UnknownDate".Translate();
            if (MemorialRecordUtility.ValidDate(year, quadrumValue, dayZeroBased))
            {
                Quadrum quadrum = (Quadrum)quadrumValue;
                int day = dayZeroBased + 1;
                string ordinal = Find.ActiveLanguageWorker.OrdinalNumber(day, Gender.None);
                date = "FullDate".Translate(ordinal, quadrum.Label(), year);
            }
            return (birth ? "Born" : "Columbarium_Died").Translate(date);
        }
    }
}
