namespace Infrastructure.Data.Models.Games.Settings
{
    public class RussianRouletteSettings : GameSettings
    {
        public string UnicodeSmileKilled { get; set; } = null!;
        public string UnicodeSmileSurvived { get; set; } = null!;

        public string? CustomSmileKilled { get; set; }
        public string? CustomSmileSurvived { get; set; }
    }
}
