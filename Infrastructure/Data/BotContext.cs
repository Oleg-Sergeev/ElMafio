using System.Reflection;
using Infrastructure.Data.Configurations;
using Infrastructure.Data.Models;
using Infrastructure.Data.Models.Games.Settings;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Infrastructure.Data.Models.Games.Stats;
using Infrastructure.Data.Models.Guild;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class BotContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<GuildSettings> GuildSettings => Set<GuildSettings>();

    public DbSet<MafiaStats> MafiaStats => Set<MafiaStats>();
    public DbSet<MafiaSettings> MafiaSettings => Set<MafiaSettings>();
    public DbSet<MafiaSettingsTemplate> MafiaSettingsTemplates => Set<MafiaSettingsTemplate>();


    public DbSet<RussianRouletteStats> RussianRouletteStats => Set<RussianRouletteStats>();
    public DbSet<RussianRouletteSettings> RussianRouletteSettings => Set<RussianRouletteSettings>();

    public DbSet<QuizStats> QuizStats => Set<QuizStats>();


    public BotContext()
    {
        Database.EnsureCreated();
    }

    public BotContext(DbContextOptions<BotContext> options) : base(options)
    {
        Database.EnsureCreated();
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new GameStatsConfiguration<QuizStats>());
        modelBuilder.ApplyConfiguration(new GameStatsConfiguration<RussianRouletteStats>());

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
