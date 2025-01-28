using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
using BotTemplate.BotCore.Repositories;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace BotTemplate.BotCore.Interactions.SlashCommands
{
    [Group("strike", "Kommandoer til at administrere strikes")]
    public class StrikeCommands : InteractionsCore
    {
        private readonly ILogger<StrikeCommands> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IStrikeRepository _strikeRepository;

        public StrikeCommands(ILogger<StrikeCommands> logger, IUserRepository userRepository, IStrikeRepository strikeRepository)
        {
            _logger = logger;
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
                if (!ulong.TryParse(personDiscordId, out ulong idToSearch))
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("Fejl")
                        .WithDescription("Indtast et gyldigt heltal for Discord ID.")
                        .WithColor(Color.Red)
                        .Build();
                    await RespondAsync(embed: errorEmbed);
                    return;
                }

                if (!Context.Interaction.Permissions.Administrator)
                {
                    var errorEmbed = new EmbedBuilder()
                        .WithTitle("Fejl")
                        .WithDescription("Du har ikke tilladelse til at se andres strikes.")
                        .WithColor(Color.Red)
                        .Build();
                    await RespondAsync(embed: errorEmbed);
                    return;
                }
            }

            var user = _userRepository.GetByDiscordId(ulong.Parse(personDiscordId));
            if (user != null)
            {
                var strikes = _strikeRepository.GetStrikeForUser(user);
                if (strikes != null)
                {
                    var strikeEmbed = new EmbedBuilder()
                        .WithTitle("Strikes")
                        .WithDescription($"Bruger: {user.IngameName}\nRang: {user.Role}\nDiscord ID: {user.DiscordId}\nTilsluttet: {user.JoinDate.Date}")
                        .WithColor(Color.Gold)
                        .Build();
                    await RespondAsync(embed: strikeEmbed);
                }
                else
                {
                    var noStrikesEmbed = new EmbedBuilder()
                        .WithTitle("Ingen Strikes")
                        .WithDescription("Brugeren har ingen strikes.")
                        .WithColor(Color.Gold)
                        .Build();
                    await RespondAsync(embed: noStrikesEmbed);
                }
            }
            else
            {
                var notFoundEmbed = new EmbedBuilder()
                    .WithTitle("Bruger Ikke Fundet")
                    .WithDescription("Bruger ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: notFoundEmbed);
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
                await RespondAsync(embed: errorEmbed);
                return;
            }

            if (string.IsNullOrEmpty(reason))
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Indtast en grund for straffen.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed);
                return;
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
                await RespondAsync(embed: notFoundEmbed);
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

                _strikeRepository.Add(strike);
                var successEmbed = new EmbedBuilder()
                    .WithTitle("Strike Oprettet")
                    .WithDescription($"Strike oprettet succesfuldt.")
                    .WithColor(Color.Gold)
                    .Build();
                await RespondAsync(embed: successEmbed);
            }
            else
            {
                var notFoundEmbed = new EmbedBuilder()
                    .WithTitle("Bruger Ikke Fundet")
                    .WithDescription("Bruger ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: notFoundEmbed);
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
                await RespondAsync(embed: errorEmbed);
                return;
            }

            var strike = _strikeRepository.GetById(idToRemove);
            if (strike != null)
            {
                _strikeRepository.Delete(strike.StrikeId);
                var successEmbed = new EmbedBuilder()
                    .WithTitle("Strike Fjernet")
                    .WithDescription($"Strike fjernet succesfuldt.")
                    .WithColor(Color.Gold)
                    .Build();
                await RespondAsync(embed: successEmbed);
            }
            else
            {
                var notFoundEmbed = new EmbedBuilder()
                    .WithTitle("Strike Ikke Fundet")
                    .WithDescription("Strike ikke fundet.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: notFoundEmbed);
            }
        }
    }
}