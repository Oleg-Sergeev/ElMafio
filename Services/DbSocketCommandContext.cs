using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Infrastructure.Data;
using Infrastructure.Data.Models.Games.Settings;
using Infrastructure.Data.Models.ServerInfo;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class DbSocketCommandContext : SocketCommandContext
{
    public BotContext Db { get; }


    public DbSocketCommandContext(DiscordSocketClient client, SocketUserMessage msg, BotContext db) : base(client, msg)
    {
        Db = db;
    }



    public async Task<Server> GetServerAsync()
    {
        var server = await Db.Servers.FindAsync(Guild.Id);

        if (server is null)
            throw new InvalidOperationException($"Guild with id {Guild.Id} was not found in database");

        return server;
    }
}
