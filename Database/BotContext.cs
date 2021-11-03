using Database.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Database
{
    public class BotContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<GuildSettings> GuildSettings => Set<GuildSettings>();
        public DbSet<MafiaStats> MafiaStats => Set<MafiaStats>();
        public DbSet<RussianRouletteStats> RussianRouletteStats => Set<RussianRouletteStats>();


        public BotContext(DbContextOptions<BotContext> options) : base(options)
        {
            Database.EnsureCreated();
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
