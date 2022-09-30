using Microsoft.AspNetCore.Mvc;

namespace Ranker
{
    [Route("[controller]")]
    public class LevelsController : Controller
    {
        private readonly IRankerRepository _database;

        public LevelsController(IRankerRepository database)
        {
            _database = database;
        }

        [Route("{id}")]
        [HttpGet]
        public async Task<IActionResult> Get(ulong id, [FromQuery] int page = 0)
        {
            try
            {
                using (HttpClient client = new())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(
                            "Bot",
                            Environment.GetEnvironmentVariable("RANKER_TOKEN"));
                    var response = await client.GetAsync($"https://discord.com/api/v9/guilds/{id}");
                    string responseJson = await response.Content.ReadAsStringAsync();
                    JToken jsonParsed = JToken.Parse(responseJson);

                    IEnumerable<Rank> ranks = (await _database.Ranks.GetAsync(id))
                        .OrderByDescending(f => f.TotalXp)
                        .Chunk(100)
                        .ElementAt(page);

                    Settings settings = await _database.Settings.GetAsync(id);

                    List<Role> roles = await _database.Roles.GetAsync(id);

                    using (HttpClient client2 = new())
                    {
                        bool managesGuild = false;

                        if (!String.IsNullOrEmpty(Request.Headers["Code"]))
                        {
                            client2.DefaultRequestHeaders.Authorization =
                                new AuthenticationHeaderValue(
                                    "Bearer",
                                    Request.Headers["Code"]);
                            var response2 = await client2.GetAsync("https://discord.com/api/v9/users/@me/guilds");
                            string responseJson2 = await response2.Content.ReadAsStringAsync();
                            JArray jsonParsed2 = JArray.Parse(responseJson2);

                            managesGuild = (ulong.Parse(jsonParsed2.ToList().Find(x => x["id"].Value<string>() == id.ToString())["permissions"].Value<string>()) & 0x0000000020) == 0x0000000020;
                        }
                        return Ok(
                            new Dictionary<string, object>()
                            {
                                { "managesGuild", managesGuild },
                                {
                                    "guild",
                                    new Dictionary<string, object>()
                                    {
                                        { "name", jsonParsed["name"].Value<string>() },
                                        { "description", jsonParsed["description"].Value<string>() },
                                        { "icon", jsonParsed["icon"].Value<string>() },
                                        { "is_joinable", jsonParsed["rules_channel_id"] != null }
                                    }
                                },
                                {
                                    "settings",
                                    new Dictionary<string, object>() {
                                        { "minRange", settings.MinRange },
                                        { "maxRange", settings.MaxRange }
                                    }
                                },
                                { "roles", roles.ConvertAll(new Converter<Role, RoleStringy>(StringifyRole)).OrderBy(x => x.Level) },
                                { "players", ranks.ToList().ConvertAll(new Converter<Rank, RankStringy>(StringifyRank)) }
                            }
                        );
                    }
                }
            }
            catch
            {
                return BadRequest();
            }
        }

        public static RoleStringy StringifyRole(Role role)
        {
            return new RoleStringy()
            {
                Guild = role.Guild.ToString(),
                Level = role.Level,
                RoleId = role.RoleId.ToString(),
                RoleName = role.RoleName
            };
        }

        public static RankStringy StringifyRank(Rank rank)
        {
            return new RankStringy()
            {
                LastCreditDate = rank.LastCreditDate,
                Messages = rank.Messages,
                NextXp = rank.NextXp,
                Level = rank.Level,
                TotalXp = rank.TotalXp,
                Xp = rank.Xp,
                Guild = rank.Guild.ToString(),
                User = rank.User.ToString(),
                Username = rank.Username,
                Discriminator = rank.Discriminator,
                Avatar = rank.Avatar,
                Style = rank.Style
            };
        }
    }
}
