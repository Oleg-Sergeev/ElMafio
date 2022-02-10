using System.Collections.Generic;

namespace Modules.Games.Mafia.Common.GameRoles;

public interface IChecker
{
    protected const string SuccessfullCheckPhrases = "SuccessfullCheck";
    protected const string FailedCheckPhrases = "FailedCheck";

    GameRole? CheckedRole { get; }

    IEnumerable<GameRole> CheckableRoles { get; }
}
