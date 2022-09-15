using System.Reflection;
using Infrastructure.Data.Configurations;
using Infrastructure.Data.Entities;
using Infrastructure.Data.Entities.Games.Settings;
using Infrastructure.Data.Entities.Games.Settings.Mafia;
using Infrastructure.Data.Entities.Games.Stats;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class BotContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Server> Servers => Set<Server>();

    public DbSet<MafiaStats> MafiaStats => Set<MafiaStats>();
    public DbSet<MafiaSettings> MafiaSettings => Set<MafiaSettings>();
    public DbSet<MafiaSettingsTemplate> MafiaSettingsTemplates => Set<MafiaSettingsTemplate>();


    public DbSet<RussianRouletteStats> RussianRouletteStats => Set<RussianRouletteStats>();
    public DbSet<RussianRouletteSettings> RussianRouletteSettings => Set<RussianRouletteSettings>();

    public DbSet<QuizStats> QuizStats => Set<QuizStats>();

    public DbSet<ServerUser> ServerUsers => Set<ServerUser>();

    public DbSet<AccessLevel> AccessLevels => Set<AccessLevel>();


    public BotContext()
    {
    }

    public BotContext(DbContextOptions<BotContext> options) : base(options)
    {
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new GameStatsConfiguration<QuizStats>());
        modelBuilder.ApplyConfiguration(new GameStatsConfiguration<RussianRouletteStats>());

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
