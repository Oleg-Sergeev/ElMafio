using System.Collections.Generic;
using Modules.Games.Mafia.Common.GameRoles;

namespace Modules.Games.Mafia.Common.Interfaces;

public interface IChecker
{
    protected const string SuccessfullCheckPhrases = "SuccessfullCheck";
    protected const string FailedCheckPhrases = "FailedCheck";

    GameRole? CheckedRole { get; }

    IEnumerable<GameRole> CheckableRoles { get; }
}
