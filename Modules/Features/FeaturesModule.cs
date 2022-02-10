using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Modules.Common.MultiSelect;

namespace Modules.Features;
#nullable disable
[Group("Фичи")]
public class FeaturesModule : GuildModuleBase
{
    public FeaturesModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }

    public CommandService CommandService { get; set; }

    // Sends a multi selection (a message with multiple select menus with options)
    [Command("select", RunMode = RunMode.Async)]
    public async Task MultiSelectionAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        var modules = CommandService.Modules.ToArray();
        var color = Color.Gold;

        string selectedModule = null;

        IUserMessage message = null;
        InteractiveMessageResult<MultiSelectionOption<string>> result = null;

        var timeoutPage = new PageBuilder()
            .WithDescription("Timeout!")
            .WithColor(color);

        while (result is null || result.IsSuccess && result.Value?.Row != 1)
        {
            var options = modules
                .Select(x => new MultiSelectionOption<string>(option: x.Name, row: 0, isDefault: x.Name == selectedModule))
                .DistinctBy(o => o.Option);

            string description = "Select a module";


            if (result != null)
            {
                description = "Select a command\nNote: You can also update your selected module.";
                var commands = modules
                    .First(x => x.Name == result.Value!.Option)
                    .Commands
                    .Select(x => new MultiSelectionOption<string>(x.Name, 1))
                    .DistinctBy(o => o.Option);

                options = options.Concat(commands);
            }

            var pageBuilder = new PageBuilder()
                .WithDescription(description)
                .WithColor(color);

            var multiSelection = new MultiSelectionBuilder<string>()
                .WithSelectionPage(pageBuilder)
                .WithTimeoutPage(timeoutPage)
                .WithCanceledPage(timeoutPage)
                .WithActionOnTimeout(ActionOnStop.ModifyMessage | ActionOnStop.DeleteInput)
                .WithActionOnCancellation(ActionOnStop.ModifyMessage | ActionOnStop.DeleteInput)
                .WithOptions(options.ToArray())
                .WithStringConverter(x => x.Option)
                .AddUser(Context.User)
                .Build();

            result = message is null
            ? await Interactive.SendSelectionAsync(multiSelection, Context.Channel, TimeSpan.FromMinutes(2), null, cts.Token)
            : await Interactive.SendSelectionAsync(multiSelection, message, TimeSpan.FromMinutes(2), null, cts.Token);

            message = result.Message;

            if (result.IsSuccess && result.Value!.Row == 0)
            {
                // We need to track the selected module so we can set it as the default option.
                selectedModule = result.Value!.Option;
            }
        }

        if (!result.IsSuccess)
            return;

        var embed = new EmbedBuilder()
            .WithDescription($"You selected:\n**Module**: {selectedModule}\n**Command**: {result.Value!.Option}")
            .WithColor(color)
            .Build();

        await message.ModifyAsync(x =>
        {
            x.Embed = embed;
            x.Components = new ComponentBuilder().Build(); // Remove components
        });
    }
}
