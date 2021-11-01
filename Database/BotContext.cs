using Database.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Database
{
    public class BotContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<GuildSettings> GuildSettings { get; set; }
        public DbSet<MafiaStats> MafiaStats { get; set; }
        public DbSet<RussianRouletteStats> RussianRouletteStats { get; set; }


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
