﻿using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace Modules.EnsureCriterias;

public class EnsureReactionFromSourceMessageCriterion : ICriterion<SocketReaction>
{
    public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketReaction parameter)
    {
        bool ok = parameter.MessageId == sourceContext.Message.Id;
        return Task.FromResult(ok);
    }
}