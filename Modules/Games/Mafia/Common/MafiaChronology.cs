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
    private int _currentDay;


    public MafiaChronology()
    {
        AddAction(new StringChronology());

        _currentDay = 0;
    }


    public string AddAction(string action, GameRole role)
    {
        var str = role is not GroupRole
            ? $"[{role.Name}] {role.Player.GetFullMention()}: **{action}**"
            : $"{role.Name}: **{action}**";

        Actions[_currentDay].AddAction(str);

        return str;
    }

    public void AddAction(string action)
    {
        Actions[_currentDay].AddAction(action);
    }

    public void NextDay()
    {
        AddAction(new StringChronology());

        _currentDay++;
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
