namespace Infrastructure.Data.Entities.Games.Stats;

public class MafiaStats : GameStats
{
    public const float KoefHeals = 1.5f;
    public const float KoefReveals = 1.3f;
    public const float KoefBlackWins = 1.25f;

    public int BlacksGamesCount { get; set; }
    public int BlacksWinsCount { get; set; }

    public float BlacksWinRate { get; private set; }


    public int DoctorMovesCount { get; set; }
    public int DoctorHealsCount { get; set; }

    public float DoctorEfficiency { get; private set; }


    public int SheriffMovesCount { get; set; }
    public int SheriffRevealsCount { get; set; }

    public float SheriffEfficiency { get; private set; }


    public int DonMovesCount { get; set; }
    public int DonRevealsCount { get; set; }

    public float DonEfficiency { get; private set; }



    public float ExtraScores { get; set; }

    public float PenaltyScores { get; set; }


    public float Scores { get; private set; }


    public override void Reset()
    {
        base.Reset();

        BlacksGamesCount = 0;
        BlacksWinsCount = 0;
        BlacksWinRate = 0;

        DoctorMovesCount = 0;
        DoctorHealsCount = 0;
        DoctorEfficiency = 0;

        SheriffMovesCount = 0;
        SheriffRevealsCount = 0;
        SheriffEfficiency = 0;

        DonMovesCount = 0;
        DonRevealsCount = 0;
        DonEfficiency = 0;

        ExtraScores = 0;

        PenaltyScores = 0;

        Scores = 0;
    }
}