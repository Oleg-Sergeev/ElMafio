using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings;

namespace Infrastructure.Data.Models.Games.Settings.Mafia;

public class SettingsTemplate
{
    public int Id { get; set; }

    public int MafiaSettingsId { get; set; }

    public string Name { get; set; }

    public string? RoleAmountSubSettingsJsonData { get; private set; }

    public string? RolesInfoSubSettingsJsonData { get; private set; }

    public string? GuildSubSettingsJsonData { get; private set; }

    public string? GameSubSettingsJsonData { get; private set; }




    private RoleAmountSubSettings? _roleAmountSubSettings;

    [NotMapped]
    public RoleAmountSubSettings RoleAmountSubSettings
    {
        get => _roleAmountSubSettings ??= (JsonSerializer.Deserialize<RoleAmountSubSettings>(RoleAmountSubSettingsJsonData ?? "{}") ?? new());
        set
        {
            RoleAmountSubSettingsJsonData = JsonSerializer.Serialize(value);

            _roleAmountSubSettings = value;
        }
    }



    private ServerSubSettings? _serverSubSettings;

    [NotMapped]
    public ServerSubSettings ServerSubSettings
    {
        get => _serverSubSettings ??= (JsonSerializer.Deserialize<ServerSubSettings>(GuildSubSettingsJsonData ?? "{}") ?? new());
        set
        {
            GuildSubSettingsJsonData = JsonSerializer.Serialize(value);

            _serverSubSettings = value;
        }
    }



    private GameSubSettings? _gameSubSettings;

    [NotMapped]
    public GameSubSettings GameSubSettings
    {
        get => _gameSubSettings ??= (JsonSerializer.Deserialize<GameSubSettings>(GameSubSettingsJsonData ?? "{}") ?? new());
        set
        {
            GameSubSettingsJsonData = JsonSerializer.Serialize(value);

            _gameSubSettings = value;
        }
    }


    private RolesInfoSubSettings? _rolesInfoSubSettings;

    [NotMapped]
    public RolesInfoSubSettings RolesInfoSubSettings
    {
        get => _rolesInfoSubSettings ??= (JsonSerializer.Deserialize<RolesInfoSubSettings>(RolesInfoSubSettingsJsonData ?? "{}") ?? new());
        set
        {
            RolesInfoSubSettingsJsonData = JsonSerializer.Serialize(value);

            _rolesInfoSubSettings = value;
        }
    }


    public SettingsTemplate(string name)
    {
        Name = name;
    }
}
