using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
using BotTemplate.BotCore.Helpers;
using BotTemplate.BotCore.Repositories;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace BotTemplate.BotCore.Interactions.SlashCommands
{
    [Group("strike", "Kommandoer til at administrere strikes")]
    public class StrikeCommands : InteractionsCore
    {
        private readonly ILogger<StrikeCommands> _logger;
        private readonly Microsoft.Extensions.Logging.ILogger _discordLogger; // Separate logger for Discord
        private readonly IUserRepository _userRepository;
        private readonly IStrikeRepository _strikeRepository;

        public StrikeCommands(ILogger<StrikeCommands> logger, ILogger<Helpers.DiscordLogger> discordLogger, IUserRepository userRepository, IStrikeRepository strikeRepository)
        {
            _logger = logger;
            _discordLogger = discordLogger;
            _userRepository = userRepository;
            _strikeRepository = strikeRepository;
        }

        [SlashCommand("se", "Ser strikes")]
        public async Task SeeStrike(string personDiscordId = null)
        {
            if (personDiscordId == null)
            {
                personDiscordId = Context.User.Id.ToString();
            }
            else
            {
                if (!ulong.TryParse(personDiscordId, out ulong idToSearch) && personDiscordId != "*")
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("Fejl")
                        .WithDescription("Indtast et gyldigt heltal for Discord ID.")
                        .WithColor(Color.Red)
                        .Build();
                    await RespondAsync(embed: errorEmbed, ephemeral: true);
                    return;
                }

                var socketUser = Context.Interaction.User as SocketGuildUser;
                if (!socketUser.GuildPermissions.BanMembers)
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("Fejl")
                        .WithDescription("Du har ikke tilladelse til at se andres strikes.")
                        .WithColor(Color.Red)
                        .Build();
                    await RespondAsync(embed: errorEmbed, ephemeral: true);
                    return;
                }
            }

            if (personDiscordId == "*")
            {
                var strikes = _strikeRepository.GetAllStrikes();
                if (strikes != null && strikes.Any())
                {
                    var strikeEmbed = new EmbedBuilder()
                        .WithTitle("Strikes")
                        .WithDescription($"**Alle Strikes**")
                        .WithColor(Color.Gold);

                    foreach (var strike in strikes)
                    {
                        strikeEmbed.AddField($"Strike ID: {strike.StrikeId}",
                            $"**Bruger:** {strike.GivenTo.IngameName} (<@{strike.GivenTo.DiscordId}>)\n" +
                            $"**Grund:** {strike.Reason}\n" +
                            $"**Dato:** {strike.Date}\n" +
                            $"**Givet af:** {strike.GivenBy?.IngameName ?? "Ukendt"} (<@{strike.GivenBy?.DiscordId}>)\n" +
                            $"**Aktiv:** {(strike.IsStrikeActive() ? "Ja" : "Nej")}",
                            inline: false);
                    }

                    await RespondAsync(embed: strikeEmbed.Build(), ephemeral: true);
                }
                else
                {
                    var noStrikesEmbed = new EmbedBuilder()
                        .WithTitle("Ingen Strikes")
                        .WithDescription("Der er ingen strikes.")
                        .WithColor(Color.Gold)
                        .Build();
                    await RespondAsync(embed: noStrikesEmbed, ephemeral: true);
                }
                return;
            }

            var user = _userRepository.GetByDiscordId(ulong.Parse(personDiscordId));
            if (user != null)
            {
                var strikes = _strikeRepository.GetStrikeForUser(user);
                if (strikes != null && strikes.Any())
                {
                    var strikeEmbed = new EmbedBuilder()
                        .WithTitle("Strikes")
                        .WithDescription($"**Bruger:** {user.IngameName}\n**Tilsluttet:** {user.JoinDate.Date}")
                        .WithColor(Color.Gold);

                    foreach (var strike in strikes)
                    {
                        strikeEmbed.AddField($"Strike ID: {strike.StrikeId}",
                            $"**Grund:** {strike.Reason}\n" +
                            $"**Dato:** {strike.Date}\n" +
                            $"**Givet af:** {strike.GivenBy?.IngameName ?? "Ukendt"} (<@{strike.GivenBy?.DiscordId}>)\n" +
                            $"**Aktiv:** {(strike.IsStrikeActive() ? "Ja" : "Nej")}",
                            inline: false);
                    }

                    await RespondAsync(embed: strikeEmbed.Build(), ephemeral: true);
                }
                else
                {
                    var noStrikesEmbed = new EmbedBuilder()
                        .WithTitle("Ingen Strikes")
                        .WithDescription("Brugeren har ingen strikes.")
                        .WithColor(Color.Gold)
                        .Build();
                    await RespondAsync(embed: noStrikesEmbed, ephemeral: true);
                }
            }
            else
            {
                var notFoundEmbed = new EmbedBuilder()
                    .WithTitle("Bruger Ikke Fundet")
                    .WithDescription("Bruger ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: notFoundEmbed, ephemeral: true);
            }
        }

        [SlashCommand("opret", "Opretter en ny strike")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task CreateUserAsync(string discordId, string reason, DateTime? dateGiven = null, string? givenById = null)
        {
            if (!ulong.TryParse(discordId, out ulong idToSearch))
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Indtast et gyldigt heltal for Discord ID.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            if (string.IsNullOrEmpty(reason))
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Indtast en grund for straffen.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            if (givenById == null)
            {
                givenById = Context.Interaction.User.Id.ToString();
            }

            var user = _userRepository.GetByDiscordId(idToSearch);
            var givenBy = givenById != null ? _userRepository.GetByDiscordId(ulong.Parse(givenById)) : null;
            if (givenById != null && givenBy == null)
            {
                var notFoundEmbed = new EmbedBuilder()
                    .WithTitle("Giver Ikke Fundet")
                    .WithDescription("Giver ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: notFoundEmbed, ephemeral: true);
                return;
            }

            if (user != null)
            {
                var strike = new Strike
                {
                    Reason = reason,
                    Date = dateGiven ?? DateTime.Now,
                    GivenTo = user,
                    GivenBy = givenBy
                };

                _strikeRepository.Add(strike);

                var successEmbed = new EmbedBuilder()
                    .WithTitle("Strike Oprettet")
                    .WithDescription($"Strike oprettet succesfuldt.")
                    .WithColor(Color.Gold)
                    .Build();
                await RespondAsync(embed: successEmbed, ephemeral: true);

                // Log to Discord
                _discordLogger.Log(LogLevel.Information, new EventId(), $"Strike created for user {user.IngameName} with reason: {reason}", null, (s, e) => s);

                // Send DM to the user
                var socketUser = Context.Client.GetUser(user.DiscordId);
                if (socketUser != null)
                {
                    var dmEmbed = new EmbedBuilder()
                        .WithTitle("Du har modtaget en strike")
                        .WithDescription($"**Grund:** {reason}\n" +
                                         $"**Dato:** {strike.Date}\n" +
                                         $"**Givet af:** {givenBy?.IngameName ?? "Ukendt"} (<@{givenBy?.DiscordId}>)")
                        .WithColor(Color.Red)
                        .Build();
                    await socketUser.SendMessageAsync(embed: dmEmbed);
                }
            }
            else
            {
                var notFoundEmbed = new EmbedBuilder()
                    .WithTitle("Bruger Ikke Fundet")
                    .WithDescription("Bruger ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: notFoundEmbed, ephemeral: true);
            }
        }

        [SlashCommand("slet", "Sletter en strike fra en spiller")]
        public async Task RemoveStrikeAsync(string strikeId)
        {
            if (!int.TryParse(strikeId, out int idToRemove))
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Indtast et gyldigt heltal for Strike ID.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            var strike = _strikeRepository.GetStrike(idToRemove);
            if (strike != null)
            {
                _strikeRepository.Delete(strike.StrikeId);
                var successEmbed = new EmbedBuilder()
                    .WithTitle("Strike Fjernet")
                    .WithDescription($"Strike fjernet succesfuldt.")
                    .WithColor(Color.Gold)
                    .Build();

                // Send DM to the user
                var socketUser = Context.Client.GetUser(strike.GivenTo.DiscordId);
                if (socketUser != null)
                {
                    var dmEmbed = new EmbedBuilder()
                        .WithTitle("Du har fået fjernet en strike.")
                        .WithDescription($"**Grund:** {strike.Reason}\n" +
                                         $"**Dato:** {strike.Date}\n" +
                                         $"**Givet af:** {strike.GivenBy?.IngameName ?? "Ukendt"} (<@{strike.GivenBy?.DiscordId}>)")
                        .WithColor(Color.Red)
                        .Build();
                    await socketUser.SendMessageAsync(embed: dmEmbed);
                }

                await RespondAsync(embed: successEmbed, ephemeral: true);
            }
            else
            {
                var notFoundEmbed = new EmbedBuilder()
                    .WithTitle("Strike Ikke Fundet")
                    .WithDescription("Strike ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: notFoundEmbed, ephemeral: true);
            }
        }
    }
}