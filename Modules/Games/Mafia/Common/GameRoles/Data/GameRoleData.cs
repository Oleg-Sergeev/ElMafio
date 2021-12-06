using System;
using Modules.Games.Mafia.Common.GameRoles.RolesGroups;

namespace Modules.Games.Mafia.Common.GameRoles.Data;

public class GameRoleData
{
    public const string RootSection = "Games:Mafia:GameRoles";
    public const string InnocentSection = nameof(Innocent);
    public const string MurderSection = nameof(Murder);
    public const string DonSection = nameof(Don);
    public const string DoctorSection = nameof(Doctor);
    public const string SheriffSection = nameof(Sheriff);
    public const string MurderGroupSection = nameof(MurdersGroup);
    public const string AliveGroupSection = nameof(AliveGroup);

    public string Name { get; set; } = string.Empty;

    public string[] YourMovePhrases { get; set; } = Array.Empty<string>();

    public string[] SuccessPhrases { get; set; } = Array.Empty<string>();

    public string[] FailurePhrases { get; set; } = Array.Empty<string>();
}
