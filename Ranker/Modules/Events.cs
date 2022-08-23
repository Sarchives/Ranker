﻿using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace Ranker
{
    public class Events : BaseExtension
    {
        private readonly IRankerRepository _database;

        public Events(IRankerRepository database)
        {
            _database = database;
        }

        protected override void Setup(DiscordClient client)
        {
            client.Ready += Client_Ready;
            client.GuildAvailable += Client_GuildAvaliable;
            client.GuildMemberAdded += Client_GuildMemberAdded;
            client.GuildMemberUpdated += Client_GuildMemberUpdated;
            client.GuildRoleUpdated += Client_GuildRoleUpdated;
            client.GuildRoleDeleted += Client_GuildRoleDeleted;
            client.MessageCreated += Client_MessageCreated;
            client.ComponentInteractionCreated += Client_ComponentInteractionCreated;
        }

        private Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            sender.Logger.LogInformation("Bot is ready!");
            return Task.CompletedTask;
        }

        private async Task Client_GuildAvaliable(DiscordClient sender, DSharpPlus.EventArgs.GuildCreateEventArgs e)
        {
            List<Role> roles = await _database.Roles.GetAsync(e.Guild.Id);
            roles.ForEach(async role =>
            {
                try
                {
                    await _database.Roles.UpsertAsync(e.Guild.Id, role.Level, role.RoleId, e.Guild.GetRole(role.RoleId).Name);
                }
                catch
                {
                    await _database.Roles.RemoveAsync(e.Guild.Id, role.Level);
                }
            });
        }

        private async Task Client_GuildMemberUpdated(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberUpdateEventArgs e)
        {
            if (e.Member.IsBot) return;

            Rank rank = await _database.Ranks.GetAsync(e.Member.Id, e.Guild.Id);
            rank.Avatar = e.Member.GetAvatarUrl(DSharpPlus.ImageFormat.Png, 128);
            rank.Discriminator = e.Member.Discriminator;
            rank.Username = e.Member.Username;
            await _database.Ranks.UpsertAsync(e.Member.Id, e.Guild.Id, rank, e.Guild);
        }

        private async Task Client_GuildMemberAdded(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberAddEventArgs e)
        {
            if (e.Member.IsBot) return;

            Rank rank = new()
            {
                Avatar = e.Member.GetAvatarUrl(DSharpPlus.ImageFormat.Png, 128),
                Username = e.Member.Username,
                Discriminator = e.Member.Discriminator,
                LastCreditDate = DateTimeOffset.UnixEpoch
            };
            await _database.Ranks.UpsertAsync(e.Member.Id, e.Guild.Id, rank, e.Guild);
        }

        private async Task Client_Guild​Role​Update​d(DiscordClient sender, DSharpPlus.EventArgs.Guild​Role​Update​Event​Args e)
        {
            List<Role> roles = await _database.Roles.GetAsync(e.Guild.Id);
            Role role = roles.Find(x => x.RoleId == e.RoleAfter.Id);
            if (role != null)
            {
                await _database.Roles.UpsertAsync(e.Guild.Id, role.Level, role.RoleId, e.RoleAfter.Name);
            }
        }

        private async Task Client_Guild​Role​Deleted(DiscordClient sender, DSharpPlus.EventArgs.Guild​RoleDelete​Event​Args e)
        {
            List<Role> roles = await _database.Roles.GetAsync(e.Guild.Id);
            Role role = roles.Find(x => x.RoleId == e.Role.Id);
            if (role != null)
            {
                await _database.Roles.RemoveAsync(e.Guild.Id, role.Level);
            }
        }

        private async Task Client_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Author.IsBot) return;

            Rank rank = await _database.Ranks.GetAsync(e.Author.Id, e.Guild.Id);
            rank.Avatar = e.Author.GetAvatarUrl(DSharpPlus.ImageFormat.Png, 128);
            rank.Username = e.Author.Username;
            rank.Discriminator = e.Author.Discriminator;


            Settings settings = await _database.Settings.GetAsync(e.Guild.Id);

            if (!settings.ExcludedChannels.Contains(e.Channel.Id))
            {

                if (e.Message.CreationTimestamp >= rank.LastCreditDate.AddMinutes(1))
                {
                    ulong newXp = Convert.ToUInt64(new Random().Next(settings.MinRange, settings.MaxRange));
                    rank.Messages += 1;
                    rank.Xp += newXp;
                    rank.TotalXp += newXp;
                    rank.LastCreditDate = e.Message.CreationTimestamp;
                }
            }

            await _database.Ranks.UpsertAsync(e.Author.Id, e.Guild.Id, rank, e.Guild);
        }

        private async Task Client_ComponentInteractionCreated(DiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs e)
        {
            if (sender.CurrentApplication.Owners.ToList().Contains(e.User) || (await e.Guild.GetMemberAsync(e.User.Id)).Permissions.HasPermission(Permissions.ManageGuild))
            {
                switch (e.Id)
                {
                    case "continueMEE6":
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Please wait while we migrate the data. Even if it may appear to be stuck, we're still working. We will notify you when we're done. You can check console for the logged pages if you're self-hosting."));

                        await _database.Roles.Empty(e.Guild.Id);
                        await _database.Ranks.Empty(e.Guild.Id);
                        bool hasPlayers = true;
                        int times = 0;
                        while (hasPlayers)
                        {
                            try
                            {
                                using (HttpClient client = new())
                                {
                                    var response = await client.GetAsync("https://mee6.xyz/api/plugins/levels/leaderboard/" + e.Guild.Id.ToString() + "?page=" + times.ToString());
                                    string responseJson = await response.Content.ReadAsStringAsync();
                                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        JObject jsonParsed = JObject.Parse(responseJson);
                                        if (times == 0)
                                        {
                                            jsonParsed["role_rewards"].Value<JArray>().ToList().ForEach(role =>
                                            {
                                                _database.Roles.UpsertAsync(e.Guild.Id, role["rank"].Value<ulong>(), ulong.Parse(role["role"]["id"].Value<string>()), role["role"]["name"].Value<string>());
                                            });
                                        }
                                        if (jsonParsed["players"].Value<JArray>().Count != 0)
                                        {
                                            Console.WriteLine(jsonParsed["players"].Value<JArray>().Count);
                                            jsonParsed["players"].Value<JArray>().ToList().ForEach(player =>
                                            {
                                                string userId = player["id"].Value<string>();
                                                string avatarHash = player["avatar"].Value<string>();
                                                Rank rank = new Rank()
                                                {
                                                    LastCreditDate = DateTimeOffset.MinValue, // We just empty it
                                                    Messages = player["message_count"].Value<ulong>(),
                                                    Xp = player["detailed_xp"].Value<JArray>()[0].Value<ulong>(),
                                                    NextXp = player["detailed_xp"].Value<JArray>()[1].Value<ulong>(),
                                                    Level = player["level"].Value<ulong>(),
                                                    TotalXp = player["xp"].Value<ulong>(),
                                                    Guild = e.Guild.Id,
                                                    User = ulong.Parse(userId),
                                                    Username = player["username"].Value<string>(),
                                                    Discriminator = player["discriminator"].Value<string>(),
                                                    Avatar = avatarHash != "" ? "https://cdn.discordapp.com/avatars/" + userId + "/" + avatarHash + ".png?size=128" : "https://cdn.discordapp.com/embed/avatars/1.png",
                                                    Fleuron = false
                                                };
                                                _database.Ranks.UpsertAsync(ulong.Parse(userId), e.Guild.Id, rank, e.Guild);
                                            });

                                            Console.WriteLine("Migrated page number " + times + " in " + e.Guild.Name + ".");
                                            times++;
                                        }
                                        else
                                        {
                                            hasPlayers = false;
                                        }
                                    }
                                    else
                                    {
                                        hasPlayers = false;
                                    }
                                }
                            }
                            catch { }
                        }

                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("We finished migrating the data!"));

                        break;

                    case "continueUser":
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Please wait while we migrate the data. This shouldn't take long."));
                        ulong oldUser = ulong.Parse(e.Id.Split("-")[1]);
                        ulong newUser = ulong.Parse(e.Id.Split("-")[2]);
                        Rank oldRank = await _database.Ranks.GetAsync(oldUser, e.Guild.Id);
                        Rank newRank = await _database.Ranks.GetAsync(newUser, e.Guild.Id);
                        oldRank.User = newUser;
                        oldRank.Username = newRank.Username;
                        oldRank.Discriminator = newRank.Discriminator;
                        oldRank.Avatar = newRank.Avatar;
                        await _database.Ranks.UpsertAsync(oldUser, e.Guild.Id, new Rank()
                        {
                            Guild = e.Guild.Id,
                            User = oldUser
                        }, e.Guild);
                        await _database.Ranks.UpsertAsync(newUser, e.Guild.Id, oldRank, e.Guild);
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("We finished migrating the data!"));
                        break;

                    case "continueMerge":
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Please wait while we merge the data. This shouldn't take long."));
                        ulong oldUser2 = ulong.Parse(e.Id.Split("-")[1]);
                        ulong newUser2 = ulong.Parse(e.Id.Split("-")[2]);
                        Rank oldRank2 = await _database.Ranks.GetAsync(oldUser2, e.Guild.Id);
                        Rank newRank2 = await _database.Ranks.GetAsync(newUser2, e.Guild.Id);
                        newRank2.TotalXp += oldRank2.TotalXp;
                        ulong totalXp = newRank2.TotalXp;
                        ulong level = 0;
                        ulong min = 0;
                        while (totalXp > min)
                        {
                            Console.WriteLine("tx: " + totalXp.ToString() + " - min: " + min.ToString());
                            min = Convert.ToUInt64(5 * Math.Pow(level, 2) + (50 * (float)level) + 100);
                            if (min <= totalXp)
                            {
                                level++;
                                totalXp -= min;
                            }
                        }
                        level++;
                        newRank2.Xp = totalXp;
                        newRank2.Level = level;
                        await _database.Ranks.UpsertAsync(oldUser2, e.Guild.Id, new Rank()
                        {
                            Guild = e.Guild.Id,
                            User = oldUser2
                        }, e.Guild);
                        await _database.Ranks.UpsertAsync(newUser2, e.Guild.Id, newRank2, e.Guild);
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("We finished merging the data!"));
                        break;

                    default:
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Migration/merge cancelled."));
                        break;
                }
            }
        }
    }
}