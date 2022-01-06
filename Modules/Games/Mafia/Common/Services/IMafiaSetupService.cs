using System.Threading.Tasks;
using Modules.Games.Mafia.Common.Data;

namespace Modules.Games.Mafia.Common.Services;

public interface IMafiaSetupService
{
    Task SetupGuildAsync(MafiaContext context);

    Task SetupUsersAsync(MafiaContext context);

    void SetupRoles(MafiaContext context);

    Task SendRolesInfoAsync(MafiaContext context);
}
