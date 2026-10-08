using RimWorld;
using Verse;

namespace Columbarium
{
    internal static class MemorialRecordUtility
    {
        public static bool ValidDate(int year, int quadrum, int dayOfSeasonZeroBased)
        {
            return year != int.MinValue && quadrum >= 0 && quadrum <= 3 &&
                   dayOfSeasonZeroBased >= 0 && dayOfSeasonZeroBased <= 14;
        }

        public static void Record(IMemorialRecord record, Corpse corpse)
        {
            Pawn pawn = corpse?.InnerPawn;
            if (record == null || pawn?.ageTracker == null) return;

            record.FlowerIndex = FlowerCatalog.ForPawn(pawn);
            record.PersonName = pawn.Name?.ToStringFull ?? pawn.LabelCap;
            Pawn_AgeTracker age = pawn.ageTracker;
            record.BirthYear = age.BirthYear;
            record.BirthQuadrum = (int)age.BirthQuadrum;
            record.BirthDayOfSeasonZeroBased = age.BirthDayOfSeasonZeroBased;

            // Corpse.PostMake stores TickManager.TicksGame in this public Int32 field.
            // It is always a game tick on a normally created corpse.
            int deathAbsTicks = GenDate.TickGameToAbs(corpse.timeOfDeath);
            SetDateFromAbsoluteTicks(record, deathAbsTicks, birth: false);

            record.CauseOfDeath = "Unknown";
            Hediff best = null;
            Hediff lethal = null;
            if (pawn.health?.hediffSet?.hediffs != null)
            {
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff is Hediff_Injury && (best == null || hediff.Severity > best.Severity)) best = hediff;
                    if (hediff.def.lethalSeverity >= 0f && hediff.Severity >= hediff.def.lethalSeverity &&
                        (lethal == null || hediff.Severity > lethal.Severity)) lethal = hediff;
                }
            }
            if (lethal != null) best = lethal;
            if (best != null)
                record.CauseOfDeath = best.LabelCap + (best.Part != null ? " (" + best.Part.Label + ")" : "");
        }

        public static bool SetDateFromAbsoluteTicks(IMemorialRecord record, long absoluteTicks, bool birth)
        {
            if (record == null) return false;
            try
            {
                int year = GenDate.Year(absoluteTicks, 0f);
                int quadrum = (int)GenDate.Quadrum(absoluteTicks, 0f);
                int day = GenDate.DayOfSeason(absoluteTicks, 0f);
                if (quadrum < 0 || quadrum > 3 || day < 0 || day > 14) return false;

                if (birth)
                {
                    record.BirthYear = year;
                    record.BirthQuadrum = quadrum;
                    record.BirthDayOfSeasonZeroBased = day;
                }
                else
                {
                    record.DeathYear = year;
                    record.DeathQuadrum = quadrum;
                    record.DeathDayOfSeasonZeroBased = day;
                }
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
