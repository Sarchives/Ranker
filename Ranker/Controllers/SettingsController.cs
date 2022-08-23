using System.IO;

namespace Ranker
{
    [Route("[controller]")]
    public class SettingsController : Controller
    {
        private readonly IRankerRepository _database;

        public SettingsController(IRankerRepository database)
        {
            _database = database;
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
                    var response = await client.GetAsync("https://discord.com/api/v9/users/@me/guilds");
                    string responseJson = await response.Content.ReadAsStringAsync();
                    JArray jsonParsed = JArray.Parse(responseJson);

                    bool managesGuild = (ulong.Parse(jsonParsed.ToList().Find(x => x["id"].Value<string>() == guildId.ToString())["permissions"].Value<string>()) & 0x0000000020) == 0x0000000020;

                    if (managesGuild)
                    {
                        using (StreamReader stream = new StreamReader(Request.Body))
                        {
                            JObject jsonParsedBody = JObject.Parse(await stream.ReadToEndAsync());

                            Settings settings = await _database.Settings.GetAsync(guildId);

                            // Sapphire moment

                            try
                            {
                                settings.MinRange = jsonParsedBody["minRange"]?.Value<int>() ?? settings.MinRange;
                            }
                            catch { }
                            try
                            {
                                settings.MaxRange = jsonParsedBody["maxRange"]?.Value<int>() ?? settings.MaxRange;
                            }
                            catch { }

                            await _database.Settings.UpsertAsync(guildId, settings);

                            return Ok(settings);
                        }
                    }
                    else
                    {
                        return Unauthorized();
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
