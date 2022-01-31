using System.Threading.Tasks;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using GroupAttribute = Discord.Interactions.GroupAttribute;

namespace Modules;

[Group("фичи", "Современные фичи 2023ого года")]
public class FeaturesModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ало", "позвонить боту")]
    public async Task GreetUserAsync([Remainder] string? text = null)
           => await RespondAsync($"Ало, {text}");


    [UserCommand("ало")]
    public async Task GreetUserAsync(IUser user)
           => await RespondAsync($"Ало, {user.GetFullMention()}");


    [MessageCommand("ало")]
    public async Task GreetUserAsync(IMessage message)
           => await RespondAsync($"Ало, {message.Content} [{message.Author.GetFullMention()}]");
}
