using System.Threading.Tasks;
using Discord;

namespace Modules.Games.Mafia.Common.Interfaces;

public interface IHealer
{
    IGuildUser? HealedPlayer { get; }
}
