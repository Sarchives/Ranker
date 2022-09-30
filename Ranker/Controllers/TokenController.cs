namespace Ranker
{
    [Route("[controller]")]
    public class TokenController : Controller
    {
        public TokenController()
        {
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                using (HttpClient client = new())
                {
                    Console.WriteLine(Environment.GetEnvironmentVariable("CLIENT_ID") + "dog");
                    Dictionary<string, string> dictionary = new Dictionary<string, string>() {
                        { "client_id", Environment.GetEnvironmentVariable("CLIENT_ID") },
                        { "client_secret", Environment.GetEnvironmentVariable("RANKER_CLIENT_SECRET") },
                        { "grant_type", "authorization_code" },
                        { "code", Request.Headers["Code"] },
                        { "redirect_uri", Environment.GetEnvironmentVariable("DOMAIN") + "/callback" },
                        { "scope", "identify guilds" }
                    };

                    using (FormUrlEncodedContent requestContent = new FormUrlEncodedContent(dictionary))
                    {
                        var response = await client.PostAsync($"https://discord.com/api/v9/oauth2/token", requestContent);
                        string responseJson = await response.Content.ReadAsStringAsync();
                        return Ok(responseJson);
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
