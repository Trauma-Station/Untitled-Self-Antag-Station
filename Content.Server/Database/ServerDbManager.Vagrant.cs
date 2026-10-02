using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Network;

namespace Content.Server.Database;

public sealed partial class ServerDbManager
{
    public Task<DateTime?> GetPrisonSentence(Guid id)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPrisonSentence(id));
    }

    public Task SetPrisonSentence(Guid id, DateTime? sentence)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetPrisonSentence(id, sentence));
    }
}
