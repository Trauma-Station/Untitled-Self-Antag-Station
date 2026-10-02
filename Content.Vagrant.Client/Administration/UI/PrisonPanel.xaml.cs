// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using static Content.Client.Administration.UI.BanPanel.BanPanel;

namespace Content.Vagrant.Client.Administration.UI;

[GenerateTypedNameReferences]
public sealed partial class PrisonPanel : FancyWindow
{
    private string _username = string.Empty;
    private TimeSpan _timeEntered;

    public PrisonPanel()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        TimeLine.OnTextChanged += args => OnTimeChanged(args.Text);
    }

    private void OnTimeChanged(string text)
    {
        if (!TimeSpanExt.TryTimeSpan(text, out var result))
        {
            ExpiresLabel.Text = Loc.GetString("ban-panel-expiry-error");
            TimeLine.ModulateSelfOverride = Color.Red;
            return;
        }

        _timeEntered = result;

        TimeLine.ModulateSelfOverride = null;
    }
}
