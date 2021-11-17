using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace Core.EnsureCriterias;

public class EnsureReactionFromMessageCriterion : ICriterion<SocketReaction>
{
    private readonly IMessage _message;

    public EnsureReactionFromMessageCriterion(IMessage message)
    {
        _message = message;
    }

    public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketReaction parameter)
    {
        bool ok = parameter.MessageId == _message.Id;
        return Task.FromResult(ok);
    }
}
