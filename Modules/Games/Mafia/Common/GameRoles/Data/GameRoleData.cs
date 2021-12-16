using System;
using System.Collections.Generic;
using System.Linq;
using Core.Extensions;
using Modules.Games.Mafia.Common.GameRoles.RolesGroups;

namespace Modules.Games.Mafia.Common.GameRoles.Data;

public class GameRoleData
{
    public const string RootSection = "Games:Mafia:GameRoles";


    public static readonly IEnumerable<string> Sections;



    static GameRoleData()
    {
        Sections = typeof(GameRole).GetAllDerivedTypes()
            .Select(t => t.Name);
    }


    public string Name { get; set; } = string.Empty;

    public string[] YourMovePhrases { get; set; } = Array.Empty<string>();

    public string[] SuccessPhrases { get; set; } = Array.Empty<string>();

    public string[] FailurePhrases { get; set; } = Array.Empty<string>();
}
