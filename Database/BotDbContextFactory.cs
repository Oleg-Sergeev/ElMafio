using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace Database
{
    public class BotDbContextFactory : IDesignTimeDbContextFactory<BotContext>
    {
        private const string ConfigPath = @"data\config.json";


        public BotContext CreateDbContext(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(ConfigPath)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<BotContext>();
            optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultSQLServer"));

            return new BotContext(optionsBuilder.Options);
        }
    }
}