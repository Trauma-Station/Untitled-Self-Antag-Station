// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Vagrant.Shared.Administration;
using Content.Vagrant.Client.Administration.UI;

namespace Content.Vagrant.Client.Administration;

/// <summary>
/// Tracks prison sentence data sent from the server.
/// </summary>
public sealed partial class PrisonSentenceSystem : EntitySystem
{
    private DateTime? _sentence;

    [ViewVariables]
    public bool IsActive => _sentence is { } expiry && expiry > DateTime.UtcNow;

    [ViewVariables]
    public TimeSpan SentenceLength => _sentence is { } expiry ? expiry - DateTime.UtcNow : TimeSpan.Zero;

    private PrisonSentenceDisplay? _display;

    public override void Initialize()
    {
        base.Initialize();

        LobbyState.OnCreated += OnLobbyCreated;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        LobbyState.OnCreated -= OnLobbyCreated;
    }

    private void OnLobbyCreated(LobbyState state)
    {
        if (_display != null || state.Lobby is not { } lobby)
            return;

        _display = new(this);
        // put it below balance
        var parent = lobby.Balance.Parent;
        var index = lobby.Balance.GetPositionInParent() + 1;
        parent.AddChild(_display);
        _display.SetPositionInParent(index);
    }

    [SubscribeNetworkEvent]
    private void OnUpdatePrisonSentence(UpdatePrisonSentenceEvent args)
    {
        _sentence = args.Sentence;
    }
}
