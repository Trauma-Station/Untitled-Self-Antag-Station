// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Vagrant.Shared.Administration;

/// <summary>
/// Sent to players after they join with a sentence, or if an admin updates it.
/// </summary>
[Serializable, NetSerializable]
public sealed class UpdatePrisonSentenceEvent(DateTime? sentence) : EntityEventArgs
{
    public DateTime? Sentence = sentence;
}
