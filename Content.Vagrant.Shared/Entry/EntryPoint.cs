// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.ContentPack;

namespace Content.Vagrant.Shared.Entry;

public sealed partial class EntryPoint : GameShared
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void PreInit()
    {
        Dependencies.InjectDependencies(this);
    }

    public override void Init()
    {
        _proto.PartialDirectory(new("/Prototypes/_Vagrant/Partials"), 2); // Trauma + 1
    }
}
