using System.Collections.Generic;
using Core.Common.Data;
using Core.Exceptions;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings;
using Microsoft.Extensions.Configuration;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.Services;
using Moq;
using Xunit;

namespace UnitTests.Modules.Mafia;

public class MafiaSetupServiceTests
{
    [Fact(DisplayName = "Should throw WrongPlayersCountException")]
    public void SetupRoles_ThrowWrongPlayersCountException()
    {
        var roleData = TestsHelper.GetMockedGameRoleData();
        var mockConfig = new Mock<IConfiguration>();

        var service = new MafiaSetupService(mockConfig.Object, roleData);

        var mafiaSettings = new MafiaSettings()
        {
            CurrentTemplate = new()
            {
                GameSubSettings = new()
                {
                    IsCustomGame = true,
                    IsFillWithMurders = true
                },
                RoleAmountSubSettings = new()
                {
                    InnocentsCount = 4
                }
            }
        };

        var mafiaData = new MafiaData("Мафия", 3, null!);
        mafiaData.Players.AddRange(TestsHelper.GetPlayers(3));


        var context = new MafiaContext(null!, mafiaData, mafiaSettings, null!, null!);

        Assert.Throws<WrongPlayersCountException>(() => service.SetupRoles(context));
    }


    [Theory(DisplayName = "Should setup roles correctly")]
    [MemberData(nameof(GetData_RoleAmountSettings))]
    public void SetupRoles_ValidRoleSetuping(int players, GameSubSettings gameSettings, RoleAmountSubSettings roleAmount, RoleAmountSubSettings expectedRoleAmount)
    {
        var roleGameData = TestsHelper.GetMockedGameRoleData();
        var mockConfig = new Mock<IConfiguration>();

        var service = new MafiaSetupService(mockConfig.Object, roleGameData);

        var mafiaSettings = new MafiaSettings()
        {
            CurrentTemplate = new()
            {
                GameSubSettings = gameSettings,
                RoleAmountSubSettings = roleAmount
            }
        };

        var mafiaData = new MafiaData("Мафия", 3, null!);
        mafiaData.Players.AddRange(TestsHelper.GetPlayers(players));


        var context = new MafiaContext(null!, mafiaData, mafiaSettings, null!, null!);

        service.SetupRoles(context);

        var rolesData = context.RolesData;

        Assert.Equal(rolesData.Doctors.Count, expectedRoleAmount.DoctorsCount);
        Assert.Equal(rolesData.Sheriffs.Count, expectedRoleAmount.SheriffsCount);
        Assert.Equal(rolesData.Dons.Count, expectedRoleAmount.DonsCount);
        Assert.Equal(rolesData.Maniacs.Count, expectedRoleAmount.ManiacsCount);
        Assert.Equal(rolesData.Hookers.Count, expectedRoleAmount.HookersCount);

        Assert.Equal(rolesData.Innocents.Count - rolesData.Doctors.Count - rolesData.Sheriffs.Count, expectedRoleAmount.InnocentsCount);
        Assert.Equal(rolesData.Murders.Count - rolesData.Dons.Count, expectedRoleAmount.MurdersCount);
    }



    private static IEnumerable<object[]> GetData_RoleAmountSettings()
    {
        return new List<object[]>
        {
            new object[]
            {
                3,
                new GameSubSettings()
                {
                    IsCustomGame = false
                },
                new RoleAmountSubSettings(),
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 1,
                    InnocentsCount = 0
                }
            },
            new object[]
            {
                4,
                new GameSubSettings()
                {
                    IsCustomGame = false
                },
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 12,
                    SheriffsCount = -11111,
                    MurdersCount = 99,
                    InnocentsCount = 0
                },
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 1,
                    InnocentsCount = 1
                }
            },
            new object[]
            {
                6,
                new GameSubSettings()
                {
                    IsCustomGame = false
                },
                new RoleAmountSubSettings(),
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 2,
                    InnocentsCount = 2
                }
            },
            new object[]
            {
                9,
                new GameSubSettings()
                {
                    IsCustomGame = false
                },
                new RoleAmountSubSettings(),
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 2,
                    DonsCount = 1,
                    InnocentsCount = 4
                }
            },
            new object[]
            {
                9,
                new GameSubSettings()
                {
                    IsCustomGame = false,
                    MafiaCoefficient = 4
                },
                new RoleAmountSubSettings(),
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 2,
                    InnocentsCount = 5
                }
            },
            new object[]
            {
                15,
                new GameSubSettings()
                {
                    IsCustomGame = false
                },
                new RoleAmountSubSettings(),
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 2,
                    SheriffsCount = 2,
                    MurdersCount = 4,
                    DonsCount = 1,
                    InnocentsCount = 6
                }
            },
            new object[]
            {
                15,
                new GameSubSettings()
                {
                    IsCustomGame = false,
                    MafiaCoefficient = 5
                },
                new RoleAmountSubSettings(),
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 2,
                    DonsCount = 1,
                    InnocentsCount = 10
                }
            },
            new object[]
            {
                6,
                new GameSubSettings()
                {
                    IsCustomGame = true,
                    IsFillWithMurders = false
                },
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 2,
                    InnocentsCount = 2
                },
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 2,
                    InnocentsCount = 2
                }
            },
            new object[]
            {
                6,
                new GameSubSettings()
                {
                    IsCustomGame = true,
                    IsFillWithMurders = true
                },
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 2,
                    InnocentsCount = 2
                },
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 2,
                    InnocentsCount = 2
                }
            },
            new object[]
            {
                8,
                new GameSubSettings()
                {
                    IsCustomGame = true,
                    IsFillWithMurders = false
                },
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 1,
                    ManiacsCount = 1
                },
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    SheriffsCount = 1,
                    MurdersCount = 1,
                    ManiacsCount = 1,
                    InnocentsCount = 4
                }
            },
            new object[]
            {
                8,
                new GameSubSettings()
                {
                    IsCustomGame = true,
                    IsFillWithMurders = true
                },
                new RoleAmountSubSettings()
                {
                    InnocentsCount = 1
                },
                new RoleAmountSubSettings()
                {
                    InnocentsCount = 1,
                    MurdersCount = 7
                }
            },
            new object[]
            {
                8,
                new GameSubSettings()
                {
                    IsCustomGame = true,
                    IsFillWithMurders = true
                },
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    HookersCount = 2,
                    DonsCount = 2,
                },
                new RoleAmountSubSettings()
                {
                    DoctorsCount = 1,
                    HookersCount = 2,
                    DonsCount = 2,
                    MurdersCount = 3
                }
            },
            new object[]
            {
                12,
                new GameSubSettings()
                {
                    IsCustomGame = true,
                    IsFillWithMurders = false
                },
                new RoleAmountSubSettings()
                {
                    MurdersCount = 1
                },
                new RoleAmountSubSettings()
                {
                    MurdersCount = 1,
                    InnocentsCount = 11
                }
            },
            new object[]
            {
                12,
                new GameSubSettings()
                {
                    IsCustomGame = true,
                    MafiaCoefficient = 111,
                    IsFillWithMurders = false
                },
                new RoleAmountSubSettings()
                {
                    DonsCount = 11
                },
                new RoleAmountSubSettings()
                {
                    DonsCount = 11,
                    InnocentsCount = 1
                }
            },
        };
    }
}
