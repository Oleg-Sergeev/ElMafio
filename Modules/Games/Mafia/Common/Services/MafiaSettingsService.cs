using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Microsoft.EntityFrameworkCore;
using Modules.Games.Services;
using Services;

namespace Modules.Games.Mafia.Common.Services;

public class MafiaSettingsService : GameSettingsService<MafiaSettings>
{
    public override async Task<MafiaSettings> GetSettingsOrCreateAsync(DbSocketCommandContext context, bool isTracking = true)
    {
        var settingsQuery = isTracking
            ? context.Db.MafiaSettings.AsTracking()
            : context.Db.MafiaSettings.AsNoTracking();

        settingsQuery
            .Include(m => m.CurrentTemplate!)
                .ThenInclude(c => c.GameSubSettings)
            .Include(m => m.CurrentTemplate!)
                .ThenInclude(c => c.ServerSubSettings)
            .Include(m => m.CurrentTemplate!)
                .ThenInclude(c => c.RoleAmountSubSettings)
            .Include(m => m.CurrentTemplate!)
                .ThenInclude(c => c.RolesExtraInfoSubSettings);


        var settings = await settingsQuery
            .Include(s => s.CurrentTemplate)
            .FirstOrDefaultAsync(m => m.GuildSettingsId == context.Guild.Id);

        settings ??= await CreateSettingsAsync(context);

        if (settings.CurrentTemplate is null)
        {
            var template = new MafiaSettingsTemplate()
            {
                MafiaSettingsId = settings.Id
            };

            await context.Db.MafiaSettingsTemplates.AddAsync(template);

            await context.Db.SaveChangesAsync();


            settings.CurrentTemplateId = template.Id;

            await context.Db.SaveChangesAsync();
        }


        return settings;
    }
}