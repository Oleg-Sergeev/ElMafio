using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Selection;

namespace Modules.Common.MultiSelect;

public class MultiSelection<T> : BaseSelection<MultiSelectionOption<T>> where T : notnull
{
    public IReadOnlyList<string?>? Placeholders { get;}

    public MultiSelection(MultiSelectionBuilder<T> builder)
        : base(builder)
    {
        Placeholders = builder.Placeholders;
    }

    public override ComponentBuilder GetOrAddComponents(bool disableAll, ComponentBuilder? builder = null)
    {
        builder ??= new ComponentBuilder();
        var selectMenus = new Dictionary<int, SelectMenuBuilder>();

        ButtonBuilder? cancelButton = null;

        foreach (var option in Options)
        {
            var emote = EmoteConverter?.Invoke(option);
            var label = StringConverter?.Invoke(option);

            if (option.Row == -1)
            {
                cancelButton = new(label, label, ButtonStyle.Danger, null, emote, isDisabled: disableAll);

                continue;
            }

            if (!selectMenus.ContainsKey(option.Row))
            {
                selectMenus[option.Row] = new SelectMenuBuilder()
                    .WithCustomId($"selectmenu_{option.Row}")
                    .WithDisabled(disableAll);

                if (Placeholders is not null && Placeholders.Count < option.Row && Placeholders[option.Row] is not null)
                    selectMenus[option.Row].WithPlaceholder(Placeholders[option.Row]);
            }

            if (emote is null && label is null)
                throw new InvalidOperationException($"Neither {nameof(EmoteConverter)} nor {nameof(StringConverter)} returned a valid emote or string.");

            string optionValue = emote?.ToString() ?? label!;

            var optionBuilder = new SelectMenuOptionBuilder()
                .WithLabel(label)
                .WithDescription(option.Description)
                .WithEmote(emote)
                .WithValue(optionValue)
                .WithDefault(option.IsDefault);

            selectMenus[option.Row].AddOption(optionBuilder);
        }

        foreach ((var row, var selectMenu) in selectMenus)
        {
            builder.WithSelectMenu(selectMenu, row);
        }

        if (cancelButton is not null)
            builder.WithButton(cancelButton, 4);

        return builder;
    }

    public override async Task<InteractiveInputResult<MultiSelectionOption<T>>> HandleInteractionAsync(SocketMessageComponent input, IUserMessage message)
    {
        var x = await base.HandleInteractionAsync(input, message);

        return x;
    }
}
