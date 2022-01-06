using System.Collections.Generic;
using Discord;

namespace Modules.Games.Mafia.Common;

public record VoteInfo<T>(IMessageChannel Channel, int VoteTime, IEnumerable<T> Options, IEnumerable<string>? DisplayOptions = null) where T : notnull;
