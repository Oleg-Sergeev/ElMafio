using System;
using Core.Common;
using Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data;

public class BotDbContextFactory : IDesignTimeDbContextFactory<BotContext>
{
    public BotContext CreateDbContext(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(Constants.AppConfigPath)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<BotContext>();
        optionsBuilder.UseSqlServer(config.GetConnectionStringDebugDb());

        return new BotContext(optionsBuilder.Options);
    }
}