using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Discord.Commands;

namespace Modules.Comparers;

public class CommandInfoComparer : IEqualityComparer<CommandInfo>
{
    public bool Equals(CommandInfo? cmd1, CommandInfo? cmd2)
    {
        if (cmd1 is null || cmd2 is null)
            return false;

        return cmd1.Name.Equals(cmd2.Name) && cmd1.Module.Name.Equals(cmd2.Module.Name);
    }

    public int GetHashCode([DisallowNull] CommandInfo cmd)
        => HashCode.Combine(cmd.Name, cmd.Module.Name);
}
