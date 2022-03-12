using Infrastructure.Data.Entities.Games.Settings.Mafia.SubSettings;

namespace Infrastructure.Data.Entities.Games.Settings.Mafia;

public class MafiaSettingsTemplate
{
    public const string DefaultTemplateName = "__Default";


    public int Id { get; set; }

    public int MafiaSettingsId { get; set; }


    public string Name { get; set; }


    public GameSubSettings GameSubSettings { get; set; }

    public ServerSubSettings ServerSubSettings { get; set; }

    public RoleAmountSubSettings RoleAmountSubSettings { get; set; }

    public RolesExtraInfoSubSettings RolesExtraInfoSubSettings { get; set; }


    public MafiaSettingsTemplate()
    {
        Name = DefaultTemplateName;


        GameSubSettings = new();

        ServerSubSettings = new();

        RoleAmountSubSettings = new();

        RolesExtraInfoSubSettings = new();
    }
}
