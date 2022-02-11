using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Data.Models.Games.Settings;
using Microsoft.EntityFrameworkCore;
using Services;

namespace Modules.Games.Services;


public class GameSettingsService<TSettings> : IGameSettingsService<TSettings> where TSettings : GameSettings, new()
{
    public virtual async Task<TSettings> GetSettingsOrCreateAsync(DbSocketCommandContext context, bool isTracking = true)
    {
        TSettings? settings;
        if (isTracking)
            settings = await context.Db.Set<TSettings>()
               .AsTracking()
               .FirstOrDefaultAsync(s => s.GuildSettingsId == context.Guild.Id);
        else
            settings = await context.Db.Set<TSettings>()
               .AsNoTracking()
               .FirstOrDefaultAsync(s => s.GuildSettingsId == context.Guild.Id);


        settings ??= await CreateSettingsAsync(context);
        return settings;
    }

    protected virtual async Task<TSettings> CreateSettingsAsync(DbSocketCommandContext context)
    {
        TSettings settings = new()
        {
            GuildSettingsId = context.Guild.Id
        };

        context.Db.Set<TSettings>().Add(settings);

        await context.Db.SaveChangesAsync();

        return settings;
    }
}
