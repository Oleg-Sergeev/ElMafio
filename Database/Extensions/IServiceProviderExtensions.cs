using System;
using System.Threading.Tasks;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class IServiceProviderExtensions
{
    public static async Task DatabaseMigrateAsync(this IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<BotContext>();

        if (context.Database.IsSqlServer())
            await context.Database.MigrateAsync();


        context.SeedDatabase();

        await context.SaveChangesAsync();
    }
}
