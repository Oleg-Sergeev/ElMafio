using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Common.Chronology;
using Core.Extensions;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Modules.Games.Mafia.Common.GameRoles;

namespace Modules.Games.Mafia.Common;

public class MafiaChronology : Chronology<StringChronology>
{
    public int CurrentDay { get; private set; }


    public MafiaChronology()
    {
        AddAction(new StringChronology());

        CurrentDay = 0;
    }


    public string AddAction(string action, GameRole role)
    {
        var str = role is not GroupRole
            ? $"[{role.Name}] {role.Player.GetFullMention()}: **{action}**"
            : $"{role.Name}: **{action}**";

        Actions[CurrentDay].AddAction(str);

        return str;
    }

    public void AddAction(string action)
    {
        Actions[CurrentDay].AddAction(action);
    }

    public void NextDay()
    {
        AddAction(new StringChronology());

        CurrentDay++;
    }


    public LazyPaginator BuildActionsHistoryPaginator(IEnumerable<IUser> withUsers)
    {
        var paginator = new LazyPaginatorBuilder()
               .WithPageFactory(GeneratePageBuilderAsync)
               .WithMaxPageIndex(Actions.Count - 1)
               .WithCacheLoadedPages(true)
               .WithUsers(withUsers)
               .WithActionOnCancellation(ActionOnStop.None)
               .WithActionOnTimeout(ActionOnStop.DeleteMessage)
               .Build();

        return paginator;


        Task<PageBuilder> GeneratePageBuilderAsync(int index)
        {
            var pageBuilder = new PageBuilder()
                .WithDescription(Actions[index].FlattenActionsHistory())
                .WithTitle($"День {index}");

            return Task.FromResult(pageBuilder);
        }
    }
}
