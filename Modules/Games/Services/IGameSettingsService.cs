using System.Threading.Tasks;
using Infrastructure.Data.Models.Games.Settings;
using Services;

namespace Modules.Games.Services;

public interface IGameSettingsService<TSettings> where TSettings : GameSettings, new()
{
    public Task<TSettings> GetSettingsOrCreateAsync(DbSocketCommandContext context, bool isTracking = true);
}
