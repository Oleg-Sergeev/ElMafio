using Infrastructure.Data.Models.Games.Stats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class GameStatsConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : GameStats
{
    protected const string WinRateComputed = $"CAST([{nameof(GameStats.WinsCount)}] AS REAL) / [{nameof(GameStats.GamesCount)}]";


    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(nameof(GameStats.UserId), nameof(GameStats.GuildSettingsId));

        builder.Property(s => s.WinRate)
            .HasComputedColumnSql(
            $"IIF([{nameof(GameStats.GamesCount)}] != 0, " +
            $"{WinRateComputed}, " +
            $"0.0)", true);

        builder.Property(s => s.Rating)
            .HasComputedColumnSql(
            $"IIF([{nameof(GameStats.GamesCount)}] != 0, " +
            $" {WinRateComputed} * [{nameof(GameStats.WinsCount)}] * 100.0, " +
            $"0.0)", true);
    }
}
