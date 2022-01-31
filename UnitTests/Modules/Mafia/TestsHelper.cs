using System.Collections.Generic;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Moq;

namespace UnitTests.Modules.Mafia;

public static class TestsHelper
{
    public static IGuildUser GetPlayer()
        => new Mock<IGuildUser>().Object;

    public static List<IGuildUser> GetPlayers(int n)
    {
        var players = new List<IGuildUser>();

        for (int i = 0; i < n; i++)
            players.Add(GetPlayer());

        return players;
    }

    public static GameRole GetMockedGameRole()
    {
        var roleData = GetMockedGameRoleData();

        return new Mock<GameRole>(new Mock<IGuildUser>().Object, roleData).Object;
    }

    public static IOptionsSnapshot<GameRoleData> GetMockedGameRoleData()
    {
        var mock = new Mock<IOptionsSnapshot<GameRoleData>>();
        mock.Setup(g => g.Get(It.IsAny<string>())).Returns(new GameRoleData());

        return mock.Object;
    }
}
