using System.IO;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.Entities;
using DSharpPlus;

namespace Ranker
{
    [SlashRequireGuild]
    public class SlashCommands : ApplicationCommandModule
    {
        private readonly IRankerRepository _database;
        public SlashCommands(IRankerRepository database)
        {
            _database = database;
        }

        [SlashCommand("rank", "View the rank of a user.")]
        public async Task RankCommand(InteractionContext ctx, [Option("user", "User to view ranks for")] DiscordUser user = null)
        {
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral(true));
            if (user == null)
            {
                user = ctx.User;
            }
            Rank currentUserRank = await _database.Ranks.GetAsync(ctx.User.Id, ctx.Guild.Id);
            Rank rank = await _database.Ranks.GetAsync(user.Id, ctx.Guild.Id);
            if (rank.Xp > 0)
            {
                MemoryStream stream;
                if (currentUserRank.Style == "fleuron")
                {
                    stream = await Commands.RankFleuron(_database, ctx.Guild, user, rank);
                }
                else
                {
                    stream = await Commands.RankZeealeid(_database, ctx.Guild, user, rank);
                }

                DiscordWebhookBuilder builder = new DiscordWebhookBuilder();

                builder.AddFile("rank.png", stream);

                await ctx.EditResponseAsync(builder);
            }
            else if (user.Id == ctx.User.Id)
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You aren't ranked yet. Send some messages first, then try again."));
            else
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This member isn't ranked yet."));
        }

        [ContextMenu(ApplicationCommandType.UserContextMenu, "Rank")]
        public async Task RankContext(ContextMenuContext ctx)
        {
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral(true));

