using System.Collections.Generic;
using Fergun.Interactive;
using Fergun.Interactive.Selection;

namespace Modules.Common.MultiSelect;


public class MultiSelectionBuilder<T> : BaseSelectionBuilder<MultiSelection<T>, MultiSelectionOption<T>, MultiSelectionBuilder<T>> where T : notnull
{
    public override InputType InputType => InputType.SelectMenus | InputType.Buttons;

    public override MultiSelection<T> Build() => new(this);


    public MultiSelectionBuilder<T> WithCancelButton(T option)
    {
        Options = new List<MultiSelectionOption<T>>(Options)
        {
            new MultiSelectionOption<T>(option, -1)
        };

        return this;
    }
}
