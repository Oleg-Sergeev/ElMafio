using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data.Models.Games.Settings.Mafia;

public class MafiaSettings : GameSettings
{
    public const string DefaultTemplateName = "_Current";

    public string CurrentTemplateName { get; set; }

    public List<SettingsTemplate> SettingsTemplates { get; private set; } = null!;


    [NotMapped]
    public SettingsTemplate Current { get; set; }


    public MafiaSettings()
    {
        CurrentTemplateName = "_Default";

        Current = new(CurrentTemplateName);
    }
}