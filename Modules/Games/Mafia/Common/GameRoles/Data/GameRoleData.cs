using System;

namespace Modules.Games.Mafia.Common.GameRoles.Data;

public class GameRoleData
{
    public const string RootSection = "Games:Mafia:GameRoles";
    public const string InnocentSection = nameof(Innocent);
    public const string MurderSection = nameof(Murder);

    public string Name { get; set; } = string.Empty;

    public string[] SuccessPhrases { get; set; } = Array.Empty<string>();

    public string[] FailurePhrases { get; set; } = Array.Empty<string>();
}
