using System.Diagnostics.CodeAnalysis;
using Discord.Commands;

namespace Core.Comparers;

public class CommandInfoComparer : IEqualityComparer<CommandInfo>
{
    private readonly bool _compareModuleNames;

    public CommandInfoComparer(bool compareModuleNames = true)
    {
        _compareModuleNames = compareModuleNames;
    }

    public bool Equals(CommandInfo? cmd1, CommandInfo? cmd2)
    {
        if (cmd1 is null || cmd2 is null)
            return false;

        return cmd1.Name.Equals(cmd2.Name) && (!_compareModuleNames || cmd1.Module.Name.Equals(cmd2.Module.Name));
    }

    public int GetHashCode([DisallowNull] CommandInfo cmd)
        => _compareModuleNames
        ? HashCode.Combine(cmd.Name, cmd.Module.Name)
        : HashCode.Combine(cmd.Name);
}