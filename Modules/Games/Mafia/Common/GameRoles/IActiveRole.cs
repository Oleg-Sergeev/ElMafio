namespace Modules.Games.Mafia.Common.GameRoles;

public interface IActiveRole
{
    int MovesCount { get; set; }

    bool IsSkip { get; }
}
