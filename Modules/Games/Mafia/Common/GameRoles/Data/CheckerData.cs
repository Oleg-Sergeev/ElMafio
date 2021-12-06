using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Games.Mafia.Common.GameRoles.Data;

public class CheckerData : GameRoleData
{
    public string[] SuccessfullCheckPhrases { get; set; } = Array.Empty<string>();

    public string[] FailedCheckPhrases { get; set; } = Array.Empty<string>();
}
