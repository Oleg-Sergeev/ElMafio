using Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class BotContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<GuildSettings> GuildSettings => Set<GuildSettings>();

        public DbSet<MafiaStats> MafiaStats => Set<MafiaStats>();
        public DbSet<MafiaSettings> MafiaSettings => Set<MafiaSettings>();

        public DbSet<RussianRouletteStats> RussianRouletteStats => Set<RussianRouletteStats>();
        public DbSet<RussianRouletteSettings> RussianRouletteSettings => Set<RussianRouletteSettings>();



        public BotContext(DbContextOptions<BotContext> options) : base(options)
        {
            Database.EnsureCreated();
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RussianRouletteSettings>()
                .Property(s => s.UnicodeSmileKilled)
                    .HasDefaultValue("💀");

            modelBuilder.Entity<RussianRouletteSettings>()
                .Property(s => s.UnicodeSmileSurvived)
                    .HasDefaultValue("😎");


            modelBuilder.Entity<GuildSettings>()
                .Property(g => g.Prefix)
                .HasDefaultValue("/");

            modelBuilder.Entity<RussianRouletteStats>()
                .HasKey(nameof(GameStats.UserId), nameof(GameStats.GuildId));

            modelBuilder.Entity<MafiaStats>()
                .HasKey(nameof(GameStats.UserId), nameof(GameStats.GuildId));
        }
    }
}
