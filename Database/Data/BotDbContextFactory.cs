using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data;

public class BotDbContextFactory : IDesignTimeDbContextFactory<BotContext>
{
    private const string ConfigPath = @"Data\Configs\AppConfig.json";


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
