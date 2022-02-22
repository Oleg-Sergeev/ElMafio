using Discord.Commands;

namespace Core.Extensions;

public static class ModuleInfoExtensions
{
    public static string GetModulePath(this ModuleInfo module, bool includeNames = true)
    {
        if (!module.IsSubmodule)
            return !string.IsNullOrEmpty(module.Group) ? module.Group : includeNames ? module.Name : string.Empty;

        return $"{module.Parent.GetModulePath()}.{module.Group}".Trim('.');
    }

    public static ModuleInfo GetRootModule(this ModuleInfo module)
        => module.IsSubmodule
        ? module.Parent.GetRootModule()
        : module;
}
