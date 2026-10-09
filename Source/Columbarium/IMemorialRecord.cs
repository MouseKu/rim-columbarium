namespace Columbarium
{
    public interface IMemorialRecord
    {
        string PersonName { get; set; }
        int AgeBiologicalYears { get; set; }
        int BirthYear { get; set; }
        int BirthQuadrum { get; set; }
        int BirthDayOfSeasonZeroBased { get; set; }
        int DeathYear { get; set; }
        int DeathQuadrum { get; set; }
        int DeathDayOfSeasonZeroBased { get; set; }
        string CauseOfDeath { get; set; }
        string MemorialNote { get; set; }
        int FlowerIndex { get; set; }
        float BaseUrnMarketValueAtCreation { get; set; }
    }
}
