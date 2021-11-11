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
    public int DoctorSuccessfullMovesCount { get; set; }

    public float DoctorEfficiency
    {
        get => DoctorMovesCount != 0 ? (float)DoctorSuccessfullMovesCount / DoctorMovesCount : 0;
        private set { }
    }


    public int CommissionerMovesCount { get; set; }
    public int CommissionerSuccessfullMovesCount { get; set; }

    public float CommissionerEfficiency
    {
        get => CommissionerMovesCount != 0 ? (float)CommissionerSuccessfullMovesCount / CommissionerMovesCount : 0;
        private set { }
    }


    public int DonMovesCount { get; set; }
    public int DonSuccessfullMovesCount { get; set; }

    public float DonEfficiency
    {
        get => DonMovesCount != 0 ? (float)DonSuccessfullMovesCount / DonMovesCount : 0;
        private set { }
    }



    public float ExtraScores { get; set; }

    public float PenaltyScores { get; set; }


    public float Scores
    {
        get => WinsCount + BlacksWinsCount + DoctorSuccessfullMovesCount + CommissionerSuccessfullMovesCount + DonSuccessfullMovesCount;
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
        DoctorSuccessfullMovesCount = 0;

        CommissionerMovesCount = 0;
        CommissionerSuccessfullMovesCount = 0;

        DonMovesCount = 0;
        DonSuccessfullMovesCount = 0;
    }
}