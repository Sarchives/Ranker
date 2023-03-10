using DSharpPlus.Entities;

namespace Ranker
{
    public interface IRanksRepository
    {
        /// <summary>
        /// Gets ranks for a specific guild.
        /// </summary>
        Task<List<Rank>> GetAsync(ulong guildId);

        /// <summary>
        /// Gets rank for a particular member.
        /// </summary>
        /// <param name="userId">The member ID.</param>
        /// <param name="guildId">The guild (server) ID.</param>
        Task<Rank> GetAsync(ulong userId, ulong guildId);

        /// <summary>
        /// Modify an existing rank or add a new rank.
        /// </summary>
        /// <param name="userId">The member ID.</param>
        /// <param name="guildId">The guild ID.</param>
        /// <param name="newRank">The new rank.</param>
        /// <param name="newRank">The guild (optional but recommended).</param>
        #nullable enable
        Task UpsertAsync(ulong userId, ulong guildId, Rank newRank, DiscordGuild? guild);
        #nullable restore

        /// <summary>
        /// Empties ranks.
        /// </summary>
        /// <param name="guildId">The guild id.</param>
        Task Empty(ulong guildId);
    }
}
