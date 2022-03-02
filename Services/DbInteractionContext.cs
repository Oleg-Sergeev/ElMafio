using Discord;
using Discord.Interactions;
using Infrastructure.Data;

namespace Services;

public class DbInteractionContext : InteractionContext
{
    public BotContext Db { get; }

    public DbInteractionContext(IDiscordClient client, IDiscordInteraction interaction, BotContext db, IMessageChannel? channel = null) : base(client, interaction, channel)
    {
        Db = db;
    }
}