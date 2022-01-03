using System.Collections.Generic;

namespace Modules.Games.Mafia.Common.GameRoles.Data;

public class GameRoleData
{
    public const string RootSection = "Games:Mafia:GameRoles";


    public string Name { get; set; } = string.Empty;

    public Dictionary<string, string[]> Phrases { get; set; } = new();
}
