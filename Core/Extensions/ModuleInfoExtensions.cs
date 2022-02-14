using Discord.Commands;

namespace Core.Extensions;

public static class ModuleInfoExtensions
{
    public static string? GetModulePath(this ModuleInfo module)
    {
        if (!module.IsSubmodule)
            return !string.IsNullOrEmpty(module.Group) ? module.Group : module.Name ?? null;

        return $"{module.Parent.GetModulePath()}.{module.Group}".TrimEnd('.');
    }

    public static ModuleInfo GetRootModule(this ModuleInfo module)
        => module.IsSubmodule
        ? module.Parent.GetRootModule()
        : module;
}
