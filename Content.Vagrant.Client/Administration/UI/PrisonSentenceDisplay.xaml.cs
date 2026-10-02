// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;

namespace Content.Vagrant.Client.Administration.UI;

[GenerateTypedNameReferences]
public sealed partial class PrisonSentenceDisplay : BoxContainer
{
    private PrisonSentenceSystem _sys;

    private int _seconds;

    public PrisonSentenceDisplay(PrisonSentenceSystem sys)
    {
        RobustXamlLoader.Load(this);

        _sys = sys;

        UpdateTime();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        UpdateTime();
    }

    private void UpdateTime()
    {
        Contents.Visible = _sys.IsActive;
        if (!Contents.Visible)
            return;

        var time = _sys.SentenceLength;
        var seconds = (int) time.TotalSeconds;
        if (_seconds == seconds)
            return;

        _seconds = seconds;
        TimeLabel.Text = time.TotalDays >= 1.0
            ? $"{time:ddd' days, 'hh' hours'}"
            : $"{time:hh\\:mm\\:ss}";
    }
}
