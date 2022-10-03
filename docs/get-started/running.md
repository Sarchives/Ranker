# Get started (Running the bot)

## Docker
This method assumes you've already installed Docker Compose. If you haven't, visit [this link](https://docs.docker.com/compose/install/#install-compose) to find out how.

1. Rename `.env.example` to `.env` (outside Ranker folder)
2. Fill the values with these you got in **step 8, Set up a bot account**, your desired prefix, and the website (if set up).
3. Save your changes and run `docker-compose up -d`
   
   **Note:** Should you wish to rebuild the bot, add `--build` to the previous command.

## Standalone
You'll need .NET 6 SDK for this method. You can download the latest version [here](https://dotnet.microsoft.com/download/dotnet/6.0).

1. Rename `.env.example` to `.env` (inside Ranker folder)
2. Fill the values with these you got in **step 8, Set up a bot account**, your desired prefix, and the website (if set up).
3. Open a command line window and `cd` to Ranker. Make sure that `Ranker.csproj` exists in the selected folder.
4. Execute `dotnet build` and wait for the build to finish.
5. Execute `dotnet run` to run the bot.

Your bot is now up and running!
