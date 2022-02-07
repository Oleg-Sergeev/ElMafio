using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings;

namespace Infrastructure.Data.Models.Games.Settings.Mafia;

public class MafiaSettingsTemplate
{
    private const string DefaultTemplateName = "_Default";


    public int Id { get; set; }

    public int MafiaSettingsId { get; set; }


    public string Name { get; set; } = DefaultTemplateName;

    public string? PreGameMessage { get; set; }


    public GameSubSettings GameSubSettings { get; set; } = new();

    public ServerSubSettings ServerSubSettings { get; set; } = new();

    public RoleAmountSubSettings RoleAmountSubSettings { get; set; } = new();

    public RolesExtraInfoSubSettings RolesExtraInfoSubSettings { get; set; } = new();
}
