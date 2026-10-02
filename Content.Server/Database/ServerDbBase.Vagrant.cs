using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    /// <summary>
    /// Gets the time when a player's prison sentence expires.
    /// </summary>
    public async Task<DateTime?> GetPrisonSentence(Guid id)
    {
        await using var db = await GetDb();
        return await db.DbContext.Player
            .Where(player => player.UserId == id)
            .Select(player => player.PrisonSentence)
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// Sets when a player's prison sentence expires.
    /// Null or a time in the past will free them.
    /// </summary>
    public async Task SetPrisonSentence(Guid id, DateTime? sentence)
    {
        await using var db = await GetDb();
        var player = await db.DbContext.Player.Where(player => player.UserId == id).SingleOrDefaultAsync();
        if (player == null)
            return;

        player.PrisonSentence = sentence;
        await db.DbContext.SaveChangesAsync();
    }
}
