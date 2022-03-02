using Infrastructure.Data.Models.Games.Stats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class MafiaStatsConfiguration : GameStatsConfiguration<MafiaStats>
{
    private static readonly string ScoresComputed =
            $"CAST(([{nameof(MafiaStats.WinsCount)}] " +
            $"+ [{nameof(MafiaStats.BlacksWinsCount)}] * {MafiaStats.KoefBlackWins} " +
            $"+ [{nameof(MafiaStats.DoctorHealsCount)}] * {MafiaStats.KoefHeals} " +
            $"+ [{nameof(MafiaStats.SheriffRevealsCount)}] * {MafiaStats.KoefReveals} " +
            $"+ [{nameof(MafiaStats.DonRevealsCount)}] * {MafiaStats.KoefReveals}) AS REAL)";


    public override void Configure(EntityTypeBuilder<MafiaStats> builder)
    {
        base.Configure(builder);


        builder.Property(ms => ms.BlacksWinRate)
            .HasComputedColumnSql($"" +
            $"IIF([{nameof(MafiaStats.BlacksGamesCount)}] != 0, " +
            $"CAST([{nameof(MafiaStats.BlacksWinsCount)}] AS REAL) / [{nameof(MafiaStats.BlacksGamesCount)}], " +
            $"0.0)", true);

        builder.Property(ms => ms.DoctorEfficiency)
            .HasComputedColumnSql($"" +
            $"IIF([{nameof(MafiaStats.DoctorMovesCount)}] != 0, " +
            $"CAST([{nameof(MafiaStats.DoctorHealsCount)}] AS REAL) / [{nameof(MafiaStats.DoctorMovesCount)}], " +
            $"0.0)", true);

        builder.Property(ms => ms.SheriffEfficiency)
            .HasComputedColumnSql($"" +
            $"IIF([{nameof(MafiaStats.SheriffMovesCount)}] != 0, " +
            $"CAST([{nameof(MafiaStats.SheriffRevealsCount)}] AS REAL) / [{nameof(MafiaStats.SheriffMovesCount)}], " +
            $"0.0)", true);

        builder.Property(ms => ms.DonEfficiency)
            .HasComputedColumnSql($"" +
            $"IIF([{nameof(MafiaStats.DonMovesCount)}] != 0, " +
            $"CAST([{nameof(MafiaStats.DonRevealsCount)}] AS REAL) / [{nameof(MafiaStats.DonMovesCount)}], " +
            $"0.0)", true);


        builder.Property(ms => ms.Scores)
            .HasComputedColumnSql(ScoresComputed, true);


        builder.Property(ms => ms.Rating)
            .HasComputedColumnSql($"" +
            $"IIF([{nameof(MafiaStats.GamesCount)}] != 0, " +
            $"100.0 * ({ScoresComputed} + [{nameof(MafiaStats.ExtraScores)}] - [{nameof(MafiaStats.PenaltyScores)}]) " +
            $"* ({WinRateComputed}), " +
            $"0.0)", true);
    }
}
