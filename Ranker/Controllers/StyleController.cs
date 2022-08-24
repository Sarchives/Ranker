using System.IO;

namespace Ranker
{
    [Route("[controller]")]
    public class StyleController : Controller
    {
        private readonly IRankerRepository _database;

        public StyleController(IRankerRepository database)
        {
            _database = database;
        }

        [HttpGet("{guildId}")]
        public async Task<IActionResult> Get(ulong guildId)
        {
            try
            {
                using (HttpClient client = new())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            Request.Headers["Code"]);
                    var response = await client.GetAsync("https://discord.com/api/v9/users/@me");
                    string responseJson = await response.Content.ReadAsStringAsync();
                    JObject jsonParsed = JObject.Parse(responseJson);
                    ulong userId = ulong.Parse(jsonParsed["id"].Value<string>());
                    Rank rankCard = await _database.Ranks.GetAsync(userId, guildId);
                    return Ok(
                        new Dictionary<string, string>()
                        {
                            { "style", rankCard.Style }
                        }
                    );
                }
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpPost("{guildId}")]
        public async Task<IActionResult> Post(ulong guildId)
        {
            try
            {
                using (HttpClient client = new())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            Request.Headers["Code"]);
                    var response = await client.GetAsync("https://discord.com/api/v9/users/@me");
                    string responseJson = await response.Content.ReadAsStringAsync();
                    JObject jsonParsed = JObject.Parse(responseJson);
                    ulong userId = ulong.Parse(jsonParsed["id"].Value<string>());
                    Rank rankCard = await _database.Ranks.GetAsync(userId, guildId);
                    using (StreamReader stream = new StreamReader(Request.Body))
                    {
                        string style = JObject.Parse(await stream.ReadToEndAsync())["style"].Value<string>();
                        if (style == "zeealeid" || style == "fleuron" || style == "custom")
                        {
                            rankCard.Style = style;
                            await _database.Ranks.UpsertAsync(userId, guildId, rankCard, null);
                            return Ok(
                                new Dictionary<string, string>() {
                                { "style", rankCard.Style }
                                }
                            );
                        } else
                        {
                            return BadRequest();
                        }
                    }
                }
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}
