using System.Collections.Generic;
using System.Linq;
using Discord;
using Modules.Games.Mafia.Common.GameRoles;

namespace Modules.Games.Mafia.Common.Data;

public class MafiaRolesData
{
    public Dictionary<IGuildUser, GameRole> AllRoles { get; }

    public Dictionary<IGuildUser, GameRole> AliveRoles { get; }


    public IReadOnlyDictionary<IGuildUser, Innocent> Innocents => _innocents;
    private Dictionary<IGuildUser, Innocent> _innocents;

    public IReadOnlyDictionary<IGuildUser, Doctor> Doctors => _doctors;
    private Dictionary<IGuildUser, Doctor> _doctors;

    public IReadOnlyDictionary<IGuildUser, Sheriff> Sheriffs => _sheriffs;
    private Dictionary<IGuildUser, Sheriff> _sheriffs;

    public IReadOnlyDictionary<IGuildUser, Murder> Murders => _murders;
    private Dictionary<IGuildUser, Murder> _murders;

    public IReadOnlyDictionary<IGuildUser, Don> Dons => _dons;
    private Dictionary<IGuildUser, Don> _dons;

    public IReadOnlyDictionary<IGuildUser, Neutral> Neutrals => _neutrals;
    private Dictionary<IGuildUser, Neutral> _neutrals;

    public IReadOnlyDictionary<IGuildUser, Maniac> Maniacs => _maniacs;
    private Dictionary<IGuildUser, Maniac> _maniacs;

    public IReadOnlyDictionary<IGuildUser, Hooker> Hookers => _hookers;
    private Dictionary<IGuildUser, Hooker> _hookers;


    public MafiaRolesData()
    {
        AllRoles = new();

        AliveRoles = new();

        _innocents = new();
        _doctors = new();
        _sheriffs = new();
        _murders = new();
        _dons = new();
        _neutrals = new();
        _maniacs = new();
        _hookers = new();
    }


    public void AssignRoles()
    {
        foreach (var role in AllRoles)
            AliveRoles.Add(role.Key, role.Value);


        _innocents = AllRoles.Values.Where(r => r is Innocent).ToDictionary(r => r.Player, r => (Innocent)r);
        _doctors = Innocents.Values.Where(i => i is Doctor).ToDictionary(i => i.Player, i => (Doctor)i);
        _sheriffs = Innocents.Values.Where(i => i is Sheriff).ToDictionary(i => i.Player, i => (Sheriff)i);

        _murders = AllRoles.Values.Where(r => r is Murder).ToDictionary(r => r.Player, r => (Murder)r);
        _dons = Murders.Values.Where(m => m is Don).ToDictionary(m => m.Player, m => (Don)m);

        _neutrals = AllRoles.Values.Where(r => r is Neutral).ToDictionary(r => r.Player, r => (Neutral)r);
        _maniacs = Neutrals.Values.Where(n => n is Maniac).ToDictionary(n => n.Player, n => (Maniac)n);
        _hookers = Neutrals.Values.Where(n => n is Hooker).ToDictionary(n => n.Player, n => (Hooker)n);
    }
}
