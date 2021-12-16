using System;

namespace Modules.Games.Mafia.Common.GameRoles.Data;

public class CheckerData : GameRoleData
{
    public string[] SuccessfullCheckPhrases { get; set; } = Array.Empty<string>();

    public string[] FailedCheckPhrases { get; set; } = Array.Empty<string>();
}
