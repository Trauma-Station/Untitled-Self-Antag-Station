// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Systems;
using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Antag;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Content.Shared.Station.Systems;
using Content.Trauma.Shared.Areas;
using Content.Vagrant.Shared.Administration;
using Robust.Shared.Collections;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Linq;
using System.Threading.Tasks;

namespace Content.Vagrant.Server.Administration;

/// <summary>
/// Spawns people who have a meta prison sentence in prison.
/// </summary>
public sealed partial class PrisonSentenceSystem : EntitySystem
{
    [Dependency] private AreaSystem _area = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private AdminSystem _admin = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private ISharedChatManager _chat = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private StationSpawningSystem _spawning = default!;
    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;

    private static readonly ProtoId<JobPrototype> Prisoner = "Prisoner";

    private List<Entity<TransformComponent>> _areas = new();

    /// <summary>
    /// Cache of prison sentences for each player session's id.
    /// </summary>
    [ViewVariables]
    private Dictionary<NetUserId, DateTime?> _sentenceCache = new();

    public override void Initialize()
    {
        base.Initialize();

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            return;

        // immediately this will update the cache so when players join it will be correct for when players spawn
        // let clients know if theyre sentenced so they can see it in the lobby
        // it defaults to null so don't send anything for the good players
        if (await GetSentenceExpiry(args.Session.UserId) is { } expiry)
            RaiseNetworkEvent(new UpdatePrisonSentenceEvent(expiry), args.Session);
    }

    [SubscribeLocalEvent(before: [typeof(AntagSelectionSystem)])] // no roundstart antag chud
    private void OnRulePlayerSpawning(RulePlayerSpawningEvent args)
    {
        var banned = new ValueList<ICommonSession>();
        var now = DateTime.UtcNow;
        args.PlayerPool.RemoveAll(session =>
        {
            // this only checks the cache so it can never wait for DB
            if (!_sentenceCache.TryGetValue(session.UserId, out var expiry) || !ShouldImprison(expiry))
                return false;

            banned.Add(session);
            return true;
        });

        if (banned.Count == 0)
            return;

        if (!FindStation(out var station, out var map))
        {
            Log.Error("Couldn't find a map for imprisoned players to be spawned at!");
            return;
        }

        _areas.Clear();
        _area.AddOpenAreas<PrisonAreaComponent>(map, _areas, _ => true);
        if (_areas.Count == 0)
        {
            Log.Error($"Map {map} {ToPrettyString(_map.GetMap(map))} had no prison areas mapped!");
            return;
        }

        // pad it if the map is extremely small to prevent pickandtake throwing
        while (_areas.Count < banned.Count)
        {
            _areas.Add(_random.Pick(_areas));
        }

        foreach (var session in banned)
        {
            Log.Info($"Spawning {session.Name} in prison");
            Imprison(session, station, args.Profiles.GetValueOrDefault(session.UserId));
            _ticker.PlayerJoinGame(session);
        }
    }

    [SubscribeLocalEvent]
    private void OnGhostPlayerAttached(Entity<PrisonGhostComponent> ent, ref PlayerAttachedEvent args)
    {
        SetGhost(ent, args.Player);
    }

    private async void SetGhost(EntityUid uid, ICommonSession player)
    {
        var disabled = await ShouldImprison(player.UserId);
        SetGhostRolesEnabled(uid, !disabled);
        if (disabled)
            Notify(player);
    }

    private void SetGhostRolesEnabled(EntityUid mob, bool enabled)
    {
        if (!_ghostQuery.TryComp(mob, out var ghost) || ghost.CanTakeGhostRoles == enabled)
            return;

        ghost.CanTakeGhostRoles = enabled;
        Dirty(mob, ghost);
    }

    public async Task<bool> ShouldImprison(NetUserId id)
        => ShouldImprison(await GetSentenceExpiry(id));

    public bool ShouldImprison(DateTime? sentence)
        => sentence is { } expiry && expiry > DateTime.UtcNow;

    public async Task<DateTime?> GetSentenceExpiry(NetUserId id)
    {
        if (_sentenceCache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var expiry = await _db.GetPrisonSentence(id);
        lock (_sentenceCache) // just incase...
        {
            _sentenceCache[id] = expiry;
        }
        return expiry;
    }

    public async Task SetSentence(NetUserId id, DateTime? sentence)
    {
        // odds of 2 admins on different servers setting the same guys sentence is very slim so synchronization doesnt matter
        _sentenceCache[id] = sentence;
        await _db.SetPrisonSentence(id, sentence);
        if (!_player.TryGetSessionById(id, out var session))
            return;

        RaiseNetworkEvent(new UpdatePrisonSentenceEvent(sentence), session);

        // no trolling if they're currently a ghost
        if (session.AttachedEntity is { } mob)
            SetGhostRolesEnabled(mob, ShouldImprison(sentence));
    }

    public async Task Pardon(NetUserId id)
    {
        SetSentence(id, null);
    }

    public async Task IncreaseSentence(NetUserId id, TimeSpan add)
    {
        var now = DateTime.UtcNow;
        var expiry = await GetSentenceExpiry(id) ?? now;
        if (expiry < now)
            expiry = now;

        SetSentence(id, expiry + add);
    }

    private bool FindStation(out EntityUid station, out MapId map)
    {
        var query = EntityQueryEnumerator<StationDataComponent>();
        foreach (var ent in query)
        {
            if (ent.Comp.Grids.Count == 0)
                continue;

            station = ent;
            map = Transform(ent.Comp.Grids.First()).MapID;
            return true;
        }

        station = EntityUid.Invalid;
        map = MapId.Nullspace;
        return false;
    }

    private void Imprison(ICommonSession session, EntityUid station, HumanoidCharacterProfile? profile)
    {
        var coords = _random.PickAndTake(_areas).Comp.Coordinates;
        var mob = _spawning.SpawnPlayerMob(coords, Prisoner, profile, null);

        var mind = _mind.CreateMind(session.UserId, Name(mob));
        _playTime.PlayerRolesChanged(session); // for shits n giggles
        _mind.TransferTo(mind, mob);
        _role.MindAddJobRole(mind, jobPrototype: Prisoner);
        _admin.UpdatePlayerList(session);

        Notify(session);
    }

    private void Notify(ICommonSession session)
    {
        var client = session.Channel;
        var message = "You are serving your prison sentence. After it's up you will be able to play normally.";
        _chat.ChatMessageToOne(ChatChannel.Server, message, message, EntityUid.Invalid, false, client, Color.Red);
    }
}
