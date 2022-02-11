namespace Infrastructure.Data.Models.Games.Stats;

public class MafiaStats : GameStats
{
    public int BlacksGamesCount { get; set; }
    public int BlacksWinsCount { get; set; }

    public float BlacksWinRate
    {
        get => BlacksGamesCount != 0 ? (float)BlacksWinsCount / BlacksGamesCount : 0;
        private set { }
    }


    public int DoctorMovesCount { get; set; }
    public int DoctorHealsCount { get; set; }

    public float DoctorEfficiency
    {
        get => DoctorMovesCount != 0 ? (float)DoctorHealsCount / DoctorMovesCount : 0;
        private set { }
    }


    public int SheriffMovesCount { get; set; }
    public int SheriffRevealsCount { get; set; }
    public int SheriffKillsCount { get; set; }

    public float SheriffEfficiency
    {
        get => SheriffMovesCount != 0 ? (float)(SheriffRevealsCount + SheriffKillsCount) / SheriffMovesCount : 0;
        private set { }
    }


    public int DonMovesCount { get; set; }
    public int DonRevealsCount { get; set; }

    public float DonEfficiency
    {
        get => DonMovesCount != 0 ? (float)DonRevealsCount / DonMovesCount : 0;
        private set { }
    }



    public float ExtraScores { get; set; }

    public float PenaltyScores { get; set; }


    public float Scores
    {
        get => WinsCount + BlacksWinsCount + DoctorHealsCount + SheriffRevealsCount + SheriffKillsCount + DonRevealsCount;
        private set { }
    }

    public float Rating
    {
        get => GamesCount != 0 ? 100f * (Scores + ExtraScores - PenaltyScores) / GamesCount : 0;
        private set { }
    }



    public override void Reset()
    {
        base.Reset();

        ExtraScores = 0;

        PenaltyScores = 0;


        BlacksGamesCount = 0;
        BlacksWinsCount = 0;

        DoctorMovesCount = 0;
        DoctorHealsCount = 0;

        SheriffMovesCount = 0;
        SheriffRevealsCount = 0;
        SheriffKillsCount = 0;

        DonMovesCount = 0;
        DonRevealsCount = 0;
    }
}