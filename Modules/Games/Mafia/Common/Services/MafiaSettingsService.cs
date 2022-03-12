using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Data.Entities.Games.Settings.Mafia;
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

        settingsQuery = settingsQuery
                .Include(ms => ms.CurrentTemplate!)
                    .ThenInclude(c => c.GameSubSettings)
                .Include(ms => ms.CurrentTemplate!)
                    .ThenInclude(c => c.ServerSubSettings)
                .Include(ms => ms.CurrentTemplate!)
                    .ThenInclude(c => c.RoleAmountSubSettings)
                .Include(ms => ms.CurrentTemplate!)
                    .ThenInclude(c => c.RolesExtraInfoSubSettings);


        var settings = await settingsQuery
            .FirstOrDefaultAsync(m => m.ServerId == context.Guild.Id);

        settings ??= await CreateSettingsAsync(context);

        if (settings.CurrentTemplate is null)
        {
            var template = new MafiaSettingsTemplate()
            {
                MafiaSettingsId = settings.Id
            };

            context.Db.MafiaSettingsTemplates.Add(template);

            await context.Db.SaveChangesAsync();


            settings.CurrentTemplateId = template.Id;

            settings.CurrentTemplate = template;

            await context.Db.SaveChangesAsync();
        }

        return settings;
    }
}