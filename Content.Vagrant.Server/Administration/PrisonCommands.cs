// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Vagrant.Server.Administration;

[AdminCommand(AdminFlags.Ban)]
public sealed partial class PrisonAddCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    private PrisonSentenceSystem? _prison;

    public override string Command => "prisonadd";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        if (await _locator.LookupIdByNameOrIdAsync(args[0]) is not { } player)
        {
            shell.WriteError(Loc.GetString("cmd-ban-player"));
            return;
        }

        if (!TimeSpanExt.TryTimeSpan(args[1], out var time))
        {
            shell.WriteError("Invalid time string. Use a number followed by one of s, m, h, d, w. Use decimals for fractions e.g. 1.5m instead of 1m30s");
            return;
        }

        _prison ??= _ent.System<PrisonSentenceSystem>();

        var id = player.UserId;
        await _prison.IncreaseSentence(id, time);

        var expiry = (await _prison.GetSentenceExpiry(id))!.Value - DateTime.Now;
        shell.WriteLine($"Sentence set to expire after {expiry}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _player.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(options, LocalizationManager.GetString("cmd-ban-hint"));
        }

        if (args.Length == 2)
            return CompletionResult.FromHint(LocalizationManager.GetString("cmd-ban-hint-duration"));

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed partial class PrisonTimeCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    private PrisonSentenceSystem? _prison;

    public override string Command => "prisontime";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        if (await _locator.LookupIdByNameOrIdAsync(args[0]) is not { } player)
        {
            shell.WriteError(Loc.GetString("cmd-ban-player"));
            return;
        }

        _prison ??= _ent.System<PrisonSentenceSystem>();

        var id = player.UserId;
        var sentence = await _prison.GetSentenceExpiry(id);
        var now = DateTime.Now;
        shell.WriteLine(sentence is { } expiry && expiry > now
            ? $"Player sentenced to prison for {sentence - now}"
            : "Player is not sentenced to prison.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _player.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(options, LocalizationManager.GetString("cmd-ban-hint"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed partial class PrisonPardonCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    private PrisonSentenceSystem? _prison;

    public override string Command => "prisonpardon";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        if (await _locator.LookupIdByNameOrIdAsync(args[0]) is not { } player)
        {
            shell.WriteError(Loc.GetString("cmd-ban-player"));
            return;
        }

        _prison ??= _ent.System<PrisonSentenceSystem>();

        var id = player.UserId;
        shell.WriteLine(await _prison.ShouldImprison(id)
            ? "Prison sentence pardoned"
            : "There was probably no prison sentence to begin with?");

        await _prison.Pardon(id);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _player.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(options, LocalizationManager.GetString("cmd-ban-hint"));
        }

        return CompletionResult.Empty;
    }
}
