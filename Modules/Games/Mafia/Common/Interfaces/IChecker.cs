using System.Collections.Generic;
using Modules.Games.Mafia.Common.GameRoles;

namespace Modules.Games.Mafia.Common.Interfaces;

public interface IChecker
{
    GameRole? CheckedRole { get; }

    IEnumerable<GameRole> CheckableRoles { get; }
}
