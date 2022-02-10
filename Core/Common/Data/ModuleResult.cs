using Discord.Commands;

namespace Core.Common.Data;

public class ModuleResult : RuntimeResult
{
    public ModuleResult(CommandError? error, string reason) : base(error, reason)
    {

    }


    public static ModuleResult Ok(string reason = "Ok")
        => new(null, reason);
}
