using Discord;

namespace Modules.Games.Mafia.Common;

public record MoveInfo(IGuildUser? Player, bool IsSkip);
