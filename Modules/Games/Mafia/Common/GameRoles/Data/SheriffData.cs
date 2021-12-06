using System;

namespace Modules.Games.Mafia.Common.GameRoles.Data;

public class SheriffData : CheckerData
{
    public string[] SuccessfullKillPhrases { get; set; } = Array.Empty<string>();

    public string[] FailedKillPhrases { get; set; } = Array.Empty<string>();
}
