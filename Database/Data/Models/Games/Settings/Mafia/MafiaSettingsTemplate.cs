using Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings;

namespace Infrastructure.Data.Models.Games.Settings.Mafia;

public class MafiaSettingsTemplate
{
    private const string DefaultTemplateName = "__Default";


    public int Id { get; set; }

    public int MafiaSettingsId { get; set; }


    public string Name { get; set; }

    public string? PreGameMessage { get; set; }


    public GameSubSettings GameSubSettings { get; set; }

    public ServerSubSettings ServerSubSettings { get; set; }

    public RoleAmountSubSettings RoleAmountSubSettings { get; set; }

    public RolesExtraInfoSubSettings RolesExtraInfoSubSettings { get; set; }


    public MafiaSettingsTemplate()
    {
        Name = DefaultTemplateName;


        GameSubSettings = null!;

        ServerSubSettings = null!;

        RoleAmountSubSettings = null!;

        RolesExtraInfoSubSettings = null!;
    }
}
