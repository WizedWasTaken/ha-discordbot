using BotTemplate.BotCore.DataAccess;
using BotTemplate.BotCore.Entities;
using BotTemplate.BotCore.Repositories;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace BotTemplate.BotCore.Interactions.SlashCommands
{
    [Group("bruger", "Kommandoer til at administrere brugere")]
    public class UserCommands : InteractionsCore
    {
        private readonly ILogger<UserCommands> _logger;
        private readonly IUserRepository _userRepository;

        public UserCommands(ILogger<UserCommands> logger, IUserRepository userRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
        }

        [SlashCommand("opret", "Opretter en ny bruger")]
        public async Task CreateUserAsync(string ingameName, Role role, DateTime? joinDate = null)
        {
            var userDiscordId = Context.User.Id.ToString();

            ulong idToCreate;

            if (!ulong.TryParse(userDiscordId, out idToCreate))
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Kunne ikke finde Discord ID.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed);
                return;
            }

            var userExists = _userRepository.GetByDiscordId(idToCreate);

            if (userExists != null)
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Bruger med dette Discord ID eksisterer allerede.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed);
                return;
            }

            var user = new User
            {
                IngameName = ingameName,
                Role = role,
                DiscordId = idToCreate,
                JoinDate = joinDate ?? DateTime.Now
            };

            _userRepository.Add(user);
            var successEmbed = new EmbedBuilder()
                .WithTitle("Bruger Oprettet")
                .WithDescription($"Bruger {ingameName} oprettet succesfuldt.")
                .WithColor(Color.Gold)
                .Build();
            await RespondAsync(embed: successEmbed);
        }

        [SlashCommand("find", "Finder en bruger efter Discord ID")]
        public async Task FindUserAsync(string discordId = null)
        {
            ulong idToSearch;
            if (discordId == null)
            {
                idToSearch = Context.User.Id;
            }
            else if (!ulong.TryParse(discordId, out idToSearch))
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Indtast et gyldigt heltal for Discord ID.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed);
                return;
            }

            var res = _userRepository.GetByDiscordId(idToSearch);
            if (res != null)
            {
                var userEmbed = new EmbedBuilder()
                    .WithTitle("Bruger Fundet")
                    .WithDescription($"Bruger: {res.IngameName}\nRang: {res.Role}\nDiscord ID: {res.DiscordId}\nTilsluttet: {res.JoinDate.Date}")
                    .WithColor(Color.Gold)
                    .Build();
                await RespondAsync(embed: userEmbed);
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

        [SlashCommand("slet", "Sletter en bruger efter Discord ID")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task DeleteUserAsync(string discordId)
        {
            if (!ulong.TryParse(discordId, out ulong idToDelete))
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Fejl")
                    .WithDescription("Indtast et gyldigt heltal for Discord ID.")
                    .WithColor(Color.Red)
                    .Build();
                await RespondAsync(embed: errorEmbed);
                return;
            }

            var user = _userRepository.GetByDiscordId(idToDelete);
            if (user != null)
            {
                _userRepository.Delete(user.UserId);
                var successEmbed = new EmbedBuilder()
                    .WithTitle("Bruger Slettet")
                    .WithDescription($"Bruger med Discord ID {idToDelete} slettet succesfuldt.")
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
    }
}