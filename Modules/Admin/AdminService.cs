using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Serilog;

namespace Modules.Admin;

public class AdminService
{
    private readonly ILogger _logger;


    public AdminService()
    {
        _logger = Log.ForContext<AdminService>();
    }


    public async Task SetSlowModeAsync(ITextChannel textChannel, int secs)
    {
        _logger.Verbose("Execute {0}. Args: {1}; {2}", nameof(SetSlowModeAsync), textChannel, secs);

        await textChannel.ModifyAsync(props => props.SlowModeInterval = secs);
    }


    public async Task ClearAsync(ITextChannel textChannel, int count)
    {
        _logger.Verbose("Execute {0}. Args: {1}; {2}", nameof(ClearAsync), textChannel, count);

        var messagesToDelete = (await textChannel
           .GetMessagesAsync(count)
           .FlattenAsync())
           .Where(msg => (DateTime.UtcNow - msg.Timestamp).TotalDays <= 14);

        await textChannel.DeleteMessagesAsync(messagesToDelete);
    }

    public async Task<int> ClearAsync(ITextChannel textChannel, IMessage message)
    {
        _logger.Verbose("Execute {0}. Args: {1}; {2}", nameof(ClearAsync), textChannel, message);

        var messages = (await textChannel.GetMessagesAsync(message.Id, Direction.After, 100).FlattenAsync())
            .Where(msg => (DateTime.UtcNow - msg.Timestamp).TotalDays <= 14)
            .ToList();

        if ((DateTime.UtcNow - message.Timestamp).TotalDays <= 14)
            messages.Add(message);

        await textChannel.DeleteMessagesAsync(messages);

        var count = messages.Count;

        return count;
    }

    public async Task<int> ClearAsync(ITextChannel textChannel, IMessage from, IMessage to)
    {
        _logger.Verbose("Execute {0}. Args: {1}; {2}; {3}", nameof(ClearAsync), textChannel, from, to);

        if (from.Id < to.Id)
            (from, to) = (to, from);

        var toCount = (await textChannel.GetMessagesAsync(to.Id, Direction.After, 100).FlattenAsync())
            .Where(msg => (DateTime.UtcNow - msg.Timestamp).TotalDays <= 14)
            .TakeWhile(msg => msg.Id != from.Id)
            .Count();

        var messages = new List<IMessage>();

        if ((DateTime.UtcNow - from.Timestamp).TotalDays <= 14)
            messages.Add(from);

        var messagesBefore = (await textChannel.GetMessagesAsync(from.Id, Direction.Before, toCount).FlattenAsync())
            .Where(msg => (DateTime.UtcNow - msg.Timestamp).TotalDays <= 14)
            .TakeWhile(msg => msg.Id != to.Id)
            .ToList();

        messages.AddRange(messagesBefore);

        if ((DateTime.UtcNow - to.Timestamp).TotalDays <= 14)
            messages.Add(to);

        await textChannel.DeleteMessagesAsync(messages);

        var count = messages.Count;

        return count;
    }


    public async Task UpdateRoleColorAsync(IRole role, Color color)
    {
        _logger.Verbose("Execute {0}. Args: {1}; {2};", nameof(UpdateRoleColorAsync), role, color);

        await role.ModifyAsync(r => r.Color = color);
    }
}