            Rank currentUserRank = await _database.Ranks.GetAsync(ctx.Member.Id, ctx.Guild.Id);
            Rank rank = await _database.Ranks.GetAsync(ctx.TargetMember.Id, ctx.Guild.Id);
            if (rank.Xp > 0)
            {
                MemoryStream stream;
                if (currentUserRank.Style == "fleuron")
                {
                    stream = await Commands.RankFleuron(_database, ctx.Guild, ctx.TargetMember, rank);
                }
                else
                {
                    stream = await Commands.RankZeealeid(_database, ctx.Guild, ctx.TargetMember, rank);
                }

                DiscordWebhookBuilder builder = new DiscordWebhookBuilder();

                builder.AddFile("rank.png", stream);

                await ctx.EditResponseAsync(builder);
            }
            else await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This member isn't ranked yet."));
        }

        [SlashCommand("role", "Configures a level up role.")]
        [SlashRequireUserPermissions(Permissions.ManageGuild, false)]
        public async Task RoleCommand(InteractionContext ctx, [Option("level", "Level to configure")] long level, [Option("role", "Role to configure")] DiscordRole role = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            if (role == null)
            {
                await _database.Roles.RemoveAsync(ctx.Guild.Id, (ulong)level);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Role for level " + level.ToString() + " deconfigured!"));
            }
            else
            {
                await _database.Roles.UpsertAsync(ctx.Guild.Id, (ulong)level, role.Id, role.Name);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Role for level " + level.ToString() + " configured!"));
            }
        }

        public enum Styles
        {
            [ChoiceName("Zeealeid (default)")]
            zeealeid,
            [ChoiceName("Fleuron")]
            fleuron
        }

        [SlashCommand("style", "Changes the rank card style.")]
        public async Task FleuronCommand(InteractionContext ctx, [Option("style", "Choose a style")] Styles styles = Styles.zeealeid)
        {
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral(true));

            Rank rank = await _database.Ranks.GetAsync(ctx.Member.Id, ctx.Guild.Id);

                rank.Style = styles.ToString();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Style changed!"));

            await _database.Ranks.UpsertAsync(ctx.Member.Id, ctx.Guild.Id, rank, ctx.Guild);
        }

        [SlashCommand("levels", "Send leaderboard.")]
        public async Task LevelsCommand(InteractionContext ctx)
        {
            string domain = Environment.GetEnvironmentVariable("RANKER_DOMAIN");
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral(true));
            if (Uri.IsWellFormedUriString(domain, UriKind.Absolute))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(domain + "/leaderboard/" + ctx.Guild.Id));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Leaderboard setup not completed!"));
            }
        }

        [SlashCommandGroup("migrate", "Migrates data.")]
        [RequireBotOwnerOrAdmin]
        public class MigrateGroup : ApplicationCommandModule
        {
            [SlashCommand("mee6", "Migrates MEE6's data")]
            public async Task MEE6Command(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var continueButton = new DiscordButtonComponent(ButtonStyle.Success, "continueMEE6", "Start migration");

                var cancelButton = new DiscordButtonComponent(ButtonStyle.Danger, "cancelMEE6", "Cancel");

                DiscordWebhookBuilder webhook = new DiscordWebhookBuilder();

                webhook.WithContent("Running this command will DELETE all ranking and roles data and REPLACE it with the one this server had with MEE6. Do you want to CONTINUE?");

                webhook.AddComponents(continueButton, cancelButton);

                await ctx.EditResponseAsync(webhook);
            }

            [SlashCommand("user", "Migrates an user's data")]
            public async Task UserCommand(InteractionContext ctx, [Option("old_user", "User to get the data")] DiscordUser oldUser, [Option("new_user", "User where the data is being transferred")] DiscordUser newUser)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var continueButton = new DiscordButtonComponent(ButtonStyle.Success, "continueUser-" + oldUser.Id + "-" + newUser.Id, "Start migration");

                var cancelButton = new DiscordButtonComponent(ButtonStyle.Danger, "cancelUser", "Cancel");

                DiscordWebhookBuilder webhook = new DiscordWebhookBuilder();

                webhook.WithContent("Running this command will DELETE all ranking data from both the source and target user and REPLACE it. Do you want to CONTINUE?");

                webhook.AddComponents(continueButton, cancelButton);

                await ctx.EditResponseAsync(webhook);
            }
        }

        [SlashCommand("merge", "Merges two users' data")]
        [RequireBotOwnerOrAdmin]
        public async Task UserCommand(InteractionContext ctx, [Option("old_user", "User to get the data")] DiscordUser oldUser, [Option("new_user", "User where the data is being transferred")] DiscordUser newUser)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var continueButton = new DiscordButtonComponent(ButtonStyle.Success, "continueMerge-" + oldUser.Id + "-" + newUser.Id, "Start merging");

            var cancelButton = new DiscordButtonComponent(ButtonStyle.Danger, "cancelMerge", "Cancel");

            DiscordWebhookBuilder webhook = new DiscordWebhookBuilder();

            webhook.WithContent("Running this command will DELETE all ranking data from the source and the target user data will me MERGED. Do you want to CONTINUE?");

            webhook.AddComponents(continueButton, cancelButton);

            await ctx.EditResponseAsync(webhook);
        }

        [SlashCommand("range", "Changes how much XP is won every minute.")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task RangeCommand(InteractionContext ctx, [Option("min", "Minimum XP that can be win")] long min, [Option("max", "Maximum XP that can be win")] long max)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if ((max >= min) && (min > -1) && (max > -1) && (min < int.MaxValue) && (max < int.MaxValue))
            {
                Settings settings = await _database.Settings.GetAsync(ctx.Guild.Id);
                settings.MinRange = Convert.ToInt32(min);
                settings.MaxRange = Convert.ToInt32(max) + 1;
                await _database.Settings.UpsertAsync(ctx.Guild.Id, settings);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Range updated successfully!"));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Max has to be bigger or equal to min! (also, both have to be positive and less than 2147483647)"));
            }
        }

        [SlashCommand("exclude", "Excludes a channel.")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task ExcludeCommand(InteractionContext ctx, [Option("channel", "Channel to exclude")] DiscordChannel channel)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            Settings settings = await _database.Settings.GetAsync(ctx.Guild.Id);
            settings.ExcludedChannels.Add(channel.Id);
            await _database.Settings.UpsertAsync(ctx.Guild.Id, settings);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Channel excluded successfully!"));
        }

        [SlashCommand("unexclude", "Unexcludes a channel.")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task IncludeCommand(InteractionContext ctx, [Option("channel", "Channel to unexclude")] DiscordChannel channel)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            Settings settings = await _database.Settings.GetAsync(ctx.Guild.Id);
            if (settings.ExcludedChannels.Remove(channel.Id))
            {
                await _database.Settings.UpsertAsync(ctx.Guild.Id, settings);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Channel unexcluded successfully!"));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The channel is not excluded!"));
            }
        }

        [SlashCommand("about", "Shows bot information.")]
        public async Task AboutCommand(InteractionContext ctx)
        {
            DiscordInteractionResponseBuilder embed = new DiscordInteractionResponseBuilder();
            embed.AddEmbed(new DiscordEmbedBuilder().WithTitle("About Ranker")
                .WithColor(0x007FFF)
                .AddField("Website (and invite)", "https://rankerbot.xyz")
                .AddField("GitHub", "https://github.com/Ranker-Team")
                .AddField("More...", "Soon, perhaps.")
                .Build());
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, embed);
        }
    }
}
